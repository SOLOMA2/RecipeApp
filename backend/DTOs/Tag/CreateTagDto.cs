using System.ComponentModel.DataAnnotations;

public class CreateTagDto
{
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Title { get; set; } = string.Empty;
}