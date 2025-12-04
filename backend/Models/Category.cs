using CSharpFunctionalExtensions;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RecipeManager.Models
{
    public class Category : Entity<long>
    {
        [Required(ErrorMessage = "Name cannot be empty")]
        [StringLength(100, MinimumLength = 1)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        public ICollection<Recipe> Recipes { get; set; } = new List<Recipe>();

        public bool IsDeleted { get; set; } = false;

        public byte[] RowVersion { get; set; } = null!;

        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public string NormalizedName { get; private set; } = string.Empty;
    }

    public class CategoryDto
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}