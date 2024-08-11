using PitchSync.Shared.Enums;
using System.ComponentModel.DataAnnotations;

namespace PitchSync.MatchService.Entities;

public sealed class MatchEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid MatchRoomId { get; set; }
    public MatchRoom MatchRoom { get; set; } = null!;

    [Required]
    public string PostedByUserId { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string PostedByDisplayName { get; set; } = string.Empty;

    public int Minute { get; set; }

    public MatchEventType EventType { get; set; }

    [MaxLength(10)]
    public string? Team { get; set; }

    [MaxLength(100)]
    public string? PlayerName { get; set; }

    [MaxLength(100)]
    public string? SecondaryPlayerName { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
