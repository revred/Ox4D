using FluentAssertions;
using Ox4D.Core.Models;
using Xunit;

namespace Ox4D.Tests;

/// <summary>
/// Tests for DealFilter - Epic G3: Actionable Intelligence
/// Stories: S3.4.4 (Search and filter deals)
/// </summary>
public class DealFilterTests
{
    private readonly DateTime _referenceDate = new(2025, 1, 15);

    #region G3-046: Filter by Owner

    [Fact]
    public void DealFilter_MatchesByOwner_Correctly()
    {
        // Arrange
        var filter = new DealFilter { Owner = "Alice" };
        var matchingDeal = CreateDeal("Alice");
        var nonMatchingDeal = CreateDeal("Bob");

        // Act & Assert
        filter.Matches(matchingDeal, _referenceDate).Should().BeTrue();
        filter.Matches(nonMatchingDeal, _referenceDate).Should().BeFalse();
    }

    [Fact]
    public void DealFilter_MatchesByOwner_IsCaseInsensitive()
    {
        // Arrange
        var filter = new DealFilter { Owner = "alice" };
        var deal = CreateDeal("Alice");

        // Act & Assert
        filter.Matches(deal, _referenceDate).Should().BeTrue();
    }

    #endregion

    #region G3-047: Filter by Stage

    [Fact]
    public void DealFilter_MatchesByStage_Correctly()
    {
        // Arrange
        var filter = new DealFilter { Stages = new List<DealStage> { DealStage.Lead } };
        var matchingDeal = CreateDeal(stage: DealStage.Lead);
        var nonMatchingDeal = CreateDeal(stage: DealStage.Proposal);

        // Act & Assert
        filter.Matches(matchingDeal, _referenceDate).Should().BeTrue();
        filter.Matches(nonMatchingDeal, _referenceDate).Should().BeFalse();
    }

    [Fact]
    public void DealFilter_MatchesByStage_MultipleStages()
    {
        // Arrange
        var filter = new DealFilter { Stages = new List<DealStage> { DealStage.Lead, DealStage.Qualified } };
        var lead = CreateDeal(stage: DealStage.Lead);
        var qualified = CreateDeal(stage: DealStage.Qualified);
        var proposal = CreateDeal(stage: DealStage.Proposal);

        // Act & Assert
        filter.Matches(lead, _referenceDate).Should().BeTrue();
        filter.Matches(qualified, _referenceDate).Should().BeTrue();
        filter.Matches(proposal, _referenceDate).Should().BeFalse();
    }

    #endregion

    #region G3-048: Filter by Region

    [Fact]
    public void DealFilter_MatchesByRegion_Correctly()
    {
        // Arrange
        var filter = new DealFilter { Region = "London" };
        var matchingDeal = CreateDeal(region: "London");
        var nonMatchingDeal = CreateDeal(region: "Scotland");

        // Act & Assert
        filter.Matches(matchingDeal, _referenceDate).Should().BeTrue();
        filter.Matches(nonMatchingDeal, _referenceDate).Should().BeFalse();
    }

    [Fact]
    public void DealFilter_MatchesByRegion_IsCaseInsensitive()
    {
        // Arrange
        var filter = new DealFilter { Region = "london" };
        var deal = CreateDeal(region: "London");

        // Act & Assert
        filter.Matches(deal, _referenceDate).Should().BeTrue();
    }

    #endregion

    #region G3-049 to G3-050: Filter by Amount

    [Fact]
    public void DealFilter_MatchesByMinAmount_Correctly()
    {
        // Arrange
        var filter = new DealFilter { MinAmount = 50000 };
        var matchingDeal = CreateDeal(amount: 75000);
        var nonMatchingDeal = CreateDeal(amount: 25000);

        // Act & Assert
        filter.Matches(matchingDeal, _referenceDate).Should().BeTrue();
        filter.Matches(nonMatchingDeal, _referenceDate).Should().BeFalse();
    }

    [Fact]
    public void DealFilter_MatchesByMinAmount_ExcludesNullAmounts()
    {
        // Arrange
        var filter = new DealFilter { MinAmount = 50000 };
        var deal = CreateDeal(amount: null);

        // Act & Assert
        filter.Matches(deal, _referenceDate).Should().BeFalse();
    }

    [Fact]
    public void DealFilter_MatchesByMaxAmount_Correctly()
    {
        // Arrange
        var filter = new DealFilter { MaxAmount = 50000 };
        var matchingDeal = CreateDeal(amount: 25000);
        var nonMatchingDeal = CreateDeal(amount: 75000);

        // Act & Assert
        filter.Matches(matchingDeal, _referenceDate).Should().BeTrue();
        filter.Matches(nonMatchingDeal, _referenceDate).Should().BeFalse();
    }

    [Fact]
    public void DealFilter_MatchesByAmountRange_Correctly()
    {
        // Arrange
        var filter = new DealFilter { MinAmount = 20000, MaxAmount = 80000 };
        var inRange = CreateDeal(amount: 50000);
        var belowRange = CreateDeal(amount: 10000);
        var aboveRange = CreateDeal(amount: 100000);

        // Act & Assert
        filter.Matches(inRange, _referenceDate).Should().BeTrue();
        filter.Matches(belowRange, _referenceDate).Should().BeFalse();
        filter.Matches(aboveRange, _referenceDate).Should().BeFalse();
    }

    #endregion

    #region G3-051 to G3-053: Filter by Search Text

    [Fact]
    public void DealFilter_MatchesBySearchTerm_InAccountName()
    {
        // Arrange
        var filter = new DealFilter { SearchText = "apex" };
        var matchingDeal = CreateDeal(accountName: "Apex Technologies Ltd");
        var nonMatchingDeal = CreateDeal(accountName: "Beta Solutions");

        // Act & Assert
        filter.Matches(matchingDeal, _referenceDate).Should().BeTrue();
        filter.Matches(nonMatchingDeal, _referenceDate).Should().BeFalse();
    }

    [Fact]
    public void DealFilter_MatchesBySearchTerm_InDealName()
    {
        // Arrange
        var filter = new DealFilter { SearchText = "enterprise" };
        var deal = new Deal
        {
            DealId = "D-001",
            AccountName = "Test Co",
            DealName = "Enterprise Software Deal"
        };

        // Act & Assert
        filter.Matches(deal, _referenceDate).Should().BeTrue();
    }

    [Fact]
    public void DealFilter_MatchesBySearchTerm_InContactName()
    {
        // Arrange
        var filter = new DealFilter { SearchText = "smith" };
        var deal = new Deal
        {
            DealId = "D-001",
            AccountName = "Test Co",
            DealName = "Test Deal",
            ContactName = "John Smith"
        };

        // Act & Assert
        filter.Matches(deal, _referenceDate).Should().BeTrue();
    }

    [Fact]
    public void DealFilter_MatchesBySearchTerm_InDealId()
    {
        // Arrange
        var filter = new DealFilter { SearchText = "D-ABC" };
        var deal = new Deal
        {
            DealId = "D-ABC-123",
            AccountName = "Test Co",
            DealName = "Test Deal"
        };

        // Act & Assert
        filter.Matches(deal, _referenceDate).Should().BeTrue();
    }

    [Fact]
    public void DealFilter_MatchesBySearchTerm_InOwner()
    {
        // Arrange
        var filter = new DealFilter { SearchText = "alice" };
        var deal = CreateDeal("Alice Smith");

        // Act & Assert
        filter.Matches(deal, _referenceDate).Should().BeTrue();
    }

    [Fact]
    public void DealFilter_SearchText_IsCaseInsensitive()
    {
        // Arrange
        var filter = new DealFilter { SearchText = "APEX" };
        var deal = CreateDeal(accountName: "apex technologies");

        // Act & Assert
        filter.Matches(deal, _referenceDate).Should().BeTrue();
    }

    #endregion

    #region G3-054: Filter by Date Range

    [Fact]
    public void DealFilter_MatchesByDateRange_Correctly()
    {
        // Arrange
        var filter = new DealFilter
        {
            CloseDateFrom = new DateTime(2025, 1, 1),
            CloseDateTo = new DateTime(2025, 3, 31)
        };
        var inRange = CreateDeal(closeDate: new DateTime(2025, 2, 15));
        var beforeRange = CreateDeal(closeDate: new DateTime(2024, 12, 15));
        var afterRange = CreateDeal(closeDate: new DateTime(2025, 4, 15));

        // Act & Assert
        filter.Matches(inRange, _referenceDate).Should().BeTrue();
        filter.Matches(beforeRange, _referenceDate).Should().BeFalse();
        filter.Matches(afterRange, _referenceDate).Should().BeFalse();
    }

    [Fact]
    public void DealFilter_MatchesByCloseDateFrom_ExcludesNullDates()
    {
        // Arrange
        var filter = new DealFilter { CloseDateFrom = new DateTime(2025, 1, 1) };
        var deal = CreateDeal(closeDate: null);

        // Act & Assert
        filter.Matches(deal, _referenceDate).Should().BeFalse();
    }

    #endregion

    #region G3-055: Combined Filters

    [Fact]
    public void DealFilter_CombinesMultipleFilters_WithAnd()
    {
        // Arrange
        var filter = new DealFilter
        {
            Owner = "Alice",
            Region = "London",
            MinAmount = 50000
        };

        var matchesAll = new Deal
        {
            DealId = "D-001",
            AccountName = "Test",
            DealName = "Test",
            Owner = "Alice",
            Region = "London",
            AmountGBP = 75000
        };

        var wrongOwner = new Deal
        {
            DealId = "D-002",
            AccountName = "Test",
            DealName = "Test",
            Owner = "Bob",
            Region = "London",
            AmountGBP = 75000
        };

        var wrongRegion = new Deal
        {
            DealId = "D-003",
            AccountName = "Test",
            DealName = "Test",
            Owner = "Alice",
            Region = "Scotland",
            AmountGBP = 75000
        };

        var wrongAmount = new Deal
        {
            DealId = "D-004",
            AccountName = "Test",
            DealName = "Test",
            Owner = "Alice",
            Region = "London",
            AmountGBP = 25000
        };

        // Act & Assert
        filter.Matches(matchesAll, _referenceDate).Should().BeTrue();
        filter.Matches(wrongOwner, _referenceDate).Should().BeFalse();
        filter.Matches(wrongRegion, _referenceDate).Should().BeFalse();
        filter.Matches(wrongAmount, _referenceDate).Should().BeFalse();
    }

    [Fact]
    public void DealFilter_EmptyFilter_MatchesAllDeals()
    {
        // Arrange
        var filter = new DealFilter();
        var deal = CreateDeal();

        // Act & Assert
        filter.Matches(deal, _referenceDate).Should().BeTrue();
    }

    #endregion

    #region Overdue and No Contact Filters

    [Fact]
    public void DealFilter_MatchesByHasOverdueNextStep()
    {
        // Arrange
        var filter = new DealFilter { HasOverdueNextStep = true };
        var overdue = CreateDeal(nextStepDueDate: _referenceDate.AddDays(-5));
        var notOverdue = CreateDeal(nextStepDueDate: _referenceDate.AddDays(5));
        var noNextStep = new Deal { DealId = "D-001", AccountName = "Test", DealName = "Test" };

        // Act & Assert
        filter.Matches(overdue, _referenceDate).Should().BeTrue();
        filter.Matches(notOverdue, _referenceDate).Should().BeFalse();
        filter.Matches(noNextStep, _referenceDate).Should().BeFalse();
    }

    [Fact]
    public void DealFilter_MatchesByNoContactDays()
    {
        // Arrange
        var filter = new DealFilter { NoContactDays = 10 };
        var noContactOver10Days = CreateDeal(lastContactedDate: _referenceDate.AddDays(-15));
        var recentContact = CreateDeal(lastContactedDate: _referenceDate.AddDays(-5));
        var neverContacted = new Deal { DealId = "D-001", AccountName = "Test", DealName = "Test" };

        // Act & Assert
        filter.Matches(noContactOver10Days, _referenceDate).Should().BeTrue();
        filter.Matches(recentContact, _referenceDate).Should().BeFalse();
        filter.Matches(neverContacted, _referenceDate).Should().BeTrue(); // Never contacted counts
    }

    #endregion

    #region Promoter Filters

    [Fact]
    public void DealFilter_MatchesByPromoterId()
    {
        // Arrange
        var filter = new DealFilter { PromoterId = "PROMO-001" };
        var deal = new Deal
        {
            DealId = "D-001",
            AccountName = "Test",
            DealName = "Test",
            PromoterId = "PROMO-001"
        };
        var noPromoter = CreateDeal();

        // Act & Assert
        filter.Matches(deal, _referenceDate).Should().BeTrue();
        filter.Matches(noPromoter, _referenceDate).Should().BeFalse();
    }

    [Fact]
    public void DealFilter_MatchesByPromoCode()
    {
        // Arrange
        var filter = new DealFilter { PromoCode = "SAVE20" };
        var deal = new Deal
        {
            DealId = "D-001",
            AccountName = "Test",
            DealName = "Test",
            PromoCode = "SAVE20"
        };

        // Act & Assert
        filter.Matches(deal, _referenceDate).Should().BeTrue();
    }

    [Fact]
    public void DealFilter_MatchesByHasPromoter_True()
    {
        // Arrange
        var filter = new DealFilter { HasPromoter = true };
        var withPromoter = new Deal
        {
            DealId = "D-001",
            AccountName = "Test",
            DealName = "Test",
            PromoterId = "PROMO-001"
        };
        var noPromoter = CreateDeal();

        // Act & Assert
        filter.Matches(withPromoter, _referenceDate).Should().BeTrue();
        filter.Matches(noPromoter, _referenceDate).Should().BeFalse();
    }

    [Fact]
    public void DealFilter_MatchesByHasPromoter_False()
    {
        // Arrange
        var filter = new DealFilter { HasPromoter = false };
        var withPromoter = new Deal
        {
            DealId = "D-001",
            AccountName = "Test",
            DealName = "Test",
            PromoterId = "PROMO-001"
        };
        var noPromoter = CreateDeal();

        // Act & Assert
        filter.Matches(withPromoter, _referenceDate).Should().BeFalse();
        filter.Matches(noPromoter, _referenceDate).Should().BeTrue();
    }

    #endregion

    #region Tags Filter

    [Fact]
    public void DealFilter_MatchesByTags_AllTagsRequired()
    {
        // Arrange
        var filter = new DealFilter { Tags = new List<string> { "Priority", "Enterprise" } };
        var hasAllTags = new Deal
        {
            DealId = "D-001",
            AccountName = "Test",
            DealName = "Test",
            Tags = new List<string> { "Priority", "Enterprise", "Urgent" }
        };
        var hasSomeTags = new Deal
        {
            DealId = "D-002",
            AccountName = "Test",
            DealName = "Test",
            Tags = new List<string> { "Priority" }
        };

        // Act & Assert
        filter.Matches(hasAllTags, _referenceDate).Should().BeTrue();
        filter.Matches(hasSomeTags, _referenceDate).Should().BeFalse();
    }

    #endregion

    #region Helper Methods

    private static Deal CreateDeal(
        string? owner = null,
        DealStage stage = DealStage.Lead,
        string region = "London",
        decimal? amount = 50000,
        string accountName = "Test Account",
        DateTime? closeDate = null,
        DateTime? nextStepDueDate = null,
        DateTime? lastContactedDate = null)
    {
        return new Deal
        {
            DealId = $"D-{Guid.NewGuid():N}"[..12],
            AccountName = accountName,
            DealName = "Test Deal",
            Owner = owner ?? "Test Owner",
            Stage = stage,
            Region = region,
            AmountGBP = amount,
            CloseDate = closeDate,
            NextStepDueDate = nextStepDueDate,
            LastContactedDate = lastContactedDate
        };
    }

    #endregion
}
