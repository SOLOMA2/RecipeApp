using RecipeManager.DTOs.Category;

public class CategoryDetailsDto : CategoryListDto
{
    public bool IsDeleted { get; set; }
    public string? RowVersion { get; set; }
}