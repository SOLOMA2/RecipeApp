using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using RecipeManager.Infrastucture.Pagiantion;
using RecipeManager.Interfaces.UnitOfWork;
using RecipeManager.Models;

namespace RecipeManager.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    public class RecipesController : ControllerBase
    {
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;
        private readonly ILogger<RecipesController> _logger;

        public RecipesController(IUnitOfWork uow, IMapper mapper, ILogger<RecipesController> logger)
        {
            _uow = uow ?? throw new ArgumentNullException(nameof(uow));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Создать рецепт. Только роли Creator и Admin.
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Creator,Admin")]
        public async Task<IActionResult> Create([FromBody] CreateRecipeDto dto, CancellationToken cancellationToken = default)
        {
            if (dto == null) return BadRequest("Body is required.");
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (string.IsNullOrWhiteSpace(userId))
            {
                _logger.LogWarning("Create recipe: user id not found in token.");
                return Unauthorized();
            }

            var recipe = _mapper.Map<Recipe>(dto);
            recipe.AuthorId = userId;
            recipe.CookingMethod = string.IsNullOrWhiteSpace(recipe.CookingMethod) ? "Not specified" : recipe.CookingMethod;
            recipe.Description ??= string.Empty;
            recipe.ImageUrl ??= string.Empty;
                if (dto.CategoryIds != null && dto.CategoryIds.Count > 0)
                {
                    var cats = dto.CategoryIds.Distinct().ToList();
                    recipe.Categories = new System.Collections.Generic.List<Category>();
                    foreach (var catId in cats)
                    {
                        var c = await _uow.Category.GetByIdAsync(catId, asNoTracking: false, cancellationToken: cancellationToken);
                        if (c != null) recipe.Categories.Add(c);
                        else _logger.LogInformation("Create recipe: category id {CatId} not found, ignored.", catId);
                    }
                }

            if (recipe.Ingredients != null && recipe.Ingredients.Any())
            {
                recipe.Calories = recipe.Ingredients.Sum(i => i.Calories);
                recipe.Weight = recipe.Ingredients.Sum(i => i.Weight);
                recipe.Protein = recipe.Ingredients.Sum(i => i.Protein);
                recipe.Fat = recipe.Ingredients.Sum(i => i.Fat);
                recipe.Carbohydrates = recipe.Ingredients.Sum(i => i.Carbohydrates);
            }
            else
            {
                recipe.Calories = dto.Calories;
                recipe.Weight = dto.Weight;
                recipe.Protein = dto.Protein;
                recipe.Fat = dto.Fat;
                recipe.Carbohydrates = dto.Carbohydrates;
            }

            try
            {
                await _uow.Recipe.AddAsync(recipe, cancellationToken);
                await _uow.SaveChangesAsync();

                var created = await _uow.Recipe.GetWithDetailsAsync(recipe.Id, cancellationToken);
                var resultDto = _mapper.Map<RecipeDetailsDto>(created);

                return CreatedAtAction(nameof(GetById), new { id = recipe.Id }, resultDto);
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error while creating recipe for user {UserId}.", userId);
                return StatusCode(500, "Database error while creating recipe.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while creating recipe for user {UserId}.", userId);
                throw;
            }
        }

        /// <summary>
        /// Получить рецепт с деталями (ингредиенты, тэги, категории).
        /// </summary>
        [HttpGet("{id:long}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken = default)
        {
            if (id <= 0) return BadRequest();

            var recipe = await _uow.Recipe.GetWithDetailsAsync(id, cancellationToken);
            if (recipe == null) return NotFound();

            var dto = _mapper.Map<RecipeDetailsDto>(recipe);
            return Ok(dto);
        }

        /// <summary>
        /// Get paginated list of recipes with optional search and category filter.
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetPaged(int page = 1, int pageSize = 20, string? search = null, long? categoryId = null, CancellationToken cancellationToken = default)
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0) pageSize = 20;

            var paged = await _uow.Recipe.GetPagedAsync(page, pageSize, search, categoryId, cancellationToken);
            var items = paged.Items.Select(r => _mapper.Map<RecipeListItemDto>(r)).ToList();
            var result = new PagedResult<RecipeListItemDto>(items, paged.TotalCount, paged.Page, paged.PageSize);

            return Ok(result);
        }

        /// <summary>
        /// Rate a recipe (1-5). Currently calculates simple average rating without user history tracking.
        /// </summary>
        [HttpPost("{id:long}/rate")]
        [AllowAnonymous]
        public async Task<IActionResult> Rate(long id, [FromBody] RateRecipeRequest request, CancellationToken cancellationToken = default)
        {
            if (id <= 0) return BadRequest("Invalid id.");
            if (request == null || request.Value < 1 || request.Value > 5)
                return BadRequest("Rating value must be between 1 and 5.");

            var recipe = await _uow.Recipe.GetByIdAsync(id, asNoTracking: false, cancellationToken);
            if (recipe == null) return NotFound();

            var totalScore = recipe.Rating * recipe.RatingCount + request.Value;
            recipe.RatingCount += 1;
            recipe.Rating = Math.Round(totalScore / Math.Max(recipe.RatingCount, 1), 2);

            await _uow.SaveChangesAsync();

            return Ok(new { recipeId = recipe.Id, recipe.Rating, recipe.RatingCount, recipe.LikesCount });
        }

        /// <summary>
        /// Like a recipe (increments likes count).
        /// </summary>
        [HttpPost("{id:long}/like")]
        [AllowAnonymous]
        public async Task<IActionResult> Like(long id, CancellationToken cancellationToken = default)
        {
            if (id <= 0) return BadRequest("Invalid id.");

            var recipe = await _uow.Recipe.GetByIdAsync(id, asNoTracking: false, cancellationToken);
            if (recipe == null) return NotFound();

            recipe.LikesCount += 1;
            await _uow.SaveChangesAsync();

            return Ok(new { recipeId = recipe.Id, recipe.Rating, recipe.RatingCount, recipe.LikesCount });
        }
    }
}
