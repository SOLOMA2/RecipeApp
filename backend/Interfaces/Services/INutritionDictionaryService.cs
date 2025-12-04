using System.Collections.Generic;

namespace RecipeManager.Interfaces.Services
{
    public record NutritionDictionaryMatch(string VariantName, double Calories, double Protein, double Fat, double Carbohydrates);
    public record NutritionDictionarySuggestion(string VariantName, string BaseProduct, string DisplayName, string Query, double Calories, double Protein, double Fat, double Carbohydrates);

    public interface INutritionDictionaryService
    {
        NutritionDictionaryMatch? FindBestMatch(string query);
        IReadOnlyList<NutritionDictionarySuggestion> Suggest(string query, int limit = 5);
    }
}

