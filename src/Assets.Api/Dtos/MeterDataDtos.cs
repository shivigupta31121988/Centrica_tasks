namespace Assets.Api.Dtos;

public sealed class ImportAcceptedDto
{
    public string JobId { get; set; } = string.Empty;
}

public sealed class ImportJobStatusDto
{
    public string JobId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int RowsImported { get; set; }
    public int RowsSkipped { get; set; }
    public double ElapsedSeconds { get; set; }
    public string? ErrorMessage { get; set; }
}
