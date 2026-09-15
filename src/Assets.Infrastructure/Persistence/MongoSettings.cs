namespace Assets.Infrastructure.Persistence;

/// <summary>
/// Bound from configuration (env vars / appsettings / AWS Secrets Manager
/// in the pipeline). The connection string is never hardcoded or committed.
/// </summary>
public sealed class MongoSettings
{
    public const string SectionName = "Mongo";

    public string ConnectionString { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = "renewable_assets";
    public string AssetsCollectionName { get; set; } = "assets";
    public string UsersCollectionName { get; set; } = "users";
    public string MeterReadingsCollectionName { get; set; } = "meterReadings";
    public string ImportJobsCollectionName { get; set; } = "importJobs";
}
