using ApiGateway.Features.Orders.Models;

namespace ApiGateway.Features.Orders;

public static class OrderEndpoints
{
    public static IEndpointRouteBuilder MapOrderEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/orders");

        group.MapPost("/", (CreateOrderRequest _) =>
        {
            return Results.Ok(new { OrderId = Guid.NewGuid(), Status = "Created" });
        });

        return app;
    }
}
