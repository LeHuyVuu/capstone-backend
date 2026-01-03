namespace capstone_backend.Api.Middleware;

// Middleware thêm TraceId vào request để theo dõi
public class TraceIdMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TraceIdMiddleware> _logger;

    public TraceIdMiddleware(RequestDelegate next, ILogger<TraceIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var traceId = context.Request.Headers["X-Trace-Id"].FirstOrDefault() ?? context.TraceIdentifier;
        
        context.Response.Headers.TryAdd("X-Trace-Id", traceId);
        context.Items["TraceId"] = traceId;

        _logger.LogInformation("{Method} {Path} - TraceId: {TraceId}", 
            context.Request.Method, context.Request.Path, traceId);

        await _next(context);
    }
}

public static class TraceIdMiddlewareExtensions
{
    public static IApplicationBuilder UseTraceId(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<TraceIdMiddleware>();
    }
}
