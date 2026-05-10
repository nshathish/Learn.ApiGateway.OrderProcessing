using Microsoft.Data.Sqlite;
using ProductService.Features.Products;

namespace ProductService.Infrastructure;

public static class DbInitializer
{
    public static void Initialize(IServiceProvider serviceProvider)
    {
        var db = serviceProvider.GetRequiredService<ProductDbContext>();
        db.Database.EnsureCreated();

        try
        {
            _ = db.Products.Any();
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode == 1 && ex.Message.Contains("no such table", StringComparison.OrdinalIgnoreCase))
        {
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();
        }

        if (db.Products.Any())
        {
            return;
        }

        db.Products.AddRange(
            new Product { Name = "Laptop", Price = 1299.99m, Stock = 12 },
            new Product { Name = "Mechanical Keyboard", Price = 149.99m, Stock = 40 },
            new Product { Name = "Wireless Mouse", Price = 59.99m, Stock = 60 },
            new Product { Name = "4K Monitor", Price = 399.99m, Stock = 18 },
            new Product { Name = "USB-C Dock", Price = 89.99m, Stock = 25 });

        db.SaveChanges();
    }
}
