using FluentAssertions;
using Ox4D.Core.Models;
using Ox4D.Core.Models.Config;
using Ox4D.Core.Services;
using Xunit;

namespace Ox4D.Tests;

public class DealNormalizerTests
{
    private readonly DealNormalizer _normalizer;
    private readonly LookupTables _lookups;

    public DealNormalizerTests()
    {
        _lookups = LookupTables.CreateDefault();
        _normalizer = new DealNormalizer(_lookups);
    }

    [Fact]
    public void Normalize_GeneratesDealId_WhenMissing()
    {
        var deal = new Deal { AccountName = "Test", DealName = "Test Deal" };

        var result = _normalizer.Normalize(deal);

        result.DealId.Should().NotBeNullOrEmpty();
        result.DealId.Should().StartWith("D-");
    }

    [Fact]
    public void Normalize_SetsProbabilityFromStage_WhenZero()
    {
        var deal = new Deal
        {
            DealId = "TEST-001",
            AccountName = "Test",
            DealName = "Test Deal",
            Stage = DealStage.Proposal,
            Probability = 0
        };

        var result = _normalizer.Normalize(deal);

        result.Probability.Should().Be(60); // Default for Proposal
    }

    [Fact]
    public void Normalize_ExtractsPostcodeArea()
    {
        var deal = new Deal
        {
            DealId = "TEST-001",
            AccountName = "Test",
            DealName = "Test Deal",
            Postcode = "SW1A 1AA"
        };

        var result = _normalizer.Normalize(deal);

        result.PostcodeArea.Should().Be("SW");
    }

    [Fact]
    public void Normalize_SetsRegion_FromPostcode()
    {
        var deal = new Deal
        {
            DealId = "TEST-001",
            AccountName = "Test",
            DealName = "Test Deal",
            Postcode = "M1 1AA"
        };

        var result = _normalizer.Normalize(deal);

        result.Region.Should().Be("North West");
    }

    [Fact]
    public void Normalize_GeneratesMapLink()
    {
        var deal = new Deal
        {
            DealId = "TEST-001",
            AccountName = "Test",
            DealName = "Test Deal",
            Postcode = "SW1A 1AA"
        };

        var result = _normalizer.Normalize(deal);

        result.MapLink.Should().Contain("google.com/maps");
        result.MapLink.Should().Contain("SW1A%201AA");
    }

    [Theory]
    [InlineData("2024-01-15", 2024, 1, 15)]
    [InlineData("15/01/2024", 2024, 1, 15)]
    [InlineData("15-01-2024", 2024, 1, 15)]
    public void ParseDate_HandlesVariousFormats(string input, int year, int month, int day)
    {
        var result = DealNormalizer.ParseDate(input);

        result.Should().NotBeNull();
        result!.Value.Year.Should().Be(year);
        result.Value.Month.Should().Be(month);
        result.Value.Day.Should().Be(day);
    }

    [Theory]
    [InlineData("£10,000", 10000)]
    [InlineData("10000", 10000)]
    [InlineData("£1,234.56", 1234.56)]
    public void ParseAmount_HandlesVariousFormats(string input, decimal expected)
    {
        var result = DealNormalizer.ParseAmount(input);

        result.Should().Be(expected);
    }

    // Change Tracking Tests

    [Fact]
    public void NormalizeWithTracking_ReturnsNoChanges_WhenDealIsComplete()
    {
        var deal = new Deal
        {
            DealId = "TEST-001",
            AccountName = "Test",
            DealName = "Test Deal",
            Stage = DealStage.Qualified,
            Probability = 30,
            Postcode = "SW1A 1AA",
            PostcodeArea = "SW",
            Region = "London",
            MapLink = "https://example.com/map",
            CreatedDate = DateTime.Today
        };

        var result = _normalizer.NormalizeWithTracking(deal);

        result.HasChanges.Should().BeFalse();
        result.Changes.Should().BeEmpty();
    }

    [Fact]
    public void NormalizeWithTracking_TracksDealIdGeneration()
    {
        var deal = new Deal { AccountName = "Test", DealName = "Test Deal" };

        var result = _normalizer.NormalizeWithTracking(deal);

        result.HasChanges.Should().BeTrue();
        result.Changes.Should().Contain(c => c.FieldName == "DealId");
        var change = result.Changes.First(c => c.FieldName == "DealId");
        change.OldValue.Should().BeNull();
        change.NewValue.Should().StartWith("D-");
        change.Reason.Should().Contain("Auto-generated");
    }

    [Fact]
    public void NormalizeWithTracking_TracksProbabilityChange()
    {
        var deal = new Deal
        {
            DealId = "TEST-001",
            AccountName = "Test",
            DealName = "Test Deal",
            Stage = DealStage.Proposal,
            Probability = 0
        };

        var result = _normalizer.NormalizeWithTracking(deal);

        result.Changes.Should().Contain(c => c.FieldName == "Probability");
        var change = result.Changes.First(c => c.FieldName == "Probability");
        change.OldValue.Should().Be("0");
        change.NewValue.Should().Be("60");
        change.Reason.Should().Contain("Proposal");
    }

    [Fact]
    public void NormalizeWithTracking_TracksPostcodeAreaExtraction()
    {
        var deal = new Deal
        {
            DealId = "TEST-001",
            AccountName = "Test",
            DealName = "Test Deal",
            Postcode = "SW1A 1AA",
            CreatedDate = DateTime.Today
        };

        var result = _normalizer.NormalizeWithTracking(deal);

        result.Changes.Should().Contain(c => c.FieldName == "PostcodeArea");
        var change = result.Changes.First(c => c.FieldName == "PostcodeArea");
        change.OldValue.Should().BeNull();
        change.NewValue.Should().Be("SW");
    }

    [Fact]
    public void NormalizeWithTracking_TracksRegionDerivation()
    {
        var deal = new Deal
        {
            DealId = "TEST-001",
            AccountName = "Test",
            DealName = "Test Deal",
            Postcode = "M1 1AA",
            CreatedDate = DateTime.Today
        };

        var result = _normalizer.NormalizeWithTracking(deal);

        result.Changes.Should().Contain(c => c.FieldName == "Region");
        var change = result.Changes.First(c => c.FieldName == "Region");
        change.NewValue.Should().Be("North West");
    }

    [Fact]
    public void NormalizeWithTracking_TracksMapLinkGeneration()
    {
        var deal = new Deal
        {
            DealId = "TEST-001",
            AccountName = "Test",
            DealName = "Test Deal",
            Postcode = "SW1A 1AA",
            CreatedDate = DateTime.Today
        };

        var result = _normalizer.NormalizeWithTracking(deal);

        result.Changes.Should().Contain(c => c.FieldName == "MapLink");
        var change = result.Changes.First(c => c.FieldName == "MapLink");
        change.NewValue.Should().Contain("google.com/maps");
    }

    [Fact]
    public void NormalizeWithTracking_TracksCreatedDateDefault()
    {
        var deal = new Deal
        {
            DealId = "TEST-001",
            AccountName = "Test",
            DealName = "Test Deal"
        };

        var result = _normalizer.NormalizeWithTracking(deal);

        result.Changes.Should().Contain(c => c.FieldName == "CreatedDate");
        var change = result.Changes.First(c => c.FieldName == "CreatedDate");
        change.OldValue.Should().BeNull();
        change.NewValue.Should().Be(DateTime.Today.ToString("yyyy-MM-dd"));
    }

    [Fact]
    public void NormalizeWithTracking_TracksTagCleaning()
    {
        var deal = new Deal
        {
            DealId = "TEST-001",
            AccountName = "Test",
            DealName = "Test Deal",
            CreatedDate = DateTime.Today,
            Tags = new List<string> { "  tag1  ", "TAG1", " ", "tag2" }
        };

        var result = _normalizer.NormalizeWithTracking(deal);

        result.Changes.Should().Contain(c => c.FieldName == "Tags");
        var change = result.Changes.First(c => c.FieldName == "Tags");
        change.Reason.Should().Contain("deduplicated");
    }

    [Fact]
    public void NormalizeWithTracking_ReturnsMultipleChanges_WhenManyFieldsMissing()
    {
        var deal = new Deal
        {
            AccountName = "Test",
            DealName = "Test Deal",
            Stage = DealStage.Negotiation,
            Probability = 0,
            Postcode = "M1 1AA"
        };

        var result = _normalizer.NormalizeWithTracking(deal);

        result.HasChanges.Should().BeTrue();
        result.Changes.Should().HaveCountGreaterThan(3);
        result.Changes.Select(c => c.FieldName).Should()
            .Contain(new[] { "DealId", "Probability", "PostcodeArea", "Region" });
    }

    [Fact]
    public void NormalizeWithTracking_PreservesExistingRegion_WhenAlreadySet()
    {
        var deal = new Deal
        {
            DealId = "TEST-001",
            AccountName = "Test",
            DealName = "Test Deal",
            Postcode = "M1 1AA",
            Region = "Custom Region",
            CreatedDate = DateTime.Today
        };

        var result = _normalizer.NormalizeWithTracking(deal);

        result.Changes.Should().NotContain(c => c.FieldName == "Region");
        result.Deal.Region.Should().Be("Custom Region");
    }
}
