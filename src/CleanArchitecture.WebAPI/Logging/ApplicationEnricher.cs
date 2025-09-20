using Serilog.Core;
using Serilog.Events;

namespace CleanArchitecture.WebAPI.Logging;

public class ApplicationEnricher : ILogEventEnricher
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ApplicationEnricher(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null) return;

        // Add user information if available
        if (httpContext.User?.Identity?.IsAuthenticated == true)
        {
            var userIdProperty = propertyFactory.CreateProperty("UserId", 
                httpContext.User.FindFirst("sub")?.Value ?? "Unknown");
            logEvent.AddPropertyIfAbsent(userIdProperty);

            var userNameProperty = propertyFactory.CreateProperty("UserName", 
                httpContext.User.Identity.Name ?? "Unknown");
            logEvent.AddPropertyIfAbsent(userNameProperty);
        }

        // Add request information
        var requestIdProperty = propertyFactory.CreateProperty("RequestId", httpContext.TraceIdentifier);
        logEvent.AddPropertyIfAbsent(requestIdProperty);

        var ipAddressProperty = propertyFactory.CreateProperty("ClientIP", 
            GetClientIpAddress(httpContext));
        logEvent.AddPropertyIfAbsent(ipAddressProperty);

        var userAgentProperty = propertyFactory.CreateProperty("UserAgent", 
            httpContext.Request.Headers.UserAgent.ToString());
        logEvent.AddPropertyIfAbsent(userAgentProperty);

        // Add request path and method
        var pathProperty = propertyFactory.CreateProperty("RequestPath", httpContext.Request.Path);
        logEvent.AddPropertyIfAbsent(pathProperty);

        var methodProperty = propertyFactory.CreateProperty("RequestMethod", httpContext.Request.Method);
        logEvent.AddPropertyIfAbsent(methodProperty);
    }

    private static string GetClientIpAddress(HttpContext context)
    {
        // Check for forwarded IP first (in case of proxy/load balancer)
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',')[0].Trim();
        }

        var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
        {
            return realIp;
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
    }
}