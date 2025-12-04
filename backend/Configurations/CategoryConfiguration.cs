using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RecipeManager.Models;

namespace RecipeManager.Configurations
{
    public class CategoryConfiguration : IEntityTypeConfiguration<Category>
    {
        public void Configure(EntityTypeBuilder<Category> builder)
        {
            builder.HasKey(c => c.Id);
            builder.Property(c => c.Name).IsRequired().HasMaxLength(100);
            builder.Property(c => c.Description).HasMaxLength(500);

            builder.Property(c => c.NormalizedName).HasComputedColumnSql("UPPER([Name]) PERSISTED");
            builder.HasIndex(c => c.NormalizedName).IsUnique();

            builder.HasQueryFilter(c => !c.IsDeleted);

            builder.Property(c => c.RowVersion).IsRowVersion();

            builder.HasIndex(c => c.Name);
        }
    }
}