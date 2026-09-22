using FitBook_App.Data;
using FitBook_App.Domain;
using FitBook_App.Domain.Enums;
using FitBook_App.Helpers;
using FitBook_App.Models;
using Microsoft.EntityFrameworkCore;

namespace FitBook_App.Services;

public interface IBookingService
{
    Task<BookingResponse> BookAsync(int sessionId, int userId, BookingSource source);
    Task<BookingResponse?> CancelAsync(int bookingId, int callerId, bool callerCanManage);
    Task<PagedResponse<BookingResponse>> GetForUserAsync(
        int userId, DateTime? from, DateTime? to, int page, int pageSize);
    Task<BookingResponse?> GetByIdAsync(int bookingId);
}

public class BookingService : IBookingService
{
    private readonly AppDbContext _db;

    public BookingService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<BookingResponse> BookAsync(int sessionId, int userId, BookingSource source)
    {
        var session = await _db.Sessions
            .Include(item => item.Status)
            .FirstOrDefaultAsync(item => item.Id == sessionId);
        if (session == null)
        {
            throw new AppException(StatusCodes.Status404NotFound, "Session was not found.");
        }

        if (session.Status.Value == LookupNames.Cancelled)
        {
            throw new AppException(StatusCodes.Status409Conflict, "This session has been cancelled.");
        }

        if (session.StartAt <= DateTime.UtcNow)
        {
            throw new AppException(StatusCodes.Status409Conflict, "This session has already started.");
        }

        var memberExists = await _db.Users.AnyAsync(user => user.Id == userId);
        if (!memberExists)
        {
            throw new AppException(StatusCodes.Status400BadRequest, "Member was not found.");
        }

        var alreadyBooked = await _db.Bookings
            .AnyAsync(booking => booking.SessionId == sessionId && booking.UserId == userId);
        if (alreadyBooked)
        {
            throw new AppException(StatusCodes.Status409Conflict, "This member already has a booking for this session.");
        }

        var confirmedId = await LookupIdAsync(LookupNames.BookingStatusType, LookupNames.Confirmed);

        await using var transaction = await _db.Database.BeginTransactionAsync();

        // Claim the seat and check capacity in a single statement. A read-then-write here
        // would let two members take the last seat at the same time.
        var seatClaimed = await _db.Sessions
            .Where(item => item.Id == sessionId && item.SeatsTaken < item.Capacity)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(item => item.SeatsTaken, item => item.SeatsTaken + 1)
                .SetProperty(item => item.UpdatedAt, DateTime.UtcNow));

        if (seatClaimed == 0)
        {
            await transaction.RollbackAsync();
            throw new AppException(StatusCodes.Status409Conflict, "This session is full.");
        }

        var now = DateTime.UtcNow;
        var booking = new Booking
        {
            SessionId = sessionId,
            UserId = userId,
            StatusId = confirmedId,
            AttendanceStatus = AttendanceStatus.Booked,
            Source = source,
            CreatedAt = now,
            UpdatedAt = now
        };

        _db.Bookings.Add(booking);
        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        return (await GetByIdAsync(booking.Id))!;
    }

    public async Task<BookingResponse?> CancelAsync(int bookingId, int callerId, bool callerCanManage)
    {
        var booking = await _db.Bookings
            .Include(item => item.Status)
            .FirstOrDefaultAsync(item => item.Id == bookingId);
        if (booking == null)
        {
            return null;
        }

        // The role gets the caller to the endpoint; this decides whose booking they may touch.
        if (!callerCanManage && booking.UserId != callerId)
        {
            throw new AppException(StatusCodes.Status403Forbidden, "You can only cancel your own bookings.");
        }

        if (booking.Status.Value == LookupNames.Cancelled)
        {
            throw new AppException(StatusCodes.Status409Conflict, "This booking is already cancelled.");
        }

        var cancelledId = await LookupIdAsync(LookupNames.BookingStatusType, LookupNames.Cancelled);

        await using var transaction = await _db.Database.BeginTransactionAsync();

        booking.StatusId = cancelledId;
        booking.CancelledAt = DateTime.UtcNow;
        booking.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        // Give the seat back. Guarded so a double cancel cannot drive the counter negative.
        await _db.Sessions
            .Where(item => item.Id == booking.SessionId && item.SeatsTaken > 0)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(item => item.SeatsTaken, item => item.SeatsTaken - 1)
                .SetProperty(item => item.UpdatedAt, DateTime.UtcNow));

        // A member who had already been checked in should stop counting as attended.
        if (booking.CheckedInAt != null)
        {
            await _db.Sessions
                .Where(item => item.Id == booking.SessionId && item.CheckedInCount > 0)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(item => item.CheckedInCount, item => item.CheckedInCount - 1));
        }

        await transaction.CommitAsync();
        return await GetByIdAsync(booking.Id);
    }

    public async Task<PagedResponse<BookingResponse>> GetForUserAsync(
        int userId, DateTime? from, DateTime? to, int page, int pageSize)
    {
        var query = BookingQuery().Where(booking => booking.UserId == userId);

        if (from.HasValue)
        {
            var fromUtc = SessionService.ToUtc(from.Value);
            query = query.Where(booking => booking.Session.StartAt >= fromUtc);
        }

        if (to.HasValue)
        {
            var toUtc = SessionService.ToUtc(to.Value);
            query = query.Where(booking => booking.Session.StartAt < toUtc);
        }

        var total = await query.CountAsync();
        var bookings = await query
            .OrderByDescending(booking => booking.Session.StartAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResponse<BookingResponse>
        {
            Items = bookings.Select(ToResponse).ToList(),
            Page = page,
            PageSize = pageSize,
            Total = total
        };
    }

    public async Task<BookingResponse?> GetByIdAsync(int bookingId)
    {
        var booking = await BookingQuery().FirstOrDefaultAsync(item => item.Id == bookingId);
        return booking == null ? null : ToResponse(booking);
    }

    private async Task<int> LookupIdAsync(string type, string value)
    {
        var lookup = await _db.Lookups
            .FirstOrDefaultAsync(item => item.Type == type && item.Value == value);
        if (lookup == null)
        {
            throw new AppException(StatusCodes.Status500InternalServerError, $"Lookup '{type}/{value}' is missing.");
        }

        return lookup.Id;
    }

    private IQueryable<Booking> BookingQuery()
    {
        return _db.Bookings
            .Include(booking => booking.User)
            .Include(booking => booking.Status)
            .Include(booking => booking.Session).ThenInclude(session => session.Class)
                .ThenInclude(gymClass => gymClass.Trainer);
    }

    internal static BookingResponse ToResponse(Booking booking)
    {
        return new BookingResponse
        {
            Id = booking.Id,
            SessionId = booking.SessionId,
            UserId = booking.UserId,
            MemberName = booking.User.Name,
            ClassId = booking.Session.ClassId,
            ClassName = booking.Session.Class.Name,
            TrainerName = booking.Session.Class.Trainer.Name,
            StartAt = DateTime.SpecifyKind(booking.Session.StartAt, DateTimeKind.Utc),
            BookingStatus = booking.Status.Value,
            AttendanceStatus = booking.AttendanceStatus.ToString(),
            Source = booking.Source.ToString(),
            CheckedInAt = booking.CheckedInAt,
            CancelledAt = booking.CancelledAt,
            CreatedAt = booking.CreatedAt
        };
    }
}
