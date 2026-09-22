namespace FitBook_App.Models;

public class SessionResponse
{
    public int Id { get; set; }
    public int ClassId { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public int TrainerId { get; set; }
    public string TrainerName { get; set; } = string.Empty;
    public DateTime StartAt { get; set; }
    public int Capacity { get; set; }
    public int SeatsTaken { get; set; }
    public int SeatsLeft { get; set; }
    public int CheckedInCount { get; set; }
    public int StatusId { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class RosterEntryResponse
{
    public int BookingId { get; set; }
    public int UserId { get; set; }
    public string MemberName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string BookingStatus { get; set; } = string.Empty;
    public string AttendanceStatus { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public DateTime? CheckedInAt { get; set; }
    public DateTime BookedAt { get; set; }
}

public class SessionRosterResponse
{
    public SessionResponse Session { get; set; } = new();
    public List<RosterEntryResponse> Entries { get; set; } = [];
}
