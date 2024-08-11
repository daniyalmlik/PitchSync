using PitchSync.Shared.Enums;
using System.ComponentModel.DataAnnotations;

namespace PitchSync.MatchService.Entities;

public sealed class RoomParticipant
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid MatchRoomId { get; set; }
    public MatchRoom MatchRoom { get; set; } = null!;

    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string DisplayName { get; set; } = string.Empty;

    public RoomRole Role { get; set; }

    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
}
