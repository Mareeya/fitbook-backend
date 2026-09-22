using FitBook_App.Domain.Enums;

namespace FitBook_App.Domain;

public class Booking
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int SessionId { get; set; }
    public int StatusId { get; set; }
    public AttendanceStatus AttendanceStatus { get; set; } = AttendanceStatus.Booked;
    public BookingSource Source { get; set; } = BookingSource.Online;
    public DateTime? CheckedInAt { get; set; }
    public int? MarkedByUserId { get; set; }
    public DateTime? CancelledAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public User User { get; set; } = null!;
    public Session Session { get; set; } = null!;
    public Lookup Status { get; set; } = null!;
    public User? MarkedByUser { get; set; }
}
