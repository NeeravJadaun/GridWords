using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WordDuel.Api.Auth;
using WordDuel.Api.Dtos;
using WordDuel.Api.Errors;
using WordDuel.Domain.Model;
using WordDuel.Api.Services;

namespace WordDuel.Api.Controllers;

[ApiController]
[Route("api/matches")]
public sealed class MatchesController : ControllerBase
{
    private const string IdempotencyHeaderName = "Idempotency-Key";

    private readonly IMatchService _matchService;

    public MatchesController(IMatchService matchService)
    {
        _matchService = matchService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(CreateMatchResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<CreateMatchResponse>> CreateMatch([FromBody] CreateMatchRequest request, CancellationToken ct)
    {
        var response = await _matchService.CreateMatchAsync(request.DisplayName.Trim(), ct);
        return CreatedAtAction(nameof(GetMatch), new { id = response.MatchId }, response);
    }

    [HttpPost("{id:guid}/join")]
    [ProducesResponseType(typeof(JoinMatchResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<JoinMatchResponse>> JoinMatch(Guid id, [FromBody] JoinMatchRequest request, CancellationToken ct)
    {
        var response = await _matchService.JoinMatchAsync(id, request.DisplayName.Trim(), ct);
        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(MatchSnapshotDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<MatchSnapshotDto>> GetMatch(Guid id, CancellationToken ct)
    {
        // Authorization is optional here: an authenticated player additionally
        // sees their own rack; an anonymous caller sees public state only.
        Guid? requestingPlayerId = null;
        if (Request.Headers.TryGetValue("Authorization", out var header) && header.ToString().StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            var authResult = await HttpContext.AuthenticateAsync();
            if (authResult.Succeeded && authResult.Principal.GetMatchId() == id)
            {
                requestingPlayerId = authResult.Principal.GetPlayerId();
            }
        }

        var snapshot = await _matchService.GetMatchAsync(id, requestingPlayerId, ct);
        return Ok(snapshot);
    }

    [HttpGet("{id:guid}/moves")]
    [ProducesResponseType(typeof(MoveHistoryResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<MoveHistoryResponse>> GetMoveHistory(Guid id, CancellationToken ct)
    {
        var history = await _matchService.GetMoveHistoryAsync(id, ct);
        return Ok(history);
    }

    [HttpPost("{id:guid}/moves")]
    [Authorize]
    [ProducesResponseType(typeof(MoveResultResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<MoveResultResponse>> SubmitMove(Guid id, [FromBody] SubmitMoveRequest request, CancellationToken ct)
    {
        var playerId = RequirePlayerForMatch(id);
        var idempotencyKey = Request.Headers.TryGetValue(IdempotencyHeaderName, out var key) ? key.ToString() : null;
        var response = await _matchService.SubmitMoveAsync(id, playerId, request, idempotencyKey, ct);
        return Ok(response);
    }

    [HttpPost("{id:guid}/pass")]
    [Authorize]
    [ProducesResponseType(typeof(MatchActionResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<MatchActionResponse>> Pass(Guid id, [FromBody] MatchVersionedActionRequest request, CancellationToken ct)
    {
        var playerId = RequirePlayerForMatch(id);
        var idempotencyKey = Request.Headers.TryGetValue(IdempotencyHeaderName, out var key) ? key.ToString() : null;
        var response = await _matchService.PassAsync(id, playerId, request, idempotencyKey, ct);
        return Ok(response);
    }

    [HttpPost("{id:guid}/resign")]
    [Authorize]
    [ProducesResponseType(typeof(MatchActionResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<MatchActionResponse>> Resign(Guid id, [FromBody] MatchVersionedActionRequest request, CancellationToken ct)
    {
        var playerId = RequirePlayerForMatch(id);
        var idempotencyKey = Request.Headers.TryGetValue(IdempotencyHeaderName, out var key) ? key.ToString() : null;
        var response = await _matchService.ResignAsync(id, playerId, request, idempotencyKey, ct);
        return Ok(response);
    }

    private Guid RequirePlayerForMatch(Guid matchId)
    {
        var tokenMatchId = User.GetMatchId();
        var playerId = User.GetPlayerId();

        if (tokenMatchId is null || playerId is null || tokenMatchId != matchId)
        {
            throw new GameRuleException(GameErrorCode.PlayerNotInMatch, "This token is not valid for the specified match.");
        }

        return playerId.Value;
    }
}
