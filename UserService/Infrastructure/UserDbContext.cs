using Microsoft.EntityFrameworkCore;
using UserService.Features.Users.Entities;

namespace UserService.Infrastructure;

public class UserDbContext(DbContextOptions<UserDbContext> options) : DbContext(options)
{
    public DbSet<User> Users { get; set; }
    public DbSet<AddressHistory> AddressHistories { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<AddressHistory>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(ah => ah.UserId);
    }
}
