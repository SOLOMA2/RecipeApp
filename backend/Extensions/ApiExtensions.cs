using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using RecipeManager.Infrastucture;
using System.Security.Claims;
using System.Text;

namespace RecipeManager.Extensions
{
    public static class ApiExtensions
    {
        public static IServiceCollection AddApiAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<JwtOptions>(configuration.GetSection("JwtOptions"));
            var jwt = configuration.GetSection("JwtOptions").Get<JwtOptions>() ?? new JwtOptions();

            var key = Encoding.UTF8.GetBytes(jwt.SecretKey);

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false; // Allow HTTP in development
                options.SaveToken = true;
                options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ClockSkew = TimeSpan.FromSeconds(30),
                    RoleClaimType = ClaimTypes.Role

                };

                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        // если Authorization header пустой, попробуем cookie
                        if (string.IsNullOrEmpty(context.Token))
                        {
                            var cookie = context.Request.Cookies["access_token"];
                            if (!string.IsNullOrEmpty(cookie))
                            {
                                context.Token = cookie;
                            }
                        }
                        return Task.CompletedTask;
                    }
                };
            });

            services.AddAuthorization(
                );
            return services;
        }
    }
}
