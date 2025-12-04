using RecipeManager.Infrastucture.Nutrition;

namespace RecipeManager.Interfaces.Services
{
    public record NutritionInfo(double Calories, double Protein, double Fat, double Carbohydrates, double WeightGrams);

    public interface INutritionService
    {
        Task<NutritionInfo?> LookupAsync(string query, double weightGrams, CancellationToken cancellationToken = default);
    }
}

