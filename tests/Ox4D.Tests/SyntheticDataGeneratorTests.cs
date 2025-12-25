using FluentAssertions;
using Ox4D.Core.Models;
using Ox4D.Core.Models.Config;
using Ox4D.Core.Services;
using Xunit;

namespace Ox4D.Tests;

public class SyntheticDataGeneratorTests
{
    private readonly SyntheticDataGenerator _generator;

    public SyntheticDataGeneratorTests()
    {
        var lookups = LookupTables.CreateDefault();
        var settings = new PipelineSettings();
        _generator = new SyntheticDataGenerator(lookups, settings);
    }

    [Fact]
    public void Generate_CreatesCorrectNumberOfDeals()
    {
        var deals = _generator.Generate(100, seed: 42);

        deals.Should().HaveCount(100);
    }

    [Fact]
    public void Generate_ProducesConsistentResults_WithSameSeed()
    {
        var deals1 = _generator.Generate(50, seed: 12345);
        var deals2 = _generator.Generate(50, seed: 12345);

        deals1.Should().BeEquivalentTo(deals2);
    }

    [Fact]
    public void Generate_ProducesDifferentResults_WithDifferentSeeds()
    {
        var deals1 = _generator.Generate(50, seed: 1);
        var deals2 = _generator.Generate(50, seed: 2);

        deals1.Should().NotBeEquivalentTo(deals2);
    }

    [Fact]
    public void Generate_CreatesDealsWithValidStages()
    {
        var deals = _generator.Generate(100, seed: 42);

        deals.Should().OnlyContain(d => Enum.IsDefined(d.Stage));
    }

    [Fact]
    public void Generate_CreatesDealsWithNormalizedData()
    {
        var deals = _generator.Generate(50, seed: 42);

        deals.Should().OnlyContain(d => !string.IsNullOrEmpty(d.DealId));
        deals.Should().OnlyContain(d => !string.IsNullOrEmpty(d.AccountName));
        deals.Should().OnlyContain(d => !string.IsNullOrEmpty(d.DealName));
    }

    [Fact]
    public void Generate_InjectsHygieneIssues()
    {
        var deals = _generator.Generate(200, seed: 42);

        // Should have some missing amounts (~8%)
        var missingAmounts = deals.Count(d => !d.AmountGBP.HasValue && !d.Stage.IsClosed());
        missingAmounts.Should().BeGreaterThan(0);

        // Should have some missing last contact dates (~12%)
        var missingContact = deals.Count(d => !d.LastContactedDate.HasValue && !d.Stage.IsClosed());
        missingContact.Should().BeGreaterThan(0);
    }
}
