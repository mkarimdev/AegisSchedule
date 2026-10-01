using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AegisSchedule.Api.Security;

public class ApiKeyAuthFilter : IAsyncActionFilter
{
    public const string ApiKeyHeaderName = "X-Admin-Api-Key";
    private readonly IConfiguration _configuration;
    private readonly ILogger<ApiKeyAuthFilter> _logger;

    public ApiKeyAuthFilter(IConfiguration configuration, ILogger<ApiKeyAuthFilter> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!context.HttpContext.Request.Headers.TryGetValue(ApiKeyHeaderName, out var extractedApiKey) ||
            string.IsNullOrWhiteSpace(extractedApiKey))
        {
            context.Result = new UnauthorizedObjectResult(new { error = "Unauthorized: Missing X-Admin-Api-Key header." });
            return;
        }

        var configuredApiKey = _configuration["Security:AdminApiKey"] ?? _configuration["AdminApiKey"];

        if (string.IsNullOrWhiteSpace(configuredApiKey))
        {
            _logger.LogCritical("Security:AdminApiKey is not configured on the server.");
            context.Result = new ObjectResult(new { error = "Server security configuration error." }) { StatusCode = 500 };
            return;
        }

        var isValid = CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(extractedApiKey.ToString()),
            Encoding.UTF8.GetBytes(configuredApiKey));

        if (!isValid)
        {
            _logger.LogWarning("Invalid admin API key attempt from IP {RemoteIp}.", context.HttpContext.Connection.RemoteIpAddress);
            context.Result = new UnauthorizedObjectResult(new { error = "Unauthorized: Invalid API key." });
            return;
        }

        await next();
    }
}
