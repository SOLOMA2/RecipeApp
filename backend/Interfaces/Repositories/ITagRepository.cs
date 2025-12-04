using RecipeManager.Infrastucture.Pagiantion;
using RecipeManager.Models;

namespace RecipeManager.Interfaces.Repositories
{
    public interface ITagRepository
    {
        //Task<Tag?> GetByIdIngredientAsync(long id, CancellationToken cancellationToken = default);
        Task<PagedResult<TagDto>> GetPagedAllAsync(int page = 1, int pageSize = 100, CancellationToken ct = default);
        Task AddAsync(Tag tag, CancellationToken cancellationToken = default);
        Task<bool> DeleteAsync(long id, CancellationToken cancellationToken = default);
        Task AddRangeAsync(IEnumerable<Tag> tags, CancellationToken cancellationToken = default);
        Task<bool> ExistsAsync(long id, CancellationToken ct = default);
        Task<Tag?> GetByIdAsync(long id, CancellationToken ct = default);
        Task<Tag?> GetByTitleAsync(string title, CancellationToken ct = default);
        Task<PagedResult<TagDto>> GetPagedAsync(int page = 1, int pageSize = 20, string? search = null, CancellationToken ct = default);

    }
}
