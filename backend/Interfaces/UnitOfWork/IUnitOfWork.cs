using RecipeManager.Interfaces.Repositories;

namespace RecipeManager.Interfaces.UnitOfWork
{
    public interface IUnitOfWork: IDisposable
    {
        IRecipeRepository Recipe { get; }
        ITagRepository Tag { get; }
        IIngredientRepository Ingredient { get; }
        ICategoryRepository Category { get; }
        Task<int> SaveChangesAsync();
    }
}
