using AutoMapper;
using RecipeManager.DTOs.Category;
using RecipeManager.DTOs.Nutrition;
using RecipeManager.Models;
using System;
using System.Linq;

namespace RecipeManager.Mapping
{
    public class ApiMappingProfile : Profile
    {
        public ApiMappingProfile()
        {

            CreateMap<Category, CategoryListDto>().ReverseMap();

            // Details: entity -> dto (RowVersion -> base64 string)
            CreateMap<Category, CategoryDetailsDto>()
                .ForMember(d => d.RowVersion,
                           opt => opt.MapFrom(s => s.RowVersion != null ? Convert.ToBase64String(s.RowVersion) : null));

            // Create DTO -> entity: ignore navs and service fields
            CreateMap<CreateCategoryDto, Category>()
                .ForMember(d => d.Id, opt => opt.Ignore())
                .ForMember(d => d.IsDeleted, opt => opt.Ignore())
                .ForMember(d => d.RowVersion, opt => opt.Ignore())
                .ForMember(d => d.NormalizedName, opt => opt.Ignore())
                .ForMember(d => d.Recipes, opt => opt.Ignore());

            // Update DTO -> entity: handle RowVersion conversion if provided, ignore navs/service fields
            CreateMap<UpdateCategoryDto, Category>()
                .ForMember(d => d.Id, opt => opt.MapFrom(s => s.Id))
                .ForMember(d => d.RowVersion, opt =>
                {
                    opt.PreCondition(src => !string.IsNullOrWhiteSpace(src.RowVersion));
                    opt.MapFrom(src => Convert.FromBase64String(src.RowVersion!));
                })
                .ForMember(d => d.NormalizedName, opt => opt.Ignore())
                .ForMember(d => d.Recipes, opt => opt.Ignore())
                .ForMember(d => d.IsDeleted, opt => opt.Ignore());

            CreateMap<Tag, TagDto>().ReverseMap();

            CreateMap<CreateTagDto, Tag>()
                .ForMember(d => d.Id, opt => opt.Ignore())
                .ForMember(d => d.RowVersion, opt => opt.Ignore())
                .ForMember(d => d.NormalizedTitle, opt => opt.Ignore())
                .ForMember(d => d.IsDeleted, opt => opt.Ignore())
                .ForMember(d => d.Recipes, opt => opt.Ignore());

            CreateMap<UpdateTagDto, Tag>()
                .ForMember(d => d.Id, opt => opt.MapFrom(s => s.Id))
                .ForMember(d => d.RowVersion, opt =>
                {
                    opt.PreCondition(src => !string.IsNullOrWhiteSpace(src.RowVersion));
                    opt.MapFrom(src => Convert.FromBase64String(src.RowVersion!));
                })
                .ForMember(d => d.Recipes, opt => opt.Ignore())
                .ForMember(d => d.NormalizedTitle, opt => opt.Ignore())
                .ForMember(d => d.IsDeleted, opt => opt.Ignore());

            // TagRefDto -> Tag (used when client sends short tag references)
            CreateMap<TagRefDto, Tag>()
                .ConstructUsing(src => new Tag { Title = src.Title ?? string.Empty })
                .ForMember(d => d.Id, opt => opt.Ignore())
                .ForMember(d => d.Recipes, opt => opt.Ignore())
                .ForMember(d => d.NormalizedTitle, opt => opt.Ignore())
                .ForMember(d => d.IsDeleted, opt => opt.Ignore())
                .ForMember(d => d.RowVersion, opt => opt.Ignore());


            CreateMap<Ingredient, IngredientDto>()
                .ForMember(d => d.Unit, opt => opt.MapFrom(s => s.Unit))
                .ReverseMap();            // Create / Update ingredient DTO -> entity: ignore Recipe navigation
            CreateMap<CreateIngredientDto, Ingredient>()
                .ForMember(d => d.Id, opt => opt.Ignore())
                .ForMember(d => d.Recipe, opt => opt.Ignore());

            CreateMap<UpdateIngredientDto, Ingredient>()
                .ForMember(d => d.Id, opt => opt.MapFrom(s => s.Id))
                .ForMember(d => d.Recipe, opt => opt.Ignore());

            CreateMap<Recipe, RecipeListItemDto>();

            CreateMap<Recipe, NutritionSummaryDto>()
                .ForMember(d => d.Calories, opt => opt.MapFrom(s => s.Calories))
                .ForMember(d => d.Protein, opt => opt.MapFrom(s => s.Protein))
                .ForMember(d => d.Fat, opt => opt.MapFrom(s => s.Fat))
                .ForMember(d => d.Carbohydrates, opt => opt.MapFrom(s => s.Carbohydrates));

            CreateMap<Recipe, RecipeDetailsDto>()
                .ForMember(d => d.Ingredients, opt => opt.MapFrom(s => s.Ingredients))
                .ForMember(d => d.Tags, opt => opt.MapFrom(s => s.Tags))
                .ForMember(d => d.Categories, opt => opt.MapFrom(s => s.Categories))
                .ForMember(d => d.NutritionPerRecipe, opt => opt.MapFrom(s => s))
                .ForMember(d => d.NutritionPer100g, opt => opt.Ignore())
                .AfterMap((src, dest) =>
                {
                    var weight = src.Weight > 0 ? src.Weight : 100;
                    var factor = weight > 0 ? 100 / weight : 0;
                    dest.NutritionPer100g = new NutritionSummaryDto
                    {
                        Calories = Math.Round(src.Calories * factor, 2),
                        Protein = Math.Round(src.Protein * factor, 2),
                        Fat = Math.Round(src.Fat * factor, 2),
                        Carbohydrates = Math.Round(src.Carbohydrates * factor, 2)
                    };
                });

            // Create recipe: ignore server-controlled fields and nav sync (tags/categories)
            CreateMap<CreateRecipeDto, Recipe>()
                .ForMember(d => d.Id, opt => opt.Ignore())
                .ForMember(d => d.Tags, opt => opt.Ignore())         // sync tags in service/repo
                .ForMember(d => d.Categories, opt => opt.Ignore())   // sync categories in service/repo
                .ForMember(d => d.Ingredients, opt => opt.MapFrom(s => s.Ingredients ?? Enumerable.Empty<CreateIngredientDto>()))
                .ForMember(d => d.CreatedAt, opt => opt.Ignore())    // server sets CreatedAt
                .ForMember(d => d.Author, opt => opt.Ignore())
                .ForMember(d => d.Rating, opt => opt.Ignore())
                .ForMember(d => d.RatingCount, opt => opt.Ignore())
                .ForMember(d => d.LikesCount, opt => opt.Ignore());

            CreateMap<UpdateRecipeDto, Recipe>()
                .ForMember(d => d.Tags, opt => opt.Ignore())
                .ForMember(d => d.Categories, opt => opt.Ignore())
                .ForMember(d => d.Ingredients, opt => opt.Ignore())
                .ForMember(d => d.Author, opt => opt.Ignore())
                .ForMember(d => d.CreatedAt, opt => opt.Ignore())
                .ForMember(d => d.Rating, opt => opt.Ignore())
                .ForMember(d => d.RatingCount, opt => opt.Ignore())
                .ForMember(d => d.LikesCount, opt => opt.Ignore());

        }
    }
}
