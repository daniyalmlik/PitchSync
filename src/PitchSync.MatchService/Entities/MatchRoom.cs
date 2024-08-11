using PitchSync.Shared.Enums;
using System.ComponentModel.DataAnnotations;

namespace PitchSync.MatchService.Entities;

public sealed class MatchRoom
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string HomeTeam { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string AwayTeam { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Competition { get; set; }

    public DateTime KickoffTime { get; set; }

    public MatchStatus Status { get; set; } = MatchStatus.Upcoming;

    public int HomeScore { get; set; }

    public int AwayScore { get; set; }

    [Required]
    public string CreatedByUserId { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsPublic { get; set; } = true;

    [MaxLength(8)]
    public string? InviteCode { get; set; }

    public ICollection<RoomParticipant> Participants { get; set; } = new List<RoomParticipant>();
    public ICollection<MatchEvent> Events { get; set; } = new List<MatchEvent>();
    public ICollection<PlayerLineup> PlayerLineups { get; set; } = new List<PlayerLineup>();
    public ICollection<PlayerRating> PlayerRatings { get; set; } = new List<PlayerRating>();
}
