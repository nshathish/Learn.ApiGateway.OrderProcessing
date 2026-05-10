using System.ComponentModel.DataAnnotations;

namespace UserService.Features.Users.Entities;

public class AddressHistory
{
    public int Id { get; set; }
    public int UserId { get; set; }
    [MaxLength(300)] public string Address { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}