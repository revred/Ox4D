// =============================================================================
// Ox4D Demo - Demonstration of Sales Pipeline Manager Capabilities
// =============================================================================
// PURPOSE:
//   This demo showcases all the key features of the Ox4D Sales Pipeline
//   Manager without requiring interactive input. It demonstrates:
//   - Synthetic data generation
//   - Pipeline statistics
//   - Daily brief reports
//   - Hygiene reports
//   - Forecast snapshots
//   - Deal operations
//
// USAGE:
//   dotnet run --project tools/Demo
// =============================================================================

using Ox4D.Core.Models;
using Ox4D.Core.Models.Config;
using Ox4D.Core.Models.Reports;
using Ox4D.Core.Services;
using Ox4D.Storage;

var dataDir = Path.Combine(Directory.GetCurrentDirectory(), "data");
var excelPath = Path.Combine(dataDir, "SalesPipelineV1.0.xlsx");

if (!Directory.Exists(dataDir))
    Directory.CreateDirectory(dataDir);

var lookups = LookupTables.CreateDefault();
var settings = new PipelineSettings();

// Use Excel repository with sheet-per-table design
var repository = new ExcelDealRepository(excelPath, lookups);
var pipelineService = new PipelineService(repository, lookups, settings);

Console.WriteLine("=".PadRight(60, '='));
Console.WriteLine("  Ox4D Sales Pipeline Manager - Demo");
Console.WriteLine("=".PadRight(60, '='));
Console.WriteLine();

// 1. Generate Synthetic Data
Console.WriteLine(">>> GENERATING 100 SYNTHETIC DEALS (seed=42)...");
var count = await pipelineService.GenerateSyntheticDataAsync(100, seed: 42);
Console.WriteLine($"    Generated {count} deals\n");

// 2. Pipeline Statistics
Console.WriteLine(">>> PIPELINE STATISTICS");
var stats = await pipelineService.GetStatsAsync();
Console.WriteLine($"    Total Deals:      {stats.TotalDeals}");
Console.WriteLine($"    Open Deals:       {stats.OpenDeals}");
Console.WriteLine($"    Closed Won:       {stats.ClosedWonDeals}");
Console.WriteLine($"    Closed Lost:      {stats.ClosedLostDeals}");
Console.WriteLine($"    Total Pipeline:   £{stats.TotalPipeline:N0}");
Console.WriteLine($"    Weighted Pipeline:£{stats.WeightedPipeline:N0}");
Console.WriteLine($"    Closed Won Value: £{stats.ClosedWonValue:N0}");
Console.WriteLine($"    Avg Deal Value:   £{stats.AverageDealsValue:N0}");
Console.WriteLine($"    Sales Reps:       {string.Join(", ", stats.Owners)}");
Console.WriteLine($"    Regions:          {string.Join(", ", stats.Regions.Take(5))}...\n");

// 3. Daily Brief
Console.WriteLine(">>> DAILY BRIEF");
var brief = await pipelineService.GetDailyBriefAsync();
Console.WriteLine($"    Reference Date:   {brief.ReferenceDate:dd MMM yyyy}");
Console.WriteLine($"    Due Today:        {brief.DueToday.Count} deals");
Console.WriteLine($"    Overdue:          {brief.Overdue.Count} deals");
Console.WriteLine($"    No Contact (10d+):{brief.NoContactDeals.Count} deals");
Console.WriteLine($"    High Value @ Risk:{brief.HighValueAtRisk.Count} deals (£{brief.TotalAtRiskValue:N0})");

if (brief.Overdue.Any())
{
    Console.WriteLine("\n    Top 5 Overdue:");
    foreach (var deal in brief.Overdue.Take(5))
    {
        Console.WriteLine($"      - {deal.DealName,-30} £{deal.Amount ?? 0,10:N0}  ({deal.DaysOverdue}d overdue)");
    }
}
Console.WriteLine();

// 4. Hygiene Report
Console.WriteLine(">>> HYGIENE REPORT");
var hygiene = await pipelineService.GetHygieneReportAsync();
Console.WriteLine($"    Health Score:     {hygiene.HealthScore}%");
Console.WriteLine($"    Deals with Issues:{hygiene.DealsWithIssues} / {hygiene.TotalDeals}");
Console.WriteLine("    Issues by Type:");
foreach (var (type, cnt) in hygiene.IssuesByType.OrderByDescending(x => x.Value).Take(5))
{
    Console.WriteLine($"      - {type.ToDisplayString(),-30}: {cnt}");
}
Console.WriteLine();

// 5. Forecast Snapshot
Console.WriteLine(">>> FORECAST SNAPSHOT");
var forecast = await pipelineService.GetForecastSnapshotAsync();
Console.WriteLine($"    Total Pipeline:   £{forecast.TotalPipeline:N0}");
Console.WriteLine($"    Weighted Pipeline:£{forecast.WeightedPipeline:N0}");
Console.WriteLine($"    Open Deals:       {forecast.OpenDeals}");

Console.WriteLine("\n    By Stage:");
foreach (var stage in forecast.ByStage)
{
    Console.WriteLine($"      {stage.Stage.ToDisplayString(),-15} {stage.DealCount,3} deals  £{stage.TotalAmount,12:N0}  ({stage.PercentageOfPipeline,5:F1}%)");
}

Console.WriteLine("\n    By Owner:");
foreach (var owner in forecast.ByOwner.Take(5))
{
    Console.WriteLine($"      {owner.Owner,-20} {owner.DealCount,3} open  £{owner.TotalAmount,12:N0}  Win Rate: {owner.WinRate,5:F1}%");
}

Console.WriteLine("\n    By Close Month:");
foreach (var month in forecast.ByCloseMonth.Take(4))
{
    Console.WriteLine($"      {month.MonthName,-10} {month.DealCount,3} deals  £{month.WeightedAmount,12:N0} (weighted)");
}
Console.WriteLine();

// 6. Sample Deal Lookup
Console.WriteLine(">>> SAMPLE DEAL LOOKUP");
var deals = await pipelineService.ListDealsAsync();
var sampleDeal = deals.FirstOrDefault();
if (sampleDeal != null)
{
    Console.WriteLine($"    Deal ID:      {sampleDeal.DealId}");
    Console.WriteLine($"    Deal Name:    {sampleDeal.DealName}");
    Console.WriteLine($"    Account:      {sampleDeal.AccountName}");
    Console.WriteLine($"    Contact:      {sampleDeal.ContactName}");
    Console.WriteLine($"    Stage:        {sampleDeal.Stage.ToDisplayString()}");
    Console.WriteLine($"    Amount:       £{sampleDeal.AmountGBP:N0}");
    Console.WriteLine($"    Weighted:     £{sampleDeal.WeightedAmountGBP:N0}");
    Console.WriteLine($"    Owner:        {sampleDeal.Owner}");
    Console.WriteLine($"    Region:       {sampleDeal.Region}");
    Console.WriteLine($"    Next Step:    {sampleDeal.NextStep}");
    Console.WriteLine($"    Tags:         {string.Join(", ", sampleDeal.Tags)}");
}
Console.WriteLine();

// 7. Save to Excel
Console.WriteLine(">>> SAVING TO EXCEL...");
await repository.SaveChangesAsync();
var allDeals = await repository.GetAllAsync();
Console.WriteLine($"    Saved {allDeals.Count} deals to: {excelPath}\n");

Console.WriteLine("=".PadRight(60, '='));
Console.WriteLine("  Demo Complete!");
Console.WriteLine("=".PadRight(60, '='));
