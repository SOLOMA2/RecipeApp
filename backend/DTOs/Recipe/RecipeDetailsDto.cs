using RecipeManager.DTOs.Category;

public class RecipeDetailsDto
{
    public long Id { get; set; }

    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public double Weight { get; set; }
    public double Calories { get; set; }
    public double Protein { get; set; }
    public double Fat { get; set; }
    public double Carbohydrates { get; set; }
    public string? CookingMethod { get; set; }
    public int CookingTimeMinutes { get; set; }
    public DateTime CreatedAt { get; set; }

    public string? AuthorId { get; set; }

    public NutritionSummaryDto NutritionPerRecipe { get; set; } = new();
    public NutritionSummaryDto NutritionPer100g { get; set; } = new();

    public List<IngredientDto> Ingredients { get; set; } = new();
    public List<TagDto> Tags { get; set; } = new();

    public List<CategoryListDto> Categories { get; set; } = new();

    public double Rating { get; set; }
    public int RatingCount { get; set; }
    public int LikesCount { get; set; }
}