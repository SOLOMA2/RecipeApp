using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using RecipeManager.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace RecipeManager.Infrastucture
{
    public class JwtProvider
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly IOptions<JwtOptions> _options;

        public JwtProvider(UserManager<AppUser> userManager, IOptions<JwtOptions> options)
        {
            _userManager = userManager;
            _options = options;
        }

        public async Task<string> GenerateTokenAsync(AppUser user)
        {
            var roles = await _userManager.GetRolesAsync(user);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.UserName ?? string.Empty),
                new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty)
            };

            foreach (var role in roles)
                claims.Add(new Claim(ClaimTypes.Role, role));

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Value.SecretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _options.Value.Issuer,
                audience: _options.Value.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(_options.Value.ExpiresHours),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
