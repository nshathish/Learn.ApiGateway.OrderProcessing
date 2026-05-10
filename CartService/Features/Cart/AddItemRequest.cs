namespace CartService.Features.Cart;

public class AddItemRequest
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
}
