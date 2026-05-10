namespace UserService.Features.Users.Models;

public class AddressHistoryResponse
{
    public int Id { get; set; }
    public string Address { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
