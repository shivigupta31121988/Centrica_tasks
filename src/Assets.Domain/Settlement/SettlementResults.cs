namespace Assets.Domain.Settlement;

/// <summary>Daily resolution result for a single asset.</summary>
public sealed record DailySettlement(DateOnly LocalDate, decimal Amount, int IncompleteHours);

/// <summary>Monthly resolution result summed across all assets.</summary>
public sealed record MonthlySettlement(int Year, int Month, decimal Amount, int IncompleteHours);
