using System.ComponentModel.DataAnnotations;

namespace PaymentService.Features.Payments.Entities;

public class Transaction
{
    public int Id { get; set; }
    [MaxLength(200)] public string OrderId { get; set; } = string.Empty;
    public int UserId { get; set; }
    public decimal Amount { get; set; }
    [MaxLength(10)] public string Currency { get; set; } = "USD";
    public int PaymentMethodId { get; set; }
    public PaymentMethod? PaymentMethod { get; set; }
    [MaxLength(50)] public string Status { get; set; } = "Pending";
    [MaxLength(100)] public string TransactionId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    [MaxLength(500)] public string? ErrorMessage { get; set; }
    [MaxLength(1000)] public string? Metadata { get; set; }
}