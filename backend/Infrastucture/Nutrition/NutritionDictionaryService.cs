using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using RecipeManager.Interfaces.Services;
using System.Linq;

namespace RecipeManager.Infrastucture.Nutrition
{
    public class NutritionDictionaryService : INutritionDictionaryService
    {
        private readonly ILogger<NutritionDictionaryService> _logger;
        private readonly List<Entry> _entries = new();

        private static readonly Regex MultiSpaceRegex = new("\\s+", RegexOptions.Compiled);

        private class Entry
        {
            public string Id { get; set; } = string.Empty;
            public string TitleRu { get; set; } = string.Empty;
            public string TitleEn { get; set; } = string.Empty;
            public List<string> Aliases { get; set; } = new();
            public List<Variant> Variants { get; set; } = new();
        }

        private class Variant
        {
            public string Name { get; set; } = string.Empty;
            public double Calories { get; set; }
            public double Protein { get; set; }
            public double Fat { get; set; }
            public double Carbohydrates { get; set; }
        }

        public NutritionDictionaryService(IWebHostEnvironment env, ILogger<NutritionDictionaryService> logger)
        {
            _logger = logger;
            try
            {
                var path = Path.Combine(env.ContentRootPath, "Infrastucture", "Nutrition", "dictionary.json");
                if (!File.Exists(path))
                {
                    _logger.LogWarning("Nutrition dictionary file not found at {Path}", path);
                    return;
                }

                var json = File.ReadAllText(path);
                var items = JsonSerializer.Deserialize<List<Entry>>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (items != null)
                {
                    _entries.Clear();
                    _entries.AddRange(items);
                    _logger.LogInformation("Nutrition dictionary loaded: {Count} entries", _entries.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load nutrition dictionary");
            }
        }

        public NutritionDictionaryMatch? FindBestMatch(string query)
        {
            if (string.IsNullOrWhiteSpace(query) || _entries.Count == 0)
            {
                return null;
            }

            var normalized = Normalize(query);
            NutritionDictionaryMatch? bestMatch = null;
            double bestScore = 0;

            foreach (var entry in _entries)
            {
                foreach (var variant in entry.Variants)
                {
                    foreach (var alias in entry.Aliases
                        .Append(entry.TitleRu)
                        .Append(entry.TitleEn)
                        .Append(variant.Name))
                    {
                        var aliasNormalized = Normalize(alias);
                        
                        // Точное совпадение
                        if (normalized == aliasNormalized)
                        {
                            return new NutritionDictionaryMatch(
                                variant.Name,
                                variant.Calories,
                                variant.Protein,
                                variant.Fat,
                                variant.Carbohydrates);
                        }
                        
                        // Поиск по подстроке (если запрос содержит алиас или наоборот)
                        if (normalized.Contains(aliasNormalized) || aliasNormalized.Contains(normalized))
                        {
                            var substringScore = 0.85;
                            if (substringScore > bestScore)
                            {
                                bestScore = substringScore;
                                bestMatch = new NutritionDictionaryMatch(
                                    variant.Name,
                                    variant.Calories,
                                    variant.Protein,
                                    variant.Fat,
                                    variant.Carbohydrates);
                            }
                        }
                        
                        // Поиск по схожести
                        var score = Similarity(normalized, aliasNormalized);
                        if (score > bestScore)
                        {
                            bestScore = score;
                            bestMatch = new NutritionDictionaryMatch(
                                variant.Name,
                                variant.Calories,
                                variant.Protein,
                                variant.Fat,
                                variant.Carbohydrates);
                        }
                    }
                }
            }

            // Снизили порог для лучшего поиска
            if (bestScore >= 0.65)
            {
                return bestMatch;
            }

            return null;
        }

        public IReadOnlyList<NutritionDictionarySuggestion> Suggest(string query, int limit = 5)
        {
            if (string.IsNullOrWhiteSpace(query) || _entries.Count == 0)
            {
                return Array.Empty<NutritionDictionarySuggestion>();
            }

            var normalized = Normalize(query);
            var results = new List<(double Score, NutritionDictionarySuggestion Suggestion)>();

            foreach (var entry in _entries)
            {
                foreach (var variant in entry.Variants)
                {
                    foreach (var alias in entry.Aliases.Append(entry.TitleRu).Append(entry.TitleEn).Append(variant.Name))
                    {
                        var aliasNormalized = Normalize(alias);
                        double score = 0;
                        
                        // Точное совпадение
                        if (normalized == aliasNormalized)
                        {
                            score = 1.0;
                        }
                        // Поиск по подстроке
                        else if (normalized.Contains(aliasNormalized) || aliasNormalized.Contains(normalized))
                        {
                            score = 0.85;
                        }
                        // Поиск по схожести
                        else
                        {
                            score = Similarity(normalized, aliasNormalized);
                        }
                        
                        if (score < 0.35)
                        {
                            continue;
                        }

                        var suggestion = new NutritionDictionarySuggestion(
                            VariantName: variant.Name,
                            BaseProduct: entry.TitleRu,
                            DisplayName: $"{entry.TitleRu} · {variant.Name}",
                            Query: entry.TitleEn,
                            Calories: variant.Calories,
                            Protein: variant.Protein,
                            Fat: variant.Fat,
                            Carbohydrates: variant.Carbohydrates);

                        results.Add((score, suggestion));
                    }
                }
            }

            return results
                .OrderByDescending(x => x.Score)
                .ThenBy(x => x.Suggestion.DisplayName)
                .Select(x => x.Suggestion)
                .Take(limit)
                .ToList();
        }

        private static string Normalize(string value)
        {
            return MultiSpaceRegex.Replace(value.Trim().ToLowerInvariant(), " ");
        }

        private static double Similarity(string a, string b)
        {
            if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b))
            {
                return 0;
            }

            var distance = LevenshteinDistance(a, b);
            var maxLen = Math.Max(a.Length, b.Length);
            return 1.0 - (double)distance / maxLen;
        }

        private static int LevenshteinDistance(string a, string b)
        {
            var n = a.Length;
            var m = b.Length;
            var d = new int[n + 1, m + 1];

            for (var i = 0; i <= n; i++)
            {
                d[i, 0] = i;
            }

            for (var j = 0; j <= m; j++)
            {
                d[0, j] = j;
            }

            for (var i = 1; i <= n; i++)
            {
                for (var j = 1; j <= m; j++)
                {
                    var cost = a[i - 1] == b[j - 1] ? 0 : 1;
                    d[i, j] = Math.Min(Math.Min(
                            d[i - 1, j] + 1,
                            d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost);
                }
            }

            return d[n, m];
        }
    }
}

