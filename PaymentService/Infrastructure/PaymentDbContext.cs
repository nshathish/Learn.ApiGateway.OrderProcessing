using Microsoft.EntityFrameworkCore;
using PaymentService.Features.Payments.Entities;

namespace PaymentService.Infrastructure;

public class PaymentDbContext(DbContextOptions<PaymentDbContext> options) : DbContext(options)
{
    public DbSet<PaymentMethod> PaymentMethods { get; set; }
    public DbSet<Transaction> Transactions { get; set; }
    public DbSet<Refund> Refunds { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Transaction>()
            .HasIndex(t => t.TransactionId)
            .IsUnique();

        modelBuilder.Entity<Transaction>()
            .HasIndex(t => t.OrderId);

        modelBuilder.Entity<Transaction>()
            .HasIndex(t => t.UserId);

        modelBuilder.Entity<Refund>()
            .HasIndex(r => r.RefundTransactionId)
            .IsUnique();
    }
}
