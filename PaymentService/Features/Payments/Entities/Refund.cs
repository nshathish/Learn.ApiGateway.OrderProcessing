using System.ComponentModel.DataAnnotations;

namespace PaymentService.Features.Payments.Entities;

public class Refund
{
    public int Id { get; set; }
    public int TransactionId { get; set; }
    public Transaction? Transaction { get; set; }
    public decimal Amount { get; set; }
    [MaxLength(2000)] public string Reason { get; set; } = string.Empty;
    [MaxLength(50)] public string Status { get; set; } = "Pending";
    public DateTime CreatedAt { get; set; }
    [MaxLength(100)] public string RefundTransactionId { get; set; } = string.Empty;
}