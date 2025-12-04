using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RecipeManager.Models;

namespace RecipeManager.Configurations
{
    public class AuthorConfiguration : IEntityTypeConfiguration<AppUser>
    {
        public void Configure(EntityTypeBuilder<AppUser> builder)
        {
            builder.Property(a => a.LastLogin)
                   .HasDefaultValueSql("getutcdate()");

            // Можно задать индекс по LastLogin, если нужно:
            builder.HasIndex(a => a.LastLogin);

        }
    }
}
