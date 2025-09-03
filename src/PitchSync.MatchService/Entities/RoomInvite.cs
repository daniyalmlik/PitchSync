using PitchSync.Shared.Enums;
using System.ComponentModel.DataAnnotations;

namespace PitchSync.MatchService.Entities;

public sealed class RoomInvite
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid MatchRoomId { get; set; }
    public MatchRoom MatchRoom { get; set; } = null!;

    [Required, MaxLength(200)]
    public string RoomTitle { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string HomeTeam { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string AwayTeam { get; set; } = string.Empty;

    [Required]
    public string InvitedUserId { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string InvitedDisplayName { get; set; } = string.Empty;

    [Required]
    public string InvitedByUserId { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string InvitedByDisplayName { get; set; } = string.Empty;

    public InviteStatus Status { get; set; } = InviteStatus.Pending;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
