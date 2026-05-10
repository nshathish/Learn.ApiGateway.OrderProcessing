using ApiGateway.Security;
using System.Threading.RateLimiting;

namespace ApiGateway.Configuration;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddGatewayServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOpenApi();
        services.AddHttpClient();
        services.Configure<ServiceUrlsOptions>(configuration.GetSection("ServiceUrls"));

        services.AddReverseProxy()
            .LoadFromConfig(configuration.GetSection("ReverseProxy"));

        services.AddRateLimiter(options =>
        {
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.User.Identity?.Name ?? httpContext.Request.Headers.Host.ToString(),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = 100,
                        QueueLimit = 0,
                        Window = TimeSpan.FromMinutes(1)
                    }));

            options.AddPolicy("checkout-limit", httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = 5,
                        Window = TimeSpan.FromSeconds(10)
                    }));

            options.OnRejected = async (context, token) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                await context.HttpContext.Response.WriteAsync("Rate limit exceeded. Try again later.", token);
            };
        });

        services.AddAuthentication("ApiKey")
            .AddScheme<ApiKeySchemeOptions, ApiKeyAuthenticationHandler>("ApiKey", null);

        services.AddResponseCompression(options =>
        {
            options.EnableForHttps = true;
            options.MimeTypes = ["application/json", "text/plain"];
        });

        services.AddCors(options =>
        {
            options.AddPolicy("AllowWebUI", policy =>
            {
                policy.WithOrigins("http://localhost:4200")
                    .WithMethods("GET", "POST", "PUT", "DELETE")
                    .AllowAnyHeader()
                    .WithExposedHeaders("X-RequestId", "X-RateLimit-Remaining");
            });
        });

        return services;
    }
}
