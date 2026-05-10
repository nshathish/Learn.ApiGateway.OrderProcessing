using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using UserService.Features.Users.Entities;
using UserService.Features.Users.Models;
using UserService.Infrastructure;

namespace UserService.Features.Users;

public static class UserEndpoints
{
    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/users");

        group.MapGet("/", async (UserDbContext db) =>
        {
            var users = await db.Users
                .Select(u => new UserResponse
                {
                    Id = u.Id,
                    Name = u.Name,
                    Email = u.Email,
                    Address = u.Address,
                    Phone = u.Phone,
                    CreatedAt = u.CreatedAt,
                    IsActive = u.IsActive
                })
                .ToListAsync();
            return Results.Ok(users);
        });

        group.MapGet("/{id:int}", async (int id, UserDbContext db) =>
        {
            var user = await db.Users.FindAsync(id);
            if (user is null) return Results.NotFound(new { error = $"User with ID {id} not found" });

            return Results.Ok(new UserResponse
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                Address = user.Address,
                Phone = user.Phone,
                CreatedAt = user.CreatedAt,
                IsActive = user.IsActive
            });
        });

        group.MapGet("/email/{email}", async (string email, UserDbContext db) =>
        {
            var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user is null) return Results.NotFound(new { error = $"User with email {email} not found" });

            return Results.Ok(new UserResponse
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                Address = user.Address,
                Phone = user.Phone,
                CreatedAt = user.CreatedAt,
                IsActive = user.IsActive
            });
        });

        group.MapPost("/", async (CreateUserRequest request, UserDbContext db, IConfiguration config) =>
        {
            var existingUser = await db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (existingUser is not null)
                return Results.BadRequest(new { error = "User with this email already exists" });

            var user = new User
            {
                Name = request.Name,
                Email = request.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                Address = request.Address,
                Phone = request.Phone,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            db.Users.Add(user);
            await db.SaveChangesAsync();

            var token = UserTokenService.GenerateJwtToken(user.Id, user.Email, config);

            return Results.Created($"/api/users/{user.Id}", new
            {
                user.Id,
                user.Name,
                user.Email,
                Token = token
            });
        });

        group.MapPut("/{id:int}", async (int id, UpdateUserRequest request, UserDbContext db) =>
        {
            var user = await db.Users.FindAsync(id);
            if (user is null) return Results.NotFound();

            if (!string.IsNullOrEmpty(request.Address) && user.Address != request.Address)
            {
                db.AddressHistories.Add(new AddressHistory
                {
                    UserId = user.Id,
                    Address = user.Address,
                    CreatedAt = DateTime.UtcNow
                });
            }

            user.Name = request.Name ?? user.Name;
            user.Address = request.Address ?? user.Address;
            user.Phone = request.Phone ?? user.Phone;
            user.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync();

            return Results.Ok(new UserResponse
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                Address = user.Address,
                Phone = user.Phone,
                CreatedAt = user.CreatedAt,
                IsActive = user.IsActive
            });
        });

        group.MapDelete("/{id:int}", async (int id, UserDbContext db) =>
        {
            var user = await db.Users.FindAsync(id);
            if (user is null) return Results.NotFound();

            user.IsActive = false;
            user.DeletedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();

            return Results.Ok(new { message = "User deactivated successfully" });
        });

        group.MapPost("/login", async (LoginRequest request, UserDbContext db, IConfiguration config) =>
        {
            var user = await db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                return Results.Unauthorized();

            if (!user.IsActive)
                return Results.BadRequest(new { error = "Account is deactivated" });

            var token = UserTokenService.GenerateJwtToken(user.Id, user.Email, config);

            return Results.Ok(new
            {
                user.Id,
                user.Name,
                user.Email,
                Token = token,
                ExpiresIn = 3600
            });
        });

        group.MapGet("/{id:int}/address-history", async (int id, UserDbContext db) =>
        {
            var user = await db.Users.FindAsync(id);
            if (user is null) return Results.NotFound();

            var history = await db.AddressHistories
                .Where(h => h.UserId == id)
                .OrderByDescending(h => h.CreatedAt)
                .Select(h => new AddressHistoryResponse
                {
                    Id = h.Id,
                    Address = h.Address,
                    CreatedAt = h.CreatedAt
                })
                .ToListAsync();

            return Results.Ok(new { currentAddress = user.Address, history });
        });

        group.MapGet("/validate", async (HttpContext httpContext, UserDbContext db) =>
        {
            if (!httpContext.User.Identity?.IsAuthenticated ?? true)
                return Results.Unauthorized();

            var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                return Results.Unauthorized();

            var user = await db.Users.FindAsync(userId);
            if (user is null || !user.IsActive)
                return Results.Unauthorized();

            return Results.Ok(new
            {
                userId = user.Id,
                user.Email,
                user.Name,
                isValid = true
            });
        });

        return app;
    }
}
