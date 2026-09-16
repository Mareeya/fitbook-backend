using FitBook_App.Data;
using FitBook_App.Domain;
using FitBook_App.Domain.Enums;
using FitBook_App.Helpers;
using FitBook_App.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FitBook_App.Services;

public interface IAuthService
{
    Task<LoginResponse?> LoginAsync(LoginRequest request);
    Task<LoginResponse> RegisterAsync(RegisterRequest request);
}

public class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly IPasswordHasher<User> _passwordHasher;

    public AuthService(AppDbContext db, IPasswordHasher<User> passwordHasher)
    {
        _db = db;
        _passwordHasher = passwordHasher;
    }

    public async Task<LoginResponse?> LoginAsync(LoginRequest request)
    {
        var email = EmailNormalizer.Normalize(request.Email);
        var user = await _db.Users.FirstOrDefaultAsync(item => item.Email == email);
        if (user == null)
        {
            return null;
        }

        var passwordResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password.Trim());
        if (passwordResult == PasswordVerificationResult.Failed)
        {
            return null;
        }

        return ToResponse(user);
    }

    public async Task<LoginResponse> RegisterAsync(RegisterRequest request)
    {
        var name = request.Name.Trim();
        var email = EmailNormalizer.Normalize(request.Email);
        var password = request.Password.Trim();

        if (string.IsNullOrWhiteSpace(password))
        {
            throw new AppException(StatusCodes.Status400BadRequest, "Password is required.");
        }

        var emailTaken = await _db.Users.AnyAsync(user => user.Email == email);
        if (emailTaken)
        {
            throw new AppException(StatusCodes.Status409Conflict, "This email is already registered.");
        }

        var user = new User
        {
            Name = name,
            Email = email,
            Role = UserRole.Member,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        user.PasswordHash = _passwordHasher.HashPassword(user, password);

        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        return ToResponse(user);
    }

    private static LoginResponse ToResponse(User user)
    {
        return new LoginResponse
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            Role = (byte)user.Role
        };
    }
}
