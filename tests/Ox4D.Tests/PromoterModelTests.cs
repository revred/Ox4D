using FluentAssertions;
using Ox4D.Core.Models;
using Xunit;

namespace Ox4D.Tests;

/// <summary>
/// Tests for Promoter model - Epic G4: Referral Partner Support
/// Stories: S4.1.1 (Promoter entity), S4.1.3 (Tier calculation)
/// </summary>
public class PromoterModelTests
{
    #region G4-001 to G4-004: Promoter Entity

    [Fact]
    public void Promoter_DefaultsToActiveBronze()
    {
        // Arrange & Act
        var promoter = new Promoter();

        // Assert
        promoter.Tier.Should().Be(PromoterTier.Bronze);
        promoter.Status.Should().Be(PromoterStatus.Active);
    }

    [Fact]
    public void Promoter_Clone_CreatesDeepCopy()
    {
        // Arrange
        var original = new Promoter
        {
            PromoterId = "PROMO-001",
            Name = "Original Name",
            Tags = new List<string> { "tag1" }
        };

        // Act
        var clone = original.Clone();
        clone.Name = "Modified Name";

        // Assert
        original.Name.Should().Be("Original Name");
    }

    [Fact]
    public void Promoter_ConversionRate_CalculatesCorrectly()
    {
        // Arrange
        var promoter = new Promoter
        {
            TotalReferrals = 100,
            ConvertedReferrals = 25
        };

        // Act
        var rate = promoter.ConversionRate;

        // Assert
        rate.Should().Be(25m);
    }

    [Fact]
    public void Promoter_ConversionRate_ReturnsZero_WhenNoReferrals()
    {
        // Arrange
        var promoter = new Promoter
        {
            TotalReferrals = 0,
            ConvertedReferrals = 0
        };

        // Act
        var rate = promoter.ConversionRate;

        // Assert
        rate.Should().Be(0);
    }

    [Fact]
    public void Promoter_ConversionRate_RoundsToTwoDecimals()
    {
        // Arrange
        var promoter = new Promoter
        {
            TotalReferrals = 7,
            ConvertedReferrals = 2
        };

        // Act
        var rate = promoter.ConversionRate;

        // Assert
        rate.Should().Be(28.57m); // 2/7 * 100 = 28.571... rounded to 28.57
    }

    #endregion

    #region G4-010 to G4-014: Commission Rates by Tier

    [Fact]
    public void PromoterTier_Bronze_Returns10PercentRate()
    {
        // Act
        var rate = PromoterTier.Bronze.GetCommissionRate();

        // Assert
        rate.Should().Be(10m);
    }

    [Fact]
    public void PromoterTier_Silver_Returns12PercentRate()
    {
        // Act
        var rate = PromoterTier.Silver.GetCommissionRate();

        // Assert
        rate.Should().Be(12m);
    }

    [Fact]
    public void PromoterTier_Gold_Returns15PercentRate()
    {
        // Act
        var rate = PromoterTier.Gold.GetCommissionRate();

        // Assert
        rate.Should().Be(15m);
    }

    [Fact]
    public void PromoterTier_Platinum_Returns18PercentRate()
    {
        // Act
        var rate = PromoterTier.Platinum.GetCommissionRate();

        // Assert
        rate.Should().Be(18m);
    }

    [Fact]
    public void PromoterTier_Diamond_Returns20PercentRate()
    {
        // Act
        var rate = PromoterTier.Diamond.GetCommissionRate();

        // Assert
        rate.Should().Be(20m);
    }

    #endregion

    #region G4-015 to G4-017: Minimum Referrals for Tier

    [Fact]
    public void PromoterTier_MinReferrals_Bronze()
    {
        // Act
        var minReferrals = PromoterTier.Bronze.GetMinReferralsForTier();

        // Assert
        minReferrals.Should().Be(0);
    }

    [Fact]
    public void PromoterTier_MinReferrals_Silver()
    {
        // Act
        var minReferrals = PromoterTier.Silver.GetMinReferralsForTier();

        // Assert
        minReferrals.Should().Be(10);
    }

    [Fact]
    public void PromoterTier_MinReferrals_Gold()
    {
        // Act
        var minReferrals = PromoterTier.Gold.GetMinReferralsForTier();

        // Assert
        minReferrals.Should().Be(25);
    }

    [Fact]
    public void PromoterTier_MinReferrals_Platinum()
    {
        // Act
        var minReferrals = PromoterTier.Platinum.GetMinReferralsForTier();

        // Assert
        minReferrals.Should().Be(50);
    }

    [Fact]
    public void PromoterTier_MinReferrals_Diamond()
    {
        // Act
        var minReferrals = PromoterTier.Diamond.GetMinReferralsForTier();

        // Assert
        minReferrals.Should().Be(100);
    }

    #endregion

    #region G4-018 to G4-019: Tier Parsing

    [Theory]
    [InlineData("bronze", PromoterTier.Bronze)]
    [InlineData("Bronze", PromoterTier.Bronze)]
    [InlineData("BRONZE", PromoterTier.Bronze)]
    [InlineData("silver", PromoterTier.Silver)]
    [InlineData("Silver", PromoterTier.Silver)]
    [InlineData("gold", PromoterTier.Gold)]
    [InlineData("Gold", PromoterTier.Gold)]
    [InlineData("platinum", PromoterTier.Platinum)]
    [InlineData("Platinum", PromoterTier.Platinum)]
    [InlineData("diamond", PromoterTier.Diamond)]
    [InlineData("Diamond", PromoterTier.Diamond)]
    public void PromoterTier_ParseTier_HandlesValidInput(string input, PromoterTier expected)
    {
        // Act
        var result = PromoterTierExtensions.ParseTier(input);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("invalid")]
    [InlineData("super")]
    public void PromoterTier_ParseTier_DefaultsToBronze(string? input)
    {
        // Act
        var result = PromoterTierExtensions.ParseTier(input);

        // Assert
        result.Should().Be(PromoterTier.Bronze);
    }

    #endregion

    #region Tier Display Strings

    [Theory]
    [InlineData(PromoterTier.Bronze, "Bronze")]
    [InlineData(PromoterTier.Silver, "Silver")]
    [InlineData(PromoterTier.Gold, "Gold")]
    [InlineData(PromoterTier.Platinum, "Platinum")]
    [InlineData(PromoterTier.Diamond, "Diamond")]
    public void PromoterTier_ToDisplayString_ReturnsCorrectValue(PromoterTier tier, string expected)
    {
        // Act
        var result = tier.ToDisplayString();

        // Assert
        result.Should().Be(expected);
    }

    #endregion

    #region Promoter Status

    [Theory]
    [InlineData(PromoterStatus.Pending, "Pending")]
    [InlineData(PromoterStatus.Active, "Active")]
    [InlineData(PromoterStatus.Suspended, "Suspended")]
    [InlineData(PromoterStatus.Inactive, "Inactive")]
    public void PromoterStatus_ToDisplayString_ReturnsCorrectValue(PromoterStatus status, string expected)
    {
        // Act
        var result = status.ToDisplayString();

        // Assert
        result.Should().Be(expected);
    }

    #endregion

    #region Default Values

    [Fact]
    public void Promoter_CommissionRate_DefaultsTo10Percent()
    {
        // Arrange & Act
        var promoter = new Promoter();

        // Assert
        promoter.CommissionRate.Should().Be(10m);
    }

    [Fact]
    public void Promoter_CreatedDate_DefaultsToNow()
    {
        // Arrange
        var before = DateTime.UtcNow.AddSeconds(-1);

        // Act
        var promoter = new Promoter();

        // Assert
        promoter.CreatedDate.Should().BeAfter(before);
        promoter.CreatedDate.Should().BeBefore(DateTime.UtcNow.AddSeconds(1));
    }

    [Fact]
    public void Promoter_Tags_DefaultsToEmptyList()
    {
        // Arrange & Act
        var promoter = new Promoter();

        // Assert
        promoter.Tags.Should().NotBeNull();
        promoter.Tags.Should().BeEmpty();
    }

    [Fact]
    public void Promoter_TotalReferrals_DefaultsToZero()
    {
        // Arrange & Act
        var promoter = new Promoter();

        // Assert
        promoter.TotalReferrals.Should().Be(0);
    }

    [Fact]
    public void Promoter_ConvertedReferrals_DefaultsToZero()
    {
        // Arrange & Act
        var promoter = new Promoter();

        // Assert
        promoter.ConvertedReferrals.Should().Be(0);
    }

    [Fact]
    public void Promoter_TotalCommissionEarned_DefaultsToZero()
    {
        // Arrange & Act
        var promoter = new Promoter();

        // Assert
        promoter.TotalCommissionEarned.Should().Be(0);
    }

    [Fact]
    public void Promoter_PendingCommission_DefaultsToZero()
    {
        // Arrange & Act
        var promoter = new Promoter();

        // Assert
        promoter.PendingCommission.Should().Be(0);
    }

    #endregion

    #region All Fields Present

    [Fact]
    public void Promoter_HasAllExpectedProperties()
    {
        // Arrange
        var promoter = new Promoter
        {
            PromoterId = "PROMO-001",
            PromoCode = "SAVE20",
            Name = "John Smith",
            Email = "john@example.com",
            Phone = "07700900000",
            Company = "Partner Ltd",
            CommissionRate = 15m,
            Tier = PromoterTier.Gold,
            Status = PromoterStatus.Active,
            CreatedDate = DateTime.UtcNow,
            LastActivityDate = DateTime.UtcNow,
            TotalReferrals = 50,
            ConvertedReferrals = 15,
            TotalCommissionEarned = 25000m,
            PendingCommission = 5000m,
            Notes = "Top performer",
            Tags = new List<string> { "VIP", "Tech" }
        };

        // Assert
        promoter.PromoterId.Should().Be("PROMO-001");
        promoter.PromoCode.Should().Be("SAVE20");
        promoter.Name.Should().Be("John Smith");
        promoter.Email.Should().Be("john@example.com");
        promoter.Phone.Should().Be("07700900000");
        promoter.Company.Should().Be("Partner Ltd");
        promoter.CommissionRate.Should().Be(15m);
        promoter.Tier.Should().Be(PromoterTier.Gold);
        promoter.Status.Should().Be(PromoterStatus.Active);
        promoter.TotalReferrals.Should().Be(50);
        promoter.ConvertedReferrals.Should().Be(15);
        promoter.TotalCommissionEarned.Should().Be(25000m);
        promoter.PendingCommission.Should().Be(5000m);
        promoter.Notes.Should().Be("Top performer");
        promoter.Tags.Should().HaveCount(2);
        promoter.ConversionRate.Should().Be(30m);
    }

    #endregion
}
