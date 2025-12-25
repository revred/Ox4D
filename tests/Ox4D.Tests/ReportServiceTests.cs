using FluentAssertions;
using Ox4D.Core.Models;
using Ox4D.Core.Models.Config;
using Ox4D.Core.Services;
using Ox4D.Storage;
using Xunit;

namespace Ox4D.Tests;

public class ReportServiceTests
{
    private readonly InMemoryDealRepository _repository;
    private readonly ReportService _reportService;
    private readonly PipelineSettings _settings;

    public ReportServiceTests()
    {
        _repository = new InMemoryDealRepository();
        _settings = new PipelineSettings { NoContactThresholdDays = 10 };
        _reportService = new ReportService(_repository, _settings);
    }

    [Fact]
    public async Task DailyBrief_IdentifiesOverdueDeals()
    {
        var today = new DateTime(2024, 6, 15);
        var deals = new List<Deal>
        {
            new Deal
            {
                DealId = "D-001",
                AccountName = "Test Co",
                DealName = "Test Deal",
                Stage = DealStage.Proposal,
                NextStep = "Follow up",
                NextStepDueDate = today.AddDays(-3), // 3 days overdue
                AmountGBP = 50000
            }
        };
        _repository.LoadDeals(deals);

        var brief = await _reportService.GenerateDailyBriefAsync(today);

        brief.Overdue.Should().HaveCount(1);
        brief.Overdue[0].DaysOverdue.Should().Be(3);
    }

    [Fact]
    public async Task DailyBrief_IdentifiesNoContactDeals()
    {
        var today = new DateTime(2024, 6, 15);
        var deals = new List<Deal>
        {
            new Deal
            {
                DealId = "D-001",
                AccountName = "Test Co",
                DealName = "Test Deal",
                Stage = DealStage.Proposal,
                LastContactedDate = today.AddDays(-15), // 15 days ago
                AmountGBP = 50000
            }
        };
        _repository.LoadDeals(deals);

        var brief = await _reportService.GenerateDailyBriefAsync(today);

        brief.NoContactDeals.Should().HaveCount(1);
    }

    [Fact]
    public async Task HygieneReport_DetectsMissingAmount()
    {
        var deals = new List<Deal>
        {
            new Deal
            {
                DealId = "D-001",
                AccountName = "Test Co",
                DealName = "Test Deal",
                Stage = DealStage.Proposal,
                AmountGBP = null // Missing
            }
        };
        _repository.LoadDeals(deals);

        var report = await _reportService.GenerateHygieneReportAsync();

        report.Issues.Should().Contain(i => i.IssueType == Core.Models.Reports.HygieneIssueType.MissingAmount);
    }

    [Fact]
    public async Task HygieneReport_DetectsMissingCloseDate_ForLateStages()
    {
        var deals = new List<Deal>
        {
            new Deal
            {
                DealId = "D-001",
                AccountName = "Test Co",
                DealName = "Test Deal",
                Stage = DealStage.Negotiation,
                CloseDate = null // Missing for late stage
            }
        };
        _repository.LoadDeals(deals);

        var report = await _reportService.GenerateHygieneReportAsync();

        report.Issues.Should().Contain(i => i.IssueType == Core.Models.Reports.HygieneIssueType.MissingCloseDate);
    }

    [Fact]
    public async Task ForecastSnapshot_CalculatesWeightedPipeline()
    {
        var deals = new List<Deal>
        {
            new Deal
            {
                DealId = "D-001",
                AccountName = "Test Co",
                DealName = "Deal 1",
                Stage = DealStage.Proposal,
                Probability = 60,
                AmountGBP = 100000
            },
            new Deal
            {
                DealId = "D-002",
                AccountName = "Test Co",
                DealName = "Deal 2",
                Stage = DealStage.Discovery,
                Probability = 40,
                AmountGBP = 50000
            }
        };
        _repository.LoadDeals(deals);

        var snapshot = await _reportService.GenerateForecastSnapshotAsync(DateTime.Today);

        snapshot.TotalPipeline.Should().Be(150000);
        snapshot.WeightedPipeline.Should().Be(60000 + 20000); // 60% of 100k + 40% of 50k
    }
}
