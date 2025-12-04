using RecipeManager.Infrastucture.Pagiantion;
using RecipeManager.Models;

namespace RecipeManager.Interfaces.Repositories
{
    public interface IIngredientRepository
    {
        Task<Ingredient?> GetIngredientByIdAsync(long id, CancellationToken cancellationToken = default );
        Task<PagedResult<Ingredient>?> GetPagedAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);
        Task<bool> DeleteIngredientAsync(long id, CancellationToken cancellationToken = default);
        Task UpdateIngredientAsync(Ingredient ingredient, CancellationToken cancellationToken = default);
        Task AddIngredientAsync(Ingredient ingredient, CancellationToken cancellationToken = default);
        Task<bool> ExistsAsync(long id, CancellationToken cancellationToken = default);


    }
}
