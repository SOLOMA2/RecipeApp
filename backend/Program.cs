using AutoMapper;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RecipeManager.Data;
using RecipeManager.Extensions;
using RecipeManager.Infrastructure;
using RecipeManager.Infrastucture;
using RecipeManager.Infrastucture.UOW;
using RecipeManager.Interfaces.Repositories;
using RecipeManager.Interfaces.Services;
using RecipeManager.Interfaces.UnitOfWork;
using RecipeManager.Mapping;
using RecipeManager.Models;
using RecipeManager.Repositories;
using RecipeManager.Infrastucture.Nutrition;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews()
    .AddJsonOptions(opts =>
    {
        opts.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        opts.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new() { Title = "Recipe API", Version = "v1" });
});

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontendDev", p =>
        p.WithOrigins("http://localhost:5173")
         .AllowAnyHeader()
         .AllowAnyMethod()
         .AllowCredentials());
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddIdentity<AppUser, IdentityRole>(options =>
{
    options.Password.RequireUppercase = false;
    options.Password.RequiredLength = 1;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireDigit = false;
    options.User.RequireUniqueEmail = true;
}).AddEntityFrameworkStores<AppDbContext>();

// Repositories / UnitOfWork
builder.Services.AddScoped<IRecipeRepository, RecipeRepository>();
builder.Services.AddScoped<IIngredientRepository, IngredientRepository>();
builder.Services.AddScoped<ITagRepository, TagRepository>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddApiAuthentication(builder.Configuration);
builder.Services.AddScoped<JwtProvider>();
builder.Services.Configure<NutritionOptions>(builder.Configuration.GetSection("Nutrition"));
builder.Services.AddSingleton<INutritionDictionaryService, NutritionDictionaryService>();

builder.Services.AddHttpClient<INutritionService, NutritionService>((serviceProvider, client) =>
{
    var options = serviceProvider.GetRequiredService<IOptions<NutritionOptions>>().Value;
    var baseUrl = string.IsNullOrWhiteSpace(options.ApiBaseUrl) ? "https://api.api-ninjas.com/v1/" : options.ApiBaseUrl;
    if (!baseUrl.EndsWith("/"))
    {
        baseUrl += "/";
    }
    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
}).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
});

builder.Services.AddAutoMapper(typeof(ApiMappingProfile));

// ApiBehavior / Validation
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var problemDetails = new ValidationProblemDetails(context.ModelState)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Validation errors occurred."
        };
        return new BadRequestObjectResult(problemDetails);
    };
});

var app = builder.Build();

await DataSeeder.SeedRolesAsync(app.Services);
await DataSeeder.SeedCategoriesAsync(app.Services);

using (var scope = app.Services.CreateScope())
{
    var mapper = scope.ServiceProvider.GetService<IMapper>();
    mapper?.ConfigurationProvider.AssertConfigurationIsValid();
    
    // Проверка конфигурации Nutrition API
    var nutritionOptions = scope.ServiceProvider.GetRequiredService<IOptions<RecipeManager.Infrastucture.Nutrition.NutritionOptions>>().Value;
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    if (string.IsNullOrWhiteSpace(nutritionOptions.ApiKey))
    {
        logger.LogWarning("⚠️ Nutrition API key is not configured in appsettings.json. Nutrition lookup will not work.");
        logger.LogWarning("   Please add 'Nutrition:ApiKey' to appsettings.json and restart the application.");
    }
    else
    {
        logger.LogInformation("✓ Nutrition API configured. BaseUrl: {BaseUrl}, ApiKey: {ApiKeyPrefix}...", 
            nutritionOptions.ApiBaseUrl ?? "default", 
            nutritionOptions.ApiKey.Substring(0, Math.Min(10, nutritionOptions.ApiKey.Length)));
    }
}

// Pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Only use HTTPS redirection in production
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseStaticFiles();

app.UseRouting();

app.UseCors("AllowFrontendDev");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
