namespace FitBook_App.Domain;

public class GymClass
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public int InitCapacity { get; set; }
    public int TrainerId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Lookup Category { get; set; } = null!;
    public Trainer Trainer { get; set; } = null!;
    public ICollection<Session> Sessions { get; set; } = new List<Session>();
}
