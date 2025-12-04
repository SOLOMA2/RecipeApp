using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RecipeManager.Models;

namespace RecipeManager.Configurations
{
    public class RecipeConfiguration : IEntityTypeConfiguration<Recipe>
    {
        public void Configure(EntityTypeBuilder<Recipe> builder)
        {
            builder.HasKey(r => r.Id);

            builder.Property(r => r.Title)
                   .IsRequired()
                   .HasMaxLength(200);

            builder.Property(r => r.Description)
                   .HasMaxLength(2000);

            builder.Property(r => r.ImageUrl)
                   .HasMaxLength(1000);

            builder.Property(r => r.Calories)
                   .HasPrecision(8, 2);

            builder.Property(r => r.Weight)
                   .HasPrecision(8, 2);

            builder.Property(r => r.CookingMethod)
                   .HasMaxLength(500);

            builder.Property(r => r.Protein)
                   .HasPrecision(10, 2);

            builder.Property(r => r.Fat)
                   .HasPrecision(10, 2);

            builder.Property(r => r.Carbohydrates)
                   .HasPrecision(10, 2);

            builder.Property(r => r.CreatedAt)
                   .HasDefaultValueSql("getutcdate()");

            builder.Property(r => r.AuthorId)
                   .IsRequired(false);

            builder.Property(r => r.Rating)
                   .HasPrecision(3, 2)
                   .HasDefaultValue(0);

            builder.Property(r => r.RatingCount)
                   .HasDefaultValue(0);

            builder.Property(r => r.LikesCount)
                   .HasDefaultValue(0);

            builder.HasIndex(r => r.Title);
            builder.HasIndex(r => r.AuthorId);
            builder.HasIndex(r => r.CreatedAt);

            builder
                .HasOne(r => r.Author)
                .WithMany(a => a.Recipes)   
                .HasForeignKey(r => r.AuthorId)
                .OnDelete(DeleteBehavior.SetNull); 

            builder
                .HasMany(r => r.Ingredients)
                .WithOne(i => i.Recipe)
                .HasForeignKey(i => i.RecipeId)
                .OnDelete(DeleteBehavior.Cascade);

            builder
                .HasMany(r => r.Tags)
                .WithMany(t => t.Recipes)
                .UsingEntity<Dictionary<string, object>>(
                    "RecipeTags",
                    j => j.HasOne<Tag>().WithMany().HasForeignKey("TagId").OnDelete(DeleteBehavior.Cascade),
                    j => j.HasOne<Recipe>().WithMany().HasForeignKey("RecipeId").OnDelete(DeleteBehavior.Cascade),
                    j =>
                    {
                        j.HasKey("RecipeId", "TagId");
                        j.HasIndex("TagId");
                        j.ToTable("RecipeTags");
                    });

            builder
                .HasMany(r => r.Categories)
                .WithMany(c => c.Recipes)
                .UsingEntity<Dictionary<string, object>>(
                    "RecipeCategories",
                    j => j.HasOne<Category>().WithMany().HasForeignKey("CategoryId").OnDelete(DeleteBehavior.Cascade),
                    j => j.HasOne<Recipe>().WithMany().HasForeignKey("RecipeId").OnDelete(DeleteBehavior.Cascade),
                    j =>
                    {
                        j.HasKey("RecipeId", "CategoryId");
                        j.HasIndex("CategoryId");
                        j.ToTable("RecipeCategories");
                    });
        }
    }
}
