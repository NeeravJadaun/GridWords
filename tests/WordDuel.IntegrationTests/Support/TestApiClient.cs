using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using WordDuel.Api.Dtos;

namespace WordDuel.IntegrationTests.Support;

/// <summary>Thin typed wrapper over HttpClient for exercising the real HTTP contract in tests.</summary>
public sealed class TestApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;

    public TestApiClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<CreateMatchResponse> CreateMatchAsync(string displayName)
    {
        var response = await _http.PostAsJsonAsync("/api/matches", new { displayName }, JsonOptions);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CreateMatchResponse>(JsonOptions))!;
    }

    public async Task<JoinMatchResponse> JoinMatchAsync(Guid matchId, string displayName)
    {
        var response = await JoinMatchRawAsync(matchId, displayName);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<JoinMatchResponse>(JsonOptions))!;
    }

    public Task<HttpResponseMessage> JoinMatchRawAsync(Guid matchId, string displayName)
    {
        return _http.PostAsJsonAsync($"/api/matches/{matchId}/join", new { displayName }, JsonOptions);
    }

    public async Task<MatchSnapshotDto> GetMatchAsync(Guid matchId, string? token = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/matches/{matchId}");
        if (token is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        var response = await _http.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<MatchSnapshotDto>(JsonOptions))!;
    }

    public async Task<HttpResponseMessage> SubmitMoveRawAsync(
        Guid matchId, string token, SubmitMoveRequest body, string? idempotencyKey = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/matches/{matchId}/moves")
        {
            Content = JsonContent.Create(body, options: JsonOptions)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (idempotencyKey is not null)
        {
            request.Headers.Add("Idempotency-Key", idempotencyKey);
        }

        return await _http.SendAsync(request);
    }

    public async Task<MoveResultResponse> SubmitMoveAsync(
        Guid matchId, string token, SubmitMoveRequest body, string? idempotencyKey = null)
    {
        var response = await SubmitMoveRawAsync(matchId, token, body, idempotencyKey);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<MoveResultResponse>(JsonOptions))!;
    }

    public async Task<HttpResponseMessage> PassRawAsync(Guid matchId, string token, int expectedVersion, string? idempotencyKey = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/matches/{matchId}/pass")
        {
            Content = JsonContent.Create(new { expectedMatchVersion = expectedVersion }, options: JsonOptions)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (idempotencyKey is not null)
        {
            request.Headers.Add("Idempotency-Key", idempotencyKey);
        }

        return await _http.SendAsync(request);
    }

    public async Task<MatchActionResponse> PassAsync(Guid matchId, string token, int expectedVersion, string? idempotencyKey = null)
    {
        var response = await PassRawAsync(matchId, token, expectedVersion, idempotencyKey);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<MatchActionResponse>(JsonOptions))!;
    }

    public async Task<HttpResponseMessage> ResignRawAsync(Guid matchId, string token, int expectedVersion)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/matches/{matchId}/resign")
        {
            Content = JsonContent.Create(new { expectedMatchVersion = expectedVersion }, options: JsonOptions)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await _http.SendAsync(request);
    }

    public async Task<MatchActionResponse> ResignAsync(Guid matchId, string token, int expectedVersion)
    {
        var response = await ResignRawAsync(matchId, token, expectedVersion);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<MatchActionResponse>(JsonOptions))!;
    }

    public async Task<MoveHistoryResponse> GetMoveHistoryAsync(Guid matchId)
    {
        var response = await _http.GetAsync($"/api/matches/{matchId}/moves");
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<MoveHistoryResponse>(JsonOptions))!;
    }

    public static async Task<ProblemDetailsBody> ReadProblemAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadFromJsonAsync<ProblemDetailsBody>(JsonOptions);
        return body ?? new ProblemDetailsBody();
    }
}

public sealed class ProblemDetailsBody
{
    public string? Title { get; set; }
    public string? Detail { get; set; }
    public int? Status { get; set; }
    public string? ErrorCode { get; set; }
    public int? CurrentMatchVersion { get; set; }
}
