namespace Ox4D.Core.Models.Reports;

public class ForecastSnapshot
{
    public DateTime ReferenceDate { get; set; }
    public decimal TotalPipeline { get; set; }
    public decimal WeightedPipeline { get; set; }
    public int TotalDeals { get; set; }
    public int OpenDeals { get; set; }

    public List<StageBreakdown> ByStage { get; set; } = new();
    public List<OwnerBreakdown> ByOwner { get; set; } = new();
    public List<MonthBreakdown> ByCloseMonth { get; set; } = new();
    public List<RegionBreakdown> ByRegion { get; set; } = new();
    public List<ProductBreakdown> ByProduct { get; set; } = new();
}

public class StageBreakdown
{
    public DealStage Stage { get; set; }
    public int DealCount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal WeightedAmount { get; set; }
    public double PercentageOfPipeline { get; set; }
}

public class OwnerBreakdown
{
    public string Owner { get; set; } = string.Empty;
    public int DealCount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal WeightedAmount { get; set; }
    public double WinRate { get; set; }
    public int ClosedWon { get; set; }
    public int ClosedLost { get; set; }
}

public class MonthBreakdown
{
    public int Year { get; set; }
    public int Month { get; set; }
    public string MonthName => new DateTime(Year, Month, 1).ToString("MMM yyyy");
    public int DealCount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal WeightedAmount { get; set; }
}

public class RegionBreakdown
{
    public string Region { get; set; } = string.Empty;
    public int DealCount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal WeightedAmount { get; set; }
}

public class ProductBreakdown
{
    public string ProductLine { get; set; } = string.Empty;
    public int DealCount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal WeightedAmount { get; set; }
    public double PercentageOfPipeline { get; set; }
}
