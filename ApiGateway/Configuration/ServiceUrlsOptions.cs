namespace ApiGateway.Configuration;

public class ServiceUrlsOptions
{
    public string ProductService { get; set; } = "https://localhost:5001";
    public string CartService { get; set; } = "https://localhost:5002";
    public string UserService { get; set; } = "https://localhost:5003";
    public string PaymentService { get; set; } = "https://localhost:5004";
}
