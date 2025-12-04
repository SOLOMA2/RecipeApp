using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RecipeManager.Models;

namespace RecipeManager.Configurations
{
    public class IngredientConfiguration : IEntityTypeConfiguration<Ingredient>
    {
        public void Configure(EntityTypeBuilder<Ingredient> builder)
        {
            builder.HasKey(i => i.Id);

            builder.Property(i => i.Title)
                   .IsRequired()
                   .HasMaxLength(200);

            builder.Property(i => i.Description)
                   .HasMaxLength(1000);

            builder.Property(i => i.Quantity)
                   .HasPrecision(10, 3);

            builder.Property(i => i.Weight)
                   .HasPrecision(8, 2);

            builder.Property(i => i.Calories)
                   .HasPrecision(8, 2);

            builder.Property(i => i.Protein)
                   .HasPrecision(8, 2);

            builder.Property(i => i.Fat)
                   .HasPrecision(8, 2);

            builder.Property(i => i.Carbohydrates)
                   .HasPrecision(8, 2);

            builder
                .HasOne(i => i.Recipe)
                .WithMany(r => r.Ingredients)
                .HasForeignKey(i => i.RecipeId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(i => i.RecipeId);
        }
    }
}
