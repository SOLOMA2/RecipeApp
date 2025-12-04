using System.ComponentModel.DataAnnotations;

namespace RecipeManager.DTOs.Nutrition
{
    public class NutritionLookupRequest
    {
        [Required]
        [StringLength(200)]
        public string Query { get; set; } = string.Empty;

        [Range(0.1, double.MaxValue)]
        public double WeightGrams { get; set; }
    }
}

