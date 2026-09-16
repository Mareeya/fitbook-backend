namespace FitBook_App.Configuration;

public class SeedOptions
{
    public const string SectionName = "Seed";

    public bool Enabled { get; set; }
    public SeedAdminOptions Admin { get; set; } = new();
    public string TrainerPassword { get; set; } = string.Empty;
    public List<SeedTrainerOptions> Trainers { get; set; } = [];
    public List<SeedClassOptions> Classes { get; set; } = [];
}

public class SeedAdminOptions
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class SeedTrainerOptions
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Specialty { get; set; } = string.Empty;
}

public class SeedClassOptions
{
    public string Name { get; set; } = string.Empty;
    public string TrainerEmail { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public int InitCapacity { get; set; }
}
