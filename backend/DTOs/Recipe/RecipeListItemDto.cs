public class RecipeListItemDto
{
    public long Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public double Calories { get; set; }
    public DateTime CreatedAt { get; set; }
    public int CookingTimeMinutes { get; set; }

    public double Rating { get; set; }
    public int LikesCount { get; set; }
}