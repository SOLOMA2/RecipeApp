using RecipeManager.Data;
using RecipeManager.Interfaces.Repositories;
using RecipeManager.Interfaces.UnitOfWork;

namespace RecipeManager.Infrastucture.UOW
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _context;

        public ITagRepository Tag { get; }
        public IIngredientRepository Ingredient { get; }
        public IRecipeRepository Recipe { get; }

        public ICategoryRepository Category { get; }
        
        public UnitOfWork(
            AppDbContext context, 
            ITagRepository tagRepository, 
            IRecipeRepository recipeRepository, 
            IIngredientRepository ingredientRepository,
            ICategoryRepository categoryRepository)
        {
            Tag = tagRepository;
            _context = context;
            Ingredient = ingredientRepository;
            Recipe = recipeRepository;
            Category = categoryRepository;
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }
    }
}
