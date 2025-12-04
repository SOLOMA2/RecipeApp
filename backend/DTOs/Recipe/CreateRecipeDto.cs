using System.ComponentModel.DataAnnotations;

public class CreateRecipeDto
{
    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? Description { get; set; }

    public string? ImageUrl { get; set; }

    [Range(0, double.MaxValue)]
    public double Weight { get; set; }

    [Range(0, double.MaxValue)]
    public double Calories { get; set; }

    [Range(0, double.MaxValue)]
    public double Protein { get; set; }

    [Range(0, double.MaxValue)]
    public double Fat { get; set; }

    [Range(0, double.MaxValue)]
    public double Carbohydrates { get; set; }

    public string? CookingMethod { get; set; }

    [Range(0, 60 * 24)]
    public int CookingTimeMinutes { get; set; }

    public string? AuthorId { get; set; }

    public List<CreateIngredientDto>? Ingredients { get; set; }

    public List<long>? CategoryIds { get; set; }
    public List<TagRefDto>? Tags { get; set; }
}