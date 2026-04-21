namespace Simple_Real_Time_Chat.Authentication;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public required string Issuer { get; init; }

    public required string Audience { get; init; }

    public required string SecretKey { get; init; }

    public int ExpirationMinutes { get; init; } = 60;
}