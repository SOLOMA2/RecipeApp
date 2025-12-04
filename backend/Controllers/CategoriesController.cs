using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using RecipeManager.DTOs.Category;
using RecipeManager.Infrastucture.Pagiantion;
using RecipeManager.Interfaces.UnitOfWork;
using RecipeManager.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RecipeManager.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    public class CategoriesController : ControllerBase
    {
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;
        private readonly ILogger<CategoriesController> _logger;

        public CategoriesController(IUnitOfWork uow, IMapper mapper, ILogger<CategoriesController> logger)
        {
            _uow = uow ?? throw new System.ArgumentNullException(nameof(uow));
            _mapper = mapper ?? throw new System.ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new System.ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Get paginated list of categories.
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetPaged(
            int page = 1,
            int pageSize = 100,
            string? search = null,
            CancellationToken cancellationToken = default)
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0) pageSize = 100;
            if (pageSize > 500) pageSize = 500;

            var paged = await _uow.Category.GetPagedAsync(page, pageSize, search, cancellationToken);
            return Ok(paged);
        }

        /// <summary>
        /// Get category by ID.
        /// </summary>
        [HttpGet("{id:long}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken = default)
        {
            if (id <= 0) return BadRequest();

            var category = await _uow.Category.GetByIdAsync(id, cancellationToken: cancellationToken);
            if (category == null) return NotFound();

            var dto = _mapper.Map<CategoryDetailsDto>(category);
            return Ok(dto);
        }

        /// <summary>
        /// Get count of recipes in a category.
        /// </summary>
        [HttpGet("{id:long}/recipes/count")]
        [AllowAnonymous]
        public async Task<IActionResult> GetRecipesCount(long id, CancellationToken cancellationToken = default)
        {
            if (id <= 0) return BadRequest();

            var category = await _uow.Category.GetByIdAsync(id, cancellationToken: cancellationToken);
            if (category == null) return NotFound();

            var count = await _uow.Recipe.CountByCategoryAsync(id, cancellationToken);
            return Ok(new { categoryId = id, recipesCount = count });
        }

        /// <summary>
        /// Get all categories with recipe counts.
        /// </summary>
        [HttpGet("with-counts")]
        [AllowAnonymous]
        public async Task<IActionResult> GetWithRecipeCounts(
            int page = 1,
            int pageSize = 100,
            CancellationToken cancellationToken = default)
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0) pageSize = 100;
            if (pageSize > 500) pageSize = 500;

            var paged = await _uow.Category.GetPagedAsync(page, pageSize, null, cancellationToken);
            var itemsWithCounts = new List<object>();
            foreach (var category in paged.Items)
            {
                var count = await _uow.Recipe.CountByCategoryAsync(category.Id, cancellationToken);
                itemsWithCounts.Add(new
                {
                    id = category.Id,
                    name = category.Name,
                    description = category.Description,
                    recipesCount = count
                });
            }

            return Ok(new
            {
                items = itemsWithCounts,
                totalCount = paged.TotalCount,
                page = paged.Page,
                pageSize = paged.PageSize
            });
        }
    }
}

