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
}
