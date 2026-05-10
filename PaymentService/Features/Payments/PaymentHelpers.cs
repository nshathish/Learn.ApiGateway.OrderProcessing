using PaymentService.Features.Payments.Entities;
using PaymentService.Features.Payments.Models;
using System.Security.Cryptography;

namespace PaymentService.Features.Payments;

public static class PaymentHelpers
{
    public static string GenerateTransactionId()
    {
        return $"txn_{DateTime.UtcNow:yyyyMMddHHmmss}_{RandomNumberGenerator.GetInt32(100000, 999999)}";
    }

    public static string GenerateRefundId()
    {
        return $"ref_{DateTime.UtcNow:yyyyMMddHHmmss}_{RandomNumberGenerator.GetInt32(100000, 999999)}";
    }

    public static async Task<(bool Success, string? ErrorMessage)> SimulatePaymentProcessing(ProcessPaymentRequest request, PaymentMethod method)
    {
        await Task.Delay(500);

        var random = new Random();
        if (random.Next(1, 100) <= 5)
        {
            return (false, "Payment declined by bank. Please use a different card.");
        }

        if (request.Amount <= 0)
            return (false, "Invalid amount");

        if (string.IsNullOrEmpty(request.Currency))
            return (false, "Currency is required");

        if (method.Code == "CC" && !string.IsNullOrEmpty(request.CardNumber))
        {
            if (!IsValidLuhn(request.CardNumber.Replace(" ", "")))
                return (false, "Invalid card number");
        }

        return (true, null);
    }

    private static bool IsValidLuhn(string cardNumber)
    {
        var sum = 0;
        var alternate = false;

        for (var i = cardNumber.Length - 1; i >= 0; i--)
        {
            var n = int.Parse(cardNumber[i].ToString());
            if (alternate)
            {
                n *= 2;
                if (n > 9)
                    n = (n % 10) + 1;
            }

            sum += n;
            alternate = !alternate;
        }

        return (sum % 10 == 0);
    }
}
