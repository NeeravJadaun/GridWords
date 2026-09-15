using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace WordDuel.Api.Auth;

public static class PlayerClaimTypes
{
    public const string MatchId = "match_id";
    public const string Seat = "seat";
}

public sealed class PlayerTokenService
{
    private readonly JwtOptions _options;

    public PlayerTokenService(IOptions<JwtOptions> options)
    {
        _options = options.Value;
    }

    public string IssueToken(Guid playerId, Guid matchId, int seat)
    {
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, playerId.ToString()),
            new Claim(PlayerClaimTypes.MatchId, matchId.ToString()),
            new Claim(PlayerClaimTypes.Seat, seat.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(_options.ExpiryHours),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

public static class ClaimsPrincipalExtensions
{
    public static Guid? GetPlayerId(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return Guid.TryParse(value, out var id) ? id : null;
    }

    public static Guid? GetMatchId(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(PlayerClaimTypes.MatchId);
        return Guid.TryParse(value, out var id) ? id : null;
    }

    public static int? GetSeat(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(PlayerClaimTypes.Seat);
        return int.TryParse(value, out var seat) ? seat : null;
    }
}
