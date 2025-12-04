using RecipeManager.Infrastucture.Pagiantion;
using RecipeManager.Models;
using System.Linq;

namespace RecipeManager.Interfaces.Repositories
{
    public interface IRecipeRepository
    {
        Task<Recipe?> GetByIdAsync(long id, bool asNoTracking = true, CancellationToken cancellationToken = default);

        Task<Recipe?> GetWithDetailsAsync(long id, CancellationToken cancellationToken = default);

        Task<PagedResult<Recipe>> GetPagedAsync(int page = 1, int pageSize = 20, string? search = null, long? categoryId = null, CancellationToken cancellationToken = default);

        Task AddAsync(Recipe recipe, CancellationToken cancellationToken = default);

        Task UpdateRecipeAsync(Recipe recipe, CancellationToken cancellationToken = default);

        Task DeleteAsync(long id, CancellationToken cancellationToken = default);

        Task<bool> ExistsAsync(long id, CancellationToken cancellationToken = default);

        Task<int> CountByCategoryAsync(long categoryId, CancellationToken cancellationToken = default);
    }
}
