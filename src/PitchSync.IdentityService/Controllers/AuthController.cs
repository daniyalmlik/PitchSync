using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PitchSync.IdentityService.Entities;
using PitchSync.IdentityService.Services;
using PitchSync.Shared.DTOs;

namespace PitchSync.IdentityService.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IJwtTokenService _jwtTokenService;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IJwtTokenService jwtTokenService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _jwtTokenService = jwtTokenService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            DisplayName = request.DisplayName,
            FavoriteTeam = request.FavoriteTeam,
        };

        var result = await _userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });

        var roles = await _userManager.GetRolesAsync(user);
        var tokenResponse = _jwtTokenService.GenerateToken(user, roles);

        return Ok(tokenResponse);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null)
            return Unauthorized(new { message = "Invalid credentials." });

        var result = await _signInManager.PasswordSignInAsync(
            user, request.Password, isPersistent: false, lockoutOnFailure: true);

        if (result.IsLockedOut)
            return Unauthorized(new { message = "Account locked. Try again later." });

        if (!result.Succeeded)
            return Unauthorized(new { message = "Invalid credentials." });

        var roles = await _userManager.GetRolesAsync(user);
        var tokenResponse = _jwtTokenService.GenerateToken(user, roles);

        return Ok(tokenResponse);
    }

    [Authorize]
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? User.FindFirstValue("sub");

        if (userId is null)
            return Unauthorized();

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
            return Unauthorized();

        var roles = await _userManager.GetRolesAsync(user);
        var tokenResponse = _jwtTokenService.GenerateToken(user, roles);

        return Ok(tokenResponse);
    }
}
