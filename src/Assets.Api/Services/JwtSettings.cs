namespace Assets.Api.Services;

/// <summary>
/// Bound from configuration. The signing key comes from an environment
/// variable / AWS Secrets Manager in every real environment - never
/// hardcoded or committed (the appsettings placeholder is for local dev
/// only and should be overridden).
/// </summary>
public sealed class JwtSettings
{
    public const string SectionName = "Jwt";

    public string SigningKey { get; set; } = string.Empty;
    public string Issuer { get; set; } = "renewable-asset-system";
    public string Audience { get; set; } = "renewable-asset-system-clients";
    public int ExpiryMinutes { get; set; } = 60;
}
