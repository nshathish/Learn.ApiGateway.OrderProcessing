namespace PaymentService.Features.Payments.Models;

public class WebhookPayload
{
    public string Event { get; set; } = string.Empty;
    public string TransactionId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}
