using Microsoft.EntityFrameworkCore;
using RecipeManager.Data;
using RecipeManager.Infrastucture.Pagiantion;
using RecipeManager.Interfaces.Repositories;
using RecipeManager.Models;

namespace RecipeManager.Repositories
{
    public class RecipeRepository : IRecipeRepository
    {
        private readonly AppDbContext _context;
        private readonly ILogger<RecipeRepository> _logger;

        public RecipeRepository(AppDbContext context, ILogger<RecipeRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task AddAsync(Recipe recipe, CancellationToken cancellationToken = default)
        {
            if (recipe == null) throw new ArgumentNullException(nameof(recipe));
            if (string.IsNullOrWhiteSpace(recipe.Title))
                throw new ArgumentException("Title cannot be empty", nameof(recipe.Title));
            if (recipe.CookingTimeMinutes < 0)
                throw new ArgumentException("Cooking time cannot be negative", nameof(recipe.CookingTimeMinutes));

            try
            {
                var incomingTags = (recipe.Tags ?? Enumerable.Empty<Tag>()).ToList();
                var incomingIngredients = (recipe.Ingredients ?? Enumerable.Empty<Ingredient>()).ToList();

                var trackedTags = await GetOrCreateTagsAsync(incomingTags, cancellationToken);

                recipe.Tags = trackedTags;

                foreach (var ing in incomingIngredients)
                {
                    ing.Recipe = recipe;
                    ing.RecipeId = 0;
                }
                recipe.Ingredients = incomingIngredients;

                if (recipe.CreatedAt == default) recipe.CreatedAt = DateTime.UtcNow;

                await _context.Recipes.AddAsync(recipe, cancellationToken);
                _logger.LogInformation("Prepared new recipe for insert (title={Title}).", recipe.Title);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error adding recipe.");
                throw new InvalidOperationException("Error adding recipe to database.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error adding recipe.");
                throw;
            }
        }

        private async Task<List<Tag>> GetOrCreateTagsAsync(IEnumerable<Tag> incomingTagsEnumerable, CancellationToken cancellationToken)
        {
            var incomingTags = (incomingTagsEnumerable ?? Enumerable.Empty<Tag>()).ToList();

            var ids = incomingTags.Where(t => t.Id != 0).Select(t => t.Id).Distinct().ToList();
            var newTitles = incomingTags.Where(t => t.Id == 0)
                                        .Select(t => t.Title?.Trim())
                                        .Where(s => !string.IsNullOrWhiteSpace(s))
                                        .Select(s => s!)
                                        .Distinct(StringComparer.OrdinalIgnoreCase)
                                        .ToList();

            var result = new List<Tag>();

            if (ids.Any() || newTitles.Any())
            {
                var q = _context.Tags.AsQueryable();
                if (ids.Any() && newTitles.Any())
                    q = q.Where(t => ids.Contains(t.Id) || newTitles.Contains(t.Title));
                else if (ids.Any())
                    q = q.Where(t => ids.Contains(t.Id));
                else
                    q = q.Where(t => newTitles.Contains(t.Title));

                var found = await q.ToListAsync(cancellationToken);
                result.AddRange(found);
            }

            var foundTitles = result.Select(t => t.Title).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var titlesToCreate = newTitles.Where(t => !foundTitles.Contains(t)).ToList();

            foreach (var title in titlesToCreate)
            {
                var newTag = new Tag { Title = title };
                await _context.Tags.AddAsync(newTag, cancellationToken); 
                result.Add(newTag);
            }

            if (ids.Any())
            {
                var missingById = ids.Where(id => !result.Any(t => t.Id == id)).ToList();
                if (missingById.Any())
                {
                    _logger.LogWarning("Some tag ids not found: {MissingIds}", missingById);
                }
            }

            return result;
        }

        public async Task DeleteAsync(long id, CancellationToken cancellationToken = default)
        {
            if (id <= 0) throw new ArgumentOutOfRangeException(nameof(id), "ID must be positive.");

            try
            {
                var entity = await _context.Recipes.FindAsync(id, cancellationToken);
                if (entity == null)
                    throw new InvalidOperationException($"Recipe {id} not found.");

                _context.Recipes.Remove(entity);
                _logger.LogInformation("Deleted recipe {RecipeId}", id);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogWarning(ex, "Concurrency conflict deleting recipe {RecipeId}.", id);
                throw new InvalidOperationException("Recipe was modified by another user.", ex);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error deleting recipe {RecipeId}.", id);
                throw new InvalidOperationException("Error deleting recipe from database.", ex);
            }
            catch (Exception ex)
            {  
                _logger.LogError(ex, "Unexpected error deleting recipe {RecipeId}.", id);
                throw;
            }
        }

        public Task<bool> ExistsAsync(long id, CancellationToken cancellationToken = default)
        {
            return _context.Recipes
                           .AsNoTracking()
                           .AnyAsync(r => r.Id == id, cancellationToken);
        }

        public Task<Recipe?> GetByIdAsync(long id, bool asNoTracking = true, CancellationToken cancellationToken = default)
        {
            var q = asNoTracking ? _context.Recipes.AsNoTracking() : _context.Recipes;
            return q.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        }

        public async Task<Recipe?> GetWithDetailsAsync(long id, CancellationToken cancellationToken = default)
        {
            if(id <= 0 ) return null;

            try
            {
                var q = _context.Recipes
                    .AsNoTracking()
                    .Where(r => r.Id == id)
                    .Include(r => r.Ingredients)
                    .Include(r => r.Tags)
                    .Include(r => r.Categories)
                    .Include(r => r.Author)
                    .AsSplitQuery();

                return await q.FirstOrDefaultAsync(cancellationToken);
            }
            catch (Exception ex) 
            {
                _logger.LogError(ex, "Error fetching recipe details {RecipeId}.", id);
                throw;
            }
        }

        public async Task<PagedResult<Recipe>> GetPagedAsync(int page = 1, int pageSize = 20, string? search = null, long? categoryId = null, CancellationToken cancellationToken = default)
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0) pageSize = 20;

            var q = _context.Recipes.AsNoTracking().AsQueryable();

            if (categoryId.HasValue && categoryId.Value > 0)
            {
                q = q.Where(r => r.Categories.Any(c => c.Id == categoryId.Value));
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim();
                var pattern = $"%{s}%"; 
                q = q.Where(r => EF.Functions.Like(r.Title, pattern) || EF.Functions.Like(r.Description, pattern));
            }

            var total = await q.CountAsync(cancellationToken);

            var items = await q
                               .Include(r => r.Categories)
                               .Include(r => r.Author)
                               .OrderByDescending(r => r.CreatedAt)
                               .Skip((page - 1) * pageSize)
                               .Take(pageSize)
                               .ToListAsync(cancellationToken);

            return new PagedResult<Recipe>(items, total, page, pageSize);
        }

        public async Task<int> CountByCategoryAsync(long categoryId, CancellationToken cancellationToken = default)
        {
            if (categoryId <= 0) return 0;

            return await _context.Recipes
                .AsNoTracking()
                .Where(r => r.Categories.Any(c => c.Id == categoryId))
                .CountAsync(cancellationToken);
        }

        public async Task UpdateRecipeAsync(Recipe incoming, CancellationToken cancellationToken = default)
        {
            if (incoming == null) throw new ArgumentNullException(nameof(incoming));

            if (string.IsNullOrWhiteSpace(incoming.Title))
                throw new ArgumentException("Title cannot be empty", nameof(incoming.Title));
            if (incoming.CookingTimeMinutes < 0)
                throw new ArgumentException("Cooking time cannot be negative", nameof(incoming.CookingTimeMinutes));

            try
            {
                var existing = await _context.Recipes
                     .Include(r => r.Ingredients)
                     .Include(r => r.Tags)
                     .AsSplitQuery()
                     .FirstOrDefaultAsync(r => r.Id == incoming.Id, cancellationToken);

                if (existing == null)
                    throw new InvalidOperationException($"Recipe {incoming.Id} not found");

                existing.Title = incoming.Title;
                existing.Description = incoming.Description;
                existing.ImageUrl = incoming.ImageUrl;
                existing.Weight = incoming.Weight;
                existing.Calories = incoming.Calories;
                existing.CookingMethod = incoming.CookingMethod;
                existing.CookingTimeMinutes = incoming.CookingTimeMinutes;
                existing.AuthorId = incoming.AuthorId;

                await SyncIngredientsAsync(existing, incoming.Ingredients ?? new List<Ingredient>(), cancellationToken);
                await SyncTagsAsync(existing, incoming.Tags ?? new List<Tag>(), cancellationToken);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogWarning(ex, "Concurrency conflict for recipe {RecipeId}. Reloading and retrying.", incoming.Id);
                throw new InvalidOperationException("Recipe was modified by another user. Please refresh and try again.", ex);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database update error for recipe {RecipeId}.", incoming.Id);
                throw new InvalidOperationException("Error updating recipe in database.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error updating recipe {RecipeId}.", incoming.Id);
                throw;
            }
        }

        private Task SyncIngredientsAsync(Recipe existing, IEnumerable<Ingredient> incomingIngredientsEnumerable, CancellationToken cancellationToken)
        {
            var incomingList = (incomingIngredientsEnumerable ?? Enumerable.Empty<Ingredient>()).ToList();
            var existingIngredients = existing.Ingredients ??= new List<Ingredient>();

            var incomingIds = incomingList.Where(i => i.Id != 0).Select(i => i.Id).ToHashSet();

            var toRemove = existingIngredients.Where(e => !incomingIds.Contains(e.Id)).ToList();
            foreach (var rem in toRemove)
                _context.Ingredients.Remove(rem);

            foreach (var ex in existingIngredients.Where(e => incomingIds.Contains(e.Id)))
            {
                var inc = incomingList.First(ii => ii.Id == ex.Id);
                ex.Title = inc.Title;
                ex.Description = inc.Description;
                ex.Quantity = inc.Quantity;
                ex.Weight = inc.Weight;
                ex.Calories = inc.Calories;
                ex.Protein = inc.Protein;
                ex.Fat = inc.Fat;
                ex.Carbohydrates = inc.Carbohydrates;
            }

            var newItems = incomingList.Where(i => i.Id == 0).ToList();
            foreach (var ni in newItems)
            {
                var newEntity = new Ingredient
                {
                    Title = ni.Title,
                    Description = ni.Description,
                    Quantity = ni.Quantity,
                    Weight = ni.Weight,
                    Calories = ni.Calories,
                    Protein = ni.Protein,
                    Fat = ni.Fat,
                    Carbohydrates = ni.Carbohydrates,
                    RecipeId = existing.Id
                };
                existingIngredients.Add(newEntity);
            }

            return Task.CompletedTask;
        }


        private async Task SyncTagsAsync(Recipe existing, IEnumerable<Tag> incomingTagsEnumerable, CancellationToken cancellationToken)
        {
            var incomingTags = (incomingTagsEnumerable ?? Enumerable.Empty<Tag>()).ToList();
            var existingTags = existing.Tags ??= new List<Tag>();

            var incomingTagIds = incomingTags.Where(t => t.Id != 0).Select(t => t.Id).ToHashSet();
            var newTagTitles = incomingTags.Where(t => t.Id == 0)
                                           .Select(t => t.Title?.Trim())
                                           .Where(s => !string.IsNullOrWhiteSpace(s))
                                           .Select(s => s!)
                                           .Distinct(StringComparer.OrdinalIgnoreCase)
                                           .ToList();

            var tagsFromDb = new List<Tag>();
            if (incomingTagIds.Any() || newTagTitles.Any())
            {
                var q = _context.Tags.AsQueryable();
                if (incomingTagIds.Any() && newTagTitles.Any())
                    q = q.Where(t => incomingTagIds.Contains(t.Id) || newTagTitles.Contains(t.Title));
                else if (incomingTagIds.Any())
                    q = q.Where(t => incomingTagIds.Contains(t.Id));
                else
                    q = q.Where(t => newTagTitles.Contains(t.Title));

                tagsFromDb = await q.ToListAsync(cancellationToken);
            }

            var desiredTags = new List<Tag>(tagsFromDb);

            var titlesFound = tagsFromDb.Select(t => t.Title).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var titlesToCreate = newTagTitles.Where(t => !titlesFound.Contains(t)).ToList();

            foreach (var title in titlesToCreate)
            {
                var created = new Tag { Title = title };
                await _context.Tags.AddAsync(created, cancellationToken);
                desiredTags.Add(created);
            }

            var desiredIds = desiredTags.Where(t => t.Id != 0).Select(t => t.Id).ToHashSet();
            var desiredTitles = desiredTags.Where(t => t.Id == 0).Select(t => t.Title).ToHashSet(StringComparer.OrdinalIgnoreCase);

            var toRemove = existingTags.Where(t => !(t.Id != 0 ? desiredIds.Contains(t.Id) : desiredTitles.Contains(t.Title))).ToList();
            foreach (var rem in toRemove)
                existingTags.Remove(rem);

            foreach (var dt in desiredTags)
            {
                var existsInExisting = existingTags.Any(et =>
                    et.Id != 0 && dt.Id != 0 && et.Id == dt.Id
                    || et.Id == 0 && dt.Id == 0 && string.Equals(et.Title, dt.Title, StringComparison.OrdinalIgnoreCase));

                if (existsInExisting) continue;

                if (dt.Id != 0)
                {
                    var tracked = _context.Tags.Local.FirstOrDefault(t => t.Id == dt.Id) ?? dt;
                    if (_context.Entry(tracked).State == EntityState.Detached)
                        _context.Tags.Attach(tracked);

                    existingTags.Add(tracked);
                }
                else
                {
                    existingTags.Add(dt);
                }
            }
        }
    }
}