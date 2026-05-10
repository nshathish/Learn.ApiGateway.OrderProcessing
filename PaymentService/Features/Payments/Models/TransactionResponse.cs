namespace PaymentService.Features.Payments.Models;

public class TransactionResponse
{
    public int Id { get; set; }
    public string OrderId { get; set; } = string.Empty;
    public int UserId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string TransactionId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string? ErrorMessage { get; set; }
}
