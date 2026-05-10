namespace ApiGateway.Features.Orders.Models;

public record CreateOrderRequest(int UserId, int[] ProductIds);
