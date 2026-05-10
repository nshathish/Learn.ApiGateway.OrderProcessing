using UserService.Features.Users.Entities;

namespace UserService.Infrastructure;

public static class DbInitializer
{
    public static void Initialize(IServiceProvider serviceProvider)
    {
        var db = serviceProvider.GetRequiredService<UserDbContext>();
        db.Database.EnsureCreated();

        if (!db.Users.Any())
        {
            db.Users.AddRange(
                new User
                {
                    Id = 1,
                    Name = "John Doe",
                    Email = "john@example.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
                    Address = "123 Main St, New York, NY 10001",
                    Phone = "+1 (555) 123-4567",
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                },
                new User
                {
                    Id = 2,
                    Name = "Jane Smith",
                    Email = "jane@example.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("password456"),
                    Address = "456 Oak Ave, Los Angeles, CA 90210",
                    Phone = "+1 (555) 987-6543",
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                },
                new User
                {
                    Id = 3,
                    Name = "Bob Johnson",
                    Email = "bob@example.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("password789"),
                    Address = "789 Pine Rd, Chicago, IL 60601",
                    Phone = "+1 (555) 456-7890",
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                }
            );
            db.SaveChanges();
        }

        if (!db.AddressHistories.Any())
        {
            db.AddressHistories.AddRange(
                new AddressHistory { Id = 1, UserId = 1, Address = "123 Main St, New York, NY 10001", CreatedAt = DateTime.UtcNow.AddMonths(-6) },
                new AddressHistory { Id = 2, UserId = 2, Address = "456 Oak Ave, Los Angeles, CA 90210", CreatedAt = DateTime.UtcNow.AddMonths(-3) },
                new AddressHistory { Id = 3, UserId = 3, Address = "789 Pine Rd, Chicago, IL 60601", CreatedAt = DateTime.UtcNow.AddMonths(-1) }
            );
            db.SaveChanges();
        }
    }
}
