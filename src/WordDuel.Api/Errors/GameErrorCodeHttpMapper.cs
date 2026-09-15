using Microsoft.AspNetCore.Http;
using WordDuel.Domain.Model;

namespace WordDuel.Api.Errors;

public static class GameErrorCodeHttpMapper
{
    public static int ToHttpStatusCode(GameErrorCode code) => code switch
    {
        GameErrorCode.MatchNotFound => StatusCodes.Status404NotFound,
        GameErrorCode.PlayerNotInMatch => StatusCodes.Status403Forbidden,
        GameErrorCode.StaleMatchVersion => StatusCodes.Status409Conflict,
        GameErrorCode.MatchFull => StatusCodes.Status409Conflict,
        GameErrorCode.MatchNotInProgress => StatusCodes.Status409Conflict,
        GameErrorCode.NotYourTurn => StatusCodes.Status409Conflict,
        _ => StatusCodes.Status422UnprocessableEntity
    };
}
