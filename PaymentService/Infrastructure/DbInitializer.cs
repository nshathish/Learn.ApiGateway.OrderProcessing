using PaymentService.Features.Payments.Entities;

namespace PaymentService.Infrastructure;

public static class DbInitializer
{
    public static void Initialize(IServiceProvider serviceProvider)
    {
        var db = serviceProvider.GetRequiredService<PaymentDbContext>();
        db.Database.EnsureCreated();

        if (!db.PaymentMethods.Any())
        {
            db.PaymentMethods.AddRange(
                new PaymentMethod { Id = 1, Name = "Credit Card", Code = "CC", IsActive = true, Icon = "💳", ProcessingFee = 2.9m, SupportedCurrencies = "USD,EUR,GBP" },
                new PaymentMethod { Id = 2, Name = "PayPal", Code = "PP", IsActive = true, Icon = "💰", ProcessingFee = 3.5m, SupportedCurrencies = "USD,EUR,GBP,CAD" },
                new PaymentMethod { Id = 3, Name = "Apple Pay", Code = "AP", IsActive = true, Icon = "📱", ProcessingFee = 2.5m, SupportedCurrencies = "USD,EUR" },
                new PaymentMethod { Id = 4, Name = "Google Pay", Code = "GP", IsActive = true, Icon = "🤖", ProcessingFee = 2.5m, SupportedCurrencies = "USD,EUR" },
                new PaymentMethod { Id = 5, Name = "Bank Transfer", Code = "BT", IsActive = true, Icon = "🏦", ProcessingFee = 0.5m, SupportedCurrencies = "USD,EUR,GBP" }
            );
            db.SaveChanges();
        }

        if (!db.Transactions.Any())
        {
            db.Transactions.AddRange(
                new Transaction { Id = 1, OrderId = "ORD-001", UserId = 1, Amount = 99.99m, Currency = "USD", PaymentMethodId = 1, Status = "Completed", TransactionId = "txn_001", CreatedAt = DateTime.UtcNow.AddDays(-5) },
                new Transaction { Id = 2, OrderId = "ORD-002", UserId = 2, Amount = 149.99m, Currency = "USD", PaymentMethodId = 2, Status = "Completed", TransactionId = "txn_002", CreatedAt = DateTime.UtcNow.AddDays(-3) },
                new Transaction { Id = 3, OrderId = "ORD-003", UserId = 1, Amount = 49.99m, Currency = "USD", PaymentMethodId = 3, Status = "Pending", TransactionId = "txn_003", CreatedAt = DateTime.UtcNow.AddDays(-1) }
            );
            db.SaveChanges();
        }

        if (!db.Refunds.Any())
        {
            db.Refunds.Add(new Refund
            {
                Id = 1,
                TransactionId = 1,
                Amount = 99.99m,
                Reason = "Customer requested refund",
                Status = "Completed",
                CreatedAt = DateTime.UtcNow.AddDays(-4),
                RefundTransactionId = "ref_001"
            });
            db.SaveChanges();
        }
    }
}
