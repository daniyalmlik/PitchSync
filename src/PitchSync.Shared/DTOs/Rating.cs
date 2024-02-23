namespace PitchSync.Shared.DTOs;

public record RatePlayerRequest(decimal Rating);
public record PlayerRatingResponse(string PlayerName, string Team, decimal AverageRating, int RatingCount, decimal? MyRating);
