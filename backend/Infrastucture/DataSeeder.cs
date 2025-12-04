using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RecipeManager.Data;
using RecipeManager.Models;

namespace RecipeManager.Infrastructure
{
    public static class DataSeeder
    {
        private static readonly string[] DefaultRoles = new[] { "Guest", "Creator", "Admin" };

        public static async Task SeedRolesAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var provider = scope.ServiceProvider;

            var logger = provider.GetService<ILoggerFactory>()?.CreateLogger("DataSeeder");
            try
            {
                var roleManager = provider.GetRequiredService<RoleManager<IdentityRole>>();
                var userManager = provider.GetRequiredService<UserManager<AppUser>>();
                var config = provider.GetRequiredService<IConfiguration>();

                foreach (var r in DefaultRoles)
                {
                    if (!await roleManager.RoleExistsAsync(r))
                    {
                        var createResult = await roleManager.CreateAsync(new IdentityRole(r));
                        if (!createResult.Succeeded)
                        {
                            logger?.LogWarning("Failed to create role {Role}: {Errors}", r, string.Join(",", createResult.Errors.Select(x => x.Description)));
                        }
                        else
                        {
                            logger?.LogInformation("Created role {Role}", r);
                        }
                    }
                }

                var adminEmail = config["Data:Email"];
                var adminPassword = config["Data:Password"];
                var adminUserName = config["Data:UserName"] ?? adminEmail;
                var anyUsers = userManager.Users.Any();

                if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
                {
                    logger?.LogInformation("Admin credentials not provided via configuration.");
                    if (!anyUsers)
                    {
                        logger?.LogWarning("Database has no users and Admin credentials are missing. Consider providing Admin:Email/Admin:Password via secrets or env.");
                    }
                    return;
                }

                var existing = await userManager.FindByEmailAsync(adminEmail);
                if (existing == null)
                {
                    var admin = new AppUser
                    {
                        Email = adminEmail,
                        UserName = adminUserName,
                        EmailConfirmed = true,
                    };

                    var createAdminResult = await userManager.CreateAsync(admin, adminPassword);
                    if (!createAdminResult.Succeeded)
                    {
                        logger?.LogError("Failed to create admin user {Email}: {Errors}", adminEmail, string.Join(",", createAdminResult.Errors.Select(x => x.Description)));
                        return;
                    }

                    var addRoleResult = await userManager.AddToRoleAsync(admin, "Admin");
                    if (!addRoleResult.Succeeded)
                    {
                        logger?.LogError("Failed to add Admin role to {Email}: {Errors}", adminEmail, string.Join(",", addRoleResult.Errors.Select(x => x.Description)));
                    }
                    else
                    {
                        logger?.LogInformation("Admin user {Email} created and added to Admin role.", adminEmail);
                    }
                }
                else
                {
                    if (!await userManager.IsInRoleAsync(existing, "Admin"))
                    {
                        var addRoleResult = await userManager.AddToRoleAsync(existing, "Admin");
                        if (addRoleResult.Succeeded)
                            logger?.LogInformation("Existing user {Email} added to Admin role.", adminEmail);
                        else
                            logger?.LogError("Failed to add Admin role to existing user {Email}: {Errors}", adminEmail, string.Join(",", addRoleResult.Errors.Select(x => x.Description)));
                    }
                    else
                    {
                        logger?.LogInformation("User {Email} already exists and is in Admin role.", adminEmail);
                    }
                }
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Unexpected error during data seeding.");
                throw;
            }
        }

        /// <summary>
        /// Первичное наполнение БД базовыми категориями,
        /// чтобы они сразу отображались в форме создания рецепта.
        /// </summary>
        public static async Task SeedCategoriesAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var provider = scope.ServiceProvider;

            var logger = provider.GetService<ILoggerFactory>()?.CreateLogger("DataSeeder");

            try
            {
                var db = provider.GetRequiredService<AppDbContext>();

                if (db.Categories.Any())
                {
                    logger?.LogInformation("Categories already exist, seeding skipped.");
                    return;
                }

                var categories = new[]
                {
                    new Category { Name = "Завтраки", Description = "Быстрые и сытные варианты на утро" },
                    new Category { Name = "Основные блюда", Description = "Горячие блюда на каждый день" },
                    new Category { Name = "Салаты", Description = "Лёгкие и витаминные салаты" },
                    new Category { Name = "Десерты", Description = "Сладкие блюда и выпечка" },
                    new Category { Name = "Закуски", Description = "Небольшие блюда к столу" }
                };

                await db.Categories.AddRangeAsync(categories);
                await db.SaveChangesAsync();

                logger?.LogInformation("Seeded default categories into database.");
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Error while seeding categories.");
                throw;
            }
        }
    }
}
