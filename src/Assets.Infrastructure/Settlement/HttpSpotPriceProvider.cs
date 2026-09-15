using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Assets.Domain.Settlement;

namespace Assets.Infrastructure.Settlement;

/// <summary>
/// Real implementation matching the brief's documented contract:
///   GET /v7/SpotPrices/?start=&lt;ISO8601&gt;&amp;end=&lt;ISO8601&gt;
///   -&gt; { "Intervals": [ { "Start", "End", "Value": "2.38 DKK/kWh" } ] }
///
/// NOT registered in DI by default - the real API isn't available yet
/// (per the brief). When it is, swap FakeSpotPriceProvider for this in
/// Program.cs's DI registration; nothing else changes.
/// </summary>
public sealed class HttpSpotPriceProvider : ISpotPriceProvider
{
    private readonly HttpClient _httpClient;

    public HttpSpotPriceProvider(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IReadOnlyList<SpotPriceInterval>> GetPricesAsync(DateTime startUtc, DateTime endUtc, CancellationToken ct = default)
    {
        var start = startUtc.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);
        var end = endUtc.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);

        var response = await _httpClient.GetAsync($"/v7/SpotPrices/?start={start}&end={end}", ct);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<SpotPriceResponse>(cancellationToken: ct)
            ?? new SpotPriceResponse();

        return payload.Intervals
            .Select(i => new SpotPriceInterval(i.Start, i.End, ParsePrice(i.Value)))
            .ToList();
    }

    /// <summary>Parses "2.38 DKK/kWh" -&gt; 2.38m. Currency suffix is validated, not silently discarded.</summary>
    private static decimal ParsePrice(string rawValue)
    {
        var numberPart = rawValue.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()
            ?? throw new FormatException($"Unrecognised spot price value: '{rawValue}'");

        if (!rawValue.Contains("DKK", StringComparison.OrdinalIgnoreCase))
        {
            throw new NotSupportedException($"Expected DKK-denominated prices, got: '{rawValue}'");
        }

        return decimal.Parse(numberPart, CultureInfo.InvariantCulture);
    }

    private sealed class SpotPriceResponse
    {
        [JsonPropertyName("Intervals")]
        public List<SpotPriceIntervalPayload> Intervals { get; set; } = new();
    }

    private sealed class SpotPriceIntervalPayload
    {
        [JsonPropertyName("Start")]
        public DateTime Start { get; set; }

        [JsonPropertyName("End")]
        public DateTime End { get; set; }

        [JsonPropertyName("Value")]
        public string Value { get; set; } = string.Empty;
    }
}
