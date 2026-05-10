using Microsoft.EntityFrameworkCore;
using CartService.Infrastructure;

namespace CartService.Features.Cart;

public static class CartEndpoints
{
    public static IEndpointRouteBuilder MapCartEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/cart");

        group.MapGet("/{userId:int}", async (int userId, CartDbContext db) =>
        {
            var cart = await db.Carts
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            return cart is not null ? Results.Ok(cart) : Results.Ok(new Cart { UserId = userId });
        });

        group.MapPost("/{userId:int}/items",
            async (
                int userId,
                AddItemRequest request,
                CartDbContext db,
                IHttpClientFactory http,
                IConfiguration config) =>
            {
                var productServiceUrl = config["ServiceUrls:ProductService"] ?? "https://localhost:5001";

                var productClient = http.CreateClient();
                var productResponse =
                    await productClient.GetAsync($"{productServiceUrl}/api/products/{request.ProductId}");

                if (!productResponse.IsSuccessStatusCode)
                {
                    return Results.BadRequest("Product not found");
                }

                var product = await productResponse.Content.ReadFromJsonAsync<Product>();

                var cart = await db.Carts
                    .Include(c => c.Items)
                    .FirstOrDefaultAsync(c => c.UserId == userId);

                if (cart is null)
                {
                    cart = new Cart { UserId = userId };
                    db.Carts.Add(cart);
                }

                var existingItem = cart.Items.FirstOrDefault(i => i.ProductId == request.ProductId);
                if (existingItem is not null)
                {
                    existingItem.Quantity += request.Quantity;
                }
                else
                {
                    cart.Items.Add(new CartItem
                    {
                        ProductId = request.ProductId,
                        ProductName = product!.Name,
                        Price = product.Price,
                        Quantity = request.Quantity
                    });
                }

                await db.SaveChangesAsync();
                return Results.Ok(cart);
            });

        group.MapDelete("/{userId:int}/items/{productId:int}", async (
            int userId,
            int productId,
            CartDbContext db) =>
        {
            var cart = await db.Carts
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart is null)
            {
                return Results.NotFound();
            }

            var item = cart.Items.FirstOrDefault(i => i.ProductId == productId);
            if (item is not null)
            {
                cart.Items.Remove(item);
                await db.SaveChangesAsync();
            }

            return Results.Ok(cart);
        });

        return app;
    }
}