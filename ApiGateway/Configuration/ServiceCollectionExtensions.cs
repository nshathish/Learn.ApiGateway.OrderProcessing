using ApiGateway.Security;
using System.Threading.RateLimiting;

namespace ApiGateway.Configuration;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddGatewayServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOpenApi();
        services.Configure<ServiceUrlsOptions>(configuration.GetSection("ServiceUrls"));

        var productServiceUrl = configuration["ServiceUrls:ProductService"] ?? "https://localhost:5001";
        var cartServiceUrl = configuration["ServiceUrls:CartService"] ?? "https://localhost:5002";
        var userServiceUrl = configuration["ServiceUrls:UserService"] ?? "https://localhost:5003";
        var paymentServiceUrl = configuration["ServiceUrls:PaymentService"] ?? "https://localhost:5004";

        services.AddHttpClient("ProductService", client => client.BaseAddress = new Uri(productServiceUrl));
        services.AddHttpClient("CartService", client => client.BaseAddress = new Uri(cartServiceUrl));
        services.AddHttpClient("UserService", client => client.BaseAddress = new Uri(userServiceUrl));
        services.AddHttpClient("PaymentService", client => client.BaseAddress = new Uri(paymentServiceUrl));

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
                policy.WithOrigins(
                        "http://localhost:4200",
                        "https://icy-water-07cfdfb03.7.azurestaticapps.net"
                    )
                    .WithMethods("GET", "POST", "PUT", "DELETE")
                    .AllowAnyHeader()
                    .WithExposedHeaders("X-RequestId", "X-RateLimit-Remaining");
            });
        });

        services.AddHealthChecks();

        return services;
    }
}
