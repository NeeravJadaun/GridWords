using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WordDuel.Api.Auth;
using WordDuel.Api.Dtos;
using WordDuel.Api.Errors;
using WordDuel.Api.Hubs;
using WordDuel.Domain.Board;
using WordDuel.Domain.Model;
using WordDuel.Domain.Rules;
using WordDuel.Domain.Tiles;
using WordDuel.Domain.WordList;
using WordDuel.Infrastructure.Caching;
using WordDuel.Infrastructure.Persistence;

namespace WordDuel.Api.Services;

public sealed class MatchService : IMatchService
{
    private const int InitialRackSize = 7;
    private static readonly TimeSpan SnapshotCacheTtl = TimeSpan.FromHours(1);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly WordDuelDbContext _db;
    private readonly IMatchCache _cache;
    private readonly IMatchNotifier _notifier;
    private readonly PlayerTokenService _tokenService;
    private readonly WordListProvider _wordList;
    private readonly IdempotencyStore _idempotency;
    private readonly ILogger<MatchService> _logger;

    public MatchService(
        WordDuelDbContext db,
        IMatchCache cache,
        IMatchNotifier notifier,
        PlayerTokenService tokenService,
        WordListProvider wordList,
        IdempotencyStore idempotency,
        ILogger<MatchService> logger)
    {
        _db = db;
        _cache = cache;
        _notifier = notifier;
        _tokenService = tokenService;
        _wordList = wordList;
        _idempotency = idempotency;
        _logger = logger;
    }

    public async Task<CreateMatchResponse> CreateMatchAsync(string displayName, CancellationToken ct)
    {
        var matchId = Guid.NewGuid();
        var boardSeed = BoardSeedFactory.FromMatchId(matchId);
        var bag = new TileBag(boardSeed);
        var rackTiles = bag.DrawUpTo(InitialRackSize);
        var playerId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var match = new MatchEntity
        {
            Id = matchId,
            Status = (int)MatchStatus.WaitingForOpponent,
            BoardSeed = boardSeed,
            BoardStateFlat = new GameBoard().ToFlatString(),
            TilesDrawnCount = rackTiles.Count,
            CurrentTurnSeat = 0,
            ConsecutivePasses = 0,
            EndReason = (int)MatchEndReason.None,
            WinnerSeat = null,
            MoveSequenceCounter = 0,
            Version = 1,
            CreatedAt = now
        };

        var player = new PlayerEntity
        {
            Id = playerId,
            MatchId = matchId,
            Seat = 0,
            DisplayName = displayName,
            RackFlat = new string(rackTiles.ToArray()),
            Score = 0,
            HasResigned = false,
            JoinedAt = now
        };

        match.Players.Add(player);
        _db.Matches.Add(match);
        await _db.SaveChangesAsync(ct);

        var snapshot = MatchSnapshotMapper.ToSnapshot(match, match.MoveSequenceCounter);
        await CacheSnapshotAsync(matchId, snapshot, ct);

        return new CreateMatchResponse
        {
            MatchId = matchId,
            PlayerId = playerId,
            Seat = 0,
            PlayerToken = _tokenService.IssueToken(playerId, matchId, 0),
            Rack = player.RackFlat,
            Match = snapshot
        };
    }

    public async Task<JoinMatchResponse> JoinMatchAsync(Guid matchId, string displayName, CancellationToken ct)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(ct);

        var match = await LoadMatchForMutationAsync(matchId, ct);
        if (match.Players.Count >= 2)
        {
            throw new GameRuleException(GameErrorCode.MatchFull, "This match already has two players.");
        }

        var bag = TileBag.Reconstruct(match.BoardSeed, match.TilesDrawnCount);
        var rackTiles = bag.DrawUpTo(InitialRackSize);
        var playerId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var player = new PlayerEntity
        {
            Id = playerId,
            MatchId = matchId,
            Seat = 1,
            DisplayName = displayName,
            RackFlat = new string(rackTiles.ToArray()),
            Score = 0,
            HasResigned = false,
            JoinedAt = now
        };

        match.Players.Add(player);
        _db.Players.Add(player);
        match.TilesDrawnCount += rackTiles.Count;
        match.Status = (int)MatchStatus.InProgress;
        match.StartedAt = now;
        match.Version += 1;

        await SaveWithConcurrencyGuardAsync(transaction, matchId, ct, staleErrorCode: GameErrorCode.MatchFull);

        var snapshot = MatchSnapshotMapper.ToSnapshot(match, match.MoveSequenceCounter);
        await CacheSnapshotAsync(matchId, snapshot, ct);
        await _notifier.NotifyMatchUpdatedAsync(matchId, snapshot, ct);

        return new JoinMatchResponse
        {
            PlayerId = playerId,
            Seat = 1,
            PlayerToken = _tokenService.IssueToken(playerId, matchId, 1),
            Rack = player.RackFlat,
            Match = snapshot
        };
    }

    public async Task<MatchSnapshotDto> GetMatchAsync(Guid matchId, Guid? requestingPlayerId, CancellationToken ct)
    {
        if (requestingPlayerId is null)
        {
            var cached = await _cache.GetAsync(CacheKeys.MatchSnapshot(matchId), ct);
            if (cached is not null)
            {
                return JsonSerializer.Deserialize<MatchSnapshotDto>(cached, JsonOptions)!;
            }
        }

        var match = await _db.Matches.AsNoTracking()
            .Include(m => m.Players)
            .FirstOrDefaultAsync(m => m.Id == matchId, ct)
            ?? throw new GameRuleException(GameErrorCode.MatchNotFound, "No match exists with that ID.");

        var snapshot = MatchSnapshotMapper.ToSnapshot(match, match.MoveSequenceCounter, requestingPlayerId);

        if (requestingPlayerId is null)
        {
            await CacheSnapshotAsync(matchId, snapshot, ct);
        }

        return snapshot;
    }

    public async Task<MoveHistoryResponse> GetMoveHistoryAsync(Guid matchId, CancellationToken ct)
    {
        var match = await _db.Matches.AsNoTracking()
            .Include(m => m.Players)
            .FirstOrDefaultAsync(m => m.Id == matchId, ct)
            ?? throw new GameRuleException(GameErrorCode.MatchNotFound, "No match exists with that ID.");

        var moves = await _db.Moves.AsNoTracking()
            .Where(m => m.MatchId == matchId)
            .OrderBy(m => m.SequenceNumber)
            .ToListAsync(ct);

        var namesBySeat = match.Players.ToDictionary(p => p.Seat, p => p.DisplayName);

        var items = moves.Select(m => new MoveHistoryItemDto
        {
            MoveId = m.Id,
            SequenceNumber = m.SequenceNumber,
            Seat = m.Seat,
            PlayerDisplayName = namesBySeat.GetValueOrDefault(m.Seat, "Unknown"),
            Type = ((MoveType)m.Type).ToString(),
            StartRow = m.StartRow,
            StartCol = m.StartCol,
            Direction = m.Direction is null ? null : ((Direction)m.Direction.Value).ToString(),
            TilesSubmitted = m.TilesSubmitted,
            WordsFormed = m.WordsFormedJson is null
                ? new List<FormedWordDto>()
                : JsonSerializer.Deserialize<List<FormedWordDto>>(m.WordsFormedJson, JsonOptions) ?? new List<FormedWordDto>(),
            PointsScored = m.PointsScored,
            CreatedAt = m.CreatedAt
        }).ToList();

        return new MoveHistoryResponse { Moves = items };
    }

    public async Task<MoveResultResponse> SubmitMoveAsync(
        Guid matchId, Guid playerId, SubmitMoveRequest request, string? idempotencyKey, CancellationToken ct)
    {
        if (idempotencyKey is not null)
        {
            var cached = await _idempotency.TryGetCachedResponseAsync<MoveResultResponse>(
                _db, matchId, playerId, "moves", idempotencyKey, ct);
            if (cached is not null)
            {
                return cached;
            }
        }

        if (!Enum.TryParse<Direction>(request.Direction, ignoreCase: true, out var direction))
        {
            throw new GameRuleException(GameErrorCode.InvalidDirection, "Direction must be 'Across' or 'Down'.");
        }

        await using var transaction = await _db.Database.BeginTransactionAsync(ct);

        var match = await LoadMatchForMutationAsync(matchId, ct);
        var player = GetPlayerOrThrow(match, playerId);
        EnsureInProgress(match);
        EnsureExpectedVersion(match, request.ExpectedMatchVersion);
        EnsureIsPlayersTurn(match, player);

        var board = GameBoard.FromFlatString(match.BoardStateFlat);
        var rack = new Rack(player.RackFlat);
        var isFirstMove = !board.HasAnyTiles();

        var placementRequest = new PlacementRequest(
            request.StartRow, request.StartCol, direction, request.Tiles.ToUpperInvariant().ToCharArray());

        var result = MoveProcessor.ProcessPlacement(board, rack, placementRequest, isFirstMove, _wordList);
        if (!result.IsSuccess)
        {
            throw new GameRuleException(result.ErrorCode, result.ErrorMessage!);
        }

        var outcome = result.Value!;

        foreach (var tile in outcome.NewlyPlacedTiles)
        {
            board.SetLetter(tile.Row, tile.Col, tile.Letter);
        }

        rack.RemoveAll(outcome.NewlyPlacedTiles.Select(t => t.Letter));
        var bag = TileBag.Reconstruct(match.BoardSeed, match.TilesDrawnCount);
        var drawn = bag.DrawUpTo(outcome.NewlyPlacedTiles.Count);
        rack.Add(drawn);
        match.TilesDrawnCount += drawn.Count;

        match.BoardStateFlat = board.ToFlatString();
        player.RackFlat = new string(rack.Tiles.ToArray());
        player.Score += outcome.TotalScore;
        match.ConsecutivePasses = 0;
        match.CurrentTurnSeat = MatchRules.OtherSeat(player.Seat);
        match.Version += 1;
        match.MoveSequenceCounter += 1;

        var wordsFormedDto = outcome.WordsFormed.Select(w => new FormedWordDto { Word = w.Word, Points = w.Points }).ToList();

        _db.Moves.Add(new MoveEntity
        {
            Id = Guid.NewGuid(),
            MatchId = matchId,
            PlayerId = playerId,
            Seat = player.Seat,
            SequenceNumber = match.MoveSequenceCounter,
            Type = (int)MoveType.Place,
            StartRow = request.StartRow,
            StartCol = request.StartCol,
            Direction = (int)direction,
            TilesSubmitted = request.Tiles.ToUpperInvariant(),
            WordsFormedJson = JsonSerializer.Serialize(wordsFormedDto, JsonOptions),
            PointsScored = outcome.TotalScore,
            CreatedAt = DateTimeOffset.UtcNow
        });

        var snapshot = MatchSnapshotMapper.ToSnapshot(match, match.MoveSequenceCounter, playerId);
        var response = new MoveResultResponse
        {
            Match = snapshot,
            WordsFormed = wordsFormedDto,
            PointsScored = outcome.TotalScore,
            UsedFullRackBonus = outcome.UsedFullRackBonus,
            YourRack = player.RackFlat
        };

        if (idempotencyKey is not null)
        {
            _idempotency.RecordResponse(_db, matchId, playerId, "moves", idempotencyKey, response);
        }

        await SaveWithConcurrencyGuardAsync(transaction, matchId, ct, staleErrorCode: GameErrorCode.StaleMatchVersion);

        // The public broadcast must never leak rack letters; strip "your" fields.
        var publicSnapshot = MatchSnapshotMapper.ToSnapshot(match, match.MoveSequenceCounter);
        await CacheSnapshotAsync(matchId, publicSnapshot, ct);
        await _notifier.NotifyMatchUpdatedAsync(matchId, publicSnapshot, ct);

        return response;
    }

    public async Task<MatchActionResponse> PassAsync(
        Guid matchId, Guid playerId, MatchVersionedActionRequest request, string? idempotencyKey, CancellationToken ct)
    {
        if (idempotencyKey is not null)
        {
            var cached = await _idempotency.TryGetCachedResponseAsync<MatchActionResponse>(
                _db, matchId, playerId, "pass", idempotencyKey, ct);
            if (cached is not null)
            {
                return cached;
            }
        }

        await using var transaction = await _db.Database.BeginTransactionAsync(ct);

        var match = await LoadMatchForMutationAsync(matchId, ct);
        var player = GetPlayerOrThrow(match, playerId);
        EnsureInProgress(match);
        EnsureExpectedVersion(match, request.ExpectedMatchVersion);
        EnsureIsPlayersTurn(match, player);

        match.ConsecutivePasses += 1;
        match.MoveSequenceCounter += 1;
        match.Version += 1;

        _db.Moves.Add(new MoveEntity
        {
            Id = Guid.NewGuid(),
            MatchId = matchId,
            PlayerId = playerId,
            Seat = player.Seat,
            SequenceNumber = match.MoveSequenceCounter,
            Type = (int)MoveType.Pass,
            PointsScored = 0,
            CreatedAt = DateTimeOffset.UtcNow
        });

        if (MatchRules.ShouldEndDueToPasses(match.ConsecutivePasses))
        {
            var seat0 = match.Players.First(p => p.Seat == 0).Score;
            var seat1 = match.Players.First(p => p.Seat == 1).Score;
            match.Status = (int)MatchStatus.Completed;
            match.EndReason = (int)MatchEndReason.ThreeConsecutivePasses;
            match.WinnerSeat = MatchRules.DetermineWinnerSeat(seat0, seat1);
            match.FinishedAt = DateTimeOffset.UtcNow;
        }
        else
        {
            match.CurrentTurnSeat = MatchRules.OtherSeat(player.Seat);
        }

        var response = new MatchActionResponse
        {
            Match = MatchSnapshotMapper.ToSnapshot(match, match.MoveSequenceCounter, playerId)
        };

        if (idempotencyKey is not null)
        {
            _idempotency.RecordResponse(_db, matchId, playerId, "pass", idempotencyKey, response);
        }

        await SaveWithConcurrencyGuardAsync(transaction, matchId, ct, staleErrorCode: GameErrorCode.StaleMatchVersion);

        var publicSnapshot = MatchSnapshotMapper.ToSnapshot(match, match.MoveSequenceCounter);
        await CacheSnapshotAsync(matchId, publicSnapshot, ct);
        await _notifier.NotifyMatchUpdatedAsync(matchId, publicSnapshot, ct);

        return response;
    }

    public async Task<MatchActionResponse> ResignAsync(
        Guid matchId, Guid playerId, MatchVersionedActionRequest request, string? idempotencyKey, CancellationToken ct)
    {
        if (idempotencyKey is not null)
        {
            var cached = await _idempotency.TryGetCachedResponseAsync<MatchActionResponse>(
                _db, matchId, playerId, "resign", idempotencyKey, ct);
            if (cached is not null)
            {
                return cached;
            }
        }

        await using var transaction = await _db.Database.BeginTransactionAsync(ct);

        var match = await LoadMatchForMutationAsync(matchId, ct);
        var player = GetPlayerOrThrow(match, playerId);
        EnsureInProgress(match);
        EnsureExpectedVersion(match, request.ExpectedMatchVersion);

        player.HasResigned = true;
        match.Status = (int)MatchStatus.Completed;
        match.EndReason = (int)MatchEndReason.Resignation;
        match.WinnerSeat = MatchRules.OtherSeat(player.Seat);
        match.FinishedAt = DateTimeOffset.UtcNow;
        match.MoveSequenceCounter += 1;
        match.Version += 1;

        _db.Moves.Add(new MoveEntity
        {
            Id = Guid.NewGuid(),
            MatchId = matchId,
            PlayerId = playerId,
            Seat = player.Seat,
            SequenceNumber = match.MoveSequenceCounter,
            Type = (int)MoveType.Resign,
            PointsScored = 0,
            CreatedAt = DateTimeOffset.UtcNow
        });

        var response = new MatchActionResponse
        {
            Match = MatchSnapshotMapper.ToSnapshot(match, match.MoveSequenceCounter, playerId)
        };

        if (idempotencyKey is not null)
        {
            _idempotency.RecordResponse(_db, matchId, playerId, "resign", idempotencyKey, response);
        }

        await SaveWithConcurrencyGuardAsync(transaction, matchId, ct, staleErrorCode: GameErrorCode.StaleMatchVersion);

        var publicSnapshot = MatchSnapshotMapper.ToSnapshot(match, match.MoveSequenceCounter);
        await CacheSnapshotAsync(matchId, publicSnapshot, ct);
        await _notifier.NotifyMatchUpdatedAsync(matchId, publicSnapshot, ct);

        return response;
    }

    // ---- shared helpers ----

    private async Task<MatchEntity> LoadMatchForMutationAsync(Guid matchId, CancellationToken ct)
    {
        return await _db.Matches
            .Include(m => m.Players)
            .FirstOrDefaultAsync(m => m.Id == matchId, ct)
            ?? throw new GameRuleException(GameErrorCode.MatchNotFound, "No match exists with that ID.");
    }

    private static PlayerEntity GetPlayerOrThrow(MatchEntity match, Guid playerId)
    {
        return match.Players.FirstOrDefault(p => p.Id == playerId)
            ?? throw new GameRuleException(GameErrorCode.PlayerNotInMatch, "This player does not belong to the specified match.");
    }

    private static void EnsureInProgress(MatchEntity match)
    {
        if (match.Status != (int)MatchStatus.InProgress)
        {
            throw new GameRuleException(GameErrorCode.MatchNotInProgress, "The match is not currently in progress.");
        }
    }

    private static void EnsureExpectedVersion(MatchEntity match, int expectedVersion)
    {
        if (match.Version != expectedVersion)
        {
            throw new GameRuleException(
                GameErrorCode.StaleMatchVersion,
                $"Expected match version {expectedVersion} but the current version is {match.Version}.",
                match.Version);
        }
    }

    private static void EnsureIsPlayersTurn(MatchEntity match, PlayerEntity player)
    {
        if (match.CurrentTurnSeat != player.Seat)
        {
            throw new GameRuleException(GameErrorCode.NotYourTurn, "It is not this player's turn.");
        }
    }

    private async Task SaveWithConcurrencyGuardAsync(
        Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction,
        Guid matchId,
        CancellationToken ct,
        GameErrorCode staleErrorCode)
    {
        try
        {
            await _db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync(ct);
            var currentVersion = await _db.Matches.AsNoTracking()
                .Where(m => m.Id == matchId)
                .Select(m => (int?)m.Version)
                .FirstOrDefaultAsync(ct);

            _logger.LogInformation("Concurrency conflict on match {MatchId}", matchId);
            throw new GameRuleException(
                staleErrorCode,
                "The match was updated by another request. Refresh and try again.",
                currentVersion);
        }
    }

    private async Task CacheSnapshotAsync(Guid matchId, MatchSnapshotDto publicSnapshot, CancellationToken ct)
    {
        try
        {
            await _cache.SetAsync(
                CacheKeys.MatchSnapshot(matchId), JsonSerializer.Serialize(publicSnapshot, JsonOptions), SnapshotCacheTtl, ct);
        }
        catch (Exception ex)
        {
            // Redis is a cache, not the source of truth — a caching failure must never fail the request.
            _logger.LogWarning(ex, "Failed to write match snapshot cache for {MatchId}", matchId);
        }
    }
}
