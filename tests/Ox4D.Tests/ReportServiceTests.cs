using FluentAssertions;
using Ox4D.Core.Models;
using Ox4D.Core.Models.Config;
using Ox4D.Core.Models.Reports;
using Ox4D.Core.Services;
using Ox4D.Store;
using Xunit;

namespace Ox4D.Tests;

/// <summary>
/// Tests for ReportService - Epic G3: Actionable Intelligence
/// Stories: S3.1.1-S3.1.4 (Daily Brief), S3.2.1-S3.2.4 (Hygiene), S3.3.1-S3.3.3 (Forecast)
/// </summary>
public class ReportServiceTests
{
    private readonly InMemoryDealRepository _repository;
    private readonly ReportService _reportService;
    private readonly PipelineSettings _settings;
    private readonly DateTime _today = new(2025, 1, 15);

    public ReportServiceTests()
    {
        _repository = new InMemoryDealRepository();
        _settings = new PipelineSettings
        {
            NoContactThresholdDays = 10,
            HighValueThreshold = 50000,
            HighValueTopN = 5
        };
        _reportService = new ReportService(_repository, _settings);
    }

    #region G3-001 to G3-004: Daily Brief - Overdue Actions

    [Fact]
    public async Task DailyBrief_IdentifiesOverdue_WhenNextStepPast()
    {
        var deals = new List<Deal>
        {
            CreateDeal("D-001", nextStepDueDate: _today.AddDays(-3))
        };
        _repository.LoadDeals(deals);

        var brief = await _reportService.GenerateDailyBriefAsync(_today);

        brief.Overdue.Should().HaveCount(1);
    }

    [Fact]
    public async Task DailyBrief_CalculatesOverdueDays_Correctly()
    {
        var deals = new List<Deal>
        {
            CreateDeal("D-001", nextStepDueDate: _today.AddDays(-5))
        };
        _repository.LoadDeals(deals);

        var brief = await _reportService.GenerateDailyBriefAsync(_today);

        brief.Overdue[0].DaysOverdue.Should().Be(5);
    }

    [Fact]
    public async Task DailyBrief_SortsOverdue_ByDateThenAmount()
    {
        var deals = new List<Deal>
        {
            CreateDeal("D-001", nextStepDueDate: _today.AddDays(-2), amount: 10000),
            CreateDeal("D-002", nextStepDueDate: _today.AddDays(-5), amount: 50000),
            CreateDeal("D-003", nextStepDueDate: _today.AddDays(-5), amount: 100000)
        };
        _repository.LoadDeals(deals);

        var brief = await _reportService.GenerateDailyBriefAsync(_today);

        brief.Overdue.Should().HaveCount(3);
        // Sorted by date ascending (oldest first), then by amount descending
        brief.Overdue[0].DealId.Should().Be("D-003"); // Oldest and highest value
        brief.Overdue[1].DealId.Should().Be("D-002");
    }

    [Fact]
    public async Task DailyBrief_ExcludesClosedDeals_FromOverdue()
    {
        var deals = new List<Deal>
        {
            CreateDeal("D-001", stage: DealStage.ClosedWon, nextStepDueDate: _today.AddDays(-5)),
            CreateDeal("D-002", stage: DealStage.Lead, nextStepDueDate: _today.AddDays(-5))
        };
        _repository.LoadDeals(deals);

        var brief = await _reportService.GenerateDailyBriefAsync(_today);

        brief.Overdue.Should().HaveCount(1);
        brief.Overdue[0].DealId.Should().Be("D-002");
    }

    #endregion

    #region G3-005 to G3-007: Daily Brief - Due Today

    [Fact]
    public async Task DailyBrief_IdentifiesDueToday_WhenMatchDate()
    {
        var deals = new List<Deal>
        {
            CreateDeal("D-001", nextStepDueDate: _today),
            CreateDeal("D-002", nextStepDueDate: _today.AddDays(1))
        };
        _repository.LoadDeals(deals);

        var brief = await _reportService.GenerateDailyBriefAsync(_today);

        brief.DueToday.Should().HaveCount(1);
        brief.DueToday[0].DealId.Should().Be("D-001");
    }

    [Fact]
    public async Task DailyBrief_SortsDueToday_ByAmount()
    {
        var deals = new List<Deal>
        {
            CreateDeal("D-001", nextStepDueDate: _today, amount: 10000),
            CreateDeal("D-002", nextStepDueDate: _today, amount: 100000),
            CreateDeal("D-003", nextStepDueDate: _today, amount: 50000)
        };
        _repository.LoadDeals(deals);

        var brief = await _reportService.GenerateDailyBriefAsync(_today);

        brief.DueToday[0].DealId.Should().Be("D-002"); // Highest amount first
    }

    [Fact]
    public async Task DailyBrief_UsesReferenceDate_NotSystemDate()
    {
        var referenceDate = new DateTime(2025, 6, 15);
        var deals = new List<Deal>
        {
            CreateDeal("D-001", nextStepDueDate: referenceDate)
        };
        _repository.LoadDeals(deals);

        var brief = await _reportService.GenerateDailyBriefAsync(referenceDate);

        brief.ReferenceDate.Should().Be(referenceDate);
        brief.DueToday.Should().HaveCount(1);
    }

    #endregion

    #region G3-008 to G3-010: Daily Brief - No Contact

    [Fact]
    public async Task DailyBrief_IdentifiesNoContact_WhenNeverContacted()
    {
        var deals = new List<Deal>
        {
            CreateDeal("D-001", setDefaultContactDate: false)
        };
        _repository.LoadDeals(deals);

        var brief = await _reportService.GenerateDailyBriefAsync(_today);

        brief.NoContactDeals.Should().HaveCount(1);
    }

    [Fact]
    public async Task DailyBrief_IdentifiesNoContact_WhenOverThreshold()
    {
        var deals = new List<Deal>
        {
            CreateDeal("D-001", lastContactedDate: _today.AddDays(-15)), // Over 10 days
            CreateDeal("D-002", lastContactedDate: _today.AddDays(-5))   // Under threshold
        };
        _repository.LoadDeals(deals);

        var brief = await _reportService.GenerateDailyBriefAsync(_today);

        brief.NoContactDeals.Should().HaveCount(1);
        brief.NoContactDeals[0].DealId.Should().Be("D-001");
    }

    [Fact]
    public async Task DailyBrief_UsesConfiguredThreshold_ForNoContact()
    {
        var customSettings = new PipelineSettings { NoContactThresholdDays = 5 };
        var service = new ReportService(_repository, customSettings);

        var deals = new List<Deal>
        {
            CreateDeal("D-001", lastContactedDate: _today.AddDays(-6))  // Over 5 days
        };
        _repository.LoadDeals(deals);

        var brief = await service.GenerateDailyBriefAsync(_today);

        brief.NoContactDeals.Should().HaveCount(1);
    }

    #endregion

    #region G3-011 to G3-014: Daily Brief - High Value At Risk

    [Fact]
    public async Task DailyBrief_IdentifiesHighValue_AtRisk()
    {
        var deals = new List<Deal>
        {
            CreateDeal("D-001", amount: 100000, lastContactedDate: _today.AddDays(-15)),
            CreateDeal("D-002", amount: 20000, lastContactedDate: _today.AddDays(-15))  // Not high value
        };
        _repository.LoadDeals(deals);

        var brief = await _reportService.GenerateDailyBriefAsync(_today);

        brief.HighValueAtRisk.Should().HaveCount(1);
        brief.HighValueAtRisk[0].DealId.Should().Be("D-001");
    }

    [Fact]
    public async Task DailyBrief_UsesConfiguredThreshold_ForHighValue()
    {
        var customSettings = new PipelineSettings { HighValueThreshold = 100000, NoContactThresholdDays = 10 };
        var service = new ReportService(_repository, customSettings);

        var deals = new List<Deal>
        {
            CreateDeal("D-001", amount: 75000, lastContactedDate: _today.AddDays(-15)),   // Below threshold
            CreateDeal("D-002", amount: 150000, lastContactedDate: _today.AddDays(-15))  // Above threshold
        };
        _repository.LoadDeals(deals);

        var brief = await service.GenerateDailyBriefAsync(_today);

        brief.HighValueAtRisk.Should().HaveCount(1);
        brief.HighValueAtRisk[0].DealId.Should().Be("D-002");
    }

    [Fact]
    public async Task DailyBrief_DeterminesRiskReason_Correctly()
    {
        var deals = new List<Deal>
        {
            CreateDeal("D-001", amount: 100000, lastContactedDate: _today.AddDays(-15), nextStepDueDate: _today.AddDays(-5))
        };
        _repository.LoadDeals(deals);

        var brief = await _reportService.GenerateDailyBriefAsync(_today);

        brief.HighValueAtRisk[0].RiskReason.Should().Contain("days");
    }

    [Fact]
    public async Task DailyBrief_LimitsHighValueAtRisk_ToTopN()
    {
        var deals = Enumerable.Range(1, 10)
            .Select(i => CreateDeal($"D-{i:D3}", amount: 100000, lastContactedDate: _today.AddDays(-15)))
            .ToList();
        _repository.LoadDeals(deals);

        var brief = await _reportService.GenerateDailyBriefAsync(_today);

        brief.HighValueAtRisk.Should().HaveCount(5); // HighValueTopN = 5
    }

    #endregion

    #region G3-015 to G3-017: Hygiene Report - Missing Amounts

    [Fact]
    public async Task HygieneReport_DetectsMissingAmount_WhenNull()
    {
        var deals = new List<Deal>
        {
            CreateDeal("D-001", amount: null)
        };
        _repository.LoadDeals(deals);

        var report = await _reportService.GenerateHygieneReportAsync();

        report.Issues.Should().Contain(i => i.IssueType == HygieneIssueType.MissingAmount);
    }

    [Fact]
    public async Task HygieneReport_SetsCorrectSeverity_ForMissingAmount()
    {
        var deals = new List<Deal>
        {
            CreateDeal("D-001", amount: null)
        };
        _repository.LoadDeals(deals);

        var report = await _reportService.GenerateHygieneReportAsync();

        var issue = report.Issues.First(i => i.IssueType == HygieneIssueType.MissingAmount);
        issue.Severity.Should().Be(HygieneSeverity.Medium);
    }

    #endregion

    #region G3-018 to G3-021: Hygiene Report - Missing Close Dates

    [Fact]
    public async Task HygieneReport_DetectsMissingCloseDate_ForProposal()
    {
        var deals = new List<Deal>
        {
            CreateDeal("D-001", stage: DealStage.Proposal, closeDate: null)
        };
        _repository.LoadDeals(deals);

        var report = await _reportService.GenerateHygieneReportAsync();

        report.Issues.Should().Contain(i => i.IssueType == HygieneIssueType.MissingCloseDate);
    }

    [Fact]
    public async Task HygieneReport_DetectsMissingCloseDate_ForNegotiation()
    {
        var deals = new List<Deal>
        {
            CreateDeal("D-001", stage: DealStage.Negotiation, closeDate: null)
        };
        _repository.LoadDeals(deals);

        var report = await _reportService.GenerateHygieneReportAsync();

        report.Issues.Should().Contain(i => i.IssueType == HygieneIssueType.MissingCloseDate);
    }

    [Fact]
    public async Task HygieneReport_IgnoresMissingCloseDate_ForEarlyStage()
    {
        var deals = new List<Deal>
        {
            CreateDeal("D-001", stage: DealStage.Lead, closeDate: null),
            CreateDeal("D-002", stage: DealStage.Qualified, closeDate: null)
        };
        _repository.LoadDeals(deals);

        var report = await _reportService.GenerateHygieneReportAsync();

        report.Issues.Should().NotContain(i => i.IssueType == HygieneIssueType.MissingCloseDate);
    }

    [Fact]
    public async Task HygieneReport_SetsHighSeverity_ForMissingCloseDate()
    {
        var deals = new List<Deal>
        {
            CreateDeal("D-001", stage: DealStage.Negotiation, closeDate: null)
        };
        _repository.LoadDeals(deals);

        var report = await _reportService.GenerateHygieneReportAsync();

        var issue = report.Issues.First(i => i.IssueType == HygieneIssueType.MissingCloseDate);
        issue.Severity.Should().Be(HygieneSeverity.High);
    }

    #endregion

    #region G3-022 to G3-025: Hygiene Report - Probability Mismatch

    [Fact]
    public async Task HygieneReport_DetectsMismatch_WhenProbabilityTooHigh()
    {
        var deals = new List<Deal>
        {
            new Deal
            {
                DealId = "D-001",
                AccountName = "Test",
                DealName = "Test",
                Stage = DealStage.Lead,
                Probability = 80 // Expected 10 for Lead, diff is 70
            }
        };
        _repository.LoadDeals(deals);

        var report = await _reportService.GenerateHygieneReportAsync();

        report.Issues.Should().Contain(i => i.IssueType == HygieneIssueType.ProbabilityStageMismatch);
    }

    [Fact]
    public async Task HygieneReport_DetectsMismatch_WhenProbabilityTooLow()
    {
        var deals = new List<Deal>
        {
            new Deal
            {
                DealId = "D-001",
                AccountName = "Test",
                DealName = "Test",
                Stage = DealStage.Negotiation,
                Probability = 20 // Expected 80 for Negotiation, diff is 60
            }
        };
        _repository.LoadDeals(deals);

        var report = await _reportService.GenerateHygieneReportAsync();

        report.Issues.Should().Contain(i => i.IssueType == HygieneIssueType.ProbabilityStageMismatch);
    }

    [Fact]
    public async Task HygieneReport_AllowsReasonableVariance_InProbability()
    {
        var deals = new List<Deal>
        {
            new Deal
            {
                DealId = "D-001",
                AccountName = "Test",
                DealName = "Test",
                Stage = DealStage.Proposal,
                Probability = 50 // Expected 60, diff is only 10
            }
        };
        _repository.LoadDeals(deals);

        var report = await _reportService.GenerateHygieneReportAsync();

        report.Issues.Should().NotContain(i => i.IssueType == HygieneIssueType.ProbabilityStageMismatch);
    }

    #endregion

    #region G3-026 to G3-029: Hygiene Report - Missing Data

    [Fact]
    public async Task HygieneReport_DetectsMissingPostcode()
    {
        var deals = new List<Deal>
        {
            new Deal
            {
                DealId = "D-001",
                AccountName = "Test",
                DealName = "Test",
                Stage = DealStage.Lead,
                Postcode = null
            }
        };
        _repository.LoadDeals(deals);

        var report = await _reportService.GenerateHygieneReportAsync();

        report.Issues.Should().Contain(i => i.IssueType == HygieneIssueType.MissingPostcode);
    }

    [Fact]
    public async Task HygieneReport_DetectsMissingContactInfo()
    {
        var deals = new List<Deal>
        {
            new Deal
            {
                DealId = "D-001",
                AccountName = "Test",
                DealName = "Test",
                Stage = DealStage.Lead,
                Email = null,
                Phone = null
            }
        };
        _repository.LoadDeals(deals);

        var report = await _reportService.GenerateHygieneReportAsync();

        report.Issues.Should().Contain(i => i.IssueType == HygieneIssueType.MissingContactInfo);
    }

    [Fact]
    public async Task HygieneReport_DetectsMissingOwner()
    {
        var deals = new List<Deal>
        {
            new Deal
            {
                DealId = "D-001",
                AccountName = "Test",
                DealName = "Test",
                Stage = DealStage.Lead,
                Owner = null
            }
        };
        _repository.LoadDeals(deals);

        var report = await _reportService.GenerateHygieneReportAsync();

        report.Issues.Should().Contain(i => i.IssueType == HygieneIssueType.MissingOwner);
    }

    [Fact]
    public async Task HygieneReport_DetectsMissingNextStep()
    {
        var deals = new List<Deal>
        {
            new Deal
            {
                DealId = "D-001",
                AccountName = "Test",
                DealName = "Test",
                Stage = DealStage.Lead,
                NextStep = null
            }
        };
        _repository.LoadDeals(deals);

        var report = await _reportService.GenerateHygieneReportAsync();

        report.Issues.Should().Contain(i => i.IssueType == HygieneIssueType.MissingNextStep);
    }

    #endregion

    #region G3-030 to G3-033: Forecast Snapshot - Pipeline Calculations

    [Fact]
    public async Task ForecastSnapshot_CalculatesTotalPipeline_Correctly()
    {
        var deals = new List<Deal>
        {
            CreateDeal("D-001", stage: DealStage.Lead, amount: 50000),
            CreateDeal("D-002", stage: DealStage.Proposal, amount: 100000),
            CreateDeal("D-003", stage: DealStage.ClosedWon, amount: 75000) // Excluded
        };
        _repository.LoadDeals(deals);

        var snapshot = await _reportService.GenerateForecastSnapshotAsync(_today);

        snapshot.TotalPipeline.Should().Be(150000);
    }

    [Fact]
    public async Task ForecastSnapshot_CalculatesWeightedPipeline_Correctly()
    {
        var deals = new List<Deal>
        {
            new Deal { DealId = "D-001", AccountName = "Test", DealName = "Test", Stage = DealStage.Lead, AmountGBP = 100000, Probability = 10 },
            new Deal { DealId = "D-002", AccountName = "Test", DealName = "Test", Stage = DealStage.Proposal, AmountGBP = 100000, Probability = 60 }
        };
        _repository.LoadDeals(deals);

        var snapshot = await _reportService.GenerateForecastSnapshotAsync(_today);

        snapshot.WeightedPipeline.Should().Be(10000 + 60000); // 70000
    }

    [Fact]
    public async Task ForecastSnapshot_ExcludesClosedDeals_FromPipeline()
    {
        var deals = new List<Deal>
        {
            CreateDeal("D-001", stage: DealStage.ClosedWon, amount: 100000),
            CreateDeal("D-002", stage: DealStage.ClosedLost, amount: 50000),
            CreateDeal("D-003", stage: DealStage.Lead, amount: 25000)
        };
        _repository.LoadDeals(deals);

        var snapshot = await _reportService.GenerateForecastSnapshotAsync(_today);

        snapshot.TotalPipeline.Should().Be(25000);
        snapshot.OpenDeals.Should().Be(1);
    }

    [Fact]
    public async Task ForecastSnapshot_HandlesNullAmounts_Gracefully()
    {
        var deals = new List<Deal>
        {
            CreateDeal("D-001", amount: null),
            CreateDeal("D-002", amount: 50000)
        };
        _repository.LoadDeals(deals);

        var snapshot = await _reportService.GenerateForecastSnapshotAsync(_today);

        snapshot.TotalPipeline.Should().Be(50000);
    }

    #endregion

    #region G3-034 to G3-040: Forecast Snapshot - Breakdowns

    [Fact]
    public async Task ForecastSnapshot_GroupsByStage_Correctly()
    {
        var deals = new List<Deal>
        {
            CreateDeal("D-001", stage: DealStage.Lead),
            CreateDeal("D-002", stage: DealStage.Lead),
            CreateDeal("D-003", stage: DealStage.Proposal)
        };
        _repository.LoadDeals(deals);

        var snapshot = await _reportService.GenerateForecastSnapshotAsync(_today);

        snapshot.ByStage.Should().Contain(s => s.Stage == DealStage.Lead && s.DealCount == 2);
        snapshot.ByStage.Should().Contain(s => s.Stage == DealStage.Proposal && s.DealCount == 1);
    }

    [Fact]
    public async Task ForecastSnapshot_GroupsByOwner_Correctly()
    {
        var deals = new List<Deal>
        {
            CreateDeal("D-001", owner: "Alice"),
            CreateDeal("D-002", owner: "Alice"),
            CreateDeal("D-003", owner: "Bob")
        };
        _repository.LoadDeals(deals);

        var snapshot = await _reportService.GenerateForecastSnapshotAsync(_today);

        snapshot.ByOwner.Should().Contain(o => o.Owner == "Alice" && o.DealCount == 2);
        snapshot.ByOwner.Should().Contain(o => o.Owner == "Bob" && o.DealCount == 1);
    }

    [Fact]
    public async Task ForecastSnapshot_GroupsByRegion_Correctly()
    {
        var deals = new List<Deal>
        {
            CreateDeal("D-001", region: "London"),
            CreateDeal("D-002", region: "London"),
            CreateDeal("D-003", region: "Scotland")
        };
        _repository.LoadDeals(deals);

        var snapshot = await _reportService.GenerateForecastSnapshotAsync(_today);

        snapshot.ByRegion.Should().Contain(r => r.Region == "London" && r.DealCount == 2);
        snapshot.ByRegion.Should().Contain(r => r.Region == "Scotland" && r.DealCount == 1);
    }

    [Fact]
    public async Task ForecastSnapshot_GroupsByCloseMonth_Correctly()
    {
        var deals = new List<Deal>
        {
            CreateDeal("D-001", closeDate: new DateTime(2025, 2, 15)),
            CreateDeal("D-002", closeDate: new DateTime(2025, 2, 20)),
            CreateDeal("D-003", closeDate: new DateTime(2025, 3, 10))
        };
        _repository.LoadDeals(deals);

        var snapshot = await _reportService.GenerateForecastSnapshotAsync(_today);

        snapshot.ByCloseMonth.Should().Contain(m => m.Year == 2025 && m.Month == 2 && m.DealCount == 2);
        snapshot.ByCloseMonth.Should().Contain(m => m.Year == 2025 && m.Month == 3 && m.DealCount == 1);
    }

    [Fact]
    public async Task ForecastSnapshot_GroupsByProduct_Correctly()
    {
        var deals = new List<Deal>
        {
            CreateDeal("D-001", productLine: "Enterprise"),
            CreateDeal("D-002", productLine: "Enterprise"),
            CreateDeal("D-003", productLine: "SaaS")
        };
        _repository.LoadDeals(deals);

        var snapshot = await _reportService.GenerateForecastSnapshotAsync(_today);

        snapshot.ByProduct.Should().Contain(p => p.ProductLine == "Enterprise" && p.DealCount == 2);
        snapshot.ByProduct.Should().Contain(p => p.ProductLine == "SaaS" && p.DealCount == 1);
    }

    [Fact]
    public async Task ForecastSnapshot_CalculatesOwnerWinRate_Correctly()
    {
        var deals = new List<Deal>
        {
            CreateDeal("D-001", owner: "Alice", stage: DealStage.ClosedWon, amount: 50000),
            CreateDeal("D-002", owner: "Alice", stage: DealStage.ClosedLost, amount: 30000),
            CreateDeal("D-003", owner: "Alice", stage: DealStage.Lead, amount: 20000)
        };
        _repository.LoadDeals(deals);

        var snapshot = await _reportService.GenerateForecastSnapshotAsync(_today);

        var alice = snapshot.ByOwner.First(o => o.Owner == "Alice");
        alice.WinRate.Should().Be(50); // 1 won, 1 lost = 50%
    }

    #endregion

    #region Health Score and Report Totals

    [Fact]
    public async Task HygieneReport_CalculatesTotalDeals()
    {
        var deals = Enumerable.Range(1, 10)
            .Select(i => CreateDeal($"D-{i:D3}"))
            .ToList();
        _repository.LoadDeals(deals);

        var report = await _reportService.GenerateHygieneReportAsync();

        report.TotalDeals.Should().Be(10);
    }

    [Fact]
    public async Task HygieneReport_CalculatesDealsWithIssues()
    {
        var deals = new List<Deal>
        {
            new Deal { DealId = "D-001", AccountName = "Test", DealName = "Test", Stage = DealStage.Lead, AmountGBP = null },
            CreateDeal("D-002", amount: 50000) // No issues
        };
        _repository.LoadDeals(deals);

        var report = await _reportService.GenerateHygieneReportAsync();

        report.DealsWithIssues.Should().BeGreaterThan(0);
    }

    #endregion

    #region Helper Methods

    private static Deal CreateDeal(
        string dealId,
        DealStage stage = DealStage.Lead,
        decimal? amount = 50000,
        string owner = "Test Owner",
        string region = "London",
        DateTime? nextStepDueDate = null,
        DateTime? lastContactedDate = null,
        DateTime? closeDate = null,
        string productLine = "Enterprise",
        bool setDefaultContactDate = true)
    {
        return new Deal
        {
            DealId = dealId,
            AccountName = $"Account {dealId}",
            DealName = $"Deal {dealId}",
            Stage = stage,
            AmountGBP = amount,
            Probability = stage.GetDefaultProbability(),
            Owner = owner,
            Region = region,
            NextStep = "Follow up",
            NextStepDueDate = nextStepDueDate,
            LastContactedDate = lastContactedDate ?? (setDefaultContactDate ? DateTime.Today.AddDays(-5) : null),
            CloseDate = closeDate,
            ProductLine = productLine,
            Email = "test@example.com",
            Phone = "0123456789",
            Postcode = "SW1A 1AA"
        };
    }

    #endregion
}
