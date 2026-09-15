using System.Text.RegularExpressions;

namespace Assets.Domain.MeterData;

/// <summary>
/// Meter data filenames must be "<meterPointId>.csv" or "<meterPointId>.xlsx"
/// - digits only for the meter point id. Every place that touches the
/// filesystem for meter data (directory scan, upload handling) goes
/// through this class rather than trusting a raw filename string, which
/// is what actually prevents path traversal - not just "checking the
/// extension".
/// </summary>
public static class MeterDataFileNaming
{
    private static readonly Regex Pattern = new(@"^(?<meterPointId>\d+)\.(?<extension>csv|xlsx)$", RegexOptions.Compiled);

    public static bool TryParse(string fileName, out string meterPointId, out string extension)
    {
        var match = Pattern.Match(fileName);
        if (!match.Success)
        {
            meterPointId = string.Empty;
            extension = string.Empty;
            return false;
        }

        meterPointId = match.Groups["meterPointId"].Value;
        extension = match.Groups["extension"].Value;
        return true;
    }

    /// <summary>
    /// Builds a safe on-disk filename from an already-validated meter
    /// point id and extension - never from a raw uploaded filename.
    /// </summary>
    public static string BuildFileName(string meterPointId, string extension) => $"{meterPointId}.{extension}";
}
