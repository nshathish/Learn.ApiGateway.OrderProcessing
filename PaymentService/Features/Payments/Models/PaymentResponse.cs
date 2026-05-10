namespace PaymentService.Features.Payments.Models;

public class PaymentResponse
{
    public bool Success { get; set; }
    public string? TransactionId { get; set; }
    public decimal? Amount { get; set; }
    public string? Currency { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public decimal? ProcessingFee { get; set; }
    public decimal? TotalAmount { get; set; }
}
