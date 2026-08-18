namespace BookDoc2026.Infrastructure.Security;

public sealed class JwtSettings
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "BookDoc2026";

    public string Audience { get; set; } = "BookDoc2026.Clients";

    public string Key { get; set; } = string.Empty;

    public int AccessTokenMinutes { get; set; } = 15;

    public int RefreshTokenDays { get; set; } = 14;
}
