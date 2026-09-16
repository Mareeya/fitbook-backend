using FitBook_App.Data;
using FitBook_App.Domain;
using FitBook_App.Helpers;
using FitBook_App.Models;
using Microsoft.EntityFrameworkCore;

namespace FitBook_App.Services;

public interface IClassService
{
    Task<List<GymClassResponse>> GetAllAsync(int? trainerUserId);
    Task<GymClassResponse> CreateAsync(GymClassRequest request);
    Task<GymClassResponse?> UpdateAsync(int id, GymClassRequest request);
    Task<string> DeleteAsync(int id);
}

public class ClassService : IClassService
{
    private readonly AppDbContext _db;

    public ClassService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<GymClassResponse>> GetAllAsync(int? trainerUserId)
    {
        var query = ClassQuery();
        if (trainerUserId.HasValue)
        {
            query = query.Where(gymClass => gymClass.Trainer.UserId == trainerUserId.Value);
        }

        var classes = await query.ToListAsync();
        return classes.Select(ToResponse).ToList();
    }

    public async Task<GymClassResponse> CreateAsync(GymClassRequest request)
    {
        var name = request.Name.Trim();
        var nameTaken = await _db.Classes.AnyAsync(item => item.Name == name);
        if (nameTaken)
        {
            throw new AppException(StatusCodes.Status409Conflict, "A class with this name already exists.");
        }

        await EnsureClassLinksExist(request.CategoryId, request.TrainerId);

        var gymClass = new GymClass
        {
            Name = name,
            CategoryId = request.CategoryId,
            TrainerId = request.TrainerId,
            InitCapacity = request.InitCapacity,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Classes.Add(gymClass);
        await _db.SaveChangesAsync();
        return await GetByIdAsync(gymClass.Id);
    }

    public async Task<GymClassResponse?> UpdateAsync(int id, GymClassRequest request)
    {
        var gymClass = await _db.Classes.FindAsync(id);
        if (gymClass == null)
        {
            return null;
        }

        var name = request.Name.Trim();
        var nameTaken = await _db.Classes.AnyAsync(item => item.Name == name && item.Id != id);
        if (nameTaken)
        {
            throw new AppException(StatusCodes.Status409Conflict, "A class with this name already exists.");
        }

        await EnsureClassLinksExist(request.CategoryId, request.TrainerId);

        gymClass.Name = name;
        gymClass.CategoryId = request.CategoryId;
        gymClass.TrainerId = request.TrainerId;
        gymClass.InitCapacity = request.InitCapacity;
        gymClass.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return await GetByIdAsync(gymClass.Id);
    }

    public async Task<string> DeleteAsync(int id)
    {
        var gymClass = await _db.Classes.FindAsync(id);
        if (gymClass == null)
        {
            return "NotFound";
        }

        var hasSessions = await _db.Sessions.AnyAsync(session => session.ClassId == id);
        if (hasSessions)
        {
            throw new AppException(StatusCodes.Status409Conflict, "This class has sessions and cannot be deleted.");
        }

        _db.Classes.Remove(gymClass);
        await _db.SaveChangesAsync();
        return "Deleted";
    }

    private async Task EnsureClassLinksExist(int categoryId, int trainerId)
    {
        var categoryExists = await _db.Lookups
            .AnyAsync(lookup => lookup.Id == categoryId && lookup.Type == LookupNames.ClassType);
        if (!categoryExists)
        {
            throw new AppException(StatusCodes.Status400BadRequest, "Category was not found.");
        }

        var trainerExists = await _db.Trainers.AnyAsync(trainer => trainer.Id == trainerId);
        if (!trainerExists)
        {
            throw new AppException(StatusCodes.Status400BadRequest, "Trainer was not found.");
        }
    }

    private async Task<GymClassResponse> GetByIdAsync(int id)
    {
        var gymClass = await ClassQuery().FirstAsync(item => item.Id == id);
        return ToResponse(gymClass);
    }

    private IQueryable<GymClass> ClassQuery()
    {
        return _db.Classes
            .Include(gymClass => gymClass.Category)
            .Include(gymClass => gymClass.Trainer)
            .Include(gymClass => gymClass.Sessions)
                .ThenInclude(session => session.Bookings)
                    .ThenInclude(booking => booking.Status);
    }

    private static GymClassResponse ToResponse(GymClass gymClass)
    {
        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var monthEnd = monthStart.AddMonths(1);
        var monthSessions = gymClass.Sessions
            .Where(session => session.StartAt >= monthStart && session.StartAt < monthEnd)
            .ToList();

        var averageFillRate = 0;
        if (monthSessions.Count > 0)
        {
            averageFillRate = (int)Math.Round(monthSessions.Average(session =>
            {
                var bookedCount = session.Bookings.Count(booking =>
                    booking.Status.Value != LookupNames.Cancelled);
                return session.Capacity <= 0 ? 0 : (double)bookedCount / session.Capacity * 100;
            }));
        }

        return new GymClassResponse
        {
            Id = gymClass.Id,
            Name = gymClass.Name,
            CategoryId = gymClass.CategoryId,
            CategoryName = gymClass.Category.Value,
            TrainerId = gymClass.TrainerId,
            TrainerName = gymClass.Trainer.Name,
            InitCapacity = gymClass.InitCapacity,
            SessionsThisMonth = monthSessions.Count,
            AverageFillRate = averageFillRate,
            CreatedAt = gymClass.CreatedAt,
            UpdatedAt = gymClass.UpdatedAt
        };
    }
}
