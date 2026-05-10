using System.ComponentModel.DataAnnotations;

namespace PaymentService.Features.Payments.Entities;

public class PaymentMethod
{
    public int Id { get; set; }
    [MaxLength(200)] public string Name { get; set; } = string.Empty;
    [MaxLength(50)] public string Code { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    [MaxLength(200)] public string Icon { get; set; } = string.Empty;
    public decimal ProcessingFee { get; set; }
    [MaxLength(100)] public string SupportedCurrencies { get; set; } = string.Empty;
}
