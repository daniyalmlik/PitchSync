using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using PitchSync.IdentityService.Data;
using PitchSync.MatchService.Data;
using PitchSync.Shared.Configuration;
using PitchSync.Shared.DTOs;
using PitchSync.Shared.Enums;

namespace Integration.Tests;

[Trait("Category", "Integration")]
public sealed class FullFlowTests : IDisposable
{
    private const string TestJwtKey = "integration-test-secret-key-needs-32-chars-min!";
    private const string TestIssuer = "pitchsync-test";
    private const string TestAudience = "pitchsync-test";

    private readonly WebApplicationFactory<PitchSync.IdentityService.Services.JwtTokenService> _identityFactory;
    private readonly WebApplicationFactory<PitchSync.MatchService.Services.MatchRoomService> _matchFactory;
    private readonly HttpClient _identityClient;

    public FullFlowTests()
    {
        var dbId = Guid.NewGuid().ToString();

        _identityFactory = new WebApplicationFactory<PitchSync.IdentityService.Services.JwtTokenService>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Test");
                builder.ConfigureAppConfiguration((_, cfg) =>
                    cfg.AddInMemoryCollection(BuildTestConfig(dbId)));
                builder.ConfigureTestServices(services =>
                {
                    ReplaceDbContext<IdentityDbContext>(services, "IdentityDb-" + dbId);
                    services.PostConfigureAll<JwtConfig>(config =>
                    {
                        config.SecretKey = TestJwtKey;
                        config.Issuer = TestIssuer;
                        config.Audience = TestAudience;
                        config.ExpiryMinutes = 60;
                    });
                });
            });

        _matchFactory = new WebApplicationFactory<PitchSync.MatchService.Services.MatchRoomService>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Test");
                builder.ConfigureAppConfiguration((_, cfg) =>
                    cfg.AddInMemoryCollection(BuildTestConfig(dbId)));
                builder.ConfigureTestServices(services =>
                {
                    ReplaceDbContext<MatchDbContext>(services, "MatchDb-" + dbId);
                    services.Configure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
                    {
                        options.TokenValidationParameters = new TokenValidationParameters
                        {
                            ValidateIssuer = true,
                            ValidateAudience = true,
                            ValidateLifetime = true,
                            ValidateIssuerSigningKey = true,
                            ValidIssuer = TestIssuer,
                            ValidAudience = TestAudience,
                            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestJwtKey)),
                            ClockSkew = TimeSpan.Zero
                        };
                    });
                });
            });

        _identityClient = _identityFactory.CreateClient();
    }

    public void Dispose()
    {
        _identityClient.Dispose();
        _identityFactory.Dispose();
        _matchFactory.Dispose();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static Dictionary<string, string?> BuildTestConfig(string dbId) => new()
    {
        ["JwtSettings:SecretKey"] = TestJwtKey,
        ["JwtSettings:Issuer"] = TestIssuer,
        ["JwtSettings:Audience"] = TestAudience,
        ["JwtSettings:ExpiryMinutes"] = "60",
        ["ConnectionStrings:DefaultConnection"] = $"Server=.;Database=Unused-{dbId}",
    };

    private static void ReplaceDbContext<TContext>(IServiceCollection services, string dbName)
        where TContext : DbContext
    {
        var descriptor = services.SingleOrDefault(
            d => d.ServiceType == typeof(DbContextOptions<TContext>));
        if (descriptor is not null)
            services.Remove(descriptor);

        services.AddDbContext<TContext>(options =>
            options.UseInMemoryDatabase(dbName));
    }

    private async Task<string> RegisterAndGetTokenAsync(string email, string password = "TestPass1!")
    {
        var request = new RegisterRequest(email, password, email.Split('@')[0]);
        var response = await _identityClient.PostAsJsonAsync("/api/auth/register", request);
        response.EnsureSuccessStatusCode();
        var tokenResponse = await response.Content.ReadFromJsonAsync<TokenResponse>();
        return tokenResponse!.Token;
    }

    private HttpClient AuthMatchClient(string token)
    {
        var client = _matchFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    // ── Tests ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Register_ThenCreateRoom_Succeeds()
    {
        var token = await RegisterAndGetTokenAsync("host@test.com");
        var matchClient = AuthMatchClient(token);

        var request = new CreateMatchRequest("Test Match", "Home FC", "Away FC", null, DateTime.UtcNow.AddHours(1));
        var response = await matchClient.PostAsJsonAsync("/api/matches", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var room = await response.Content.ReadFromJsonAsync<MatchRoomResponse>();
        room.Should().NotBeNull();
        room!.Title.Should().Be("Test Match");
    }

    [Fact]
    public async Task MatchEndpoint_WithoutToken_Returns401()
    {
        var response = await _matchFactory.CreateClient().GetAsync($"/api/matches/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task MatchEndpoint_WithValidToken_Returns200OrNotFound()
    {
        var token = await RegisterAndGetTokenAsync("viewer@test.com");
        var matchClient = AuthMatchClient(token);

        var createRequest = new CreateMatchRequest("Public Match", "Home", "Away", null, DateTime.UtcNow.AddHours(1));
        var createResponse = await matchClient.PostAsJsonAsync("/api/matches", createRequest);
        var room = await createResponse.Content.ReadFromJsonAsync<MatchRoomResponse>();

        var response = await matchClient.GetAsync($"/api/matches/{room!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task NonParticipant_PostEvent_Returns403()
    {
        var hostToken = await RegisterAndGetTokenAsync("host2@test.com");
        var hostClient = AuthMatchClient(hostToken);
        var createResponse = await hostClient.PostAsJsonAsync("/api/matches",
            new CreateMatchRequest("Match", "Home", "Away", null, DateTime.UtcNow.AddHours(1)));
        var room = await createResponse.Content.ReadFromJsonAsync<MatchRoomResponse>();

        // outsider has a valid token but never joined the room
        var outsiderToken = await RegisterAndGetTokenAsync("outsider@test.com");
        var outsiderClient = AuthMatchClient(outsiderToken);

        var eventRequest = new PostEventRequest(Minute: 10, EventType: MatchEventType.Comment);
        var response = await outsiderClient.PostAsJsonAsync(
            $"/api/matches/{room!.Id}/events", eventRequest);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task PrivateRoom_JoinWithoutCode_Returns400()
    {
        var hostToken = await RegisterAndGetTokenAsync("host3@test.com");
        var hostClient = AuthMatchClient(hostToken);
        var createResponse = await hostClient.PostAsJsonAsync("/api/matches",
            new CreateMatchRequest("Private Match", "Home", "Away", null, DateTime.UtcNow.AddHours(1), IsPublic: false));
        var room = await createResponse.Content.ReadFromJsonAsync<MatchRoomResponse>();

        var joinerToken = await RegisterAndGetTokenAsync("joiner@test.com");
        var joinerClient = AuthMatchClient(joinerToken);
        var joinResponse = await joinerClient.PostAsJsonAsync(
            $"/api/matches/{room!.Id}/join", new JoinMatchRequest(null));

        joinResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PrivateRoom_JoinWithCorrectCode_Returns200()
    {
        var hostToken = await RegisterAndGetTokenAsync("host4@test.com");
        var hostClient = AuthMatchClient(hostToken);
        var createResponse = await hostClient.PostAsJsonAsync("/api/matches",
            new CreateMatchRequest("Private Room", "Home", "Away", null, DateTime.UtcNow.AddHours(1), IsPublic: false));
        var room = await createResponse.Content.ReadFromJsonAsync<MatchRoomResponse>();

        var joinerToken = await RegisterAndGetTokenAsync("joiner2@test.com");
        var joinerClient = AuthMatchClient(joinerToken);
        var joinResponse = await joinerClient.PostAsJsonAsync(
            $"/api/matches/{room!.Id}/join", new JoinMatchRequest(room.InviteCode));

        joinResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var participant = await joinResponse.Content.ReadFromJsonAsync<ParticipantDto>();
        participant.Should().NotBeNull();
        participant!.Role.Should().Be(RoomRole.Commentator);
    }

    [Fact]
    public async Task Goal_PostThenDelete_ScoreIncrementsThenDecrements()
    {
        var hostToken = await RegisterAndGetTokenAsync("goalhost@test.com");
        var hostClient = AuthMatchClient(hostToken);

        var createResponse = await hostClient.PostAsJsonAsync("/api/matches",
            new CreateMatchRequest("Goal Test", "Home", "Away", null, DateTime.UtcNow.AddHours(1)));
        var room = await createResponse.Content.ReadFromJsonAsync<MatchRoomResponse>();
        var roomId = room!.Id;

        // Post a goal
        var eventResponse = await hostClient.PostAsJsonAsync(
            $"/api/matches/{roomId}/events",
            new PostEventRequest(Minute: 30, EventType: MatchEventType.Goal, Team: "home"));
        eventResponse.EnsureSuccessStatusCode();
        var postedEvent = await eventResponse.Content.ReadFromJsonAsync<MatchEventResponse>();

        // Score should be 1-0
        var roomAfterGoal = await (await hostClient.GetAsync($"/api/matches/{roomId}"))
            .Content.ReadFromJsonAsync<MatchRoomResponse>();
        roomAfterGoal!.HomeScore.Should().Be(1);
        roomAfterGoal.AwayScore.Should().Be(0);

        // Delete the event
        var deleteResponse = await hostClient.DeleteAsync($"/api/matches/{roomId}/events/{postedEvent!.Id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Score should revert to 0-0
        var roomAfterDelete = await (await hostClient.GetAsync($"/api/matches/{roomId}"))
            .Content.ReadFromJsonAsync<MatchRoomResponse>();
        roomAfterDelete!.HomeScore.Should().Be(0);
        roomAfterDelete.AwayScore.Should().Be(0);
    }
}
