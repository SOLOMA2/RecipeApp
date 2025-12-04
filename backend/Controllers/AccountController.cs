using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using RecipeManager.Infrastucture;
using RecipeManager.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace RecipeManager.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountController : ControllerBase
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly JwtProvider _jwtProvider;
        private readonly IOptions<JwtOptions> _jwtOptions;

        public AccountController(UserManager<AppUser> userManager, JwtProvider jwtProvider, IOptions<JwtOptions> jwtOptions)
        {
            _userManager = userManager;
            _jwtProvider = jwtProvider;
            _jwtOptions = jwtOptions;
        }

        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var existing = await _userManager.FindByEmailAsync(dto.Email);
            if (existing != null)
                return Conflict(new { message = "User with this email already exists." });

            var user = new AppUser
            {
                UserName = string.IsNullOrWhiteSpace(dto.Name) ? dto.Email : dto.Name,
                Email = dto.Email,
                EmailConfirmed = true
            };

            var createResult = await _userManager.CreateAsync(user, dto.Password);
            if (!createResult.Succeeded)
            {
                foreach (var err in createResult.Errors)
                    ModelState.AddModelError(err.Code ?? string.Empty, err.Description);

                return ValidationProblem(ModelState);
            }

            if (!await _userManager.IsInRoleAsync(user, "Creator"))
            {
                await _userManager.AddToRoleAsync(user, "Creator");
            }

            var token = await _jwtProvider.GenerateTokenAsync(user);
            var expires = DateTime.UtcNow.AddHours(_jwtOptions.Value.ExpiresHours);
            AppendAccessTokenCookie(token, expires);

            var roles = await _userManager.GetRolesAsync(user);
            return CreatedAtAction(nameof(Register), new { id = user.Id }, new { id = user.Id, userName = user.UserName, email = user.Email, roles });
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null) return Unauthorized(new { message = "Invalid credentials" });

            var passwordValid = await _userManager.CheckPasswordAsync(user, dto.Password);
            if (!passwordValid) return Unauthorized(new { message = "Invalid credentials" });

            var token = await _jwtProvider.GenerateTokenAsync(user);
            var expires = DateTime.UtcNow.AddHours(_jwtOptions.Value.ExpiresHours);
            AppendAccessTokenCookie(token, expires);

            var roles = await _userManager.GetRolesAsync(user);
            return Ok(new { id = user.Id, userName = user.UserName, email = user.Email, roles });
        }

        /// <summary>
        /// Get current authenticated user information.
        /// </summary>
        [HttpGet("me")]
        public async Task<IActionResult> Me()
        {
            if (!User.Identity?.IsAuthenticated ?? false) return Unauthorized();

            var userId = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier || c.Type == System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return Unauthorized();

            var roles = await _userManager.GetRolesAsync(user);
            return Ok(new { id = user.Id, userName = user.UserName, email = user.Email, roles });
        }

        /// <summary>
        /// Logout current user by removing authentication cookies.
        /// </summary>
        [HttpPost("logout")]
        public IActionResult Logout()
        {
            Response.Cookies.Delete("access_token");
            Response.Cookies.Delete("refresh_token");
            return Ok(new { message = "Logged out" });
        }

        private void AppendAccessTokenCookie(string token, DateTime expiresUtc)
        {
            var cookieOptions = new Microsoft.AspNetCore.Http.CookieOptions
            {
                HttpOnly = true,
                Secure = false,
                SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax,
                Expires = expiresUtc
            };
            Response.Cookies.Append("access_token", token, cookieOptions);
        }
    }
}
