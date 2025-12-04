using System.ComponentModel.DataAnnotations;

public class UpdateIngredientDto : CreateIngredientDto
{
    [Required]
    public long Id { get; set; }
}