namespace UserService.Features.Users.Models;

public class UpdateUserRequest
{
    public string? Name { get; set; }
    public string? Address { get; set; }
    public string? Phone { get; set; }
}
