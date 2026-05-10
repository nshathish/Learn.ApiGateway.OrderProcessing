namespace PaymentService.Features.Payments.Models;

public class PaymentSummary
{
    public int UserId { get; set; }
    public decimal TotalSpent { get; set; }
    public decimal TotalRefunded { get; set; }
    public decimal NetSpent { get; set; }
    public int TransactionCount { get; set; }
    public int RefundCount { get; set; }
    public DateTime? LastTransactionDate { get; set; }
}
