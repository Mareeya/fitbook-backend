namespace FitBook_App.Domain;

public class Booking
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int SessionId { get; set; }
    public int StatusId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public User User { get; set; } = null!;
    public Session Session { get; set; } = null!;
    public Lookup Status { get; set; } = null!;
}
