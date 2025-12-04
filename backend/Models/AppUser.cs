using Microsoft.AspNetCore.Identity;

namespace RecipeManager.Models
{
    public class AppUser: IdentityUser
    {
        public DateTime LastLogin { get; set; } = DateTime.UtcNow;
        public ICollection<Recipe> Recipes { get; set; } = new List<Recipe>();
    }
}
