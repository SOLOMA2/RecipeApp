using System.ComponentModel.DataAnnotations;

public class CreateCategoryDto
{
    [Required(ErrorMessage = "Name cannot be empty")]
    [StringLength(100, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }
}