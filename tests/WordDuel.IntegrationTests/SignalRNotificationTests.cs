using FluentAssertions;
using Microsoft.AspNetCore.SignalR.Client;
using WordDuel.Api.Dtos;
using WordDuel.IntegrationTests.Support;
using Xunit;

namespace WordDuel.IntegrationTests;

[Collection("WordDuelApi")]
public sealed class SignalRNotificationTests
{
    private readonly WordDuelApiFixture _fixture;
    private readonly TestApiClient _client;

    public SignalRNotificationTests(WordDuelApiFixture fixture)
    {
        _fixture = fixture;
        _client = new TestApiClient(fixture.CreateClient());
    }

    [Fact]
    public async Task MatchUpdated_IsBroadcastToConnectedPlayers_WhenAMoveIsSubmitted()
    {
        var created = await _client.CreateMatchAsync("Ada");
        var joined = await _client.JoinMatchAsync(created.MatchId, "Grace");

        var server = _fixture.Server;
        var connection = new HubConnectionBuilder()
            .WithUrl($"{server.BaseAddress}hubs/match", options =>
            {
                options.HttpMessageHandlerFactory = _ => server.CreateHandler();
                options.AccessTokenProvider = () => Task.FromResult<string?>(joined.PlayerToken);
            })
            .Build();

        var tcs = new TaskCompletionSource<MatchSnapshotDto>(TaskCreationOptions.RunContinuationsAsynchronously);
        connection.On<MatchSnapshotDto>("MatchUpdated", snapshot => tcs.TrySetResult(snapshot));

        await connection.StartAsync();
        try
        {
            var (word, row, col) = PlayableWordFinder.FindFirstMovePlacement(created.Rack);
            await _client.SubmitMoveAsync(created.MatchId, created.PlayerToken, new SubmitMoveRequest
            {
                ExpectedMatchVersion = joined.Match.Version,
                StartRow = row,
                StartCol = col,
                Direction = "Across",
                Tiles = word
            });

            var completed = await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromSeconds(10)));
            completed.Should().Be(tcs.Task, "the hub should broadcast MatchUpdated to the joined player within 10s");

            var broadcast = await tcs.Task;
            broadcast.MatchId.Should().Be(created.MatchId);
            broadcast.Version.Should().Be(joined.Match.Version + 1);
            broadcast.CurrentTurnSeat.Should().Be(1);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task Reconnect_ThenGetMatch_ReturnsCurrentAuthoritativeSnapshot()
    {
        // Simulates a client that disconnects/refreshes: it doesn't rely on any
        // buffered SignalR event, it just re-fetches current state via REST.
        var created = await _client.CreateMatchAsync("Ada");
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

        // "Reconnect" = a fresh, unrelated GET call with the player's token.
        var resynced = await _client.GetMatchAsync(created.MatchId, created.PlayerToken);

        resynced.Version.Should().Be(moveResult.Match.Version);
        resynced.Board.Should().Be(moveResult.Match.Board);
        resynced.YourRack.Should().Be(moveResult.YourRack);
    }
}
