using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using PitchSync.IdentityService.Controllers;
using PitchSync.IdentityService.Data;
using PitchSync.IdentityService.Entities;
using PitchSync.Shared.DTOs;

namespace IdentityService.Tests.Controllers;

[Trait("Category", "Unit")]
public sealed class UsersControllerTests : IDisposable
{
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly IdentityDbContext _db;
    private readonly UsersController _sut;

    public UsersControllerTests()
    {
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(
            Mock.Of<IUserStore<ApplicationUser>>(),
            null!, null!, null!, null!, null!, null!, null!, null!);

        var dbOptions = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new IdentityDbContext(dbOptions);

        _sut = new UsersController(_userManagerMock.Object, _db);
    }

    public void Dispose() => _db.Dispose();

    private static ClaimsPrincipal MakePrincipal(
        string userId = "user-id",
        string email = "alice@example.com",
        string displayName = "Alice",
        string? favoriteTeam = null)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Email, email),
            new("display_name", displayName),
        };

        if (favoriteTeam is not null)
            claims.Add(new Claim("favorite_team", favoriteTeam));

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
    }

    private void SetUser(ClaimsPrincipal principal)
    {
        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
    }

    // ── GET /me ──────────────────────────────────────────────────────────────

    [Fact]
    public void GetMe_ReturnsUserInfoFromClaims()
    {
        SetUser(MakePrincipal(favoriteTeam: "Barcelona"));

        var result = _sut.GetMe();

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var info = ok.Value.Should().BeOfType<UserInfo>().Subject;
        info.Id.Should().Be("user-id");
        info.Email.Should().Be("alice@example.com");
        info.DisplayName.Should().Be("Alice");
        info.FavoriteTeam.Should().Be("Barcelona");
    }

    [Fact]
    public void GetMe_FavoriteTeamIsNull_WhenClaimAbsent()
    {
        SetUser(MakePrincipal());

        var result = _sut.GetMe();

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var info = ok.Value.Should().BeOfType<UserInfo>().Subject;
        info.FavoriteTeam.Should().BeNull();
    }

    // ── PUT /me ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateMe_ReturnsOkWithUpdatedInfo_WhenSuccessful()
    {
        var user = new ApplicationUser
        {
            Id = "user-id", Email = "alice@example.com",
            DisplayName = "Alice", FavoriteTeam = null, AvatarUrl = null
        };
        SetUser(MakePrincipal());
        _userManagerMock.Setup(m => m.FindByIdAsync("user-id")).ReturnsAsync(user);
        _userManagerMock.Setup(m => m.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);

        var request = new UpdateProfileRequest("Alice Updated", "Real Madrid", null);
        var result = await _sut.UpdateMe(request);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var info = ok.Value.Should().BeOfType<UserInfo>().Subject;
        info.DisplayName.Should().Be("Alice Updated");
        info.FavoriteTeam.Should().Be("Real Madrid");
    }

    [Fact]
    public async Task UpdateMe_ReturnsNotFound_WhenUserDoesNotExist()
    {
        SetUser(MakePrincipal());
        _userManagerMock.Setup(m => m.FindByIdAsync("user-id"))
            .ReturnsAsync((ApplicationUser?)null);

        var result = await _sut.UpdateMe(new UpdateProfileRequest("X", null, null));

        result.Should().BeOfType<NotFoundResult>();
    }

    // ── GET /{id} ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetById_ReturnsUserInfo_WhenUserExists()
    {
        var user = new ApplicationUser
        {
            Id = "other-id", Email = "bob@example.com",
            DisplayName = "Bob", FavoriteTeam = null, AvatarUrl = null
        };
        SetUser(MakePrincipal());
        _userManagerMock.Setup(m => m.FindByIdAsync("other-id")).ReturnsAsync(user);

        var result = await _sut.GetById("other-id");

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var info = ok.Value.Should().BeOfType<UserInfo>().Subject;
        info.Id.Should().Be("other-id");
        info.DisplayName.Should().Be("Bob");
    }

    [Fact]
    public async Task GetById_ReturnsNotFound_WhenUserMissing()
    {
        SetUser(MakePrincipal());
        _userManagerMock.Setup(m => m.FindByIdAsync("missing")).ReturnsAsync((ApplicationUser?)null);

        var result = await _sut.GetById("missing");

        result.Should().BeOfType<NotFoundResult>();
    }

    // ── GET /search ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Search_ReturnsBadRequest_WhenQueryBlank()
    {
        SetUser(MakePrincipal());

        var result = await _sut.Search("   ");

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Search_ReturnsMatchingUsers_ByDisplayName()
    {
        _db.Users.AddRange(
            new ApplicationUser { Id = "1", UserName = "a@x.com", Email = "a@x.com", DisplayName = "AlphaUser", NormalizedEmail = "A@X.COM", NormalizedUserName = "A@X.COM" },
            new ApplicationUser { Id = "2", UserName = "b@x.com", Email = "b@x.com", DisplayName = "BetaUser", NormalizedEmail = "B@X.COM", NormalizedUserName = "B@X.COM" });
        await _db.SaveChangesAsync();
        SetUser(MakePrincipal());

        var result = await _sut.Search("alpha");

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var list = ok.Value.Should().BeAssignableTo<IEnumerable<UserInfo>>().Subject;
        list.Should().HaveCount(1).And.Contain(u => u.DisplayName == "AlphaUser");
    }

    [Fact]
    public async Task Search_ReturnsMatchingUsers_ByEmail()
    {
        _db.Users.AddRange(
            new ApplicationUser { Id = "3", UserName = "charlie@example.com", Email = "charlie@example.com", DisplayName = "Charlie", NormalizedEmail = "CHARLIE@EXAMPLE.COM", NormalizedUserName = "CHARLIE@EXAMPLE.COM" },
            new ApplicationUser { Id = "4", UserName = "diana@other.com", Email = "diana@other.com", DisplayName = "Diana", NormalizedEmail = "DIANA@OTHER.COM", NormalizedUserName = "DIANA@OTHER.COM" });
        await _db.SaveChangesAsync();
        SetUser(MakePrincipal());

        var result = await _sut.Search("charlie@example");

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var list = ok.Value.Should().BeAssignableTo<IEnumerable<UserInfo>>().Subject;
        list.Should().HaveCount(1).And.Contain(u => u.Email == "charlie@example.com");
    }

    [Fact]
    public async Task Search_LimitsResultsToTwenty()
    {
        for (var i = 0; i < 25; i++)
        {
            _db.Users.Add(new ApplicationUser
            {
                Id = i.ToString(),
                UserName = $"user{i}@example.com",
                Email = $"user{i}@example.com",
                DisplayName = $"SearchUser{i}",
                NormalizedEmail = $"USER{i}@EXAMPLE.COM",
                NormalizedUserName = $"USER{i}@EXAMPLE.COM"
            });
        }
        await _db.SaveChangesAsync();
        SetUser(MakePrincipal());

        var result = await _sut.Search("SearchUser");

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var list = ok.Value.Should().BeAssignableTo<IEnumerable<UserInfo>>().Subject;
        list.Should().HaveCount(20);
    }
}
