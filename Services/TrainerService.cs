using FitBook_App.Domain;
using FitBook_App.Models;
using FitBook_App.Repositories;
using Microsoft.EntityFrameworkCore;

namespace FitBook_App.Services;

public interface ITrainerService
{
    Task<List<TrainerResponse>> GetAllAsync();
    Task<TrainerResponse?> GetByIdAsync(int id);
    Task<Trainer?> GetByNameAsync(string name);
    Task<TrainerResponse> CreateAsync(TrainerRequest request);
    Task<TrainerResponse?> UpdateAsync(int id, TrainerRequest request);
    Task<string> DeleteAsync(int id);
}

public class TrainerService : ITrainerService
{
    private readonly ITrainerRepository _repo;

    public TrainerService(ITrainerRepository repo)
    {
        _repo = repo;
    }

    public async Task<List<TrainerResponse>> GetAllAsync()
    {
        var trainers = await _repo.GetAllAsync();
        return trainers.Select(ToResponse).ToList();
    }

    public async Task<TrainerResponse?> GetByIdAsync(int id)
    {
        var trainer = await _repo.GetByIdAsync(id);
        return trainer == null ? null : ToResponse(trainer);
    }

    public Task<Trainer?> GetByNameAsync(string name)
    {
        return _repo.GetByNameAsync(name);
    }

    public async Task<TrainerResponse> CreateAsync(TrainerRequest request)
    {
        var trainer = new Trainer
        {
            Name = request.Name.Trim(),
            Specialty = request.Specialty.Trim(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _repo.Add(trainer);
        await _repo.SaveChangesAsync();
        return ToResponse(trainer);
    }

    public async Task<TrainerResponse?> UpdateAsync(int id, TrainerRequest request)
    {
        var trainer = await _repo.GetByIdAsync(id);
        if (trainer == null)
        {
            return null;
        }

        trainer.Name = request.Name.Trim();
        trainer.Specialty = request.Specialty.Trim();
        trainer.UpdatedAt = DateTime.UtcNow;

        await _repo.SaveChangesAsync();
        return ToResponse(trainer);
    }

    public async Task<string> DeleteAsync(int id)
    {
        var trainer = await _repo.GetByIdAsync(id);
        if (trainer == null)
        {
            return "NotFound";
        }

        try
        {
            _repo.Remove(trainer);
            await _repo.SaveChangesAsync();
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
            Name = trainer.Name,
            Specialty = trainer.Specialty,
            CreatedAt = trainer.CreatedAt,
            UpdatedAt = trainer.UpdatedAt
        };
    }
}
