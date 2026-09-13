using System.ComponentModel.DataAnnotations;

namespace FitBook_App.Models;

public class TrainerRequest
{
    [Required]
    [MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(80)]
    public string Specialty { get; set; } = string.Empty;
}
