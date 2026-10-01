using AegisSchedule.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "RequestVerificationToken";
});

var apiBaseUrl = builder.Configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5221";
var adminApiKey = builder.Configuration["ApiSettings:AdminApiKey"] ?? "AegisAdmin_SuperSecretKey_2026_ChangeMeInProd";

builder.Services.AddHttpClient<ISchedulingApiClient, SchedulingApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl.TrimEnd('/') + "/");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

builder.Services.AddHttpClient<IAdminApiClient, AdminApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl.TrimEnd('/') + "/");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
    if (!string.IsNullOrWhiteSpace(adminApiKey))
    {
        client.DefaultRequestHeaders.Add("X-Admin-Api-Key", adminApiKey);
    }
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.MapRazorPages();

app.Run();
