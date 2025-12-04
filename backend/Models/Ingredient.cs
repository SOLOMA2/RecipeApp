using CSharpFunctionalExtensions;

namespace RecipeManager.Models
{
    public class Ingredient: Entity<long>
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public double Calories { get; set; }
        public double Weight { get; set; }
        public decimal Quantity { get; set; }
        public long RecipeId { get; set; }
        public string? Unit { get; set; }
        public Recipe? Recipe { get; set; }
        public double Protein { get; set; }
        public double Fat { get; set; }
        public double Carbohydrates { get; set; }
    }
}