using RecipeManager.Infrastucture.Pagiantion;
using RecipeManager.Models;

namespace RecipeManager.Interfaces.Repositories
{
    public interface ICategoryRepository
    {
        Task<PagedResult<CategoryDto>> GetPagedAsync(int page = 1, int pageSize = 20, string? search = null, CancellationToken ct = default);
        Task<Category?> GetByIdAsync(long id, bool asNoTracking = true, CancellationToken cancellationToken = default);
        Task<Category?> GetByNameAsync(string name, CancellationToken ct = default);
        Task AddAsync(Category category, CancellationToken cancellationToken = default);
        Task<bool> DeleteAsync(long id, CancellationToken cancellationToken = default);
        Task UpdateCategoryAsync(Category incoming, CancellationToken ct = default);
        Task<bool> ExistsAsync(long id, CancellationToken cancellationToken = default);
        Task AddRangeAsync(IEnumerable<Category> categories, CancellationToken ct = default);
        Task<PagedResult<CategoryDto>> GetPagedAllAsync(int page = 1, int pageSize = 100, CancellationToken ct = default);

    }
}
