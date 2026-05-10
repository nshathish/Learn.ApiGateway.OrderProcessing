namespace ApiGateway.Middleware;

public static class RequestTrackingMiddlewareExtensions
{
    public static IApplicationBuilder UseRequestTracking(this IApplicationBuilder app)
    {
        app.Use(async (context, next) =>
        {
            var requestId = Guid.NewGuid().ToString();
            context.Response.Headers.Append("X-RequestId", requestId);

            var startTime = DateTime.UtcNow;
            await next.Invoke();
            var duration = DateTime.UtcNow - startTime;

            var logger = context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("ApiGateway.RequestTracking");
            logger.LogInformation(
                "Request {RequestId}: {Method} {Path} -> {StatusCode} ({Duration}ms)",
                requestId, context.Request.Method, context.Request.Path,
                context.Response.StatusCode, duration.TotalMilliseconds);
        });

        return app;
    }
}
