namespace PaymentService.Features.Payments.Models;

public class ProcessPaymentRequest
{
    public string OrderId { get; set; } = string.Empty;
    public int UserId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public int PaymentMethodId { get; set; }
    public string? CardNumber { get; set; }
    public string? CardExpiry { get; set; }
    public string? CardCvv { get; set; }
    public string? CardLast4 { get; set; }
    public string? BillingAddress { get; set; }
}
