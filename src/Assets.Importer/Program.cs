using Assets.Domain.MeterData;
using Assets.Infrastructure.MeterData;
using Assets.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<MongoSettings>(builder.Configuration.GetSection(MongoSettings.SectionName));
builder.Services.Configure<MeterDataSettings>(builder.Configuration.GetSection(MeterDataSettings.SectionName));

builder.Services.AddSingleton<MongoContext>();
builder.Services.AddScoped<IAssetRepository, MongoAssetRepository>();
builder.Services.AddScoped<IMeterReadingRepository, MongoMeterReadingRepository>();
builder.Services.AddScoped<IMeterDataParser, CsvMeterDataParser>();
builder.Services.AddScoped<IMeterDataParser, ExcelMeterDataParser>();
builder.Services.AddScoped<MeterDataImportService>();

using var host = builder.Build();

using var scope = host.Services.CreateScope();
var importService = scope.ServiceProvider.GetRequiredService<MeterDataImportService>();

Console.WriteLine("Scanning meter data directory...");
var stopwatch = System.Diagnostics.Stopwatch.StartNew();

var summary = await importService.ImportAllInDirectoryAsync();

stopwatch.Stop();

Console.WriteLine(
    $"Done in {stopwatch.Elapsed.TotalSeconds:F1}s - " +
    $"{summary.FilesProcessed} file(s) processed, " +
    $"{summary.RowsImported} row(s) imported, {summary.RowsSkipped} skipped.");
