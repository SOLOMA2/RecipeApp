using CSharpFunctionalExtensions;

namespace RecipeManager.Models
{
    public class Recipe: Entity<long>
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public double Weight { get; set; }
        public double Calories { get; set; }
        public string CookingMethod { get; set; } = string.Empty;
        public int CookingTimeMinutes { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? AuthorId { get; set; }
        public AppUser? Author { get; set; }
        public ICollection<Ingredient> Ingredients { get; set; } = new List<Ingredient>();
        public ICollection<Tag> Tags { get; set; } = new List<Tag>();
        public ICollection<Category> Categories { get; set; } = new List<Category>();
        public double Protein { get; set; }
        public double Fat { get; set; }
        public double Carbohydrates { get; set; }

        /// <summary>
        /// Average recipe rating from user votes (0-5).
        /// </summary>
        public double Rating { get; set; }

        /// <summary>
        /// Total number of ratings submitted.
        /// </summary>
        public int RatingCount { get; set; }

        /// <summary>
        /// Total number of likes.
        /// </summary>
        public int LikesCount { get; set; }
    }
}
