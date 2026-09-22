namespace FitBook_App.Domain;

public class Session
{
    public int Id { get; set; }
    public int ClassId { get; set; }

    // Stored in UTC. Convert at the edges, never in the database.
    public DateTime StartAt { get; set; }
    public int Capacity { get; set; }
    public int SeatsTaken { get; set; }
    public int CheckedInCount { get; set; }
    public int StatusId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public GymClass Class { get; set; } = null!;
    public Lookup Status { get; set; } = null!;
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}
