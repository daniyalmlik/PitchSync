using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PitchSync.IdentityService.Data;
using PitchSync.IdentityService.Entities;
using PitchSync.Shared.DTOs;
using System.Security.Claims;

namespace PitchSync.IdentityService.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public sealed class UsersController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IdentityDbContext _db;

    public UsersController(UserManager<ApplicationUser> userManager, IdentityDbContext db)
    {
        _userManager = userManager;
        _db = db;
    }

    [HttpGet("me")]
    public IActionResult GetMe()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var email = User.FindFirstValue(ClaimTypes.Email);
        var displayName = User.FindFirstValue("display_name");
        var favoriteTeam = User.FindFirstValue("favorite_team");

        if (userId is null || email is null || displayName is null)
            return Unauthorized();

        return Ok(new UserInfo(userId, email, displayName, favoriteTeam, AvatarUrl: null));
    }

    [HttpPut("me")]
    public async Task<IActionResult> UpdateMe([FromBody] UpdateProfileRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Unauthorized();

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
            return NotFound();

        if (request.DisplayName is not null)
            user.DisplayName = request.DisplayName;
        user.FavoriteTeam = request.FavoriteTeam;
        user.AvatarUrl = request.AvatarUrl;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
            return BadRequest(result.Errors.Select(e => e.Description));

        return Ok(new UserInfo(user.Id, user.Email!, user.DisplayName, user.FavoriteTeam, user.AvatarUrl));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
            return NotFound();

        return Ok(new UserInfo(user.Id, user.Email!, user.DisplayName, user.FavoriteTeam, user.AvatarUrl));
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string q)
    {
        if (string.IsNullOrWhiteSpace(q))
            return BadRequest("Query parameter 'q' is required.");

        var lower = q.ToLower();

        var users = await _db.Users
            .Where(u => u.DisplayName.ToLower().Contains(lower) || u.Email!.ToLower().Contains(lower))
            .Take(20)
            .Select(u => new UserInfo(u.Id, u.Email!, u.DisplayName, u.FavoriteTeam, u.AvatarUrl))
            .ToListAsync();

        return Ok(users);
    }
}
