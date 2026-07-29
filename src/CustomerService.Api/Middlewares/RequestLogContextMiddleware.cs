using Serilog.Context;

namespace CustomerService.Api.Middlewares;

public sealed class RequestLogContextMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = GetOrCreateCorrelationId(context);
        context.Response.Headers["X-Correlation-ID"] = correlationId;

        using (LogContext.PushProperty("CorrelationId", correlationId))
        using (LogContext.PushProperty("TraceId", context.TraceIdentifier))
        using (LogContext.PushProperty("RequestId", context.TraceIdentifier))
        using (LogContext.PushProperty("ClientIp", context.Connection.RemoteIpAddress?.ToString() ?? "unknown"))
        using (LogContext.PushProperty("UserAgent", context.Request.Headers.UserAgent.ToString()))
        using (LogContext.PushProperty("RequestPath", context.Request.Path.Value ?? string.Empty))
        using (LogContext.PushProperty("RequestMethod", context.Request.Method))
        {
            await next(context);
        }
    }

    private static string GetOrCreateCorrelationId(HttpContext context)
    {
        const string headerName = "X-Correlation-ID";
        var supplied = context.Request.Headers[headerName].FirstOrDefault();
        return string.IsNullOrWhiteSpace(supplied)
            ? Guid.NewGuid().ToString("N")
            : supplied.Trim();
    }
}
