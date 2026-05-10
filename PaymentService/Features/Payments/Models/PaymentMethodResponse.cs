namespace PaymentService.Features.Payments.Models;

public class PaymentMethodResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public decimal ProcessingFee { get; set; }
    public string[] SupportedCurrencies { get; set; } = Array.Empty<string>();
}
