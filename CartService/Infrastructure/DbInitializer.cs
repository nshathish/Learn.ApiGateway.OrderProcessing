using Microsoft.Data.Sqlite;

namespace CartService.Infrastructure;

public static class DbInitializer
{
    public static void Initialize(IServiceProvider serviceProvider)
    {
        var db = serviceProvider.GetRequiredService<CartDbContext>();
        db.Database.EnsureCreated();

        try
        {
            _ = db.Carts.Any();
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode == 1 && ex.Message.Contains("no such table", StringComparison.OrdinalIgnoreCase))
        {
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();
        }
    }
}
