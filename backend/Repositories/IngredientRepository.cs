using Microsoft.EntityFrameworkCore;
using RecipeManager.Data;
using RecipeManager.Infrastucture.Pagiantion;
using RecipeManager.Interfaces.Repositories;
using RecipeManager.Models;

namespace RecipeManager.Repositories
{
    public class IngredientRepository : IIngredientRepository
    {
        private readonly AppDbContext _context;
        private readonly ILogger<IngredientRepository> _logger;

        public IngredientRepository(AppDbContext context, ILogger<IngredientRepository> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task AddIngredientAsync(Ingredient ingredient, CancellationToken cancellationToken = default)
        {
            if (ingredient == null) throw new ArgumentNullException(nameof(ingredient));

            if (string.IsNullOrWhiteSpace(ingredient.Title))
                throw new ArgumentException("Title cannot be empty", nameof(ingredient.Title));
            if (ingredient.Quantity < 0)
                throw new ArgumentException("Quantity cannot be negative", nameof(ingredient.Quantity));
            if (ingredient.Weight < 0)
                throw new ArgumentException("Weight cannot be negative", nameof(ingredient.Weight));
            if (ingredient.Calories < 0)
                throw new ArgumentException("Calories cannot be negative", nameof(ingredient.Calories));

            try
            {
                if (ingredient.Recipe != null)
                {
                    if (ingredient.Recipe.Id != 0)
                    {
                        ingredient.RecipeId = ingredient.Recipe.Id;
                    }
                    ingredient.Recipe = null;
                }

                if (ingredient.RecipeId == 0)
                    throw new ArgumentException("Ingredient must reference an existing recipe (RecipeId).", nameof(ingredient.RecipeId));

                var recipeExists = await _context.Recipes
                    .AsNoTracking()
                    .AnyAsync(r => r.Id == ingredient.RecipeId, cancellationToken);
                if (!recipeExists)
                    throw new InvalidOperationException($"Referenced Recipe {ingredient.RecipeId} not found.");

                await _context.Ingredients.AddAsync(ingredient, cancellationToken);
                _logger.LogInformation("Prepared new ingredient for insert (Title={Title}, RecipeId={RecipeId})",
                    ingredient.Title, ingredient.RecipeId);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error while preparing ingredient for insert.");
                throw new InvalidOperationException("Error preparing ingredient for insertion.", ex);
            }
        }

        public async Task<bool> DeleteIngredientAsync(long id, CancellationToken cancellationToken = default)
        {
            if (id <= 0) throw new ArgumentOutOfRangeException(nameof(id), "Id must be positive.");

            try
            {
                var entity = await _context.Ingredients.FindAsync(new object[] { id }, cancellationToken);
                if (entity == null) return false;

                _context.Ingredients.Remove(entity);
                _logger.LogInformation("Ingredient {IngredientId} marked for deletion.", id);
                return true;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogWarning(ex, "Concurrency conflict deleting ingredient {IngredientId}.", id);
                throw new InvalidOperationException("Ingredient was modified by another user.", ex);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error deleting ingredient {IngredientId}.", id);
                throw new InvalidOperationException("Error deleting ingredient.", ex);
            }
        }

        public Task<bool> ExistsAsync(long id, CancellationToken cancellationToken = default)
        {
            if (id <= 0) return Task.FromResult(false);
            return _context.Ingredients.AsNoTracking().AnyAsync(i => i.Id == id, cancellationToken);
        }

        public Task<Ingredient?> GetIngredientByIdAsync(long id, CancellationToken cancellationToken = default)
        {
            if (id <= 0) return Task.FromResult<Ingredient?>(null);
            return _context.Ingredients.AsNoTracking().FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
        }

        public async Task<IEnumerable<Ingredient>> GetByRecipeIdAsync(long recipeId, CancellationToken cancellationToken = default)
        {
            if (recipeId <= 0) return Enumerable.Empty<Ingredient>();
            return await _context.Ingredients
                .AsNoTracking()
                .Where(i => i.RecipeId == recipeId)
                .OrderBy(i => i.Title)
                .ToListAsync(cancellationToken);
        }

        public async Task<PagedResult<Ingredient>?> GetPagedAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0) pageSize = 20;

            var q = _context.Ingredients.AsNoTracking();

            var total = await q.CountAsync(cancellationToken);
            var items = await q.OrderBy(i => i.Title)
                               .Skip((page - 1) * pageSize)
                               .Take(pageSize)
                               .ToListAsync(cancellationToken);

            return new PagedResult<Ingredient>(items, total, page, pageSize);
        }

        public async Task UpdateIngredientAsync(Ingredient incoming, CancellationToken cancellationToken = default)
        {
            if (incoming == null) throw new ArgumentNullException(nameof(incoming));
            if (incoming.Id <= 0) throw new ArgumentException("Ingredient.Id must be set for update", nameof(incoming.Id));
            if (string.IsNullOrWhiteSpace(incoming.Title))
                throw new ArgumentException("Title cannot be empty", nameof(incoming.Title));
            if (incoming.Quantity < 0)
                throw new ArgumentException("Quantity cannot be negative", nameof(incoming.Quantity));
            if (incoming.Weight < 0)
                throw new ArgumentException("Weight cannot be negative", nameof(incoming.Weight));
            if (incoming.Calories < 0)
                throw new ArgumentException("Calories cannot be negative", nameof(incoming.Calories));

            try
            {
                var existing = await _context.Ingredients
                    .FirstOrDefaultAsync(i => i.Id == incoming.Id, cancellationToken);

                if (existing == null)
                    throw new InvalidOperationException($"Ingredient {incoming.Id} not found.");

                existing.Title = incoming.Title;
                existing.Description = incoming.Description;
                existing.Quantity = incoming.Quantity;
                existing.Weight = incoming.Weight;
                existing.Calories = incoming.Calories;

                if (incoming.RecipeId != 0 && incoming.RecipeId != existing.RecipeId)
                {
                    var recipeExists = await _context.Recipes.AsNoTracking()
                        .AnyAsync(r => r.Id == incoming.RecipeId, cancellationToken);
                    if (!recipeExists)
                        throw new InvalidOperationException($"Referenced Recipe {incoming.RecipeId} not found.");

                    existing.RecipeId = incoming.RecipeId;
                }

                _logger.LogInformation("Prepared ingredient {IngredientId} for update.", incoming.Id);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogWarning(ex, "Concurrency conflict updating ingredient {IngredientId}.", incoming.Id);
                throw new InvalidOperationException("Ingredient was modified by another user.", ex);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database update error for ingredient {IngredientId}.", incoming.Id);
                throw new InvalidOperationException("Error updating ingredient.", ex);
            }
        }
    }
}
