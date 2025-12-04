using System.ComponentModel.DataAnnotations;

public class UpdateCategoryDto : CreateCategoryDto
{
    [Required]
    public long Id { get; set; }

    public string? RowVersion { get; set; }
}