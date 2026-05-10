namespace ProductService.Infrastructure;

public static class DbInitializer
{
    public static void Initialize(IServiceProvider serviceProvider)
    {
        var db = serviceProvider.GetRequiredService<ProductDbContext>();
        db.Database.EnsureCreated();
    }
}
