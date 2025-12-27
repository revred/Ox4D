using FluentAssertions;
using Ox4D.Core.Models;
using Ox4D.Core.Models.Config;
using Xunit;

namespace Ox4D.Tests;

/// <summary>
/// Tests for LookupTables and PipelineSettings - Epic G5: Developer Experience
/// Stories: S5.4.1 (IDealRepository), S5.4.2 (ISyntheticDataGenerator)
/// </summary>
public class LookupTablesTests
{
    private readonly LookupTables _lookups;

    public LookupTablesTests()
    {
        _lookups = LookupTables.CreateDefault();
    }

    #region G5-031: Default Probability by Stage

    [Theory]
    [InlineData(DealStage.Lead, 10)]
    [InlineData(DealStage.Qualified, 20)]
    [InlineData(DealStage.Discovery, 40)]
    [InlineData(DealStage.Proposal, 60)]
    [InlineData(DealStage.Negotiation, 80)]
    [InlineData(DealStage.ClosedWon, 100)]
    [InlineData(DealStage.ClosedLost, 0)]
    [InlineData(DealStage.OnHold, 10)]
    public void LookupTables_ReturnsDefaultProbability_ForStage(DealStage stage, int expectedProbability)
    {
        // Act
        var result = _lookups.GetProbabilityForStage(stage);

        // Assert
        result.Should().Be(expectedProbability);
    }

    #endregion

    #region G5-032: Region Lookup

    [Theory]
    [InlineData("SW1A 1AA", "London")]
    [InlineData("EC2R 8AH", "London")]
    [InlineData("M1 1AA", "North West")]
    [InlineData("EH1 1AA", "Scotland")]
    [InlineData("CF10 1AA", "Wales")]
    [InlineData("BT1 1AA", "Northern Ireland")]
    [InlineData("LS1 1AA", "Yorkshire")]
    [InlineData("B1 1AA", "West Midlands")]
    public void LookupTables_ReturnsRegion_ForPostcode(string postcode, string expectedRegion)
    {
        // Act
        var result = _lookups.GetRegionForPostcode(postcode);

        // Assert
        result.Should().Be(expectedRegion);
    }

    #endregion

    #region G5-033: Postcode Area Extraction

    [Theory]
    [InlineData("SW1A 1AA", "SW")]
    [InlineData("EC2R 8AH", "EC")]
    [InlineData("M1 1AA", "M")]
    [InlineData("sw1a 1aa", "SW")]
    [InlineData("  SW1A 1AA  ", "SW")]
    [InlineData("SW1A1AA", "SW")]
    public void LookupTables_ExtractsPostcodeArea_Correctly(string postcode, string expectedArea)
    {
        // Act
        var result = LookupTables.ExtractPostcodeArea(postcode);

        // Assert
        result.Should().Be(expectedArea);
    }

    #endregion

    #region G5-034: Unknown Postcode Handling

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("ZZ99 9ZZ")]
    public void LookupTables_HandlesUnknownPostcode_Gracefully(string? postcode)
    {
        // Act
        var result = _lookups.GetRegionForPostcode(postcode);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region CreateDefault Tests

    [Fact]
    public void LookupTables_CreateDefault_PopulatesPostcodeToRegion()
    {
        // Act
        var lookups = LookupTables.CreateDefault();

        // Assert
        lookups.PostcodeToRegion.Should().NotBeEmpty();
        lookups.PostcodeToRegion.Should().ContainKey("SW");
        lookups.PostcodeToRegion.Should().ContainKey("M");
        lookups.PostcodeToRegion.Should().ContainKey("EH");
    }

    [Fact]
    public void LookupTables_CreateDefault_PopulatesStageProbabilities()
    {
        // Act
        var lookups = LookupTables.CreateDefault();

        // Assert
        lookups.StageProbabilities.Should().NotBeEmpty();
        lookups.StageProbabilities.Should().ContainKey(DealStage.Lead);
        lookups.StageProbabilities.Should().ContainKey(DealStage.ClosedWon);
    }

    [Fact]
    public void LookupTables_PostcodeToRegion_IsCaseInsensitive()
    {
        // Arrange
        var lookups = LookupTables.CreateDefault();

        // Assert
        lookups.PostcodeToRegion.TryGetValue("sw", out var lower).Should().BeTrue();
        lookups.PostcodeToRegion.TryGetValue("SW", out var upper).Should().BeTrue();
        lower.Should().Be(upper);
    }

    #endregion

    #region All UK Regions Covered

    [Fact]
    public void LookupTables_ContainsAllUKRegions()
    {
        // Arrange
        var expectedRegions = new[]
        {
            "London",
            "South East",
            "South West",
            "East of England",
            "West Midlands",
            "East Midlands",
            "Yorkshire",
            "North West",
            "North East",
            "Wales",
            "Scotland",
            "Northern Ireland"
        };

        // Act
        var allRegions = _lookups.PostcodeToRegion.Values.Distinct().ToList();

        // Assert
        foreach (var region in expectedRegions)
        {
            allRegions.Should().Contain(region);
        }
    }

    #endregion
}

/// <summary>
/// Tests for PipelineSettings - Epic G5: Developer Experience
/// Stories: S5.4.2 (Configuration)
/// </summary>
public class PipelineSettingsTests
{
    #region G5-035: Configurable Thresholds

    [Fact]
    public void PipelineSettings_HasConfigurableThresholds()
    {
        // Arrange
        var settings = new PipelineSettings();

        // Assert
        settings.NoContactThresholdDays.Should().Be(10);
        settings.HighValueThreshold.Should().Be(50000);
        settings.HighValueTopN.Should().Be(10);
        settings.StaleContactWarningDays.Should().Be(14);
    }

    [Fact]
    public void PipelineSettings_ThresholdsAreModifiable()
    {
        // Arrange
        var settings = new PipelineSettings
        {
            NoContactThresholdDays = 7,
            HighValueThreshold = 100000,
            HighValueTopN = 5
        };

        // Assert
        settings.NoContactThresholdDays.Should().Be(7);
        settings.HighValueThreshold.Should().Be(100000);
        settings.HighValueTopN.Should().Be(5);
    }

    #endregion

    #region Default Lists

    [Fact]
    public void PipelineSettings_HasDefaultProductLines()
    {
        // Arrange
        var settings = new PipelineSettings();

        // Assert
        settings.ProductLines.Should().NotBeEmpty();
        settings.ProductLines.Should().Contain("Enterprise Software");
        settings.ProductLines.Should().Contain("SaaS Subscription");
    }

    [Fact]
    public void PipelineSettings_HasDefaultLeadSources()
    {
        // Arrange
        var settings = new PipelineSettings();

        // Assert
        settings.LeadSources.Should().NotBeEmpty();
        settings.LeadSources.Should().Contain("Inbound");
        settings.LeadSources.Should().Contain("Referral");
    }

    [Fact]
    public void PipelineSettings_HasDefaultServicePlans()
    {
        // Arrange
        var settings = new PipelineSettings();

        // Assert
        settings.ServicePlans.Should().NotBeEmpty();
        settings.ServicePlans.Should().Contain("Basic");
        settings.ServicePlans.Should().Contain("Premium");
    }

    [Fact]
    public void PipelineSettings_HasDefaultOwners()
    {
        // Arrange
        var settings = new PipelineSettings();

        // Assert
        settings.Owners.Should().NotBeEmpty();
        settings.Owners.Should().Contain("James Wilson");
        settings.Owners.Should().Contain("Sarah Chen");
    }

    #endregion
}

/// <summary>
/// Tests for DealStage enum extensions - Epic G3/G5
/// </summary>
public class DealStageTests
{
    #region Default Probability

    [Theory]
    [InlineData(DealStage.Lead, 10)]
    [InlineData(DealStage.Qualified, 20)]
    [InlineData(DealStage.Discovery, 40)]
    [InlineData(DealStage.Proposal, 60)]
    [InlineData(DealStage.Negotiation, 80)]
    [InlineData(DealStage.ClosedWon, 100)]
    [InlineData(DealStage.ClosedLost, 0)]
    [InlineData(DealStage.OnHold, 10)]
    public void DealStage_GetDefaultProbability_ReturnsCorrectValue(DealStage stage, int expected)
    {
        // Act
        var result = stage.GetDefaultProbability();

        // Assert
        result.Should().Be(expected);
    }

    #endregion

    #region Display String

    [Theory]
    [InlineData(DealStage.Lead, "Lead")]
    [InlineData(DealStage.Qualified, "Qualified")]
    [InlineData(DealStage.Discovery, "Discovery")]
    [InlineData(DealStage.Proposal, "Proposal")]
    [InlineData(DealStage.Negotiation, "Negotiation")]
    [InlineData(DealStage.ClosedWon, "Closed Won")]
    [InlineData(DealStage.ClosedLost, "Closed Lost")]
    [InlineData(DealStage.OnHold, "On Hold")]
    public void DealStage_ToDisplayString_ReturnsCorrectValue(DealStage stage, string expected)
    {
        // Act
        var result = stage.ToDisplayString();

        // Assert
        result.Should().Be(expected);
    }

    #endregion

    #region Stage Parsing

    [Theory]
    [InlineData("lead", DealStage.Lead)]
    [InlineData("Lead", DealStage.Lead)]
    [InlineData("LEAD", DealStage.Lead)]
    [InlineData("qualified", DealStage.Qualified)]
    [InlineData("discovery", DealStage.Discovery)]
    [InlineData("proposal", DealStage.Proposal)]
    [InlineData("negotiation", DealStage.Negotiation)]
    [InlineData("closedwon", DealStage.ClosedWon)]
    [InlineData("Closed Won", DealStage.ClosedWon)]
    [InlineData("won", DealStage.ClosedWon)]
    [InlineData("closedlost", DealStage.ClosedLost)]
    [InlineData("Closed Lost", DealStage.ClosedLost)]
    [InlineData("lost", DealStage.ClosedLost)]
    [InlineData("onhold", DealStage.OnHold)]
    [InlineData("On Hold", DealStage.OnHold)]
    [InlineData("hold", DealStage.OnHold)]
    public void DealStage_ParseStage_HandlesVariousFormats(string input, DealStage expected)
    {
        // Act
        var result = DealStageExtensions.ParseStage(input);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void DealStage_ParseStage_DefaultsToLead_ForEmpty(string? input)
    {
        // Act
        var result = DealStageExtensions.ParseStage(input);

        // Assert
        result.Should().Be(DealStage.Lead);
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("unknown")]
    [InlineData("xyz")]
    public void DealStage_ParseStage_ReturnsOther_ForUnknown(string input)
    {
        // Act
        var result = DealStageExtensions.ParseStage(input);

        // Assert
        result.Should().Be(DealStage.Other);
    }

    #endregion

    #region IsClosed

    [Theory]
    [InlineData(DealStage.ClosedWon, true)]
    [InlineData(DealStage.ClosedLost, true)]
    [InlineData(DealStage.Lead, false)]
    [InlineData(DealStage.Qualified, false)]
    [InlineData(DealStage.Discovery, false)]
    [InlineData(DealStage.Proposal, false)]
    [InlineData(DealStage.Negotiation, false)]
    [InlineData(DealStage.OnHold, false)]
    public void DealStage_IsClosed_ReturnsCorrectValue(DealStage stage, bool expected)
    {
        // Act
        var result = stage.IsClosed();

        // Assert
        result.Should().Be(expected);
    }

    #endregion
}
