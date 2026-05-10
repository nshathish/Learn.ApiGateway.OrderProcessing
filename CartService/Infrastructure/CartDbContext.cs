using Microsoft.EntityFrameworkCore;
using CartService.Features.Cart;

namespace CartService.Infrastructure;

public class CartDbContext(DbContextOptions<CartDbContext> options) : DbContext(options)
{
    public DbSet<Cart> Carts { get; set; }
    public DbSet<CartItem> CartItems { get; set; }
}
