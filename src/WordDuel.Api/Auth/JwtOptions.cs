namespace WordDuel.Api.Auth;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string SigningKey { get; set; } = string.Empty;
    public string Issuer { get; set; } = "worddue-api";
    public string Audience { get; set; } = "worddue-client";
    public int ExpiryHours { get; set; } = 12;
}
