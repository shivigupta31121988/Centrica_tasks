namespace Assets.Api.Dtos;

public sealed class DailySettlementDto
{
    public string Date { get; set; } = string.Empty; // yyyy-MM-dd, local (Europe/Copenhagen) date
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "DKK";
    public int IncompleteHours { get; set; }
}

public sealed class MonthlySettlementDto
{
    public string Month { get; set; } = string.Empty; // yyyy-MM, local (Europe/Copenhagen) month
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "DKK";
    public int IncompleteHours { get; set; }
}
