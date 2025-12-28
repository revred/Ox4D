using FluentAssertions;
using Ox4D.Core.Models;
using Ox4D.Core.Models.Config;
using Ox4D.Core.Services;
using Xunit;

namespace Ox4D.Tests;

/// <summary>
/// Tests for ISystemContext and deterministic operations.
/// Verifies that the entire system becomes replayable with fixed context.
/// </summary>
public class DeterministicContextTests
{
    private readonly LookupTables _lookups = LookupTables.CreateDefault();

    #region SystemContext Factory Methods

    [Fact]
    public void SystemContext_Default_Uses_SystemClock()
    {
        var context = SystemContext.Default;

        context.Clock.Should().BeOfType<SystemClock>();
        context.DealIdGenerator.Should().BeOfType<DefaultDealIdGenerator>();
    }

    [Fact]
    public void SystemContext_ForTesting_Creates_Deterministic_Context()
    {
        var fixedDate = new DateTime(2025, 6, 15, 10, 30, 0);

        var context = SystemContext.ForTesting(fixedDate);

        context.Clock.Now.Should().Be(fixedDate);
        context.Clock.Today.Should().Be(fixedDate.Date);
        context.DealIdGenerator.Should().BeOfType<SequentialDealIdGenerator>();
    }

    [Fact]
    public void SystemContext_ForSyntheticData_Uses_Seeded_Generator()
    {
        var fixedDate = new DateTime(2025, 1, 1);
        var seed = 42;

        var context = SystemContext.ForSyntheticData(fixedDate, seed);

        context.Clock.Now.Should().Be(fixedDate);
        context.DealIdGenerator.Should().BeOfType<SeededDealIdGenerator>();
    }

    #endregion

    #region Deterministic ID Generation

    [Fact]
    public void SequentialDealIdGenerator_Produces_Predictable_Ids()
    {
        var generator = new SequentialDealIdGenerator(new DateTime(2025, 1, 1));

        var id1 = generator.Generate();
        var id2 = generator.Generate();
        var id3 = generator.Generate();

        id1.Should().Be("D-20250101-00000001");
        id2.Should().Be("D-20250101-00000002");
        id3.Should().Be("D-20250101-00000003");
    }

    [Fact]
    public void SequentialDealIdGenerator_Reset_Restarts_Counter()
    {
        var generator = new SequentialDealIdGenerator(new DateTime(2025, 1, 1));

        generator.Generate(); // 1
        generator.Generate(); // 2
        generator.Reset();
        var afterReset = generator.Generate();

        afterReset.Should().Be("D-20250101-00000001");
    }

    [Fact]
    public void SeededDealIdGenerator_Same_Seed_Same_Results()
    {
        var gen1 = new SeededDealIdGenerator(42);
        var gen2 = new SeededDealIdGenerator(42);

        var ids1 = Enumerable.Range(0, 10).Select(_ => gen1.Generate()).ToList();
        var ids2 = Enumerable.Range(0, 10).Select(_ => gen2.Generate()).ToList();

        ids1.Should().BeEquivalentTo(ids2, opts => opts.WithStrictOrdering());
    }

    [Fact]
    public void SeededDealIdGenerator_Different_Seeds_Different_Results()
    {
        var gen1 = new SeededDealIdGenerator(42);
        var gen2 = new SeededDealIdGenerator(99);

        var ids1 = Enumerable.Range(0, 5).Select(_ => gen1.Generate()).ToList();
        var ids2 = Enumerable.Range(0, 5).Select(_ => gen2.Generate()).ToList();

        ids1.Should().NotBeEquivalentTo(ids2);
    }

    #endregion

    #region DealNormalizer with ISystemContext

    [Fact]
    public void DealNormalizer_Uses_Context_For_Id_Generation()
    {
        var context = SystemContext.ForTesting(new DateTime(2025, 3, 15));
        var normalizer = new DealNormalizer(_lookups, context);

        var deal = new Deal { AccountName = "Test", DealName = "Deal" };
        var result = normalizer.NormalizeWithTracking(deal);

        result.Deal.DealId.Should().Be("D-20250315-00000001");
    }

    [Fact]
    public void DealNormalizer_Uses_Context_For_CreatedDate()
    {
        var fixedDate = new DateTime(2025, 7, 20);
        var context = SystemContext.ForTesting(fixedDate);
        var normalizer = new DealNormalizer(_lookups, context);

        var deal = new Deal { AccountName = "Test", DealName = "Deal" };
        var result = normalizer.NormalizeWithTracking(deal);

        result.Deal.CreatedDate.Should().Be(fixedDate.Date);
    }

    [Fact]
    public void DealNormalizer_Tracks_All_Context_Dependent_Changes()
    {
        var context = SystemContext.ForTesting(new DateTime(2025, 5, 1));
        var normalizer = new DealNormalizer(_lookups, context);

        var deal = new Deal
        {
            AccountName = "Test Corp",
            DealName = "New Deal",
            // No DealId - should be generated
            // No CreatedDate - should be set
            // No Probability - should be set from stage
        };

        var result = normalizer.NormalizeWithTracking(deal);

        result.Changes.Should().Contain(c => c.FieldName == nameof(Deal.DealId));
        result.Changes.Should().Contain(c => c.FieldName == nameof(Deal.CreatedDate));
        result.Changes.Should().Contain(c => c.FieldName == nameof(Deal.Probability));
    }

    #endregion

    #region Reproducibility Tests

    [Fact]
    public void Same_Context_Same_Normalization_Results()
    {
        var context1 = SystemContext.ForTesting(new DateTime(2025, 1, 15));
        var context2 = SystemContext.ForTesting(new DateTime(2025, 1, 15));

        var normalizer1 = new DealNormalizer(_lookups, context1);
        var normalizer2 = new DealNormalizer(_lookups, context2);

        var deal = new Deal { AccountName = "Acme", DealName = "Widget Sale" };

        var result1 = normalizer1.Normalize(deal);
        var result2 = normalizer2.Normalize(deal);

        result1.DealId.Should().Be(result2.DealId);
        result1.CreatedDate.Should().Be(result2.CreatedDate);
        result1.Probability.Should().Be(result2.Probability);
    }

    [Fact]
    public void Multiple_Deals_Same_Context_Sequential_Ids()
    {
        var context = SystemContext.ForTesting(new DateTime(2025, 2, 28));
        var normalizer = new DealNormalizer(_lookups, context);

        var deals = new[]
        {
            new Deal { AccountName = "A", DealName = "Deal A" },
            new Deal { AccountName = "B", DealName = "Deal B" },
            new Deal { AccountName = "C", DealName = "Deal C" }
        };

        var normalized = deals.Select(d => normalizer.Normalize(d)).ToList();

        normalized[0].DealId.Should().Be("D-20250228-00000001");
        normalized[1].DealId.Should().Be("D-20250228-00000002");
        normalized[2].DealId.Should().Be("D-20250228-00000003");
    }

    #endregion
}
