namespace AxizGeo.Infrastructure.Identity;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "axiz-geo-api";
    public string Audience { get; set; } = "axiz-geo-web";
    public string Key { get; set; } = string.Empty;
    public int ExpirationMinutes { get; set; } = 30;
    public int RefreshExpirationDays { get; set; } = 7;
}
