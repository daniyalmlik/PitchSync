using PitchSync.IdentityService.Entities;
using PitchSync.Shared.DTOs;

namespace PitchSync.IdentityService.Services;

public interface IJwtTokenService
{
    TokenResponse GenerateToken(ApplicationUser user, IList<string> roles);
}
