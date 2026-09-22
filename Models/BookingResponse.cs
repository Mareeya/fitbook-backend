namespace FitBook_App.Models;

public class BookingResponse
{
    public int Id { get; set; }
    public int SessionId { get; set; }
    public int UserId { get; set; }
    public string MemberName { get; set; } = string.Empty;
    public int ClassId { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public string TrainerName { get; set; } = string.Empty;
    public DateTime StartAt { get; set; }
    public string BookingStatus { get; set; } = string.Empty;
    public string AttendanceStatus { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public DateTime? CheckedInAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
