using System.ComponentModel.DataAnnotations;

namespace PitchSync.MatchService.Entities;

public sealed class PlayerLineup
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid MatchRoomId { get; set; }
    public MatchRoom MatchRoom { get; set; } = null!;

    [Required]
    [MaxLength(10)]
    public string Team { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string PlayerName { get; set; } = string.Empty;

    public int? ShirtNumber { get; set; }

    [MaxLength(30)]
    public string? Position { get; set; }

    public bool IsStarting { get; set; } = true;

    [Required]
    public string AddedByUserId { get; set; } = string.Empty;
}
