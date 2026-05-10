using Microsoft.EntityFrameworkCore;
using ProductService.Features.Products;

namespace ProductService.Infrastructure;

public class ProductDbContext(DbContextOptions<ProductDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products { get; set; }
}
