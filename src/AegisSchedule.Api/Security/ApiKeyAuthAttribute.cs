using Microsoft.AspNetCore.Mvc;

namespace AegisSchedule.Api.Security;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class ApiKeyAuthAttribute : ServiceFilterAttribute
{
    public ApiKeyAuthAttribute() : base(typeof(ApiKeyAuthFilter))
    {
    }
}
