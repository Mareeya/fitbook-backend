using FitBook_App.Data;
using FitBook_App.Domain;
using Microsoft.EntityFrameworkCore;

namespace FitBook_App.Repositories;

public interface ITrainerRepository
{
    Task<List<Trainer>> GetAllAsync();
    Task<Trainer?> GetByIdAsync(int id);
    Task<Trainer?> GetByNameAsync(string name);
    void Add(Trainer trainer);
    void Remove(Trainer trainer);
    Task SaveChangesAsync();
}

public class TrainerRepository : ITrainerRepository
{
    private readonly AppDbContext _db;

    public TrainerRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<List<Trainer>> GetAllAsync()
    {
        return _db.Trainers.ToListAsync();
    }

    public Task<Trainer?> GetByIdAsync(int id)
    {
        return _db.Trainers.FindAsync(id).AsTask();
    }

    public Task<Trainer?> GetByNameAsync(string name)
    {
        return _db.Trainers.Where(x => x.Name == name).FirstOrDefaultAsync();
    }

    public void Add(Trainer trainer)
    {
        _db.Trainers.Add(trainer);
    }

    public void Remove(Trainer trainer)
    {
        _db.Trainers.Remove(trainer);
    }

    public Task SaveChangesAsync()
    {
        return _db.SaveChangesAsync();
    }
}
