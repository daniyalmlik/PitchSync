using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace PitchSync.IdentityService.Entities;

public sealed class ApplicationUser : IdentityUser
{
    [Required]
    [MaxLength(100)]
    public string DisplayName { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? FavoriteTeam { get; set; }

    public string? AvatarUrl { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
