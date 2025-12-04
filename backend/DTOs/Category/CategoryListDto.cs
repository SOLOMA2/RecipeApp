namespace RecipeManager.DTOs.Category
{
    public class CategoryListDto
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
    }
}
