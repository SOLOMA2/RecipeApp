using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using RecipeManager.Data;
using RecipeManager.Infrastucture.Pagiantion;
using RecipeManager.Interfaces.Repositories;
using RecipeManager.Models;
using System.ComponentModel.DataAnnotations;

namespace RecipeManager.Repositories
{
    public class CategoryRepository : ICategoryRepository
    {
        private readonly AppDbContext _context;
        private readonly ILogger<CategoryRepository> _logger;
        private readonly IMemoryCache _cache;

        public CategoryRepository(AppDbContext context, ILogger<CategoryRepository> logger, IMemoryCache cache)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        }

        public async Task AddAsync(Category category, CancellationToken ct = default)
        {
            if (category == null) throw new ArgumentNullException(nameof(category));

            Validator.ValidateObject(category, new ValidationContext(category), true);

            try
            {
                var name = category.Name.Trim();
                var normalized = name.ToUpperInvariant();

                var existing = await _context.Categories
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.NormalizedName == normalized, ct);

                if (existing != null)
                {
                    category.Name = existing.Name;
                    category.Description = existing.Description;
                    _logger.LogInformation("Category '{Name}' exists (Id={Id}). Skipping.", existing.Name, existing.Id);
                    return;
                }

                category.Name = name;
                await _context.Categories.AddAsync(category, ct);
                _logger.LogInformation("Prepared category '{Name}' for insert.", name);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "DB error preparing category (Name={Name}).", category.Name);
                throw new InvalidOperationException("Error preparing category.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error preparing category.");
                throw;
            }
        }

        public async Task AddRangeAsync(IEnumerable<Category> categories, CancellationToken ct = default)
        {
            foreach (var cat in categories)
            {
                await AddAsync(cat, ct);
            }
        }

        public async Task<bool> DeleteAsync(long id, CancellationToken ct = default)
        {
            if (id <= 0) throw new ArgumentOutOfRangeException(nameof(id), "Id positive.");
            try
            {
                var category = await _context.Categories.FirstOrDefaultAsync(c => c.Id == id, ct);
                if (category == null) return false;

                var isUsed = await _context.Categories
                    .Where(c => c.Id == id)
                    .SelectMany(c => c.Recipes)
                    .AnyAsync(ct);

                if (isUsed)
                {
                    _logger.LogWarning("Delete attempt for used category {Id}.", id);
                    throw new InvalidOperationException("Cannot delete used category.");
                }

                category.IsDeleted = true;
                _logger.LogInformation("Category {Id} marked deleted.", id);
                return true;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogWarning(ex, "Concurrency conflict for category {Id}.", id);
                throw new InvalidOperationException("Category modified by another.", ex);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "DB error deleting category {Id}.", id);
                throw new InvalidOperationException("Error deleting category.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error deleting category {Id}.", id);
                throw;
            }
        }

        public Task<bool> ExistsAsync(long id, CancellationToken ct = default)
        {
            if (id <= 0) return Task.FromResult(false);
            return _context.Categories.AsNoTracking().AnyAsync(c => c.Id == id, ct);
        }

        public Task<Category?> GetByIdAsync(long id, bool asNoTracking = true, CancellationToken ct = default)
        {
            if (id <= 0) return Task.FromResult<Category?>(null);

            var query = asNoTracking ? _context.Categories.AsNoTracking() : _context.Categories;
            return query.FirstOrDefaultAsync(c => c.Id == id, ct);
        }

        public async Task<Category?> GetByNameAsync(string name, CancellationToken ct = default)
        {
            var nm = name?.Trim();
            if (string.IsNullOrWhiteSpace(nm)) return null;
            var normalized = nm.ToUpperInvariant();

            var cacheKey = $"CategoryByName_{normalized}";
            if (!_cache.TryGetValue(cacheKey, out Category? cached))
            {
                cached = await _context.Categories.AsNoTracking()
                    .FirstOrDefaultAsync(c => c.NormalizedName == normalized, ct);
                if (cached != null)
                    _cache.Set(cacheKey, cached, TimeSpan.FromMinutes(10));
            }
            return cached;
        }

        public async Task<PagedResult<CategoryDto>> GetPagedAllAsync(int page = 1, int pageSize = 100, CancellationToken ct = default)
        {
            if (pageSize > 500) pageSize = 500;

            var q = _context.Categories.AsNoTracking().AsQueryable();
            var total = await q.CountAsync(ct);
            var items = await q.OrderBy(c => c.Name)
                               .Skip((page - 1) * pageSize)
                               .Take(pageSize)
                               .Select(c => new CategoryDto { Id = c.Id, Name = c.Name, Description = c.Description })
                               .ToListAsync(ct);
            return new PagedResult<CategoryDto>(items, total, page, pageSize);
        }

        public async Task<PagedResult<CategoryDto>> GetPagedAsync(int page = 1, int pageSize = 20, string? search = null, CancellationToken ct = default)
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0) pageSize = 20;

            var q = _context.Categories.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToUpperInvariant();
                var pattern = $"%{s}%";
                q = q.Where(c => EF.Functions.Like(c.NormalizedName, pattern));  
            }

            var total = await q.CountAsync(ct);
            var items = await q.OrderBy(c => c.Name)
                               .Skip((page - 1) * pageSize)
                               .Take(pageSize)
                               .Select(c => new CategoryDto { Id = c.Id, Name = c.Name, Description = c.Description })
                               .ToListAsync(ct);
            return new PagedResult<CategoryDto>(items, total, page, pageSize);
        }

        public async Task UpdateCategoryAsync(Category incoming, CancellationToken ct = default)
        {
            if (incoming == null) throw new ArgumentNullException(nameof(incoming));
            if (incoming.Id <= 0) throw new ArgumentException("Id must be set.", nameof(incoming.Id));

            Validator.ValidateObject(incoming, new ValidationContext(incoming), true);

            try
            {
                var existing = await _context.Categories.FirstOrDefaultAsync(c => c.Id == incoming.Id, ct);
                if (existing == null) throw new InvalidOperationException($"Category {incoming.Id} not found.");

                var newName = incoming.Name.Trim();
                if (!string.Equals(existing.Name, newName, StringComparison.OrdinalIgnoreCase))
                {
                    var normalized = newName.ToUpperInvariant();
                    var conflict = await _context.Categories
                        .AsNoTracking()
                        .FirstOrDefaultAsync(c => c.Id != incoming.Id && c.NormalizedName == normalized, ct);
                    if (conflict != null)
                    {
                        _logger.LogWarning("Rename attempt to existing name '{Name}'.", newName);
                        throw new InvalidOperationException("Duplicate category name.");
                    }
                }

                existing.Name = newName;
                existing.Description = incoming.Description ?? string.Empty;
                _logger.LogInformation("Prepared category {Id} for update.", incoming.Id);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogWarning(ex, "Concurrency for category {Id}.", incoming.Id);
                throw new InvalidOperationException("Modified by another.", ex);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "DB error updating category {Id}.", incoming.Id);
                throw new InvalidOperationException("Error updating.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error updating category {Id}.", incoming.Id);
                throw;
            }
        }
    }
}