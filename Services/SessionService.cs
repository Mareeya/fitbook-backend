using FitBook_App.Data;
using FitBook_App.Domain;
using FitBook_App.Domain.Enums;
using FitBook_App.Helpers;
using FitBook_App.Models;
using Microsoft.EntityFrameworkCore;

namespace FitBook_App.Services;

public interface ISessionService
{
    Task<PagedResponse<SessionResponse>> GetAllAsync(
        DateTime from, DateTime to, int? classId, int? trainerId, int page, int pageSize);
    Task<SessionResponse?> GetByIdAsync(int id);
    Task<SessionRosterResponse?> GetRosterAsync(int id);
    Task<SessionResponse> CreateAsync(SessionRequest request);
    Task<List<SessionResponse>> GenerateAsync(SessionGenerateRequest request);
    Task<SessionResponse?> UpdateAsync(int id, SessionRequest request);
    Task<SessionResponse?> CancelAsync(int id);
    Task<bool> IsTrainerForSessionAsync(int sessionId, int trainerUserId);
}

public class SessionService : ISessionService
{
    private readonly AppDbContext _db;

    public SessionService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResponse<SessionResponse>> GetAllAsync(
        DateTime from, DateTime to, int? classId, int? trainerId, int page, int pageSize)
    {
        if (to < from)
        {
            throw new AppException(StatusCodes.Status400BadRequest, "The end date must be after the start date.");
        }

        var fromUtc = ToUtc(from);
        var toUtc = ToUtc(to);

        var query = SessionQuery()
            .Where(session => session.StartAt >= fromUtc && session.StartAt < toUtc);

        if (classId.HasValue)
        {
            query = query.Where(session => session.ClassId == classId.Value);
        }

        if (trainerId.HasValue)
        {
            query = query.Where(session => session.Class.TrainerId == trainerId.Value);
        }

        var total = await query.CountAsync();
        var sessions = await query
            .OrderBy(session => session.StartAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResponse<SessionResponse>
        {
            Items = sessions.Select(ToResponse).ToList(),
            Page = page,
            PageSize = pageSize,
            Total = total
        };
    }

    public async Task<SessionResponse?> GetByIdAsync(int id)
    {
        var session = await SessionQuery().FirstOrDefaultAsync(item => item.Id == id);
        return session == null ? null : ToResponse(session);
    }

    public async Task<SessionRosterResponse?> GetRosterAsync(int id)
    {
        var session = await SessionQuery().FirstOrDefaultAsync(item => item.Id == id);
        if (session == null)
        {
            return null;
        }

        // Materialise before mapping: enum.ToString() has no SQL translation.
        var bookings = await _db.Bookings
            .Where(booking => booking.SessionId == id)
            .Include(booking => booking.User)
            .Include(booking => booking.Status)
            .OrderBy(booking => booking.User.Name)
            .ToListAsync();

        var entries = bookings.Select(booking => new RosterEntryResponse
        {
            BookingId = booking.Id,
            UserId = booking.UserId,
            MemberName = booking.User.Name,
            Email = booking.User.Email,
            BookingStatus = booking.Status.Value,
            AttendanceStatus = booking.AttendanceStatus.ToString(),
            Source = booking.Source.ToString(),
            CheckedInAt = booking.CheckedInAt,
            BookedAt = booking.CreatedAt
        }).ToList();

        return new SessionRosterResponse
        {
            Session = ToResponse(session),
            Entries = entries
        };
    }

    public async Task<SessionResponse> CreateAsync(SessionRequest request)
    {
        var gymClass = await FindClassAsync(request.ClassId);
        var startAt = ToUtc(request.StartAt);
        var scheduledId = await ScheduledStatusIdAsync();

        var session = NewSession(gymClass, startAt, request.Capacity, scheduledId);
        _db.Sessions.Add(session);
        await _db.SaveChangesAsync();

        return (await GetByIdAsync(session.Id))!;
    }

    public async Task<List<SessionResponse>> GenerateAsync(SessionGenerateRequest request)
    {
        var gymClass = await FindClassAsync(request.ClassId);
        var startAt = ToUtc(request.StartAt);
        var untilAt = ToUtc(request.UntilAt);

        if (untilAt < startAt)
        {
            throw new AppException(StatusCodes.Status400BadRequest, "The end date must be after the start date.");
        }

        var scheduledId = await ScheduledStatusIdAsync();

        // Pull the existing occurrences once so repeated generation stays idempotent.
        var existing = await _db.Sessions
            .Where(session => session.ClassId == gymClass.Id
                && session.StartAt >= startAt
                && session.StartAt <= untilAt)
            .Select(session => session.StartAt)
            .ToListAsync();

        var created = new List<Session>();
        for (var occurrence = startAt; occurrence <= untilAt; occurrence = occurrence.AddDays(request.RepeatEveryDays))
        {
            if (existing.Contains(occurrence))
            {
                continue;
            }

            created.Add(NewSession(gymClass, occurrence, request.Capacity, scheduledId));
        }

        if (created.Count == 0)
        {
            return [];
        }

        _db.Sessions.AddRange(created);
        await _db.SaveChangesAsync();

        var ids = created.Select(session => session.Id).ToList();
        var sessions = await SessionQuery()
            .Where(session => ids.Contains(session.Id))
            .OrderBy(session => session.StartAt)
            .ToListAsync();

        return sessions.Select(ToResponse).ToList();
    }

    public async Task<SessionResponse?> UpdateAsync(int id, SessionRequest request)
    {
        var session = await _db.Sessions.FindAsync(id);
        if (session == null)
        {
            return null;
        }

        var gymClass = await FindClassAsync(request.ClassId);
        var capacity = request.Capacity ?? gymClass.InitCapacity;

        // Shrinking below the seats already taken would silently overbook the session.
        if (capacity < session.SeatsTaken)
        {
            throw new AppException(
                StatusCodes.Status409Conflict,
                $"This session already has {session.SeatsTaken} bookings, so capacity cannot be set below that.");
        }

        session.ClassId = gymClass.Id;
        session.StartAt = ToUtc(request.StartAt);
        session.Capacity = capacity;
        session.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return await GetByIdAsync(session.Id);
    }

    public async Task<SessionResponse?> CancelAsync(int id)
    {
        var session = await _db.Sessions.FindAsync(id);
        if (session == null)
        {
            return null;
        }

        var cancelledId = await StatusIdAsync(LookupNames.SessionStatusType, LookupNames.Cancelled);
        if (session.StatusId == cancelledId)
        {
            throw new AppException(StatusCodes.Status409Conflict, "This session is already cancelled.");
        }

        // A cancelled session keeps its bookings - they are the history the dashboard reads.
        session.StatusId = cancelledId;
        session.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return await GetByIdAsync(session.Id);
    }

    public Task<bool> IsTrainerForSessionAsync(int sessionId, int trainerUserId)
    {
        return _db.Sessions
            .AnyAsync(session => session.Id == sessionId && session.Class.Trainer.UserId == trainerUserId);
    }

    private Session NewSession(GymClass gymClass, DateTime startAt, int? capacity, int statusId)
    {
        var now = DateTime.UtcNow;
        return new Session
        {
            ClassId = gymClass.Id,
            StartAt = startAt,
            Capacity = capacity ?? gymClass.InitCapacity,
            SeatsTaken = 0,
            CheckedInCount = 0,
            StatusId = statusId,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    private async Task<GymClass> FindClassAsync(int classId)
    {
        var gymClass = await _db.Classes.FindAsync(classId);
        if (gymClass == null)
        {
            throw new AppException(StatusCodes.Status400BadRequest, "Class was not found.");
        }

        return gymClass;
    }

    private Task<int> ScheduledStatusIdAsync()
    {
        return StatusIdAsync(LookupNames.SessionStatusType, LookupNames.Scheduled);
    }

    private async Task<int> StatusIdAsync(string type, string value)
    {
        var status = await _db.Lookups
            .FirstOrDefaultAsync(lookup => lookup.Type == type && lookup.Value == value);
        if (status == null)
        {
            throw new AppException(StatusCodes.Status500InternalServerError, $"Lookup '{type}/{value}' is missing.");
        }

        return status.Id;
    }

    private IQueryable<Session> SessionQuery()
    {
        return _db.Sessions
            .Include(session => session.Class).ThenInclude(gymClass => gymClass.Category)
            .Include(session => session.Class).ThenInclude(gymClass => gymClass.Trainer)
            .Include(session => session.Status);
    }

    // Incoming values are treated as UTC whether or not the client marked them.
    internal static DateTime ToUtc(DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
    }

    internal static SessionResponse ToResponse(Session session)
    {
        return new SessionResponse
        {
            Id = session.Id,
            ClassId = session.ClassId,
            ClassName = session.Class.Name,
            CategoryName = session.Class.Category.Value,
            TrainerId = session.Class.TrainerId,
            TrainerName = session.Class.Trainer.Name,
            StartAt = DateTime.SpecifyKind(session.StartAt, DateTimeKind.Utc),
            Capacity = session.Capacity,
            SeatsTaken = session.SeatsTaken,
            SeatsLeft = Math.Max(0, session.Capacity - session.SeatsTaken),
            CheckedInCount = session.CheckedInCount,
            StatusId = session.StatusId,
            StatusName = session.Status.Value,
            CreatedAt = session.CreatedAt,
            UpdatedAt = session.UpdatedAt
        };
    }
}
