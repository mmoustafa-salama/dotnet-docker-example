using Serilog;
using Serilog.Events;

namespace dotnet_docker_example.Configuration.Logging;

/// <summary>
/// https://andrewlock.net/using-serilog-aspnetcore-in-asp-net-core-3-logging-the-selected-endpoint-name-with-serilog/
/// https://andrewlock.net/using-serilog-aspnetcore-in-asp-net-core-3-excluding-health-check-endpoints-from-serilog-request-logging
/// </summary>
public static class RequestLoggingExtensions
{
    public static void UseCustomRequestLogging(this IApplicationBuilder app)
    {
        app.UseSerilogRequestLogging(options =>
        {
            options.GetLevel = GetLogEventLevel;
            options.EnrichDiagnosticContext = EnrichFromRequest;
        });
    }

    public static void EnrichFromRequest(IDiagnosticContext diagnosticContext, HttpContext httpContext)
    {
        var request = httpContext.Request;
        diagnosticContext.Set("Host", request.Host);
        diagnosticContext.Set("Protocol", request.Protocol);
        diagnosticContext.Set("Scheme", request.Scheme);
        diagnosticContext.Set("UserAgent", request.Headers["User-Agent"]);
        diagnosticContext.Set("ClientIP", httpContext?.Connection?.RemoteIpAddress?.ToString());

        diagnosticContext.Set("UserName", httpContext.User?.Identity?.Name);

        if (request.QueryString.HasValue)
        {
            diagnosticContext.Set("QueryString", request.QueryString.Value);
        }

        diagnosticContext.Set("ContentType", httpContext.Response.ContentType);

        var endpoint = httpContext.GetEndpoint();
        if (endpoint is object)
        {
            diagnosticContext.Set("EndpointName", endpoint.DisplayName);
        }
    }

    public static LogEventLevel GetLogEventLevel(HttpContext ctx, double _, Exception ex) =>
        ex != null
        ? LogEventLevel.Error
        : ctx.Response.StatusCode > 499
            ? LogEventLevel.Error
            : ShouldEndpointExcluded(ctx) // Not an error, check if it was a health check or loggiing disabled
                ? LogEventLevel.Verbose // Should be excluded, use Verbose for the log level to be lower than the minimum level specified in the logger configuration to be filtered
                : LogEventLevel.Information;

    private static bool ShouldEndpointExcluded(HttpContext ctx)
    {
        return IsHealthCheckEndpoint(ctx);
    }

    private static bool IsHealthCheckEndpoint(HttpContext ctx)
    {
        var endpoint = ctx.GetEndpoint();
        if (endpoint is object)
        {
            return string.Equals(endpoint.DisplayName, "Health checks", StringComparison.Ordinal);
        }
        return false;
    }
}