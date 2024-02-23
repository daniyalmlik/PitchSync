using PitchSync.Shared.Enums;

namespace PitchSync.Shared.DTOs;

public record CreateMatchRequest(
    string Title,
    string HomeTeam,
    string AwayTeam,
    string? Competition,
    DateTime KickoffTime,
    bool IsPublic = true);

public record MatchRoomResponse(
    Guid Id,
    string Title,
    string HomeTeam,
    string AwayTeam,
    string? Competition,
    DateTime KickoffTime,
    MatchStatus Status,
    int HomeScore,
    int AwayScore,
    bool IsPublic,
    string? InviteCode,
    string CreatedByUserId,
    DateTime CreatedAt,
    List<ParticipantDto> Participants,
    List<MatchEventResponse> Events,
    List<PlayerLineupDto> HomeLineup,
    List<PlayerLineupDto> AwayLineup);

public record MatchRoomSummary(
    Guid Id,
    string Title,
    string HomeTeam,
    string AwayTeam,
    MatchStatus Status,
    int HomeScore,
    int AwayScore,
    DateTime KickoffTime,
    int ParticipantCount);

public record JoinMatchRequest(string? InviteCode = null);
public record UpdateScoreRequest(int HomeScore, int AwayScore);
public record UpdateStatusRequest(MatchStatus Status);
