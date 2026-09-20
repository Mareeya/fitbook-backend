using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FitBook_App.Data;
using FitBook_App.Domain;
using FitBook_App.Domain.Enums;
using FitBook_App.Helpers;
using FitBook_App.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

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
    private readonly IConfiguration _configuration;

    public AuthService(AppDbContext db, IPasswordHasher<User> passwordHasher, IConfiguration configuration)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _configuration = configuration;
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

    private LoginResponse ToResponse(User user)
    {
        return new LoginResponse
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            Role = (byte)user.Role,
            Token = CreateToken(user)
        };
    }

    private string CreateToken(User user)
    {
        var key = _configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key is missing.");
        var issuer = _configuration["Jwt:Issuer"];
        var audience = _configuration["Jwt:Audience"];
        var expireMinutes = int.TryParse(_configuration["Jwt:ExpireMinutes"], out var minutes) ? minutes : 480;

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Name),
            new Claim(ClaimTypes.Role, user.Role.ToString())
        };

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer,
            audience,
            claims,
            expires: DateTime.UtcNow.AddMinutes(expireMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
