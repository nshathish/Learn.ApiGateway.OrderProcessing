namespace CartService.Infrastructure;

public static class DbInitializer
{
    public static void Initialize(IServiceProvider serviceProvider)
    {
        var db = serviceProvider.GetRequiredService<CartDbContext>();
        db.Database.EnsureCreated();
    }
}
