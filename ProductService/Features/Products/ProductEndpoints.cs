using Microsoft.EntityFrameworkCore;
using ProductService.Infrastructure;

namespace ProductService.Features.Products;

public static class ProductEndpoints
{
    public static IEndpointRouteBuilder MapProductEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/products");

        group.MapGet("/", async (ProductDbContext db) =>
        {
            var products = await db.Products.ToListAsync();
            return Results.Ok(products);
        });

        group.MapGet("/{id:int}", async (int id, ProductDbContext db) =>
        {
            var product = await db.Products.FindAsync(id);
            return product is not null ? Results.Ok(product) : Results.NotFound();
        });

        group.MapPost("/", async (CreateProductRequest request, ProductDbContext db) =>
        {
            var product = new Product
            {
                Name = request.Name,
                Price = request.Price,
                Stock = request.Stock
            };

            db.Products.Add(product);
            await db.SaveChangesAsync();
            return Results.Created($"/api/products/{product.Id}", product);
        });

        group.MapPut("/{id:int}/stock", async (int id, UpdateStockRequest request, ProductDbContext db) =>
        {
            var product = await db.Products.FindAsync(id);
            if (product is null)
            {
                return Results.NotFound();
            }

            product.Stock = request.Quantity;
            await db.SaveChangesAsync();
            return Results.Ok(product);
        });

        return app;
    }
}
