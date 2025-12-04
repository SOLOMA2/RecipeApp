using System.ComponentModel.DataAnnotations;

public class UpdateTagDto : CreateTagDto
{
    [Required]
    public long Id { get; set; }

    public string? RowVersion { get; set; }
}