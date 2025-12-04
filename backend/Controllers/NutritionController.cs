using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using RecipeManager.DTOs.Nutrition;
using RecipeManager.Interfaces.Services;
using System;
using System.Linq;

namespace RecipeManager.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    [AllowAnonymous]
    public class NutritionController : ControllerBase
    {
        private readonly INutritionService _nutritionService;
        private readonly INutritionDictionaryService _dictionaryService;
        private readonly ILogger<NutritionController> _logger;

        public NutritionController(INutritionService nutritionService, INutritionDictionaryService dictionaryService, ILogger<NutritionController> logger)
        {
            _nutritionService = nutritionService;
            _dictionaryService = dictionaryService;
            _logger = logger;
        }

        [HttpGet("suggest")]
        public IActionResult Suggest([FromQuery] string query, [FromQuery] int limit = 5)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return Ok(Array.Empty<NutritionSuggestionDto>());
            }

            var suggestions = _dictionaryService
                .Suggest(query, Math.Clamp(limit, 1, 10))
                .Select(s => new NutritionSuggestionDto
                {
                    VariantName = s.VariantName,
                    BaseProduct = s.BaseProduct,
                    DisplayName = s.DisplayName,
                    Query = s.Query,
                    Calories = s.Calories,
                    Protein = s.Protein,
                    Fat = s.Fat,
                    Carbohydrates = s.Carbohydrates
                })
                .ToList();

            return Ok(suggestions);
        }

        [HttpPost("lookup")]
        public async Task<IActionResult> Lookup([FromBody] NutritionLookupRequest request, CancellationToken cancellationToken = default)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model state for nutrition lookup: {Errors}", ModelState);
                return ValidationProblem(ModelState);
            }

            _logger.LogInformation("Nutrition lookup request: Query={Query}, WeightGrams={WeightGrams}", request.Query, request.WeightGrams);

            try
            {
                var info = await _nutritionService.LookupAsync(request.Query, request.WeightGrams, cancellationToken);
                if (info == null)
                {
                    _logger.LogWarning("Nutrition lookup returned null for Query={Query}, WeightGrams={WeightGrams}", request.Query, request.WeightGrams);
                    return StatusCode(503, new { 
                        message = "Сервис расчёта калорий временно недоступен или не вернул данных. Проверьте API ключ в appsettings.json и перезапустите приложение.",
                        query = request.Query,
                        weightGrams = request.WeightGrams,
                        hint = "Вы можете ввести данные вручную в полях ниже"
                    });
                }

                var response = new NutritionLookupResponse
                {
                    Calories = info.Calories,
                    Protein = info.Protein,
                    Fat = info.Fat,
                    Carbohydrates = info.Carbohydrates,
                    WeightGrams = info.WeightGrams
                };

                _logger.LogInformation("Nutrition lookup successful: {Calories} kcal", info.Calories);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in nutrition lookup: {Message}", ex.Message);
                return StatusCode(500, new { 
                    message = "Внутренняя ошибка сервера при запросе к API питания",
                    error = ex.Message
                });
            }
        }
    }
}

