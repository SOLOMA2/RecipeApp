namespace RecipeManager.DTOs.Nutrition
{
    public class NutritionSuggestionDto
    {
        public string VariantName { get; set; } = string.Empty;
        public string BaseProduct { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Query { get; set; } = string.Empty;
        public double Calories { get; set; }
        public double Protein { get; set; }
        public double Fat { get; set; }
        public double Carbohydrates { get; set; }
    }
}

