using System.Text.Json.Serialization;
using AegisSchedule.Api.Integrations;
using AegisSchedule.Api.Persistence;
using AegisSchedule.Api.Security;
using AegisSchedule.Api.Solver;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<UniSchedulingDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.Configure<SolverOptions>(builder.Configuration.GetSection(SolverOptions.SectionName));
builder.Services.AddScoped<ApiKeyAuthFilter>();
builder.Services.AddScoped<ISisImportService, SisImportService>();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddTransient<ScheduleSolver>();

// Configure OpenAPI
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Info = new()
        {
            Title = "AegisSchedule API",
            Version = "v1",
            Description = "High-performance university timetable scheduling engine and administration API."
        };

        var scheme = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.ApiKey,
            Name = "X-Admin-Api-Key",
            In = ParameterLocation.Header,
            Description = "Administrative API Key required for /api/admin/* endpoints."
        };

        var components = document.Components ?? new OpenApiComponents();
        components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        components.SecuritySchemes["AdminApiKey"] = scheme;
        document.Components = components;
        return Task.CompletedTask;
    });
});

// Configure CORS
const string CorsPolicyName = "AegisScheduleCors";
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:5046", "https://localhost:7111"];

builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicyName, policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Ensure database schema and migrations are applied on startup
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();

    try
    {
        var dbContext = services.GetRequiredService<UniSchedulingDbContext>();
        await DbInitializer.SeedAsync(dbContext);
        logger.LogInformation("Database migration completed successfully.");
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "An error occurred while migrating the database. Proceeding without active database connection.");
    }
}

// Security Headers Middleware
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    await next();
});

// Configure the HTTP request pipeline.
app.UseHttpsRedirection();
app.UseRouting();
app.UseCors(CorsPolicyName);

app.MapOpenApi();
app.MapScalarApiReference();

app.MapControllers();

app.Run();
