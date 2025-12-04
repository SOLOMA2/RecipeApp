using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;  
using RecipeManager.Data;
using RecipeManager.Infrastucture.Pagiantion;
using RecipeManager.Interfaces.Repositories;
using RecipeManager.Models;
using System.ComponentModel.DataAnnotations;  

namespace RecipeManager.Repositories
{
    public class TagRepository : ITagRepository
    {
        private readonly AppDbContext _context;
        private readonly ILogger<TagRepository> _logger;
        private readonly IMemoryCache _cache;  

        public TagRepository(AppDbContext context, ILogger<TagRepository> logger, IMemoryCache cache)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        }

        public async Task AddAsync(Tag tag, CancellationToken ct = default)
        {
            if (tag == null) throw new ArgumentNullException(nameof(tag));

            Validator.ValidateObject(tag, new ValidationContext(tag), validateAllProperties: true);

            try
            {
                var title = tag.Title.Trim();  
                var normalized = title.ToUpperInvariant();

                var existing = await _context.Tags
                    .AsNoTracking()
                    .FirstOrDefaultAsync(t => t.NormalizedTitle == normalized, ct);

                if (existing != null)
                {
                    tag.Title = existing.Title;
                    _logger.LogInformation("Tag with title '{Title}' already exists (Id={TagId}). Skipping insert.", existing.Title, existing.Id);
                    return;
                }

                tag.Title = title;
                await _context.Tags.AddAsync(tag, ct);
                _logger.LogInformation("Prepared new tag '{Title}' for insert.", title);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error while preparing tag for insert (Title={Title}).", tag.Title);
                throw new InvalidOperationException("Error preparing tag for insert.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error preparing tag for insert.");
                throw;
            }
        }

        public async Task AddRangeAsync(IEnumerable<Tag> tags, CancellationToken ct = default)
        {
            foreach (var tag in tags)
            {
                await AddAsync(tag, ct);
            }
        }

        public async Task<bool> DeleteAsync(long id, CancellationToken ct = default)
        {
            if (id <= 0) throw new ArgumentOutOfRangeException(nameof(id), "Id must be positive.");

            try
            {
                var tag = await _context.Tags.FirstOrDefaultAsync(t => t.Id == id, ct);

                if (tag == null) 
                    return false;

                var isUsed = await _context.Tags.Where(t => t.Id == id).SelectMany(t => t.Recipes).AnyAsync(ct);

                if (isUsed)
                {
                    _logger.LogWarning("Attempt to delete tag {TagId} that is in use.", id);
                    throw new InvalidOperationException("Cannot delete tag that is used by recipes.");
                }

                tag.IsDeleted = true;
                _logger.LogInformation("Tag {TagId} marked as deleted.", id);

                return true;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogWarning(ex, "Concurrency conflict deleting tag {TagId}.", id);
                throw new InvalidOperationException("Tag was modified by another user.", ex);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error deleting tag {TagId}.", id);
                throw new InvalidOperationException("Error deleting tag.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error deleting tag {TagId}.", id);
                throw;
            }
        }

        public Task<bool> ExistsAsync(long id, CancellationToken ct = default)
        {
            if (id <= 0) return Task.FromResult(false);
            return _context.Tags.AsNoTracking().AnyAsync(t => t.Id == id, ct);
        }

        public Task<Tag?> GetByIdAsync(long id, CancellationToken ct = default)
        {
            if (id <= 0) return Task.FromResult<Tag?>(null);
            return _context.Tags.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id, ct);
        }

        public async Task<Tag?> GetByTitleAsync(string title, CancellationToken ct = default)
        {
            var t = title?.Trim();
            if (string.IsNullOrWhiteSpace(t)) return null;
            var normalized = t.ToUpperInvariant();

            var cacheKey = $"TagByTitle_{normalized}";
            if (!_cache.TryGetValue(cacheKey, out Tag? cachedTag))
            {
                cachedTag = await _context.Tags.AsNoTracking()
                    .FirstOrDefaultAsync(tag => tag.NormalizedTitle == normalized, ct);
                if (cachedTag != null)
                {
                    _cache.Set(cacheKey, cachedTag, TimeSpan.FromMinutes(10)); 
                }
            }
            return cachedTag;
        }

        public async Task<PagedResult<TagDto>> GetPagedAllAsync(int page = 1, int pageSize = 100, CancellationToken ct = default)
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0 || pageSize > 500) pageSize = 100; 

            var q = _context.Tags.AsNoTracking().AsQueryable();
            var total = await q.CountAsync(ct);
            var items = await q.OrderBy(t => t.Title)
                               .Skip((page - 1) * pageSize)
                               .Take(pageSize)
                               .Select(t => new TagDto { Id = t.Id, Title = t.Title })
                               .ToListAsync(ct);
            return new PagedResult<TagDto>(items, total, page, pageSize);
        }

        public async Task<PagedResult<TagDto>> GetPagedAsync(int page = 1, int pageSize = 20, string? search = null, CancellationToken ct = default)
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0) pageSize = 20;

            var q = _context.Tags.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim();
                var pattern = $"%{s.ToUpperInvariant()}%";
                q = q.Where(t => EF.Functions.Like(t.NormalizedTitle, pattern));
            }

            var total = await q.CountAsync(ct);
            var items = await q.OrderBy(t => t.Title)
                               .Skip((page - 1) * pageSize)
                               .Take(pageSize)
                               .Select(t => new TagDto { Id = t.Id, Title = t.Title })  
                               .ToListAsync(ct);
            return new PagedResult<TagDto>(items, total, page, pageSize);
        }

        public async Task UpdateAsync(Tag incoming, CancellationToken ct = default)
        {
            if (incoming == null) throw new ArgumentNullException(nameof(incoming));
            if (incoming.Id <= 0) throw new ArgumentException("Tag.Id must be set for update", nameof(incoming.Id));

            Validator.ValidateObject(incoming, new ValidationContext(incoming), true);

            try
            {
                var existing = await _context.Tags.FirstOrDefaultAsync(t => t.Id == incoming.Id, ct);
                if (existing == null) throw new InvalidOperationException($"Tag {incoming.Id} not found.");

                var newTitle = incoming.Title.Trim();
                if (string.Equals(existing.Title, newTitle, StringComparison.OrdinalIgnoreCase))  
                {
                    existing.Title = newTitle;  
                    return;
                }

                var normalized = newTitle.ToUpperInvariant();
                var conflict = await _context.Tags
                    .AsNoTracking()
                    .FirstOrDefaultAsync(t => t.Id != incoming.Id && t.NormalizedTitle == normalized, ct);
                if (conflict != null)
                {
                    _logger.LogWarning("Attempt to rename tag {TagId} to existing title '{Title}'.", incoming.Id, newTitle);
                    throw new InvalidOperationException("Another tag with the same title exists.");
                }

                existing.Title = newTitle;
                _logger.LogInformation("Prepared tag {TagId} for title update to '{Title}'.", incoming.Id, newTitle);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogWarning(ex, "Concurrency conflict updating tag {TagId}.", incoming.Id);
                throw new InvalidOperationException("Tag was modified by another user.", ex);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error updating tag {TagId}.", incoming.Id);
                throw new InvalidOperationException("Error updating tag.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error updating tag {TagId}.", incoming.Id);
                throw;
            }
        }
    }
}