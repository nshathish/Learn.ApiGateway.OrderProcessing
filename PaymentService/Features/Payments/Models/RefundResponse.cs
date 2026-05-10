namespace PaymentService.Features.Payments.Models;

public class RefundResponse
{
    public bool Success { get; set; }
    public string RefundId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
