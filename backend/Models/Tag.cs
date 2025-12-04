using CSharpFunctionalExtensions;
using System.ComponentModel.DataAnnotations; 

namespace RecipeManager.Models
{
    public class Tag : Entity<long>
    {
        [Required(ErrorMessage = "Title cannot be empty")]
        [StringLength(100, MinimumLength = 1, ErrorMessage = "Title must be between 1 and 100 characters")]
        public string Title { get; set; } = string.Empty;

        public ICollection<Recipe> Recipes { get; set; } = new List<Recipe>();

        public string NormalizedTitle { get; private set; } = string.Empty;

        public bool IsDeleted { get; set; } = false; 

        public byte[] RowVersion { get; set; } = null!;  
    }

    public class TagDto
    {
        public long Id { get; set; }
        public string Title { get; set; } = string.Empty;
    }
}