using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RecipeManager.Interfaces.Services;

namespace RecipeManager.Infrastucture.Nutrition
{
    public class NutritionService : INutritionService
    {
        private readonly HttpClient _httpClient;
        private readonly NutritionOptions _options;
        private readonly ILogger<NutritionService> _logger;
        private readonly INutritionDictionaryService _dictionary;
        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private static readonly Regex MultiSpaceRegex = new("\\s+", RegexOptions.Compiled);
        private static readonly Regex NonAsciiRegex = new("[^a-z0-9\\s]", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Dictionary<char, string> CyrillicToLatin = new()
        {
            ['а'] = "a",  ['б'] = "b",  ['в'] = "v",  ['г'] = "g",  ['д'] = "d",
            ['е'] = "e",  ['ё'] = "yo", ['ж'] = "zh", ['з'] = "z",  ['и'] = "i",
            ['й'] = "y",  ['к'] = "k",  ['л'] = "l",  ['м'] = "m",  ['н'] = "n",
            ['о'] = "o",  ['п'] = "p",  ['р'] = "r",  ['с'] = "s",  ['т'] = "t",
            ['у'] = "u",  ['ф'] = "f",  ['х'] = "kh", ['ц'] = "ts", ['ч'] = "ch",
            ['ш'] = "sh", ['щ'] = "sch",['ъ'] = "",   ['ы'] = "y",  ['ь'] = "",
            ['э'] = "e",  ['ю'] = "yu", ['я'] = "ya", ['ґ'] = "g",  ['ї'] = "yi",
            ['і'] = "i"
        };

        private readonly record struct QueryVariant(string ProductQuery, double QueryWeight, string Reason);

        private class ApiNinjaNutritionResponse
        {
            [JsonPropertyName("name")]
            public string? Name { get; set; }
            
            [JsonPropertyName("serving_size_g")]
            public double Serving_Size_G { get; set; }
            
            // Может быть число или строка "Only available for premium subscribers."
            [JsonPropertyName("calories")]
            public JsonElement Calories { get; set; }
            
            [JsonPropertyName("protein_g")]
            public JsonElement Protein_G { get; set; }
            
            [JsonPropertyName("fat_total_g")]
            public double Fat_Total_G { get; set; }
            
            [JsonPropertyName("carbohydrates_total_g")]
            public double Carbohydrates_Total_G { get; set; }
            
            // Вспомогательные методы для безопасного извлечения значений
            public double GetCalories()
            {
                if (Calories.ValueKind == JsonValueKind.Number)
                    return Calories.GetDouble();
                return 0;
            }
            
            public double GetProtein()
            {
                if (Protein_G.ValueKind == JsonValueKind.Number)
                    return Protein_G.GetDouble();
                return 0;
            }
        }

        public NutritionService(HttpClient httpClient, IOptions<NutritionOptions> options, ILogger<NutritionService> logger, INutritionDictionaryService dictionary)
        {
            _httpClient = httpClient;
            _options = options.Value;
            _logger = logger;
            _dictionary = dictionary;
        }

        public async Task<NutritionInfo?> LookupAsync(string query, double weightGrams, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(_options.ApiKey))
            {
                _logger.LogWarning("Nutrition API key is not configured. ApiKey is null or empty.");
                return null;
            }

            if (weightGrams <= 0)
            {
                _logger.LogWarning("WeightGrams is {WeightGrams}, must be > 0", weightGrams);
                return null;
            }

            if (string.IsNullOrWhiteSpace(query))
            {
                _logger.LogWarning("Query is null or empty.");
                return null;
            }

            var dictionaryMatch = _dictionary.FindBestMatch(query);
            if (dictionaryMatch != null)
            {
                _logger.LogInformation("Nutrition dictionary match found for '{Query}': {Variant}", query, dictionaryMatch.VariantName);
                return ScaleNutrition(dictionaryMatch.Calories, dictionaryMatch.Protein, dictionaryMatch.Fat, dictionaryMatch.Carbohydrates, weightGrams, $"dictionary/{dictionaryMatch.VariantName}");
            }

            var variants = BuildQueryVariants(query, weightGrams).ToList();
            if (variants.Count == 0)
            {
                _logger.LogWarning("No query variants could be generated for input '{Query}'", query);
                return null;
            }

            _logger.LogInformation("Nutrition lookup started for '{Query}' ({VariantCount} variants)", query, variants.Count);

            foreach (var variant in variants)
            {
                var result = await TryLookupVariantAsync(variant, weightGrams, cancellationToken);
                if (result != null)
                {
                    if (!string.Equals(variant.Reason, "original/primary", StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.LogInformation("Nutrition lookup succeeded via fallback '{Reason}'", variant.Reason);
                    }
                    return result;
                }
            }

            _logger.LogWarning("All nutrition lookup attempts failed for '{Query}' ({Weight}g)", query, weightGrams);
            return null;
        }

        private async Task<NutritionInfo?> TryLookupVariantAsync(QueryVariant variant, double requestedWeight, CancellationToken cancellationToken)
        {
            var requestQuery = $"{Math.Round(variant.QueryWeight, 2)} grams {variant.ProductQuery}".Trim();
            var url = $"nutrition?query={Uri.EscapeDataString(requestQuery)}";

            _logger.LogInformation("Nutrition API lookup: {Reason} ⇒ {Query}", variant.Reason, requestQuery);

            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("X-Api-Key", _options.ApiKey!);

                var response = await _httpClient.SendAsync(request, cancellationToken);
                var rawContent = await response.Content.ReadAsStringAsync(cancellationToken);

                _logger.LogInformation("Nutrition API response ({Reason}): {StatusCode}, Length={Length}", variant.Reason, response.StatusCode, rawContent?.Length ?? 0);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Nutrition API returned {StatusCode} for variant {Reason}. Body: {Content}", response.StatusCode, variant.Reason, rawContent);

                    if (response.StatusCode == System.Net.HttpStatusCode.BadGateway)
                    {
                        _logger.LogWarning("API returned 502 for variant {Reason}. Possibly due to non-Latin characters. Query='{Query}'", variant.Reason, variant.ProductQuery);
                    }

                    return null;
                }

                if (string.IsNullOrWhiteSpace(rawContent))
                {
                    _logger.LogWarning("Nutrition API returned empty content for variant {Reason}", variant.Reason);
                    return null;
                }

                List<ApiNinjaNutritionResponse>? items;
                try
                {
                    items = JsonSerializer.Deserialize<List<ApiNinjaNutritionResponse>>(rawContent, SerializerOptions);
                }
                catch (JsonException jsonEx)
                {
                    _logger.LogError(jsonEx, "Failed to parse JSON response for variant {Reason}: {Content}", variant.Reason, rawContent);
                    return null;
                }

                var first = items?.FirstOrDefault();
                if (first == null)
                {
                    _logger.LogWarning("Nutrition API returned empty list for variant {Reason}. Raw: {Content}", variant.Reason, rawContent);
                    return null;
                }

                var calories = first.GetCalories();
                var protein = first.GetProtein();
                var fat = first.Fat_Total_G;
                var carbs = first.Carbohydrates_Total_G;

                if (calories <= 0 && (protein > 0 || fat > 0 || carbs > 0))
                {
                    calories = EstimateCalories(protein, fat, carbs);
                    _logger.LogInformation("Calories estimated from macros for variant {Reason}: {Calories}", variant.Reason, calories);
                }

                var servingWeight = first.Serving_Size_G > 0 ? first.Serving_Size_G : variant.QueryWeight;
                if (servingWeight <= 0)
                {
                    servingWeight = 100;
                }

                return ScaleNutrition(calories, protein, fat, carbs, requestedWeight, variant.Reason, servingWeight);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error during nutrition API call for variant {Reason}: {Message}", variant.Reason, ex.Message);
                return null;
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(ex, "Timeout during nutrition API call for variant {Reason}", variant.Reason);
                return null;
            }
        }

        private IEnumerable<QueryVariant> BuildQueryVariants(string query, double requestedWeight)
        {
            var normalized = NormalizeSpaces(query);
            if (string.IsNullOrWhiteSpace(normalized))
            {
                yield break;
            }

            var baseVariants = new List<(string Text, string Reason)>
            {
                (normalized, "original/primary")
            };

            var transliterated = Transliterate(normalized);
            if (!string.IsNullOrWhiteSpace(transliterated) &&
                !string.Equals(transliterated, normalized, StringComparison.OrdinalIgnoreCase))
            {
                baseVariants.Add((transliterated, "transliterated"));
            }

            var asciiOnly = RemoveNonAsciiLetters(normalized);
            if (!string.IsNullOrWhiteSpace(asciiOnly) &&
                !string.Equals(asciiOnly, normalized, StringComparison.OrdinalIgnoreCase))
            {
                baseVariants.Add((asciiOnly, "ascii-only"));
            }

            var firstWord = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(firstWord) &&
                !string.Equals(firstWord, normalized, StringComparison.OrdinalIgnoreCase))
            {
                baseVariants.Add((firstWord, "first-word"));
            }

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var (text, reason) in baseVariants)
            {
                foreach (var variant in CreateWeightVariants(text, requestedWeight, reason))
                {
                    var key = $"{variant.ProductQuery}|{variant.QueryWeight}";
                    if (seen.Add(key))
                    {
                        yield return variant;
                    }
                }
            }
        }

        private IEnumerable<QueryVariant> CreateWeightVariants(string text, double requestedWeight, string reason)
        {
            foreach (var candidateWeight in GetWeightCandidates(requestedWeight))
            {
                yield return new QueryVariant(text, candidateWeight, $"{reason}/{candidateWeight}g");
            }
        }

        private IEnumerable<double> GetWeightCandidates(double requestedWeight)
        {
            var result = new List<double>();
            if (requestedWeight > 0)
            {
                result.Add(Math.Round(requestedWeight, 2));
            }

            if (requestedWeight != 100)
            {
                result.Add(100);
            }

            if (requestedWeight > 150 && requestedWeight != 200)
            {
                result.Add(200);
            }

            if (requestedWeight != 50)
            {
                result.Add(50);
            }

            return result.Where(w => w > 0).Distinct();
        }

        private static string NormalizeSpaces(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            return MultiSpaceRegex.Replace(value.Trim(), " ");
        }

        private static string RemoveNonAsciiLetters(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var sanitized = NonAsciiRegex.Replace(value, " ");
            return NormalizeSpaces(sanitized);
        }

        private static string Transliterate(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var sb = new StringBuilder(value.Length * 2);
            foreach (var ch in value)
            {
                var lower = char.ToLowerInvariant(ch);
                if (CyrillicToLatin.TryGetValue(lower, out var replacement))
                {
                    sb.Append(replacement);
                }
                else
                {
                    sb.Append(ch);
                }
            }

            return NormalizeSpaces(sb.ToString());
        }

        private static double EstimateCalories(double protein, double fat, double carbs)
        {
            var calories = protein * 4 + carbs * 4 + fat * 9;
            return Math.Round(calories, 2);
        }

        private NutritionInfo ScaleNutrition(double calories, double protein, double fat, double carbs, double requestedWeight, string context, double baseWeight = 100)
        {
            if (baseWeight <= 0)
            {
                baseWeight = 100;
            }

            var scale = requestedWeight / baseWeight;
            var result = new NutritionInfo(
                Calories: Math.Round(calories * scale, 2),
                Protein: Math.Round(protein * scale, 2),
                Fat: Math.Round(fat * scale, 2),
                Carbohydrates: Math.Round(carbs * scale, 2),
                WeightGrams: Math.Round(requestedWeight, 2));

            _logger.LogInformation("Nutrition lookup successful ({Context}): {Calories} kcal, {Protein}g protein for {Weight}g", context, result.Calories, result.Protein, result.WeightGrams);
            return result;
        }
    }
}

