using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;
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
public sealed class SignalRIntegrationTests : IDisposable
{
    private const string TestJwtKey = "integration-test-secret-key-needs-32-chars-min!";
    private const string TestIssuer = "pitchsync-test";
    private const string TestAudience = "pitchsync-test";

    private readonly WebApplicationFactory<PitchSync.IdentityService.Services.JwtTokenService> _identityFactory;
    private readonly WebApplicationFactory<PitchSync.MatchService.Services.MatchRoomService> _matchFactory;
    private readonly HttpClient _identityClient;

    public SignalRIntegrationTests()
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
                        options.Events = new JwtBearerEvents
                        {
                            OnMessageReceived = context =>
                            {
                                var accessToken = context.Request.Query["access_token"];
                                if (!string.IsNullOrEmpty(accessToken) &&
                                    context.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                                    context.Token = accessToken;
                                return Task.CompletedTask;
                            }
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

    private HubConnection BuildHubConnection(Guid roomId, string token)
    {
        var hubUrl = new Uri(_matchFactory.Server.BaseAddress, $"hubs/match?roomId={roomId}");
        return new HubConnectionBuilder()
            .WithUrl(hubUrl, options =>
            {
                options.AccessTokenProvider = () => Task.FromResult<string?>(token);
                options.HttpMessageHandlerFactory = _ => _matchFactory.Server.CreateHandler();
            })
            .AddMessagePackProtocol()
            .Build();
    }

    // ── Tests ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ClientA_PostsGoal_ClientB_ReceivesEventPostedAndScoreUpdated()
    {
        // Arrange: create host and commentator users
        var hostToken = await RegisterAndGetTokenAsync("signalr-host@test.com");
        var commentatorToken = await RegisterAndGetTokenAsync("signalr-commentator@test.com");

        var hostMatchClient = AuthMatchClient(hostToken);
        var commentatorMatchClient = AuthMatchClient(commentatorToken);

        // Create a room as host
        var createResponse = await hostMatchClient.PostAsJsonAsync("/api/matches",
            new CreateMatchRequest("SignalR Test", "Arsenal", "Chelsea", null, DateTime.UtcNow.AddHours(1)));
        createResponse.EnsureSuccessStatusCode();
        var room = await createResponse.Content.ReadFromJsonAsync<MatchRoomResponse>();
        var roomId = room!.Id;

        // Commentator joins the room
        await commentatorMatchClient.PostAsJsonAsync(
            $"/api/matches/{roomId}/join", new JoinMatchRequest(null));

        // Connect both clients to the SignalR hub
        var hostHub = BuildHubConnection(roomId, hostToken);
        var commentatorHub = BuildHubConnection(roomId, commentatorToken);

        var eventReceived = new TaskCompletionSource<MatchEventResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
        var scoreReceived = new TaskCompletionSource<(int Home, int Away)>(TaskCreationOptions.RunContinuationsAsynchronously);

        commentatorHub.On<MatchEventResponse>("EventPosted", ev =>
            eventReceived.TrySetResult(ev));
        commentatorHub.On<string, int, int>("ScoreUpdated", (_, home, away) =>
            scoreReceived.TrySetResult((home, away)));

        await hostHub.StartAsync();
        await commentatorHub.StartAsync();

        try
        {
            // Act: host posts a goal via the hub (triggers EventPosted + ScoreUpdated broadcast)
            await hostHub.InvokeAsync("PostEvent",
                new PostEventRequest(Minute: 30, EventType: MatchEventType.Goal, Team: "home"));

            // Assert: commentator receives both hub messages within 5 seconds
            var completedEvent = await Task.WhenAny(
                eventReceived.Task,
                Task.Delay(TimeSpan.FromSeconds(5)));

            // Note: EventPosted/ScoreUpdated are only sent by the hub's PostEvent method (SignalR path).
            // The HTTP controller path also calls IHubContext, so both paths broadcast.
            // The HTTP controller broadcasts to the group — both clients should receive it.
            completedEvent.Should().Be(eventReceived.Task,
                "commentator should receive EventPosted within 5 seconds");

            var ev = await eventReceived.Task;
            ev.EventType.Should().Be(MatchEventType.Goal);
            ev.Minute.Should().Be(30);

            var scoreTask = await Task.WhenAny(scoreReceived.Task, Task.Delay(TimeSpan.FromSeconds(5)));
            scoreTask.Should().Be(scoreReceived.Task,
                "commentator should receive ScoreUpdated within 5 seconds");

            var (home, away) = await scoreReceived.Task;
            home.Should().Be(1);
            away.Should().Be(0);
        }
        finally
        {
            await hostHub.StopAsync();
            await commentatorHub.StopAsync();
            await hostHub.DisposeAsync();
            await commentatorHub.DisposeAsync();
        }
    }

    [Fact]
    public async Task Connect_WithoutToken_ThrowsOrDisconnects()
    {
        // Arrange: create a room so we have a valid roomId
        var hostToken = await RegisterAndGetTokenAsync("signalr-noauth-host@test.com");
        var hostMatchClient = AuthMatchClient(hostToken);
        var createResponse = await hostMatchClient.PostAsJsonAsync("/api/matches",
            new CreateMatchRequest("Auth Test", "Home", "Away", null, DateTime.UtcNow.AddHours(1)));
        var room = await createResponse.Content.ReadFromJsonAsync<MatchRoomResponse>();

        var hubUrl = new Uri(_matchFactory.Server.BaseAddress, $"hubs/match?roomId={room!.Id}");
        var unauthHub = new HubConnectionBuilder()
            .WithUrl(hubUrl, options =>
            {
                options.HttpMessageHandlerFactory = _ => _matchFactory.Server.CreateHandler();
            })
            .Build();

        // Act & Assert: connecting without a token should fail
        var act = async () => await unauthHub.StartAsync();

        await act.Should().ThrowAsync<Exception>(
            "unauthenticated connections should be rejected by the [Authorize] hub");

        await unauthHub.DisposeAsync();
    }
}
