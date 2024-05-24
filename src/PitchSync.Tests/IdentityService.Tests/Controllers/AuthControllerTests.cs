using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Moq;
using PitchSync.IdentityService.Controllers;
using PitchSync.IdentityService.Entities;
using PitchSync.IdentityService.Services;
using PitchSync.Shared.DTOs;

namespace IdentityService.Tests.Controllers;

[Trait("Category", "Unit")]
public sealed class AuthControllerTests
{
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly Mock<SignInManager<ApplicationUser>> _signInManagerMock;
    private readonly Mock<IJwtTokenService> _jwtServiceMock;
    private readonly AuthController _sut;

    public AuthControllerTests()
    {
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(
            Mock.Of<IUserStore<ApplicationUser>>(),
            null!, null!, null!, null!, null!, null!, null!, null!);

        _signInManagerMock = new Mock<SignInManager<ApplicationUser>>(
            _userManagerMock.Object,
            Mock.Of<Microsoft.AspNetCore.Http.IHttpContextAccessor>(),
            Mock.Of<IUserClaimsPrincipalFactory<ApplicationUser>>(),
            null!, null!, null!, null!);

        _jwtServiceMock = new Mock<IJwtTokenService>();

        _sut = new AuthController(
            _userManagerMock.Object,
            _signInManagerMock.Object,
            _jwtServiceMock.Object);
    }

    private static TokenResponse MakeTokenResponse() => new(
        Token: "fake.jwt.token",
        ExpiresAt: DateTime.UtcNow.AddHours(8),
        User: new UserInfo("user-id", "alice@example.com", "Alice", null, null));

    // ── Register ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Register_ReturnsOkWithToken_WhenSuccessful()
    {
        var request = new RegisterRequest("alice@example.com", "Password1!", "Alice", null);
        var tokenResponse = MakeTokenResponse();

        _userManagerMock.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), request.Password))
            .ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(m => m.GetRolesAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync([]);
        _jwtServiceMock.Setup(m => m.GenerateToken(It.IsAny<ApplicationUser>(), It.IsAny<IList<string>>()))
            .Returns(tokenResponse);

        var result = await _sut.Register(request);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().Be(tokenResponse);
    }

    [Fact]
    public async Task Register_ReturnsBadRequest_WhenIdentityFails()
    {
        var request = new RegisterRequest("alice@example.com", "weak", "Alice", null);
        var error = new IdentityError { Code = "PasswordTooShort", Description = "Password too short." };

        _userManagerMock.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), request.Password))
            .ReturnsAsync(IdentityResult.Failed(error));

        var result = await _sut.Register(request);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    // ── Login ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Login_ReturnsOkWithToken_WhenCredentialsValid()
    {
        var request = new LoginRequest("alice@example.com", "Password1!");
        var user = new ApplicationUser { Id = "user-id", Email = request.Email, DisplayName = "Alice" };
        var tokenResponse = MakeTokenResponse();

        _userManagerMock.Setup(m => m.FindByEmailAsync(request.Email)).ReturnsAsync(user);
        _signInManagerMock.Setup(m => m.PasswordSignInAsync(user, request.Password, false, true))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);
        _userManagerMock.Setup(m => m.GetRolesAsync(user)).ReturnsAsync([]);
        _jwtServiceMock.Setup(m => m.GenerateToken(user, It.IsAny<IList<string>>()))
            .Returns(tokenResponse);

        var result = await _sut.Login(request);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().Be(tokenResponse);
    }

    [Fact]
    public async Task Login_ReturnsUnauthorized_WhenUserNotFound()
    {
        var request = new LoginRequest("ghost@example.com", "Password1!");
        _userManagerMock.Setup(m => m.FindByEmailAsync(request.Email))
            .ReturnsAsync((ApplicationUser?)null);

        var result = await _sut.Login(request);

        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task Login_ReturnsUnauthorized_WhenPasswordInvalid()
    {
        var request = new LoginRequest("alice@example.com", "WrongPassword!");
        var user = new ApplicationUser { Id = "user-id", Email = request.Email, DisplayName = "Alice" };

        _userManagerMock.Setup(m => m.FindByEmailAsync(request.Email)).ReturnsAsync(user);
        _signInManagerMock.Setup(m => m.PasswordSignInAsync(user, request.Password, false, true))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Failed);

        var result = await _sut.Login(request);

        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task Login_ReturnsUnauthorized_WhenAccountLockedOut()
    {
        var request = new LoginRequest("alice@example.com", "Password1!");
        var user = new ApplicationUser { Id = "user-id", Email = request.Email, DisplayName = "Alice" };

        _userManagerMock.Setup(m => m.FindByEmailAsync(request.Email)).ReturnsAsync(user);
        _signInManagerMock.Setup(m => m.PasswordSignInAsync(user, request.Password, false, true))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.LockedOut);

        var result = await _sut.Login(request);

        result.Should().BeOfType<UnauthorizedObjectResult>();
    }
}
