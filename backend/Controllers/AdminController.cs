using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using RecipeManager.Models;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

[Authorize(Roles = "Admin")]
[ApiController]
[Route("api/admin")]
public class AdminController : ControllerBase
{
    private readonly UserManager<AppUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public AdminController(UserManager<AppUser> userManager, RoleManager<IdentityRole> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    [HttpGet("stats")]
    public ActionResult GetStats()
    {
        var usersCount = _userManager.Users.Count();
        return Ok(new { users = usersCount, serverTime = System.DateTime.UtcNow });
    }

    [HttpGet("users")]
    public async Task<ActionResult<IEnumerable<object>>> GetUsers()
    {
        var users = _userManager.Users
            .Select(u => new { u.Id, u.UserName, u.Email })
            .ToList();

        var result = new List<object>();
        foreach (var u in users)
        {
            var appUser = await _userManager.FindByIdAsync(u.Id);
            if (appUser == null) continue;
            
            var roles = await _userManager.GetRolesAsync(appUser);
            result.Add(new { u.Id, u.UserName, u.Email, roles });
        }

        return Ok(result);
    }

    public class RoleChangeDto { public string UserId { get; set; } public string Role { get; set; } }

    [HttpPost("users/add-role")]
    public async Task<IActionResult> AddRole([FromBody] RoleChangeDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto?.UserId) || string.IsNullOrWhiteSpace(dto.Role))
            return BadRequest("UserId and Role required");

        if (!await _roleManager.RoleExistsAsync(dto.Role))
            return BadRequest("Role not found");

        var user = await _userManager.FindByIdAsync(dto.UserId);
        if (user == null) return NotFound();

        if (await _userManager.IsInRoleAsync(user, dto.Role))
            return Conflict("User already in role");

        var res = await _userManager.AddToRoleAsync(user, dto.Role);
        if (!res.Succeeded) return StatusCode(500, res.Errors.Select(e => e.Description));
        return Ok();
    }

    [HttpPost("users/remove-role")]
    public async Task<IActionResult> RemoveRole([FromBody] RoleChangeDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto?.UserId) || string.IsNullOrWhiteSpace(dto.Role))
            return BadRequest("UserId and Role required");

        var user = await _userManager.FindByIdAsync(dto.UserId);
        if (user == null) return NotFound();

        var res = await _userManager.RemoveFromRoleAsync(user, dto.Role);
        if (!res.Succeeded) return StatusCode(500, res.Errors.Select(e => e.Description));
        return Ok();
    }

    [HttpDelete("users/{id}")]
    public async Task<IActionResult> DeleteUser(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();
        var res = await _userManager.DeleteAsync(user);
        if (!res.Succeeded) return StatusCode(500, res.Errors.Select(e => e.Description));
        return Ok();
    }
}
