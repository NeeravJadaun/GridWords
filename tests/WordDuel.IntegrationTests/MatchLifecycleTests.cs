using System.Net;
using FluentAssertions;
using WordDuel.Api.Dtos;
using WordDuel.IntegrationTests.Support;
using Xunit;

namespace WordDuel.IntegrationTests;

[Collection("WordDuelApi")]
public sealed class MatchLifecycleTests
{
    private readonly TestApiClient _client;

    public MatchLifecycleTests(WordDuelApiFixture fixture)
    {
        _client = new TestApiClient(fixture.CreateClient());
    }

    /// <summary>
    /// Creates matches until the creator's deterministic first rack is
    /// actually playable (a real draw can occasionally be all-consonant).
    /// </summary>
    private async Task<CreateMatchResponse> CreateMatchWithPlayableRackAsync(string displayName)
    {
        for (var attempt = 0; attempt < 25; attempt++)
        {
            var created = await _client.CreateMatchAsync(displayName);
            if (PlayableWordFinder.TryFindFirstMovePlacement(created.Rack, out _))
            {
                return created;
            }
        }

        throw new InvalidOperationException("Could not draw a playable rack after 25 attempts.");
    }

    [Fact]
    public async Task CreateThenJoin_StartsMatchInProgressWithTwoPlayers()
    {
        var created = await _client.CreateMatchAsync("Ada");
        created.Match.Status.Should().Be("WaitingForOpponent");
        created.Match.Players.Should().HaveCount(1);

        var joined = await _client.JoinMatchAsync(created.MatchId, "Grace");

        joined.Match.Status.Should().Be("InProgress");
        joined.Match.Players.Should().HaveCount(2);
        joined.Match.Version.Should().Be(2);
        joined.Seat.Should().Be(1);
    }

    [Fact]
    public async Task ThirdPlayer_CannotJoinAFullMatch()
    {
        var created = await _client.CreateMatchAsync("Ada");
        await _client.JoinMatchAsync(created.MatchId, "Grace");

        var response = await _client.JoinMatchRawAsync(created.MatchId, "Trudy");

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var problem = await TestApiClient.ReadProblemAsync(response);
        problem.ErrorCode.Should().Be("MatchFull");
    }

    [Fact]
    public async Task Move_ScoresAndAdvancesTurn_AndIsPersistedAndQueryable()
    {
        var created = await CreateMatchWithPlayableRackAsync("Ada");
        var joined = await _client.JoinMatchAsync(created.MatchId, "Grace");

        var (word, row, col) = PlayableWordFinder.FindFirstMovePlacement(created.Rack);

        var moveResult = await _client.SubmitMoveAsync(created.MatchId, created.PlayerToken, new SubmitMoveRequest
        {
            ExpectedMatchVersion = joined.Match.Version,
            StartRow = row,
            StartCol = col,
            Direction = "Across",
            Tiles = word
        });

        moveResult.PointsScored.Should().BeGreaterThan(0);
        moveResult.Match.CurrentTurnSeat.Should().Be(1);
        moveResult.Match.Players.First(p => p.Seat == 0).Score.Should().Be(moveResult.PointsScored);

        // Persistence check: re-fetch from a fresh request and confirm the board reflects the move.
        var refetched = await _client.GetMatchAsync(created.MatchId);
        refetched.Board.Should().Contain(word);
        refetched.MoveCount.Should().Be(1);

        var history = await _client.GetMoveHistoryAsync(created.MatchId);
        history.Moves.Should().ContainSingle(m => m.TilesSubmitted == word && m.Type == "Place");
    }

    [Fact]
    public async Task Move_OutOfTurn_IsRejectedWith409()
    {
        var created = await _client.CreateMatchAsync("Ada");
        var joined = await _client.JoinMatchAsync(created.MatchId, "Grace");

        var response = await _client.SubmitMoveRawAsync(created.MatchId, joined.PlayerToken, new SubmitMoveRequest
        {
            ExpectedMatchVersion = joined.Match.Version,
            StartRow = 3,
            StartCol = 3,
            Direction = "Across",
            Tiles = "AT"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var problem = await TestApiClient.ReadProblemAsync(response);
        problem.ErrorCode.Should().Be("NotYourTurn");
    }

    [Fact]
    public async Task Move_WithStaleVersion_IsRejectedAndReportsCurrentVersion()
    {
        var created = await _client.CreateMatchAsync("Ada");
        var joined = await _client.JoinMatchAsync(created.MatchId, "Grace");

        var response = await _client.SubmitMoveRawAsync(created.MatchId, created.PlayerToken, new SubmitMoveRequest
        {
            ExpectedMatchVersion = joined.Match.Version - 1, // stale on purpose
            StartRow = 3,
            StartCol = 3,
            Direction = "Across",
            Tiles = "AT"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var problem = await TestApiClient.ReadProblemAsync(response);
        problem.ErrorCode.Should().Be("StaleMatchVersion");
        problem.CurrentMatchVersion.Should().Be(joined.Match.Version);
    }

    [Fact]
    public async Task Move_RetriedWithSameIdempotencyKey_DoesNotDoubleScore()
    {
        var created = await CreateMatchWithPlayableRackAsync("Ada");
        var joined = await _client.JoinMatchAsync(created.MatchId, "Grace");
        var (word, row, col) = PlayableWordFinder.FindFirstMovePlacement(created.Rack);
        var idempotencyKey = Guid.NewGuid().ToString();

        var request = new SubmitMoveRequest
        {
            ExpectedMatchVersion = joined.Match.Version,
            StartRow = row,
            StartCol = col,
            Direction = "Across",
            Tiles = word
        };

        var first = await _client.SubmitMoveAsync(created.MatchId, created.PlayerToken, request, idempotencyKey);
        var second = await _client.SubmitMoveAsync(created.MatchId, created.PlayerToken, request, idempotencyKey);

        second.PointsScored.Should().Be(first.PointsScored);
        second.Match.Version.Should().Be(first.Match.Version);

        var final = await _client.GetMatchAsync(created.MatchId);
        final.Players.First(p => p.Seat == 0).Score.Should().Be(first.PointsScored);
        final.MoveCount.Should().Be(1);
    }

    [Fact]
    public async Task Move_FormingAWordNotInTheDictionary_IsRejectedWithoutChangingState()
    {
        var created = await _client.CreateMatchAsync("Ada");
        var joined = await _client.JoinMatchAsync(created.MatchId, "Grace");
        var (first, second) = PlayableWordFinder.FindUnplayablePair(created.Rack);

        var response = await _client.SubmitMoveRawAsync(created.MatchId, created.PlayerToken, new SubmitMoveRequest
        {
            ExpectedMatchVersion = joined.Match.Version,
            StartRow = 3,
            StartCol = 3,
            Direction = "Across",
            Tiles = $"{first}{second}"
        });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var problem = await TestApiClient.ReadProblemAsync(response);
        problem.ErrorCode.Should().Be("WordNotInDictionary");

        var stateAfter = await _client.GetMatchAsync(created.MatchId);
        stateAfter.Version.Should().Be(joined.Match.Version);
        stateAfter.MoveCount.Should().Be(0);
    }

    [Fact]
    public async Task ThreeConsecutivePasses_EndsTheMatch()
    {
        var created = await _client.CreateMatchAsync("Ada");
        var joined = await _client.JoinMatchAsync(created.MatchId, "Grace");

        var afterP1 = await _client.PassAsync(created.MatchId, created.PlayerToken, joined.Match.Version);
        var afterP2 = await _client.PassAsync(created.MatchId, joined.PlayerToken, afterP1.Match.Version);
        var afterP3 = await _client.PassAsync(created.MatchId, created.PlayerToken, afterP2.Match.Version);

        afterP3.Match.Status.Should().Be("Completed");
        afterP3.Match.EndReason.Should().Be("ThreeConsecutivePasses");
        afterP3.Match.ConsecutivePasses.Should().Be(3);
    }

    [Fact]
    public async Task Resign_EndsTheMatchWithOpponentAsWinner()
    {
        var created = await _client.CreateMatchAsync("Ada");
        var joined = await _client.JoinMatchAsync(created.MatchId, "Grace");

        var result = await _client.ResignAsync(created.MatchId, created.PlayerToken, joined.Match.Version);

        result.Match.Status.Should().Be("Completed");
        result.Match.EndReason.Should().Be("Resignation");
        result.Match.WinnerSeat.Should().Be(1); // Grace (seat 1) wins because Ada (seat 0) resigned.
    }
}
