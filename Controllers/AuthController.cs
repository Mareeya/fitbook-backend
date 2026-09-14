using FitBook_App.Data;
using FitBook_App.Domain;
using FitBook_App.Domain.Enums;
using FitBook_App.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FitBook_App.Controllers;

[Route("api/auth")]
[ApiController]
public class AuthController : ControllerBase
{
    // tools
    private readonly AppDbContext _appDbContext; //database connection
    private readonly IPasswordHasher<User> _passwordHasher; //password hasher

    public AuthController(
        AppDbContext appDbContext,
        IPasswordHasher<User> passwordHasher)
    {
        this._appDbContext = appDbContext;
        this._passwordHasher = passwordHasher;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        try
        {
            var user = await this._appDbContext.Users
                .FirstOrDefaultAsync(user => user.Email == request.Email);

            if (user == null)
            {
                return Unauthorized("Invalid email or password.");
            }

            var passwordResult = this._passwordHasher.VerifyHashedPassword(
                user,
                user.PasswordHash,
                request.Password);

            if (passwordResult == PasswordVerificationResult.Failed)
            {
                return Unauthorized("Invalid email or password.");
            }

            var response = new LoginResponse
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                Role = (byte)user.Role
            };

            return Ok(response);
        }
        catch (Exception)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred.");
        }
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        try
        {
            var name = request.Name.Trim();
            var email = request.Email.Trim();
            var password = request.Password.Trim();

            if (string.IsNullOrWhiteSpace(password))
            {
                return Conflict("Password is required.");
            }

            var emailTaken = await this._appDbContext.Users.AnyAsync(user => user.Email == email);
            if (emailTaken)
            {
                return Conflict("This email is already registered.");
            }

            var user = new User
            {
                Name = name,
                Email = email,
                Role = UserRole.Member,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            user.PasswordHash = this._passwordHasher.HashPassword(user, password);
            this._appDbContext.Users.Add(user);
            await this._appDbContext.SaveChangesAsync();

            var response = new LoginResponse
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                Role = (byte)user.Role
            };

            return Ok(response);
        }
        catch (Exception)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred.");
        }
    }
}
