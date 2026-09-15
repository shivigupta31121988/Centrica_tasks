namespace Assets.Infrastructure.Settlement;

public sealed class SettlementSettings
{
    public const string SectionName = "Settlement";

    /// <summary>
    /// IANA timezone id used for local day/month bucketing (per the DST
    /// decision - settlement periods follow the local clock, not UTC).
    /// .NET 6+ resolves IANA ids cross-platform via ICU/tzdata.
    /// </summary>
    public string TimeZoneId { get; set; } = "Europe/Copenhagen";

    public int MaxDailyRouteDays { get; set; } = 366;

    public int MaxMonthlyRouteDays { get; set; } = 1100; // ~3 years
}
