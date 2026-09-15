namespace Assets.Infrastructure.MeterData;

public sealed class MeterDataSettings
{
    public const string SectionName = "MeterData";

    /// <summary>The one fixed directory the importer ever reads from.</summary>
    public string Directory { get; set; } = "meterdata";

    public long MaxUploadSizeBytes { get; set; } = 20 * 1024 * 1024; // 20 MB default, per the plan doc
}
