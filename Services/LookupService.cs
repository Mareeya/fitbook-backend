using FitBook_App.Data;
using FitBook_App.Models;
using Microsoft.EntityFrameworkCore;

namespace FitBook_App.Services;

public interface ILookupService
{
    Task<List<LookupResponse>> GetByTypeAsync(string type);
}

public class LookupService : ILookupService
{
    private readonly AppDbContext _db;

    public LookupService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<LookupResponse>> GetByTypeAsync(string type)
    {
        return await _db.Lookups
            .Where(lookup => lookup.Type == type)
            .OrderBy(lookup => lookup.Id)
            .Select(lookup => new LookupResponse
            {
                Id = lookup.Id,
                Value = lookup.Value
            })
            .ToListAsync();
    }
}
