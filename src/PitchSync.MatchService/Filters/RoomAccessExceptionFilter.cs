using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using PitchSync.MatchService.Exceptions;

namespace PitchSync.MatchService.Filters;

public sealed class RoomAccessExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        if (context.Exception is not RoomAccessDeniedException ex)
            return;

        context.Result = new ObjectResult(new { error = ex.Message })
        {
            StatusCode = StatusCodes.Status403Forbidden
        };

        context.ExceptionHandled = true;
    }
}
