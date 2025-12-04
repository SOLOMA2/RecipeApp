using System.ComponentModel.DataAnnotations;

public class TagDto
{
    public long Id { get; set; }

    [Required(ErrorMessage = "Title cannot be empty")]
    [StringLength(100, MinimumLength = 1)]
    public string Title { get; set; } = string.Empty;
}