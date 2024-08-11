using System.ComponentModel.DataAnnotations;

namespace PitchSync.MatchService.Entities;

public sealed class PlayerRating
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid MatchRoomId { get; set; }
    public MatchRoom MatchRoom { get; set; } = null!;

    [Required]
    [MaxLength(100)]
    public string PlayerName { get; set; } = string.Empty;

    [Required]
    [MaxLength(10)]
    public string Team { get; set; } = string.Empty;

    [Required]
    public string UserId { get; set; } = string.Empty;

    public decimal Rating { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
