namespace ApiGateway.Features.Checkout.Models;

public record CheckoutPageResponse(
    Product[] Products,
    Cart Cart,
    User User,
    PaymentMethod[] PaymentMethods
);

public record Product(int Id, string Name, decimal Price, int Stock);

public record Cart(int UserId, List<CartItem> Items);

public record CartItem(int ProductId, int Quantity);

public record User(int Id, string Name, string Email);

public record PaymentMethod(int Id, string Name);
