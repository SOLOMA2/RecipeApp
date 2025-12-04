using System.ComponentModel.DataAnnotations;

public class UpdateRecipeDto : CreateRecipeDto
{
    [Required]
    public long Id { get; set; }

}