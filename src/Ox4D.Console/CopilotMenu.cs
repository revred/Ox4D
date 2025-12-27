// =============================================================================
// CopilotMenu - Interactive Console Menu for Sales Pipeline Management
// =============================================================================
// Purpose: Provides a rich terminal UI using Spectre.Console for sales managers
//          to interact with their pipeline data, generate reports, and maintain
//          data quality. This is the unified gateway for all Ox4D operations.
//
// Menu Options:
//  1. Load Excel workbook
//  2. Save to Excel
//  3. Generate synthetic demo data
//  4. Daily Brief - action items, overdue tasks, at-risk deals
//  5. Hygiene Report - data quality issues
//  6. Forecast Snapshot - pipeline breakdown by various dimensions
//  7. Pipeline Statistics - summary metrics
//  8. Search / Deal Drilldown
//  9. Update Deal
// 10. Add New Deal
// 11. Delete Deal
// 12. List All Deals
// 13. Run Full Demo - non-interactive showcase of all features
//  0. Exit
// =============================================================================

using Ox4D.Core.Models;
using Ox4D.Core.Models.Config;
using Ox4D.Core.Models.Reports;
using Ox4D.Core.Services;
using Ox4D.Store;
using Spectre.Console;

namespace Ox4D.Console;

public class CopilotMenu
{
    private readonly PipelineService _pipelineService;
    private readonly IDealRepository _repository;
    private readonly LookupTables _lookups;
    private readonly PipelineSettings _settings;
    private readonly string _excelPath;

    public CopilotMenu(
        PipelineService pipelineService,
        IDealRepository repository,
        LookupTables lookups,
        PipelineSettings settings,
        string excelPath)
    {
        _pipelineService = pipelineService;
        _repository = repository;
        _lookups = lookups;
        _settings = settings;
        _excelPath = excelPath;
    }

    public async Task RunAsync()
    {
        while (true)
        {
            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[green]What would you like to do?[/]")
                    .PageSize(15)
                    .AddChoices(new[]
                    {
                        "1. Load Excel Workbook",
                        "2. Save to Excel",
                        "3. Generate Synthetic Data",
                        "4. Daily Brief",
                        "5. Hygiene Report",
                        "6. Forecast Snapshot",
                        "7. Pipeline Statistics",
                        "8. Search / Deal Drilldown",
                        "9. Update Deal",
                        "10. Add New Deal",
                        "11. Delete Deal",
                        "12. List All Deals",
                        "13. Run Full Demo (Non-Interactive)",
                        "0. Exit"
                    }));

            AnsiConsole.WriteLine();

            try
            {
                switch (choice[0])
                {
                    case '1': await LoadExcelAsync(); break;
                    case '2': await SaveExcelAsync(); break;
                    case '3': await GenerateSyntheticDataAsync(); break;
                    case '4': await ShowDailyBriefAsync(); break;
                    case '5': await ShowHygieneReportAsync(); break;
                    case '6': await ShowForecastSnapshotAsync(); break;
                    case '7': await ShowStatisticsAsync(); break;
                    case '8': await SearchDealsAsync(); break;
                    case '9': await UpdateDealAsync(); break;
                    case '0' when choice.Contains("Add"): await AddNewDealAsync(); break;
                    case '0' when choice.Contains("Exit"): return;
                    default:
                        if (choice.StartsWith("10")) await AddNewDealAsync();
                        else if (choice.StartsWith("11")) await DeleteDealAsync();
                        else if (choice.StartsWith("12")) await ListAllDealsAsync();
                        else if (choice.StartsWith("13")) await RunFullDemoAsync();
                        else if (choice.StartsWith("0.")) return;
                        break;
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
            }

            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[grey]Press any key to continue...[/]");
            System.Console.ReadKey(true);
            AnsiConsole.Clear();
        }
    }

    private async Task LoadExcelAsync()
    {
        var path = AnsiConsole.Ask("[blue]Excel file path[/]:", _excelPath);

        if (!File.Exists(path))
        {
            AnsiConsole.MarkupLine("[red]File not found![/]");
            return;
        }

        await AnsiConsole.Status()
            .StartAsync("Loading...", async ctx =>
            {
                if (_repository is ExcelDealRepository excelRepo)
                {
                    await excelRepo.LoadAsync();
                    var deals = await _repository.GetAllAsync();
                    AnsiConsole.MarkupLine($"[green]Loaded {deals.Count} deals from {path}[/]");
                }
            });
    }

    private async Task SaveExcelAsync()
    {
        await AnsiConsole.Status()
            .StartAsync("Saving...", async ctx =>
            {
                await _repository.SaveChangesAsync();
                var deals = await _repository.GetAllAsync();
                AnsiConsole.MarkupLine($"[green]Saved {deals.Count} deals to {_excelPath}[/]");
            });
    }

    private async Task GenerateSyntheticDataAsync()
    {
        var count = AnsiConsole.Ask("[blue]Number of deals to generate[/]:", 100);
        var seed = AnsiConsole.Ask("[blue]Random seed[/]:", Environment.TickCount);
        var clearExisting = AnsiConsole.Confirm("Clear existing data first?", false);

        await AnsiConsole.Status()
            .StartAsync("Generating...", async ctx =>
            {
                if (clearExisting)
                {
                    var existing = await _repository.GetAllAsync();
                    foreach (var deal in existing)
                        await _repository.DeleteAsync(deal.DealId);
                }

                var generated = await _pipelineService.GenerateSyntheticDataAsync(count, seed);
                AnsiConsole.MarkupLine($"[green]Generated {generated} deals with seed {seed}[/]");
            });
    }

    private async Task ShowDailyBriefAsync()
    {
        var brief = await _pipelineService.GetDailyBriefAsync();

        var panel = new Panel(
            new Markup($"[bold]Reference Date:[/] {brief.ReferenceDate:dd MMM yyyy}\n" +
                       $"[bold]Total Actions:[/] {brief.TotalActionItems}\n" +
                       $"[bold]At Risk Value:[/] £{brief.TotalAtRiskValue:N0}"))
            .Header("[blue]Daily Brief[/]")
            .Border(BoxBorder.Rounded);

        AnsiConsole.Write(panel);
        AnsiConsole.WriteLine();

        if (brief.DueToday.Any())
        {
            AnsiConsole.MarkupLine("[yellow]Due Today[/]");
            DisplayActionTable(brief.DueToday);
        }

        if (brief.Overdue.Any())
        {
            AnsiConsole.MarkupLine("[red]Overdue[/]");
            DisplayActionTable(brief.Overdue);
        }

        if (brief.NoContactDeals.Any())
        {
            AnsiConsole.MarkupLine($"[orange1]No Contact ({_settings.NoContactThresholdDays}+ days)[/]");
            DisplayActionTable(brief.NoContactDeals.Take(10).ToList());
            if (brief.NoContactDeals.Count > 10)
                AnsiConsole.MarkupLine($"[grey]...and {brief.NoContactDeals.Count - 10} more[/]");
        }

        if (brief.HighValueAtRisk.Any())
        {
            AnsiConsole.MarkupLine("[red]High Value at Risk[/]");
            DisplayActionTable(brief.HighValueAtRisk);
        }
    }

    private void DisplayActionTable(List<DealAction> actions)
    {
        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("Deal")
            .AddColumn("Account")
            .AddColumn("Owner")
            .AddColumn("Amount")
            .AddColumn("Next Step")
            .AddColumn("Due");

        foreach (var action in actions)
        {
            var dueText = action.NextStepDueDate?.ToString("dd MMM") ?? "-";
            if (action.DaysOverdue > 0)
                dueText = $"[red]{dueText} ({action.DaysOverdue}d overdue)[/]";

            table.AddRow(
                Truncate(action.DealName, 25),
                Truncate(action.AccountName, 20),
                action.Owner ?? "-",
                action.Amount.HasValue ? $"£{action.Amount:N0}" : "-",
                Truncate(action.NextStep ?? "-", 20),
                dueText
            );
        }

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
    }

    private async Task ShowHygieneReportAsync()
    {
        var report = await _pipelineService.GetHygieneReportAsync();

        var healthColor = report.HealthScore >= 80 ? "green" : report.HealthScore >= 60 ? "yellow" : "red";

        var panel = new Panel(
            new Markup($"[bold]Health Score:[/] [{healthColor}]{report.HealthScore}%[/]\n" +
                       $"[bold]Total Deals:[/] {report.TotalDeals}\n" +
                       $"[bold]Deals with Issues:[/] {report.DealsWithIssues}"))
            .Header("[blue]Pipeline Hygiene[/]")
            .Border(BoxBorder.Rounded);

        AnsiConsole.Write(panel);
        AnsiConsole.WriteLine();

        if (report.IssuesByType.Any())
        {
            var chart = new BarChart()
                .Label("[bold]Issues by Type[/]")
                .CenterLabel();

            foreach (var kvp in report.IssuesByType.OrderByDescending(k => k.Value))
            {
                chart.AddItem(kvp.Key.ToDisplayString(), kvp.Value, GetSeverityColor(kvp.Key));
            }

            AnsiConsole.Write(chart);
            AnsiConsole.WriteLine();
        }

        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("Severity")
            .AddColumn("Deal")
            .AddColumn("Account")
            .AddColumn("Issue")
            .AddColumn("Description");

        foreach (var issue in report.Issues.Take(20))
        {
            var sevColor = issue.Severity switch
            {
                HygieneSeverity.Critical => "red",
                HygieneSeverity.High => "orange1",
                HygieneSeverity.Medium => "yellow",
                _ => "grey"
            };

            table.AddRow(
                $"[{sevColor}]{issue.Severity}[/]",
                Truncate(issue.DealName, 20),
                Truncate(issue.AccountName, 15),
                issue.IssueType.ToDisplayString(),
                Truncate(issue.Description, 40)
            );
        }

        AnsiConsole.Write(table);

        if (report.Issues.Count > 20)
            AnsiConsole.MarkupLine($"[grey]...and {report.Issues.Count - 20} more issues[/]");
    }

    private Color GetSeverityColor(HygieneIssueType type) => type switch
    {
        HygieneIssueType.MissingOwner => Color.Red,
        HygieneIssueType.MissingCloseDate => Color.Orange1,
        HygieneIssueType.ProbabilityStageMismatch => Color.Orange1,
        HygieneIssueType.MissingAmount => Color.Yellow,
        HygieneIssueType.MissingNextStep => Color.Yellow,
        _ => Color.Grey
    };

    private async Task ShowForecastSnapshotAsync()
    {
        var snapshot = await _pipelineService.GetForecastSnapshotAsync();

        var panel = new Panel(
            new Markup($"[bold]Total Pipeline:[/] £{snapshot.TotalPipeline:N0}\n" +
                       $"[bold]Weighted Pipeline:[/] £{snapshot.WeightedPipeline:N0}\n" +
                       $"[bold]Open Deals:[/] {snapshot.OpenDeals} / {snapshot.TotalDeals}"))
            .Header("[blue]Forecast Snapshot[/]")
            .Border(BoxBorder.Rounded);

        AnsiConsole.Write(panel);
        AnsiConsole.WriteLine();

        // By Stage
        if (snapshot.ByStage.Any())
        {
            AnsiConsole.MarkupLine("[bold]By Stage[/]");
            var stageTable = new Table()
                .Border(TableBorder.Simple)
                .AddColumn("Stage")
                .AddColumn(new TableColumn("Deals").RightAligned())
                .AddColumn(new TableColumn("Total").RightAligned())
                .AddColumn(new TableColumn("Weighted").RightAligned())
                .AddColumn(new TableColumn("%").RightAligned());

            foreach (var s in snapshot.ByStage)
            {
                stageTable.AddRow(
                    s.Stage.ToDisplayString(),
                    s.DealCount.ToString(),
                    $"£{s.TotalAmount:N0}",
                    $"£{s.WeightedAmount:N0}",
                    $"{s.PercentageOfPipeline:F1}%"
                );
            }
            AnsiConsole.Write(stageTable);
            AnsiConsole.WriteLine();
        }

        // By Owner
        if (snapshot.ByOwner.Any())
        {
            AnsiConsole.MarkupLine("[bold]By Owner[/]");
            var ownerTable = new Table()
                .Border(TableBorder.Simple)
                .AddColumn("Owner")
                .AddColumn(new TableColumn("Open").RightAligned())
                .AddColumn(new TableColumn("Pipeline").RightAligned())
                .AddColumn(new TableColumn("Weighted").RightAligned())
                .AddColumn(new TableColumn("Win Rate").RightAligned());

            foreach (var o in snapshot.ByOwner)
            {
                ownerTable.AddRow(
                    o.Owner,
                    o.DealCount.ToString(),
                    $"£{o.TotalAmount:N0}",
                    $"£{o.WeightedAmount:N0}",
                    $"{o.WinRate:F1}%"
                );
            }
            AnsiConsole.Write(ownerTable);
            AnsiConsole.WriteLine();
        }

        // By Close Month
        if (snapshot.ByCloseMonth.Any())
        {
            AnsiConsole.MarkupLine("[bold]By Close Month[/]");
            var monthChart = new BarChart().Label("Expected Revenue").CenterLabel();
            foreach (var m in snapshot.ByCloseMonth.Take(6))
            {
                monthChart.AddItem(m.MonthName, (double)m.WeightedAmount, Color.Blue);
            }
            AnsiConsole.Write(monthChart);
            AnsiConsole.WriteLine();
        }

        // By Region
        if (snapshot.ByRegion.Any())
        {
            AnsiConsole.MarkupLine("[bold]By Region[/]");
            var regionTable = new Table()
                .Border(TableBorder.Simple)
                .AddColumn("Region")
                .AddColumn(new TableColumn("Deals").RightAligned())
                .AddColumn(new TableColumn("Total").RightAligned());

            foreach (var r in snapshot.ByRegion.Take(8))
            {
                regionTable.AddRow(r.Region, r.DealCount.ToString(), $"£{r.TotalAmount:N0}");
            }
            AnsiConsole.Write(regionTable);
        }
    }

    private async Task ShowStatisticsAsync()
    {
        var stats = await _pipelineService.GetStatsAsync();

        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("Metric")
            .AddColumn("Value");

        table.AddRow("Total Deals", stats.TotalDeals.ToString());
        table.AddRow("Open Deals", stats.OpenDeals.ToString());
        table.AddRow("Closed Won", stats.ClosedWonDeals.ToString());
        table.AddRow("Closed Lost", stats.ClosedLostDeals.ToString());
        table.AddRow("Total Pipeline", $"£{stats.TotalPipeline:N0}");
        table.AddRow("Weighted Pipeline", $"£{stats.WeightedPipeline:N0}");
        table.AddRow("Closed Won Value", $"£{stats.ClosedWonValue:N0}");
        table.AddRow("Average Deal Value", $"£{stats.AverageDealsValue:N0}");
        table.AddRow("Sales Reps", string.Join(", ", stats.Owners));
        table.AddRow("Regions", string.Join(", ", stats.Regions));

        AnsiConsole.Write(table);
    }

    private async Task SearchDealsAsync()
    {
        var searchText = AnsiConsole.Ask<string>("[blue]Search (deal, account, contact, owner)[/]:");

        var filter = new DealFilter { SearchText = searchText };
        var deals = await _pipelineService.ListDealsAsync(filter);

        if (!deals.Any())
        {
            AnsiConsole.MarkupLine("[yellow]No deals found[/]");
            return;
        }

        AnsiConsole.MarkupLine($"[green]Found {deals.Count} deals[/]");
        DisplayDealsTable(deals.Take(20).ToList());

        if (deals.Count > 20)
            AnsiConsole.MarkupLine($"[grey]...and {deals.Count - 20} more[/]");

        if (deals.Count > 0)
        {
            var viewDetails = AnsiConsole.Confirm("View deal details?", false);
            if (viewDetails)
            {
                var dealId = AnsiConsole.Ask<string>("[blue]Enter Deal ID[/]:");
                var deal = await _pipelineService.GetDealAsync(dealId);
                if (deal != null)
                    DisplayDealDetails(deal);
                else
                    AnsiConsole.MarkupLine("[red]Deal not found[/]");
            }
        }
    }

    private void DisplayDealsTable(List<Deal> deals)
    {
        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("ID")
            .AddColumn("Deal")
            .AddColumn("Account")
            .AddColumn("Stage")
            .AddColumn("Amount")
            .AddColumn("Owner");

        foreach (var deal in deals)
        {
            table.AddRow(
                deal.DealId,
                Truncate(deal.DealName, 25),
                Truncate(deal.AccountName, 20),
                deal.Stage.ToDisplayString(),
                deal.AmountGBP.HasValue ? $"£{deal.AmountGBP:N0}" : "-",
                deal.Owner ?? "-"
            );
        }

        AnsiConsole.Write(table);
    }

    private void DisplayDealDetails(Deal deal)
    {
        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("Field")
            .AddColumn("Value");

        table.AddRow("Deal ID", deal.DealId);
        table.AddRow("Deal Name", deal.DealName);
        table.AddRow("Account", deal.AccountName);
        table.AddRow("Contact", deal.ContactName ?? "-");
        table.AddRow("Email", deal.Email ?? "-");
        table.AddRow("Phone", deal.Phone ?? "-");
        table.AddRow("Postcode", deal.Postcode ?? "-");
        table.AddRow("Region", deal.Region ?? "-");
        table.AddRow("Stage", deal.Stage.ToDisplayString());
        table.AddRow("Probability", $"{deal.Probability}%");
        table.AddRow("Amount", deal.AmountGBP.HasValue ? $"£{deal.AmountGBP:N0}" : "-");
        table.AddRow("Weighted", deal.WeightedAmountGBP.HasValue ? $"£{deal.WeightedAmountGBP:N0}" : "-");
        table.AddRow("Owner", deal.Owner ?? "-");
        table.AddRow("Created", deal.CreatedDate?.ToString("dd MMM yyyy") ?? "-");
        table.AddRow("Last Contact", deal.LastContactedDate?.ToString("dd MMM yyyy") ?? "-");
        table.AddRow("Next Step", deal.NextStep ?? "-");
        table.AddRow("Next Step Due", deal.NextStepDueDate?.ToString("dd MMM yyyy") ?? "-");
        table.AddRow("Close Date", deal.CloseDate?.ToString("dd MMM yyyy") ?? "-");
        table.AddRow("Product Line", deal.ProductLine ?? "-");
        table.AddRow("Lead Source", deal.LeadSource ?? "-");
        table.AddRow("Tags", deal.Tags.Any() ? string.Join(", ", deal.Tags) : "-");
        table.AddRow("Comments", deal.Comments ?? "-");

        AnsiConsole.Write(table);
    }

    private async Task UpdateDealAsync()
    {
        var dealId = AnsiConsole.Ask<string>("[blue]Enter Deal ID to update[/]:");
        var deal = await _pipelineService.GetDealAsync(dealId);

        if (deal == null)
        {
            AnsiConsole.MarkupLine("[red]Deal not found[/]");
            return;
        }

        DisplayDealDetails(deal);
        AnsiConsole.WriteLine();

        var fieldChoice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[blue]What would you like to update?[/]")
                .AddChoices(new[]
                {
                    "Stage", "Probability", "Amount", "Owner", "Next Step",
                    "Next Step Due Date", "Close Date", "Comments", "Cancel"
                }));

        if (fieldChoice == "Cancel") return;

        var patch = new Dictionary<string, object?>();

        switch (fieldChoice)
        {
            case "Stage":
                var stage = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("Select stage")
                        .AddChoices(Enum.GetValues<DealStage>().Select(s => s.ToDisplayString())));
                patch["Stage"] = stage;
                break;
            case "Probability":
                patch["Probability"] = AnsiConsole.Ask("[blue]New probability (0-100)[/]:", deal.Probability);
                break;
            case "Amount":
                patch["AmountGBP"] = AnsiConsole.Ask("[blue]New amount (GBP)[/]:", deal.AmountGBP ?? 0);
                break;
            case "Owner":
                patch["Owner"] = AnsiConsole.Ask("[blue]New owner[/]:", deal.Owner ?? "");
                break;
            case "Next Step":
                patch["NextStep"] = AnsiConsole.Ask("[blue]New next step[/]:", deal.NextStep ?? "");
                break;
            case "Next Step Due Date":
                var dueStr = AnsiConsole.Ask("[blue]Due date (yyyy-MM-dd)[/]:", deal.NextStepDueDate?.ToString("yyyy-MM-dd") ?? "");
                patch["NextStepDueDate"] = dueStr;
                break;
            case "Close Date":
                var closeStr = AnsiConsole.Ask("[blue]Close date (yyyy-MM-dd)[/]:", deal.CloseDate?.ToString("yyyy-MM-dd") ?? "");
                patch["CloseDate"] = closeStr;
                break;
            case "Comments":
                patch["Comments"] = AnsiConsole.Ask("[blue]Comments[/]:", deal.Comments ?? "");
                break;
        }

        if (patch.Any())
        {
            await _pipelineService.PatchDealAsync(dealId, patch);
            AnsiConsole.MarkupLine("[green]Deal updated![/]");
        }
    }

    private async Task AddNewDealAsync()
    {
        AnsiConsole.MarkupLine("[blue]Add New Deal[/]");

        var deal = new Deal
        {
            AccountName = AnsiConsole.Ask<string>("[blue]Account Name[/]:"),
            DealName = AnsiConsole.Ask<string>("[blue]Deal Name[/]:"),
            ContactName = AnsiConsole.Ask("[blue]Contact Name[/]:", ""),
            Email = AnsiConsole.Ask("[blue]Email[/]:", ""),
            Phone = AnsiConsole.Ask("[blue]Phone[/]:", ""),
            Postcode = AnsiConsole.Ask("[blue]Postcode[/]:", "")
        };

        deal.Stage = DealStageExtensions.ParseStage(
            AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Stage")
                    .AddChoices(Enum.GetValues<DealStage>().Select(s => s.ToDisplayString()))));

        var amountStr = AnsiConsole.Ask("[blue]Amount (GBP)[/]:", "0");
        deal.AmountGBP = decimal.TryParse(amountStr, out var amount) ? amount : null;

        deal.Owner = AnsiConsole.Ask("[blue]Owner[/]:", "");
        deal.ProductLine = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Product Line")
                .AddChoices(_settings.ProductLines));

        deal.LeadSource = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Lead Source")
                .AddChoices(_settings.LeadSources));

        deal.NextStep = AnsiConsole.Ask("[blue]Next Step[/]:", "");

        var result = await _pipelineService.UpsertDealAsync(deal);
        AnsiConsole.MarkupLine($"[green]Deal created with ID: {result.DealId}[/]");
    }

    private async Task DeleteDealAsync()
    {
        var dealId = AnsiConsole.Ask<string>("[blue]Enter Deal ID to delete[/]:");
        var deal = await _pipelineService.GetDealAsync(dealId);

        if (deal == null)
        {
            AnsiConsole.MarkupLine("[red]Deal not found[/]");
            return;
        }

        DisplayDealDetails(deal);

        if (AnsiConsole.Confirm($"[red]Delete this deal?[/]", false))
        {
            await _pipelineService.DeleteDealAsync(dealId);
            AnsiConsole.MarkupLine("[green]Deal deleted[/]");
        }
    }

    private async Task ListAllDealsAsync()
    {
        var deals = await _pipelineService.ListDealsAsync();

        if (!deals.Any())
        {
            AnsiConsole.MarkupLine("[yellow]No deals in pipeline[/]");
            return;
        }

        AnsiConsole.MarkupLine($"[green]Total: {deals.Count} deals[/]");

        var groupBy = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Group by?")
                .AddChoices("None", "Stage", "Owner", "Region"));

        if (groupBy == "None")
        {
            DisplayDealsTable(deals.Take(30).ToList());
            if (deals.Count > 30)
                AnsiConsole.MarkupLine($"[grey]...and {deals.Count - 30} more[/]");
        }
        else
        {
            var grouped = groupBy switch
            {
                "Stage" => deals.GroupBy(d => d.Stage.ToDisplayString()),
                "Owner" => deals.GroupBy(d => d.Owner ?? "Unassigned"),
                "Region" => deals.GroupBy(d => d.Region ?? "Unknown"),
                _ => deals.GroupBy(d => "All")
            };

            foreach (var group in grouped.OrderBy(g => g.Key))
            {
                AnsiConsole.MarkupLine($"\n[bold]{group.Key}[/] ({group.Count()} deals, £{group.Sum(d => d.AmountGBP ?? 0):N0})");
                DisplayDealsTable(group.Take(5).ToList());
                if (group.Count() > 5)
                    AnsiConsole.MarkupLine($"[grey]...and {group.Count() - 5} more[/]");
            }
        }
    }

    private static string Truncate(string text, int maxLength) =>
        text.Length > maxLength ? text[..(maxLength - 3)] + "..." : text;

    private async Task RunFullDemoAsync()
    {
        AnsiConsole.Write(new Rule("[blue]Ox4D Sales Pipeline Manager - Full Demo[/]").RuleStyle("blue"));
        AnsiConsole.WriteLine();

        // 1. Generate Synthetic Data
        AnsiConsole.MarkupLine("[bold yellow]>>> STEP 1: GENERATING 100 SYNTHETIC DEALS (seed=42)[/]");
        var existingDeals = await _repository.GetAllAsync();
        foreach (var d in existingDeals)
            await _repository.DeleteAsync(d.DealId);

        var count = await _pipelineService.GenerateSyntheticDataAsync(100, seed: 42);
        AnsiConsole.MarkupLine($"    Generated [green]{count}[/] deals\n");

        // 2. Pipeline Statistics
        AnsiConsole.MarkupLine("[bold yellow]>>> STEP 2: PIPELINE STATISTICS[/]");
        var stats = await _pipelineService.GetStatsAsync();
        var statsTable = new Table().Border(TableBorder.Simple);
        statsTable.AddColumn("Metric");
        statsTable.AddColumn(new TableColumn("Value").RightAligned());
        statsTable.AddRow("Total Deals", stats.TotalDeals.ToString());
        statsTable.AddRow("Open Deals", stats.OpenDeals.ToString());
        statsTable.AddRow("Closed Won", stats.ClosedWonDeals.ToString());
        statsTable.AddRow("Closed Lost", stats.ClosedLostDeals.ToString());
        statsTable.AddRow("Total Pipeline", $"£{stats.TotalPipeline:N0}");
        statsTable.AddRow("Weighted Pipeline", $"£{stats.WeightedPipeline:N0}");
        statsTable.AddRow("Closed Won Value", $"£{stats.ClosedWonValue:N0}");
        statsTable.AddRow("Avg Deal Value", $"£{stats.AverageDealsValue:N0}");
        statsTable.AddRow("Sales Reps", string.Join(", ", stats.Owners));
        statsTable.AddRow("Regions", string.Join(", ", stats.Regions.Take(5)) + "...");
        AnsiConsole.Write(statsTable);
        AnsiConsole.WriteLine();

        // 3. Daily Brief
        AnsiConsole.MarkupLine("[bold yellow]>>> STEP 3: DAILY BRIEF[/]");
        var brief = await _pipelineService.GetDailyBriefAsync();
        var briefTable = new Table().Border(TableBorder.Simple);
        briefTable.AddColumn("Metric");
        briefTable.AddColumn(new TableColumn("Value").RightAligned());
        briefTable.AddRow("Reference Date", brief.ReferenceDate.ToString("dd MMM yyyy"));
        briefTable.AddRow("Due Today", $"{brief.DueToday.Count} deals");
        briefTable.AddRow("Overdue", $"[red]{brief.Overdue.Count} deals[/]");
        briefTable.AddRow("No Contact (10d+)", $"[orange1]{brief.NoContactDeals.Count} deals[/]");
        briefTable.AddRow("High Value @ Risk", $"[red]{brief.HighValueAtRisk.Count} deals (£{brief.TotalAtRiskValue:N0})[/]");
        AnsiConsole.Write(briefTable);

        if (brief.Overdue.Any())
        {
            AnsiConsole.MarkupLine("\n    [bold]Top 5 Overdue:[/]");
            var overdueTable = new Table().Border(TableBorder.Rounded);
            overdueTable.AddColumn("Deal");
            overdueTable.AddColumn(new TableColumn("Amount").RightAligned());
            overdueTable.AddColumn("Days Overdue");
            foreach (var deal in brief.Overdue.Take(5))
            {
                overdueTable.AddRow(
                    Truncate(deal.DealName, 30),
                    $"£{deal.Amount ?? 0:N0}",
                    $"[red]{deal.DaysOverdue}d[/]");
            }
            AnsiConsole.Write(overdueTable);
        }
        AnsiConsole.WriteLine();

        // 4. Hygiene Report
        AnsiConsole.MarkupLine("[bold yellow]>>> STEP 4: HYGIENE REPORT[/]");
        var hygiene = await _pipelineService.GetHygieneReportAsync();
        var healthColor = hygiene.HealthScore >= 80 ? "green" : hygiene.HealthScore >= 60 ? "yellow" : "red";
        AnsiConsole.MarkupLine($"    Health Score:     [{healthColor}]{hygiene.HealthScore}%[/]");
        AnsiConsole.MarkupLine($"    Deals with Issues:[red]{hygiene.DealsWithIssues}[/] / {hygiene.TotalDeals}");
        AnsiConsole.MarkupLine("    [bold]Issues by Type:[/]");
        foreach (var (type, cnt) in hygiene.IssuesByType.OrderByDescending(x => x.Value).Take(5))
        {
            AnsiConsole.MarkupLine($"      - {type.ToDisplayString(),-30}: [yellow]{cnt}[/]");
        }
        AnsiConsole.WriteLine();

        // 5. Forecast Snapshot
        AnsiConsole.MarkupLine("[bold yellow]>>> STEP 5: FORECAST SNAPSHOT[/]");
        var forecast = await _pipelineService.GetForecastSnapshotAsync();
        AnsiConsole.MarkupLine($"    Total Pipeline:   [green]£{forecast.TotalPipeline:N0}[/]");
        AnsiConsole.MarkupLine($"    Weighted Pipeline:[green]£{forecast.WeightedPipeline:N0}[/]");
        AnsiConsole.MarkupLine($"    Open Deals:       {forecast.OpenDeals}");

        AnsiConsole.MarkupLine("\n    [bold]By Stage:[/]");
        var stageTable = new Table().Border(TableBorder.Simple);
        stageTable.AddColumn("Stage");
        stageTable.AddColumn(new TableColumn("Deals").RightAligned());
        stageTable.AddColumn(new TableColumn("Total").RightAligned());
        stageTable.AddColumn(new TableColumn("%").RightAligned());
        foreach (var stage in forecast.ByStage)
        {
            stageTable.AddRow(
                stage.Stage.ToDisplayString(),
                stage.DealCount.ToString(),
                $"£{stage.TotalAmount:N0}",
                $"{stage.PercentageOfPipeline:F1}%");
        }
        AnsiConsole.Write(stageTable);

        AnsiConsole.MarkupLine("\n    [bold]By Owner:[/]");
        var ownerTable = new Table().Border(TableBorder.Simple);
        ownerTable.AddColumn("Owner");
        ownerTable.AddColumn(new TableColumn("Open").RightAligned());
        ownerTable.AddColumn(new TableColumn("Pipeline").RightAligned());
        ownerTable.AddColumn(new TableColumn("Win Rate").RightAligned());
        foreach (var owner in forecast.ByOwner.Take(5))
        {
            ownerTable.AddRow(
                owner.Owner,
                owner.DealCount.ToString(),
                $"£{owner.TotalAmount:N0}",
                $"{owner.WinRate:F1}%");
        }
        AnsiConsole.Write(ownerTable);

        AnsiConsole.MarkupLine("\n    [bold]By Close Month:[/]");
        var monthTable = new Table().Border(TableBorder.Simple);
        monthTable.AddColumn("Month");
        monthTable.AddColumn(new TableColumn("Deals").RightAligned());
        monthTable.AddColumn(new TableColumn("Weighted").RightAligned());
        foreach (var month in forecast.ByCloseMonth.Take(4))
        {
            monthTable.AddRow(
                month.MonthName,
                month.DealCount.ToString(),
                $"£{month.WeightedAmount:N0}");
        }
        AnsiConsole.Write(monthTable);
        AnsiConsole.WriteLine();

        // 6. Sample Deal Lookup
        AnsiConsole.MarkupLine("[bold yellow]>>> STEP 6: SAMPLE DEAL LOOKUP[/]");
        var deals = await _pipelineService.ListDealsAsync();
        var sampleDeal = deals.FirstOrDefault();
        if (sampleDeal != null)
        {
            var dealTable = new Table().Border(TableBorder.Rounded);
            dealTable.AddColumn("Field");
            dealTable.AddColumn("Value");
            dealTable.AddRow("Deal ID", sampleDeal.DealId);
            dealTable.AddRow("Deal Name", sampleDeal.DealName);
            dealTable.AddRow("Account", sampleDeal.AccountName);
            dealTable.AddRow("Contact", sampleDeal.ContactName ?? "-");
            dealTable.AddRow("Stage", sampleDeal.Stage.ToDisplayString());
            dealTable.AddRow("Amount", $"£{sampleDeal.AmountGBP:N0}");
            dealTable.AddRow("Weighted", $"£{sampleDeal.WeightedAmountGBP:N0}");
            dealTable.AddRow("Owner", sampleDeal.Owner ?? "-");
            dealTable.AddRow("Region", sampleDeal.Region ?? "-");
            dealTable.AddRow("Next Step", sampleDeal.NextStep ?? "-");
            dealTable.AddRow("Tags", string.Join(", ", sampleDeal.Tags));
            AnsiConsole.Write(dealTable);
        }
        AnsiConsole.WriteLine();

        // 7. Save to Excel
        AnsiConsole.MarkupLine("[bold yellow]>>> STEP 7: SAVING TO EXCEL[/]");
        await _repository.SaveChangesAsync();
        var allDeals = await _repository.GetAllAsync();
        AnsiConsole.MarkupLine($"    Saved [green]{allDeals.Count}[/] deals to: [blue]{_excelPath}[/]\n");

        AnsiConsole.Write(new Rule("[green]Demo Complete![/]").RuleStyle("green"));
    }
}
