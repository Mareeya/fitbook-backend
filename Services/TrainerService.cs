using FitBook_App.Data;
using FitBook_App.Domain;
using FitBook_App.Domain.Enums;
using FitBook_App.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FitBook_App.Services;

public interface ITrainerService
{
    Task<List<TrainerResponse>> GetAllAsync();
    Task<TrainerResponse?> GetByIdAsync(int id);
    Task<TrainerResponse?> GetByUserIdAsync(int userId);
    Task<List<TrainerResponse>> GetByNameAsync(string name);
    Task<TrainerResponse> CreateAsync(TrainerRequest request);
    Task<TrainerResponse?> UpdateAsync(int id, TrainerRequest request);
    Task<string> DeleteAsync(int id);
}

public class TrainerService : ITrainerService
{
    private readonly AppDbContext _db;
    private readonly IPasswordHasher<User> _passwordHasher;

    public TrainerService(AppDbContext db, IPasswordHasher<User> passwordHasher)
    {
        _db = db;
        _passwordHasher = passwordHasher;
    }

    public async Task<List<TrainerResponse>> GetAllAsync()
    {
        var trainers = await _db.Trainers.Include(trainer => trainer.User).ToListAsync();
        return trainers.Select(ToResponse).ToList();
    }

    public async Task<TrainerResponse?> GetByIdAsync(int id)
    {
        var trainer = await _db.Trainers.Include(item => item.User).FirstOrDefaultAsync(item => item.Id == id);
        return trainer == null ? null : ToResponse(trainer);
    }

    public async Task<TrainerResponse?> GetByUserIdAsync(int userId)
    {
        var trainer = await _db.Trainers.Include(item => item.User).FirstOrDefaultAsync(item => item.UserId == userId);
        return trainer == null ? null : ToResponse(trainer);
    }

    public async Task<List<TrainerResponse>> GetByNameAsync(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return new List<TrainerResponse>();
        }

        var trainers = await _db.Trainers
            .Include(trainer => trainer.User)
            .Where(x => x.Name.Contains(name.Trim()))
            .ToListAsync();

        return trainers.Select(ToResponse).ToList();
    }

    public async Task<TrainerResponse> CreateAsync(TrainerRequest request)
    {
        var name = request.Name.Trim();
        var email = request.Email.Trim();
        var password = request.Password.Trim();
        var specialty = request.Specialty.Trim();

        if (string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException("Password is required.");
        }

        var emailTaken = await _db.Users.AnyAsync(user => user.Email == email);
        if (emailTaken)
        {
            throw new InvalidOperationException("This email is already used.");
        }

        var user = new User
        {
            Name = name,
            Email = email,
            Role = UserRole.Trainer,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        user.PasswordHash = _passwordHasher.HashPassword(user, password);

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var trainer = new Trainer
        {
            UserId = user.Id,
            Name = name,
            Specialty = specialty,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Trainers.Add(trainer);
        await _db.SaveChangesAsync();
        trainer.User = user;
        return ToResponse(trainer);
    }

    public async Task<TrainerResponse?> UpdateAsync(int id, TrainerRequest request)
    {
        var trainer = await _db.Trainers.Include(item => item.User).FirstOrDefaultAsync(item => item.Id == id);
        if (trainer == null)
        {
            return null;
        }

        var name = request.Name.Trim();
        var email = request.Email.Trim();
        var specialty = request.Specialty.Trim();

        var emailTaken = await _db.Users.AnyAsync(user => user.Email == email && user.Id != trainer.UserId);
        if (emailTaken)
        {
            throw new InvalidOperationException("This email is already used.");
        }

        trainer.Name = name;
        trainer.Specialty = specialty;
        trainer.UpdatedAt = DateTime.UtcNow;

        trainer.User.Name = name;
        trainer.User.Email = email;
        trainer.User.UpdatedAt = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(request.Password))
        {
            trainer.User.PasswordHash = _passwordHasher.HashPassword(trainer.User, request.Password.Trim());
        }

        await _db.SaveChangesAsync();
        return ToResponse(trainer);
    }

    public async Task<string> DeleteAsync(int id)
    {
        var trainer = await _db.Trainers.Include(item => item.User).FirstOrDefaultAsync(item => item.Id == id);
        if (trainer == null)
        {
            return "NotFound";
        }

        try
        {
            var user = trainer.User;
            _db.Trainers.Remove(trainer);
            _db.Users.Remove(user);
            await _db.SaveChangesAsync();
            return "Deleted";
        }
        catch (DbUpdateException)
        {
            return "InUse";
        }
    }

    private static TrainerResponse ToResponse(Trainer trainer)
    {
        return new TrainerResponse
        {
            Id = trainer.Id,
            UserId = trainer.UserId,
            Name = trainer.Name,
            Email = trainer.User.Email,
            Specialty = trainer.Specialty,
            CreatedAt = trainer.CreatedAt,
            UpdatedAt = trainer.UpdatedAt
        };
    }
}
