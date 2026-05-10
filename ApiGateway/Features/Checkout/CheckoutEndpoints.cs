using ApiGateway.Features.Checkout.Models;
using Microsoft.AspNetCore.Mvc;

namespace ApiGateway.Features.Checkout;

public static class CheckoutEndpoints
{
    public static IEndpointRouteBuilder MapCheckoutEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api");

        group.MapGet("/checkout-page/{userId}", async (
                int userId,
                [FromServices] IHttpClientFactory httpFactory,
                CancellationToken token) =>
            {
                using var productClient = httpFactory.CreateClient("ProductService");
                using var cartClient = httpFactory.CreateClient("CartService");
                using var userClient = httpFactory.CreateClient("UserService");
                using var paymentClient = httpFactory.CreateClient("PaymentService");

                Task[] tasks =
                [
                    productClient.GetFromJsonAsync<Product[]>("/api/products", token),
                    cartClient.GetFromJsonAsync<Cart>($"/api/cart/{userId}", token),
                    userClient.GetFromJsonAsync<User>($"/api/users/{userId}", token),
                    paymentClient.GetFromJsonAsync<PaymentMethod[]>("/api/payments/methods", token)
                ];

                await Task.WhenAll(tasks);

                var result = new CheckoutPageResponse(
                    Products: ((Task<Product[]?>)tasks[0]).Result ?? [],
                    Cart: ((Task<Cart?>)tasks[1]).Result ?? new Cart(0, []),
                    User: ((Task<User?>)tasks[2]).Result ?? new User(0, string.Empty, string.Empty),
                    PaymentMethods: ((Task<PaymentMethod[]?>)tasks[3]).Result ?? []
                );

                return Results.Ok(result);
            })
            .WithName("GetCheckoutPage")
            .RequireRateLimiting("checkout-limit");

        return app;
    }
}
