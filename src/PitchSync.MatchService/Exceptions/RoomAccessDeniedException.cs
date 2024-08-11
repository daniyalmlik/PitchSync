namespace PitchSync.MatchService.Exceptions;

public sealed class RoomAccessDeniedException : Exception
{
    public RoomAccessDeniedException(string message) : base(message) { }
}
