using ApiGateway.Configuration;
using ApiGateway.Features.Checkout.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace ApiGateway.Features.Checkout;

public static class CheckoutEndpoints
{
    public static IEndpointRouteBuilder MapCheckoutEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api");

        group.MapGet("/checkout-page/{userId}", async (
                int userId,
                [FromServices] IHttpClientFactory httpFactory,
                [FromServices] IOptions<ServiceUrlsOptions> serviceUrls,
                CancellationToken token) =>
            {
                using var client = httpFactory.CreateClient();

                var products = await client.GetFromJsonAsync<Product[]>($"{serviceUrls.Value.ProductService}/api/products", token);
                var cart = await client.GetFromJsonAsync<Cart>($"{serviceUrls.Value.CartService}/api/cart/{userId}", token);
                var user = await client.GetFromJsonAsync<User>($"{serviceUrls.Value.UserService}/api/users/{userId}", token);
                var paymentMethods = await client.GetFromJsonAsync<PaymentMethod[]>($"{serviceUrls.Value.PaymentService}/api/payments/methods", token);

                var result = new CheckoutPageResponse(
                    Products: products ?? [],
                    Cart: cart ?? new Cart(0, []),
                    User: user ?? new User(0, string.Empty, string.Empty),
                    PaymentMethods: paymentMethods ?? []
                );

                return Results.Ok(result);
            })
            .WithName("GetCheckoutPage")
            .RequireRateLimiting("checkout-limit");

        return app;
    }
}
