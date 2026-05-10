using Microsoft.EntityFrameworkCore;
using PaymentService.Features.Payments.Entities;
using PaymentService.Features.Payments.Models;
using PaymentService.Infrastructure;
using System.Text.Json;

namespace PaymentService.Features.Payments;

public static class PaymentEndpoints
{
    public static IEndpointRouteBuilder MapPaymentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/payments");

        group.MapGet("/methods", async (PaymentDbContext db) =>
        {
            var methods = await db.PaymentMethods
                .Where(m => m.IsActive)
                .Select(m => new PaymentMethodResponse
                {
                    Id = m.Id,
                    Name = m.Name,
                    Code = m.Code,
                    Icon = m.Icon,
                    ProcessingFee = m.ProcessingFee,
                    SupportedCurrencies = m.SupportedCurrencies.Split(',')
                })
                .ToListAsync();

            return Results.Ok(methods);
        });

        group.MapGet("/methods/{id:int}", async (int id, PaymentDbContext db) =>
        {
            var method = await db.PaymentMethods.FindAsync(id);
            if (method is null) return Results.NotFound();

            return Results.Ok(new PaymentMethodResponse
            {
                Id = method.Id,
                Name = method.Name,
                Code = method.Code,
                Icon = method.Icon,
                ProcessingFee = method.ProcessingFee,
                SupportedCurrencies = method.SupportedCurrencies.Split(',')
            });
        });

        group.MapPost("/process", async (ProcessPaymentRequest request, PaymentDbContext db) =>
        {
            var paymentMethod = await db.PaymentMethods.FindAsync(request.PaymentMethodId);
            if (paymentMethod is null)
                return Results.BadRequest(new { error = "Invalid payment method" });

            var transactionId = PaymentHelpers.GenerateTransactionId();
            var processingResult = await PaymentHelpers.SimulatePaymentProcessing(request, paymentMethod);

            if (processingResult.Success)
            {
                var transaction = new Transaction
                {
                    OrderId = request.OrderId,
                    UserId = request.UserId,
                    Amount = request.Amount,
                    Currency = request.Currency,
                    PaymentMethodId = request.PaymentMethodId,
                    Status = "Completed",
                    TransactionId = transactionId,
                    CreatedAt = DateTime.UtcNow,
                    Metadata = JsonSerializer.Serialize(new { request.CardLast4, request.BillingAddress })
                };

                db.Transactions.Add(transaction);
                await db.SaveChangesAsync();

                return Results.Ok(new PaymentResponse
                {
                    Success = true,
                    TransactionId = transactionId,
                    Amount = request.Amount,
                    Currency = request.Currency,
                    Status = "Completed",
                    Message = "Payment processed successfully",
                    ProcessingFee = paymentMethod.ProcessingFee,
                    TotalAmount = request.Amount + (request.Amount * paymentMethod.ProcessingFee / 100)
                });
            }

            db.Transactions.Add(new Transaction
            {
                OrderId = request.OrderId,
                UserId = request.UserId,
                Amount = request.Amount,
                Currency = request.Currency,
                PaymentMethodId = request.PaymentMethodId,
                Status = "Failed",
                TransactionId = transactionId,
                CreatedAt = DateTime.UtcNow,
                ErrorMessage = processingResult.ErrorMessage
            });
            await db.SaveChangesAsync();

            return Results.BadRequest(new PaymentResponse
            {
                Success = false,
                TransactionId = transactionId,
                Status = "Failed",
                Message = processingResult.ErrorMessage ?? "Payment failed"
            });
        });

        group.MapGet("/transactions/{transactionId}", async (string transactionId, PaymentDbContext db) =>
        {
            var transaction = await db.Transactions
                .Include(t => t.PaymentMethod)
                .FirstOrDefaultAsync(t => t.TransactionId == transactionId);

            if (transaction is null) return Results.NotFound();

            return Results.Ok(new TransactionResponse
            {
                Id = transaction.Id,
                OrderId = transaction.OrderId,
                UserId = transaction.UserId,
                Amount = transaction.Amount,
                Currency = transaction.Currency,
                PaymentMethod = transaction.PaymentMethod?.Name ?? "Unknown",
                Status = transaction.Status,
                TransactionId = transaction.TransactionId,
                CreatedAt = transaction.CreatedAt,
                ErrorMessage = transaction.ErrorMessage
            });
        });

        group.MapGet("/users/{userId:int}/transactions", async (int userId, PaymentDbContext db) =>
        {
            var transactions = await db.Transactions
                .Include(t => t.PaymentMethod)
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.CreatedAt)
                .Select(t => new TransactionResponse
                {
                    Id = t.Id,
                    OrderId = t.OrderId,
                    UserId = t.UserId,
                    Amount = t.Amount,
                    Currency = t.Currency,
                    PaymentMethod = t.PaymentMethod != null ? t.PaymentMethod.Name : "Unknown",
                    Status = t.Status,
                    TransactionId = t.TransactionId,
                    CreatedAt = t.CreatedAt
                })
                .ToListAsync();

            return Results.Ok(transactions);
        });

        group.MapPost("/refund", async (RefundRequest request, PaymentDbContext db) =>
        {
            var transaction = await db.Transactions
                .FirstOrDefaultAsync(t => t.TransactionId == request.TransactionId);

            if (transaction is null)
                return Results.NotFound(new { error = "Transaction not found" });

            if (transaction.Status != "Completed")
                return Results.BadRequest(new { error = "Cannot refund non-completed transaction" });

            if (request.Amount > transaction.Amount)
                return Results.BadRequest(new { error = "Refund amount exceeds transaction amount" });

            var existingRefund = await db.Refunds
                .FirstOrDefaultAsync(r => r.TransactionId == transaction.Id);

            if (existingRefund is not null && existingRefund.Status == "Completed")
                return Results.BadRequest(new { error = "Transaction already refunded" });

            var refundId = PaymentHelpers.GenerateRefundId();
            db.Refunds.Add(new Refund
            {
                TransactionId = transaction.Id,
                Amount = request.Amount,
                Reason = request.Reason,
                Status = "Completed",
                CreatedAt = DateTime.UtcNow,
                RefundTransactionId = refundId
            });

            transaction.Status = request.Amount == transaction.Amount ? "Refunded" : "PartiallyRefunded";

            await db.SaveChangesAsync();

            return Results.Ok(new RefundResponse
            {
                Success = true,
                RefundId = refundId,
                Amount = request.Amount,
                Status = "Completed",
                Message = "Refund processed successfully"
            });
        });

        group.MapGet("/users/{userId:int}/summary", async (int userId, PaymentDbContext db) =>
        {
            var transactions = await db.Transactions
                .Where(t => t.UserId == userId && t.Status == "Completed")
                .ToListAsync();

            var refunds = await db.Refunds
                .Include(r => r.Transaction)
                .Where(r => r.Transaction != null && r.Transaction.UserId == userId && r.Status == "Completed")
                .ToListAsync();

            var totalSpent = transactions.Sum(t => t.Amount);
            var totalRefunded = refunds.Sum(r => r.Amount);

            return Results.Ok(new PaymentSummary
            {
                UserId = userId,
                TotalSpent = totalSpent,
                TotalRefunded = totalRefunded,
                NetSpent = totalSpent - totalRefunded,
                TransactionCount = transactions.Count,
                RefundCount = refunds.Count,
                LastTransactionDate = transactions.Any() ? transactions.Max(t => t.CreatedAt) : null
            });
        });

        group.MapPost("/webhook", async (HttpContext context, PaymentDbContext db) =>
        {
            using var reader = new StreamReader(context.Request.Body);
            var payload = await reader.ReadToEndAsync();

            var webhookData = JsonSerializer.Deserialize<WebhookPayload>(payload);

            if (webhookData?.Event == "payment.succeeded")
            {
                var transaction = await db.Transactions
                    .FirstOrDefaultAsync(t => t.TransactionId == webhookData.TransactionId);

                if (transaction != null)
                {
                    transaction.Status = "Confirmed";
                    transaction.Metadata = JsonSerializer.Serialize(new { webhook = payload });
                    await db.SaveChangesAsync();
                }
            }

            return Results.Ok(new { received = true });
        });

        group.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

        return app;
    }
}
