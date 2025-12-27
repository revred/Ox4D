using FluentAssertions;
using Ox4D.Core.Models;
using Ox4D.Core.Models.Config;
using Ox4D.Core.Models.Reports;
using Ox4D.Core.Services;
using Ox4D.Store;
using Xunit;

namespace Ox4D.Tests;

/// <summary>
/// Tests for PromoterService - Epic G4: Referral Partner Support
/// Stories: S4.2.1-S4.2.4 (Dashboard), S4.3.1-S4.3.3 (Actions), S4.4.1-S4.4.3 (MCP Tools)
/// </summary>
public class PromoterServiceTests
{
    private readonly PromoterService _service;
    private readonly InMemoryDealRepository _repository;
    private readonly PipelineSettings _settings;
    private readonly DateTime _today = new(2025, 1, 15);

    public PromoterServiceTests()
    {
        _repository = new InMemoryDealRepository();
        _settings = new PipelineSettings
        {
            NoContactThresholdDays = 10,
            HighValueThreshold = 50000
        };
        _service = new PromoterService(_repository, _settings);
    }

    #region G4-020 to G4-028: Dashboard - Pipeline Metrics

    [Fact]
    public async Task Dashboard_CalculatesTotalReferrals()
    {
        // Arrange
        await LoadPromoterDeals("PROMO-001", 5);

        // Act
        var dashboard = await _service.GetPromoterDashboardAsync(
            "PROMO-001", "Test Promoter", "PROMO-001",
            PromoterTier.Bronze, _today);

        // Assert
        dashboard.Summary.TotalReferrals.Should().Be(5);
    }

    [Fact]
    public async Task Dashboard_CalculatesActiveDeals()
    {
        // Arrange
        var deals = new List<Deal>
        {
            CreatePromoterDeal("D-001", "PROMO-001", stage: DealStage.Lead),
            CreatePromoterDeal("D-002", "PROMO-001", stage: DealStage.Proposal),
            CreatePromoterDeal("D-003", "PROMO-001", stage: DealStage.ClosedWon)
        };
        _repository.LoadDeals(deals);

        // Act
        var dashboard = await _service.GetPromoterDashboardAsync(
            "PROMO-001", "Test Promoter", "PROMO-001",
            PromoterTier.Bronze, _today);

        // Assert
        dashboard.Summary.ActiveDeals.Should().Be(2);
    }

    [Fact]
    public async Task Dashboard_CalculatesClosedWon()
    {
        // Arrange
        var deals = new List<Deal>
        {
            CreatePromoterDeal("D-001", "PROMO-001", stage: DealStage.ClosedWon),
            CreatePromoterDeal("D-002", "PROMO-001", stage: DealStage.ClosedWon),
            CreatePromoterDeal("D-003", "PROMO-001", stage: DealStage.Lead)
        };
        _repository.LoadDeals(deals);

        // Act
        var dashboard = await _service.GetPromoterDashboardAsync(
            "PROMO-001", "Test Promoter", "PROMO-001",
            PromoterTier.Bronze, _today);

        // Assert
        dashboard.Summary.ClosedWon.Should().Be(2);
    }

    [Fact]
    public async Task Dashboard_CalculatesClosedLost()
    {
        // Arrange
        var deals = new List<Deal>
        {
            CreatePromoterDeal("D-001", "PROMO-001", stage: DealStage.ClosedLost),
            CreatePromoterDeal("D-002", "PROMO-001", stage: DealStage.Lead)
        };
        _repository.LoadDeals(deals);

        // Act
        var dashboard = await _service.GetPromoterDashboardAsync(
            "PROMO-001", "Test Promoter", "PROMO-001",
            PromoterTier.Bronze, _today);

        // Assert
        dashboard.Summary.ClosedLost.Should().Be(1);
    }

    [Fact]
    public async Task Dashboard_CalculatesConversionRate()
    {
        // Arrange
        var deals = new List<Deal>
        {
            CreatePromoterDeal("D-001", "PROMO-001", stage: DealStage.ClosedWon),
            CreatePromoterDeal("D-002", "PROMO-001", stage: DealStage.ClosedLost),
            CreatePromoterDeal("D-003", "PROMO-001", stage: DealStage.ClosedWon),
            CreatePromoterDeal("D-004", "PROMO-001", stage: DealStage.ClosedLost)
        };
        _repository.LoadDeals(deals);

        // Act
        var dashboard = await _service.GetPromoterDashboardAsync(
            "PROMO-001", "Test Promoter", "PROMO-001",
            PromoterTier.Bronze, _today);

        // Assert
        dashboard.Summary.ConversionRate.Should().Be(50m); // 2 won / 4 closed = 50%
    }

    [Fact]
    public async Task Dashboard_CalculatesTotalPipelineValue()
    {
        // Arrange
        var deals = new List<Deal>
        {
            CreatePromoterDeal("D-001", "PROMO-001", stage: DealStage.Lead, amount: 50000),
            CreatePromoterDeal("D-002", "PROMO-001", stage: DealStage.Proposal, amount: 100000),
            CreatePromoterDeal("D-003", "PROMO-001", stage: DealStage.ClosedWon, amount: 75000)
        };
        _repository.LoadDeals(deals);

        // Act
        var dashboard = await _service.GetPromoterDashboardAsync(
            "PROMO-001", "Test Promoter", "PROMO-001",
            PromoterTier.Bronze, _today);

        // Assert
        dashboard.Summary.TotalPipelineValue.Should().Be(150000); // Only open deals
    }

    [Fact]
    public async Task Dashboard_CalculatesWeightedPipelineValue()
    {
        // Arrange
        var deals = new List<Deal>
        {
            CreatePromoterDeal("D-001", "PROMO-001", stage: DealStage.Lead, amount: 100000, probability: 10),
            CreatePromoterDeal("D-002", "PROMO-001", stage: DealStage.Proposal, amount: 100000, probability: 60)
        };
        _repository.LoadDeals(deals);

        // Act
        var dashboard = await _service.GetPromoterDashboardAsync(
            "PROMO-001", "Test Promoter", "PROMO-001",
            PromoterTier.Bronze, _today);

        // Assert
        dashboard.Summary.WeightedPipelineValue.Should().Be(70000); // 10k + 60k
    }

    [Fact]
    public async Task Dashboard_CalculatesTotalWonValue()
    {
        // Arrange
        var deals = new List<Deal>
        {
            CreatePromoterDeal("D-001", "PROMO-001", stage: DealStage.ClosedWon, amount: 50000),
            CreatePromoterDeal("D-002", "PROMO-001", stage: DealStage.ClosedWon, amount: 75000),
            CreatePromoterDeal("D-003", "PROMO-001", stage: DealStage.Lead, amount: 25000)
        };
        _repository.LoadDeals(deals);

        // Act
        var dashboard = await _service.GetPromoterDashboardAsync(
            "PROMO-001", "Test Promoter", "PROMO-001",
            PromoterTier.Bronze, _today);

        // Assert
        dashboard.Summary.TotalWonValue.Should().Be(125000);
    }

    #endregion

    #region G4-028 to G4-031: Commission Tracking

    [Fact]
    public async Task Dashboard_TracksCommissionEarned()
    {
        // Arrange
        var deals = new List<Deal>
        {
            CreatePromoterDeal("D-001", "PROMO-001", stage: DealStage.ClosedWon, amount: 100000, commissionPaid: true),
            CreatePromoterDeal("D-002", "PROMO-001", stage: DealStage.ClosedWon, amount: 50000, commissionPaid: false)
        };
        _repository.LoadDeals(deals);

        // Act
        var dashboard = await _service.GetPromoterDashboardAsync(
            "PROMO-001", "Test Promoter", "PROMO-001",
            PromoterTier.Bronze, _today);

        // Assert - Bronze = 10% commission
        dashboard.CommissionSummary.TotalEarned.Should().Be(15000); // 10% of 150k
    }

    [Fact]
    public async Task Dashboard_TracksPaidCommission()
    {
        // Arrange
        var deals = new List<Deal>
        {
            CreatePromoterDeal("D-001", "PROMO-001", stage: DealStage.ClosedWon, amount: 100000, commissionPaid: true),
            CreatePromoterDeal("D-002", "PROMO-001", stage: DealStage.ClosedWon, amount: 50000, commissionPaid: false)
        };
        _repository.LoadDeals(deals);

        // Act
        var dashboard = await _service.GetPromoterDashboardAsync(
            "PROMO-001", "Test Promoter", "PROMO-001",
            PromoterTier.Bronze, _today);

        // Assert - Bronze = 10% commission
        dashboard.CommissionSummary.TotalPaid.Should().Be(10000); // 10% of 100k (paid)
    }

    [Fact]
    public async Task Dashboard_TracksPendingCommission()
    {
        // Arrange
        var deals = new List<Deal>
        {
            CreatePromoterDeal("D-001", "PROMO-001", stage: DealStage.ClosedWon, amount: 100000, commissionPaid: true),
            CreatePromoterDeal("D-002", "PROMO-001", stage: DealStage.ClosedWon, amount: 50000, commissionPaid: false)
        };
        _repository.LoadDeals(deals);

        // Act
        var dashboard = await _service.GetPromoterDashboardAsync(
            "PROMO-001", "Test Promoter", "PROMO-001",
            PromoterTier.Bronze, _today);

        // Assert - Bronze = 10% commission
        dashboard.CommissionSummary.PendingPayment.Should().Be(5000); // 10% of 50k (not paid)
    }

    [Fact]
    public async Task Dashboard_CalculatesProjectedCommission()
    {
        // Arrange
        var deals = new List<Deal>
        {
            CreatePromoterDeal("D-001", "PROMO-001", stage: DealStage.Proposal, amount: 100000, probability: 60),
            CreatePromoterDeal("D-002", "PROMO-001", stage: DealStage.Lead, amount: 50000, probability: 10)
        };
        _repository.LoadDeals(deals);

        // Act
        var dashboard = await _service.GetPromoterDashboardAsync(
            "PROMO-001", "Test Promoter", "PROMO-001",
            PromoterTier.Bronze, _today);

        // Assert - Bronze = 10% commission, weighted pipeline = 65k
        dashboard.CommissionSummary.ProjectedFromPipeline.Should().Be(6500); // 10% of 65k
    }

    #endregion

    #region G4-032 to G4-034: Deal Health

    [Fact]
    public async Task Dashboard_CountsStalledDeals()
    {
        // Arrange
        var deals = new List<Deal>
        {
            CreatePromoterDeal("D-001", "PROMO-001", createdDate: _today.AddDays(-30), lastContactedDate: _today.AddDays(-20)),
        };
        _repository.LoadDeals(deals);

        // Act
        var dashboard = await _service.GetPromoterDashboardAsync(
            "PROMO-001", "Test Promoter", "PROMO-001",
            PromoterTier.Bronze, _today);

        // Assert
        dashboard.DealsNeedingAttention.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Dashboard_CountsAtRiskDeals()
    {
        // Arrange
        var deals = new List<Deal>
        {
            CreatePromoterDeal("D-001", "PROMO-001", lastContactedDate: _today.AddDays(-12)),
        };
        _repository.LoadDeals(deals);

        // Act
        var dashboard = await _service.GetPromoterDashboardAsync(
            "PROMO-001", "Test Promoter", "PROMO-001",
            PromoterTier.Bronze, _today);

        // Assert
        dashboard.DealsNeedingAttention.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Dashboard_IdentifiesHealthyDeals()
    {
        // Arrange - deal with recent activity is healthy
        var deals = new List<Deal>
        {
            CreatePromoterDeal("D-001", "PROMO-001", lastContactedDate: _today.AddDays(-2), createdDate: _today.AddDays(-5)),
        };
        _repository.LoadDeals(deals);

        // Act
        var dashboard = await _service.GetPromoterDashboardAsync(
            "PROMO-001", "Test Promoter", "PROMO-001",
            PromoterTier.Bronze, _today);

        // Assert - healthy deals should not be in "needs attention"
        dashboard.DealsNeedingAttention.Should().BeEmpty();
    }

    #endregion

    #region G4-035 to G4-042: Promoter Actions

    [Fact]
    public async Task Actions_IdentifiesStalledReferrals()
    {
        // Arrange
        var deals = new List<Deal>
        {
            CreatePromoterDeal("D-001", "PROMO-001", lastContactedDate: _today.AddDays(-15)),
        };
        _repository.LoadDeals(deals);

        // Act
        var actions = await _service.GetRecommendedActionsAsync(
            "PROMO-001", "PROMO-001", 10m, _today);

        // Assert
        actions.Should().NotBeEmpty();
        actions.Should().Contain(a => a.ActionType == PromoterActionType.CheckIn);
    }

    [Fact]
    public async Task Actions_GeneratesCheckIn_ForNoActivity()
    {
        // Arrange
        var deals = new List<Deal>
        {
            CreatePromoterDeal("D-001", "PROMO-001", lastContactedDate: _today.AddDays(-14)),
        };
        _repository.LoadDeals(deals);

        // Act
        var actions = await _service.GetRecommendedActionsAsync(
            "PROMO-001", "PROMO-001", 10m, _today);

        // Assert
        actions.Should().Contain(a => a.ActionType == PromoterActionType.CheckIn);
    }

    [Fact]
    public async Task Actions_GeneratesContextAction_ForStaleLead()
    {
        // Arrange
        var deals = new List<Deal>
        {
            CreatePromoterDeal("D-001", "PROMO-001", stage: DealStage.Lead, createdDate: _today.AddDays(-10), lastContactedDate: _today.AddDays(-2)),
        };
        _repository.LoadDeals(deals);

        // Act
        var actions = await _service.GetRecommendedActionsAsync(
            "PROMO-001", "PROMO-001", 10m, _today);

        // Assert
        actions.Should().Contain(a => a.ActionType == PromoterActionType.ProvideContext);
    }

    [Fact]
    public async Task Actions_GeneratesIntroductionAction_ForDiscovery()
    {
        // Arrange
        var deals = new List<Deal>
        {
            CreatePromoterDeal("D-001", "PROMO-001", stage: DealStage.Discovery, createdDate: _today.AddDays(-20), lastContactedDate: _today.AddDays(-2)),
        };
        _repository.LoadDeals(deals);

        // Act
        var actions = await _service.GetRecommendedActionsAsync(
            "PROMO-001", "PROMO-001", 10m, _today);

        // Assert
        actions.Should().Contain(a => a.ActionType == PromoterActionType.MakeIntroduction);
    }

    [Fact]
    public async Task Actions_GeneratesReferenceAction_ForProposal()
    {
        // Arrange
        var deals = new List<Deal>
        {
            CreatePromoterDeal("D-001", "PROMO-001", stage: DealStage.Proposal, createdDate: _today.AddDays(-10), lastContactedDate: _today.AddDays(-2)),
        };
        _repository.LoadDeals(deals);

        // Act
        var actions = await _service.GetRecommendedActionsAsync(
            "PROMO-001", "PROMO-001", 10m, _today);

        // Assert
        actions.Should().Contain(a => a.ActionType == PromoterActionType.ProvideReference);
    }

    [Fact]
    public async Task Actions_GeneratesEscalation_ForNegotiation()
    {
        // Arrange
        var deals = new List<Deal>
        {
            CreatePromoterDeal("D-001", "PROMO-001", stage: DealStage.Negotiation, createdDate: _today.AddDays(-10), lastContactedDate: _today.AddDays(-2)),
        };
        _repository.LoadDeals(deals);

        // Act
        var actions = await _service.GetRecommendedActionsAsync(
            "PROMO-001", "PROMO-001", 10m, _today);

        // Assert
        actions.Should().Contain(a => a.ActionType == PromoterActionType.EscalateInternal);
    }

    [Fact]
    public async Task Actions_PrioritizesByPriority_ThenCommission()
    {
        // Arrange
        var deals = new List<Deal>
        {
            CreatePromoterDeal("D-001", "PROMO-001", amount: 10000, lastContactedDate: _today.AddDays(-15)),
            CreatePromoterDeal("D-002", "PROMO-001", amount: 100000, lastContactedDate: _today.AddDays(-15)),
        };
        _repository.LoadDeals(deals);

        // Act
        var actions = await _service.GetRecommendedActionsAsync(
            "PROMO-001", "PROMO-001", 10m, _today);

        // Assert - higher value deal should come first
        var firstDealActions = actions.Where(a => a.DealId == "D-002").ToList();
        var secondDealActions = actions.Where(a => a.DealId == "D-001").ToList();

        // The first action should be for the higher value deal when priorities are equal
        actions.First().PotentialCommission.Should().Be(10000m); // 10% of 100k
    }

    [Fact]
    public async Task Actions_CalculatesPotentialCommission()
    {
        // Arrange
        var deals = new List<Deal>
        {
            CreatePromoterDeal("D-001", "PROMO-001", amount: 50000, lastContactedDate: _today.AddDays(-15)),
        };
        _repository.LoadDeals(deals);

        // Act
        var actions = await _service.GetRecommendedActionsAsync(
            "PROMO-001", "PROMO-001", 15m, _today); // 15% commission rate

        // Assert
        actions.First().PotentialCommission.Should().Be(7500m); // 15% of 50k
    }

    #endregion

    #region G4-043 to G4-045: GetPromoterDeals

    [Fact]
    public async Task GetDeals_ReturnsOnlyPromoterDeals()
    {
        // Arrange
        var deals = new List<Deal>
        {
            CreatePromoterDeal("D-001", "PROMO-001"),
            CreatePromoterDeal("D-002", "PROMO-002"),
            CreatePromoterDeal("D-003", "PROMO-001"),
        };
        _repository.LoadDeals(deals);

        // Act
        var result = await _service.GetPromoterDealsAsync("PROMO-001", "PROMO-001", 10m, _today);

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(d => d.DealId == "D-001" || d.DealId == "D-003");
    }

    [Fact]
    public async Task GetDeals_MatchesByPromoCode()
    {
        // Arrange
        var deals = new List<Deal>
        {
            new Deal { DealId = "D-001", AccountName = "Test", DealName = "Test", PromoCode = "SAVE20" },
            new Deal { DealId = "D-002", AccountName = "Test", DealName = "Test", PromoCode = "SAVE10" },
        };
        _repository.LoadDeals(deals);

        // Act
        var result = await _service.GetPromoterDealsAsync("", "SAVE20", 10m, _today);

        // Assert
        result.Should().HaveCount(1);
        result[0].DealId.Should().Be("D-001");
    }

    [Fact]
    public async Task GetDeals_MatchesByPromoterId()
    {
        // Arrange
        var deals = new List<Deal>
        {
            new Deal { DealId = "D-001", AccountName = "Test", DealName = "Test", PromoterId = "P-001" },
            new Deal { DealId = "D-002", AccountName = "Test", DealName = "Test", PromoterId = "P-002" },
        };
        _repository.LoadDeals(deals);

        // Act
        var result = await _service.GetPromoterDealsAsync("P-001", "", 10m, _today);

        // Assert
        result.Should().HaveCount(1);
        result[0].DealId.Should().Be("D-001");
    }

    [Fact]
    public async Task GetDeals_SortsByAmountDescending()
    {
        // Arrange
        var deals = new List<Deal>
        {
            CreatePromoterDeal("D-001", "PROMO-001", amount: 10000),
            CreatePromoterDeal("D-002", "PROMO-001", amount: 100000),
            CreatePromoterDeal("D-003", "PROMO-001", amount: 50000),
        };
        _repository.LoadDeals(deals);

        // Act
        var result = await _service.GetPromoterDealsAsync("PROMO-001", "PROMO-001", 10m, _today);

        // Assert
        result[0].DealId.Should().Be("D-002"); // Highest amount first
        result[1].DealId.Should().Be("D-003");
        result[2].DealId.Should().Be("D-001");
    }

    #endregion

    #region Stage Breakdown Tests

    [Fact]
    public async Task Dashboard_GeneratesStageBreakdown()
    {
        // Arrange
        var deals = new List<Deal>
        {
            CreatePromoterDeal("D-001", "PROMO-001", stage: DealStage.Lead, amount: 25000),
            CreatePromoterDeal("D-002", "PROMO-001", stage: DealStage.Lead, amount: 25000),
            CreatePromoterDeal("D-003", "PROMO-001", stage: DealStage.Proposal, amount: 50000),
        };
        _repository.LoadDeals(deals);

        // Act
        var dashboard = await _service.GetPromoterDashboardAsync(
            "PROMO-001", "Test Promoter", "PROMO-001",
            PromoterTier.Bronze, _today);

        // Assert
        dashboard.ByStage.Should().Contain(s => s.Stage == DealStage.Lead && s.DealCount == 2);
        dashboard.ByStage.Should().Contain(s => s.Stage == DealStage.Proposal && s.DealCount == 1);
    }

    [Fact]
    public async Task Dashboard_CalculatesPotentialCommissionByStage()
    {
        // Arrange
        var deals = new List<Deal>
        {
            CreatePromoterDeal("D-001", "PROMO-001", stage: DealStage.Proposal, amount: 100000),
        };
        _repository.LoadDeals(deals);

        // Act
        var dashboard = await _service.GetPromoterDashboardAsync(
            "PROMO-001", "Test Promoter", "PROMO-001",
            PromoterTier.Bronze, _today);

        // Assert - Bronze = 10% commission
        var proposalStage = dashboard.ByStage.First(s => s.Stage == DealStage.Proposal);
        proposalStage.PotentialCommission.Should().Be(10000m);
    }

    #endregion

    #region Recent Deals Tests

    [Fact]
    public async Task Dashboard_IncludesRecentDeals()
    {
        // Arrange
        var deals = new List<Deal>
        {
            CreatePromoterDeal("D-001", "PROMO-001", createdDate: _today.AddDays(-1)),
            CreatePromoterDeal("D-002", "PROMO-001", createdDate: _today.AddDays(-30)),
        };
        _repository.LoadDeals(deals);

        // Act
        var dashboard = await _service.GetPromoterDashboardAsync(
            "PROMO-001", "Test Promoter", "PROMO-001",
            PromoterTier.Bronze, _today);

        // Assert
        dashboard.RecentDeals.Should().NotBeEmpty();
        dashboard.RecentDeals.First().DealId.Should().Be("D-001"); // Most recent first
    }

    [Fact]
    public async Task Dashboard_LimitsRecentDeals()
    {
        // Arrange
        var deals = Enumerable.Range(1, 15)
            .Select(i => CreatePromoterDeal($"D-{i:D3}", "PROMO-001", createdDate: _today.AddDays(-i)))
            .ToList();
        _repository.LoadDeals(deals);

        // Act
        var dashboard = await _service.GetPromoterDashboardAsync(
            "PROMO-001", "Test Promoter", "PROMO-001",
            PromoterTier.Bronze, _today);

        // Assert
        dashboard.RecentDeals.Should().HaveCount(10);
    }

    #endregion

    #region Helper Methods

    private async Task LoadPromoterDeals(string promoCode, int count)
    {
        var deals = Enumerable.Range(1, count)
            .Select(i => CreatePromoterDeal($"D-{i:D3}", promoCode))
            .ToList();
        _repository.LoadDeals(deals);
    }

    private static Deal CreatePromoterDeal(
        string dealId,
        string promoCode,
        DealStage stage = DealStage.Lead,
        decimal amount = 50000,
        int probability = 0,
        DateTime? createdDate = null,
        DateTime? lastContactedDate = null,
        bool commissionPaid = false)
    {
        return new Deal
        {
            DealId = dealId,
            AccountName = $"Account {dealId}",
            DealName = $"Deal {dealId}",
            PromoterId = promoCode,
            PromoCode = promoCode,
            Stage = stage,
            AmountGBP = amount,
            Probability = probability > 0 ? probability : stage.GetDefaultProbability(),
            Owner = "Test Owner",
            Region = "London",
            CreatedDate = createdDate ?? DateTime.Today.AddDays(-30),
            LastContactedDate = lastContactedDate ?? DateTime.Today.AddDays(-5),
            CommissionPaid = commissionPaid
        };
    }

    #endregion
}
