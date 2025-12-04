using System.ComponentModel.DataAnnotations;

public class IngredientDto
{
    public long Id { get; set; }

    [Required]
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Range(0, double.MaxValue)]
    public double Calories { get; set; }

    [Range(0, double.MaxValue)]
    public double Protein { get; set; }

    [Range(0, double.MaxValue)]
    public double Fat { get; set; }

    [Range(0, double.MaxValue)]
    public double Carbohydrates { get; set; }

    [Range(0, double.MaxValue)]
    public double Weight { get; set; }

    [Range(0, 9999999.99)]
    public decimal Quantity { get; set; }

    public string? Unit { get; set; }

    public long RecipeId { get; set; }
}
