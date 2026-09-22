using FitBook_App.Data;
using FitBook_App.Domain;
using FitBook_App.Domain.Enums;
using FitBook_App.Helpers;
using FitBook_App.Models;
using Microsoft.EntityFrameworkCore;

namespace FitBook_App.Services;

public interface IAttendanceService
{
    Task<SessionRosterResponse?> MarkAsync(int sessionId, AttendanceMarkRequest request, int markedByUserId);
    Task<BookingResponse> WalkInAsync(int sessionId, int userId, int markedByUserId);
    Task<BookingResponse?> UpdateAsync(int bookingId, AttendanceStatus status, int markedByUserId);
}

public class AttendanceService : IAttendanceService
{
    private readonly AppDbContext _db;
    private readonly IBookingService _bookingService;
    private readonly ISessionService _sessionService;

    public AttendanceService(AppDbContext db, IBookingService bookingService, ISessionService sessionService)
    {
        _db = db;
        _bookingService = bookingService;
        _sessionService = sessionService;
    }

    public async Task<SessionRosterResponse?> MarkAsync(
        int sessionId, AttendanceMarkRequest request, int markedByUserId)
    {
        var sessionExists = await _db.Sessions.AnyAsync(session => session.Id == sessionId);
        if (!sessionExists)
        {
            return null;
        }

        var bookingIds = request.Marks.Select(mark => mark.BookingId).Distinct().ToList();
        if (bookingIds.Count != request.Marks.Count)
        {
            throw new AppException(StatusCodes.Status400BadRequest, "The same booking was listed more than once.");
        }

        var bookings = await _db.Bookings
            .Include(booking => booking.Status)
            .Where(booking => bookingIds.Contains(booking.Id))
            .ToListAsync();

        if (bookings.Count != bookingIds.Count)
        {
            throw new AppException(StatusCodes.Status400BadRequest, "One or more bookings were not found.");
        }

        var foreignBooking = bookings.FirstOrDefault(booking => booking.SessionId != sessionId);
        if (foreignBooking != null)
        {
            throw new AppException(
                StatusCodes.Status400BadRequest,
                $"Booking {foreignBooking.Id} does not belong to this session.");
        }

        var now = DateTime.UtcNow;
        var checkedInDelta = 0;

        foreach (var mark in request.Marks)
        {
            var booking = bookings.First(item => item.Id == mark.BookingId);
            if (booking.Status.Value == LookupNames.Cancelled)
            {
                throw new AppException(
                    StatusCodes.Status409Conflict,
                    $"Booking {booking.Id} is cancelled and cannot be marked.");
            }

            var wasAttended = booking.AttendanceStatus == AttendanceStatus.Attended;
            var isAttended = mark.Status == AttendanceStatus.Attended;

            if (!wasAttended && isAttended)
            {
                checkedInDelta++;
                booking.CheckedInAt = now;
            }
            else if (wasAttended && !isAttended)
            {
                checkedInDelta--;
                booking.CheckedInAt = null;
            }

            booking.AttendanceStatus = mark.Status;
            booking.MarkedByUserId = markedByUserId;
            booking.UpdatedAt = now;
        }

        await using var transaction = await _db.Database.BeginTransactionAsync();
        await _db.SaveChangesAsync();
        await ApplyCheckedInDeltaAsync(sessionId, checkedInDelta);
        await transaction.CommitAsync();

        return await _sessionService.GetRosterAsync(sessionId);
    }

    public async Task<BookingResponse> WalkInAsync(int sessionId, int userId, int markedByUserId)
    {
        // A walk-in is an ordinary booking created at the door, so it lands in every stat
        // without the rest of the system needing to know about walk-ins at all.
        var existing = await _db.Bookings
            .Include(booking => booking.Status)
            .FirstOrDefaultAsync(booking => booking.SessionId == sessionId && booking.UserId == userId);

        if (existing != null && existing.Status.Value != LookupNames.Cancelled)
        {
            return (await UpdateAsync(existing.Id, AttendanceStatus.Attended, markedByUserId))!;
        }

        if (existing != null)
        {
            throw new AppException(
                StatusCodes.Status409Conflict,
                "This member cancelled their booking for this session. Re-book before checking them in.");
        }

        await _bookingService.BookAsync(sessionId, userId, BookingSource.WalkIn);

        var booking = await _db.Bookings
            .FirstAsync(item => item.SessionId == sessionId && item.UserId == userId);

        return (await UpdateAsync(booking.Id, AttendanceStatus.Attended, markedByUserId))!;
    }

    public async Task<BookingResponse?> UpdateAsync(int bookingId, AttendanceStatus status, int markedByUserId)
    {
        var booking = await _db.Bookings
            .Include(item => item.Status)
            .FirstOrDefaultAsync(item => item.Id == bookingId);
        if (booking == null)
        {
            return null;
        }

        if (booking.Status.Value == LookupNames.Cancelled)
        {
            throw new AppException(StatusCodes.Status409Conflict, "This booking is cancelled and cannot be marked.");
        }

        var wasAttended = booking.AttendanceStatus == AttendanceStatus.Attended;
        var isAttended = status == AttendanceStatus.Attended;
        var checkedInDelta = (wasAttended, isAttended) switch
        {
            (false, true) => 1,
            (true, false) => -1,
            _ => 0
        };

        var now = DateTime.UtcNow;
        booking.AttendanceStatus = status;
        booking.MarkedByUserId = markedByUserId;
        booking.CheckedInAt = isAttended ? booking.CheckedInAt ?? now : null;
        booking.UpdatedAt = now;

        await using var transaction = await _db.Database.BeginTransactionAsync();
        await _db.SaveChangesAsync();
        await ApplyCheckedInDeltaAsync(booking.SessionId, checkedInDelta);
        await transaction.CommitAsync();

        return await _bookingService.GetByIdAsync(booking.Id);
    }

    private async Task ApplyCheckedInDeltaAsync(int sessionId, int delta)
    {
        if (delta == 0)
        {
            return;
        }

        await _db.Sessions
            .Where(session => session.Id == sessionId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(session => session.CheckedInCount, session => session.CheckedInCount + delta)
                .SetProperty(session => session.UpdatedAt, DateTime.UtcNow));
    }
}
