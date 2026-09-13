using System.ComponentModel.DataAnnotations;

namespace FitBook_App.Models;

public class GymClassRequest
{
    [Required]
    [MaxLength(80)]
    public string Name { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int CategoryId { get; set; }

    [Range(1, int.MaxValue)]
    public int TrainerId { get; set; }

    [Range(1, 200)]
    public int InitCapacity { get; set; }
}
