using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RecipeManager.Models;

namespace RecipeManager.Configurations
{
    public class TagConfiguration : IEntityTypeConfiguration<Tag>
    {
        public void Configure(EntityTypeBuilder<Tag> builder)
        {
            builder.HasKey(t => t.Id);

            builder.Property(t => t.Title)
                   .IsRequired()
                   .HasMaxLength(100);

            builder.Property(t => t.NormalizedTitle)
                   .HasMaxLength(100)
                   .ValueGeneratedOnAddOrUpdate()
                   .HasComputedColumnSql("UPPER([Title]) PERSISTED");  

            builder.HasIndex(t => t.NormalizedTitle).IsUnique();  

            builder.HasQueryFilter(t => !t.IsDeleted);

            builder.Property(t => t.RowVersion).IsRowVersion();

            builder.HasIndex(t => t.Title);
        }
    }
}