using AegisSchedule.Api.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AegisSchedule.Tests;

public class ApiKeyAuthFilterTests
{
    private const string ExpectedKey = "Test_Secret_Key_123";

    private static (ApiKeyAuthFilter filter, ActionExecutingContext context, bool[] nextCalled) CreateTestContext(
        string? headerValue = null,
        string? configuredKey = ExpectedKey)
    {
        var inMemorySettings = new Dictionary<string, string?>();
        if (configuredKey != null)
        {
            inMemorySettings["Security:AdminApiKey"] = configuredKey;
        }

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        var logger = NullLogger<ApiKeyAuthFilter>.Instance;
        var filter = new ApiKeyAuthFilter(configuration, logger);

        var httpContext = new DefaultHttpContext();
        if (headerValue != null)
        {
            httpContext.Request.Headers[ApiKeyAuthFilter.ApiKeyHeaderName] = headerValue;
        }

        var actionContext = new ActionContext(
            httpContext,
            new RouteData(),
            new ActionDescriptor()
        );

        var executingContext = new ActionExecutingContext(
            actionContext,
            new List<IFilterMetadata>(),
            new Dictionary<string, object?>(),
            controller: new object()
        );

        var nextCalled = new bool[] { false };

        return (filter, executingContext, nextCalled);
    }

    private static ActionExecutionDelegate CreateNextDelegate(ActionExecutingContext context, bool[] nextCalled)
    {
        return () =>
        {
            nextCalled[0] = true;
            return Task.FromResult(new ActionExecutedContext(context, new List<IFilterMetadata>(), context.Controller));
        };
    }

    [Fact]
    public async Task OnActionExecutionAsync_WhenHeaderMissing_Returns401Unauthorized()
    {
        // Arrange
        var (filter, context, nextCalled) = CreateTestContext(headerValue: null);

        // Act
        await filter.OnActionExecutionAsync(context, CreateNextDelegate(context, nextCalled));

        // Assert
        Assert.False(nextCalled[0]);
        var result = Assert.IsType<UnauthorizedObjectResult>(context.Result);
        Assert.Equal(401, result.StatusCode);
    }

    [Fact]
    public async Task OnActionExecutionAsync_WhenHeaderEmpty_Returns401Unauthorized()
    {
        // Arrange
        var (filter, context, nextCalled) = CreateTestContext(headerValue: "   ");

        // Act
        await filter.OnActionExecutionAsync(context, CreateNextDelegate(context, nextCalled));

        // Assert
        Assert.False(nextCalled[0]);
        var result = Assert.IsType<UnauthorizedObjectResult>(context.Result);
        Assert.Equal(401, result.StatusCode);
    }

    [Fact]
    public async Task OnActionExecutionAsync_WhenHeaderInvalid_Returns401Unauthorized()
    {
        // Arrange
        var (filter, context, nextCalled) = CreateTestContext(headerValue: "Wrong_Secret_Key");

        // Act
        await filter.OnActionExecutionAsync(context, CreateNextDelegate(context, nextCalled));

        // Assert
        Assert.False(nextCalled[0]);
        var result = Assert.IsType<UnauthorizedObjectResult>(context.Result);
        Assert.Equal(401, result.StatusCode);
    }

    [Fact]
    public async Task OnActionExecutionAsync_WhenHeaderValid_CallsNextDelegate()
    {
        // Arrange
        var (filter, context, nextCalled) = CreateTestContext(headerValue: ExpectedKey);

        // Act
        await filter.OnActionExecutionAsync(context, CreateNextDelegate(context, nextCalled));

        // Assert
        Assert.True(nextCalled[0]);
        Assert.Null(context.Result);
    }

    [Fact]
    public async Task OnActionExecutionAsync_WhenConfiguredKeyMissing_Returns500InternalServerError()
    {
        // Arrange
        var (filter, context, nextCalled) = CreateTestContext(headerValue: ExpectedKey, configuredKey: null);

        // Act
        await filter.OnActionExecutionAsync(context, CreateNextDelegate(context, nextCalled));

        // Assert
        Assert.False(nextCalled[0]);
        var result = Assert.IsType<ObjectResult>(context.Result);
        Assert.Equal(500, result.StatusCode);
    }
}
