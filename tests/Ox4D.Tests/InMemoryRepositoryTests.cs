using FluentAssertions;
using Ox4D.Core.Models;
using Ox4D.Store;
using Xunit;

namespace Ox4D.Tests;

/// <summary>
/// Tests for InMemoryDealRepository - Epic G2: Zero Vendor Lock-in
/// Stories: S2.1.1 (IDealRepository), S2.1.2 (InMemoryDealRepository)
/// </summary>
public class InMemoryRepositoryTests
{
    private readonly InMemoryDealRepository _repository;

    public InMemoryRepositoryTests()
    {
        _repository = new InMemoryDealRepository();
    }

    #region G2-001 to G2-003: GetAllAsync Tests

    [Fact]
    public async Task GetAllAsync_ReturnsEmptyList_WhenEmpty()
    {
        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllDeals_WhenPopulated()
    {
        // Arrange
        var deal1 = CreateTestDeal("D-001");
        var deal2 = CreateTestDeal("D-002");
        await _repository.UpsertAsync(deal1);
        await _repository.UpsertAsync(deal2);

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(d => d.DealId == "D-001");
        result.Should().Contain(d => d.DealId == "D-002");
    }

    [Fact]
    public async Task GetAllAsync_ReturnsClones_NotOriginals()
    {
        // Arrange
        var original = CreateTestDeal("D-001");
        original.AccountName = "Original Name";
        await _repository.UpsertAsync(original);

        // Act
        var result = await _repository.GetAllAsync();
        result[0].AccountName = "Modified Name";
        var secondResult = await _repository.GetAllAsync();

        // Assert
        secondResult[0].AccountName.Should().Be("Original Name");
    }

    #endregion

    #region G2-004 to G2-005: GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenNotFound()
    {
        // Act
        var result = await _repository.GetByIdAsync("NONEXISTENT");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsClone_WhenFound()
    {
        // Arrange
        var original = CreateTestDeal("D-001");
        original.AccountName = "Original";
        await _repository.UpsertAsync(original);

        // Act
        var result = await _repository.GetByIdAsync("D-001");
        result!.AccountName = "Modified";
        var secondResult = await _repository.GetByIdAsync("D-001");

        // Assert
        secondResult!.AccountName.Should().Be("Original");
    }

    [Fact]
    public async Task GetByIdAsync_IsCaseInsensitive()
    {
        // Arrange
        await _repository.UpsertAsync(CreateTestDeal("D-ABC-123"));

        // Act
        var result = await _repository.GetByIdAsync("d-abc-123");

        // Assert
        result.Should().NotBeNull();
        result!.DealId.Should().Be("D-ABC-123");
    }

    #endregion

    #region G2-006: QueryAsync Tests

    [Fact]
    public async Task QueryAsync_AppliesFilter_Correctly()
    {
        // Arrange
        var deal1 = CreateTestDeal("D-001");
        deal1.Owner = "Alice";
        var deal2 = CreateTestDeal("D-002");
        deal2.Owner = "Bob";
        var deal3 = CreateTestDeal("D-003");
        deal3.Owner = "Alice";

        await _repository.UpsertAsync(deal1);
        await _repository.UpsertAsync(deal2);
        await _repository.UpsertAsync(deal3);

        var filter = new DealFilter { Owner = "Alice" };

        // Act
        var result = await _repository.QueryAsync(filter, DateTime.Today);

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(d => d.Owner == "Alice");
    }

    [Fact]
    public async Task QueryAsync_FiltersByStage_Correctly()
    {
        // Arrange
        var deal1 = CreateTestDeal("D-001");
        deal1.Stage = DealStage.Lead;
        var deal2 = CreateTestDeal("D-002");
        deal2.Stage = DealStage.Proposal;
        var deal3 = CreateTestDeal("D-003");
        deal3.Stage = DealStage.Lead;

        await _repository.UpsertAsync(deal1);
        await _repository.UpsertAsync(deal2);
        await _repository.UpsertAsync(deal3);

        var filter = new DealFilter { Stages = new List<DealStage> { DealStage.Lead } };

        // Act
        var result = await _repository.QueryAsync(filter, DateTime.Today);

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(d => d.Stage == DealStage.Lead);
    }

    #endregion

    #region G2-007 to G2-009: UpsertAsync Tests

    [Fact]
    public async Task UpsertAsync_AddsNewDeal_WhenNotExists()
    {
        // Arrange
        var deal = CreateTestDeal("D-NEW");

        // Act
        await _repository.UpsertAsync(deal);
        var result = await _repository.GetAllAsync();

        // Assert
        result.Should().HaveCount(1);
        result[0].DealId.Should().Be("D-NEW");
    }

    [Fact]
    public async Task UpsertAsync_UpdatesDeal_WhenExists()
    {
        // Arrange
        var deal = CreateTestDeal("D-001");
        deal.AccountName = "Original";
        await _repository.UpsertAsync(deal);

        deal.AccountName = "Updated";
        await _repository.UpsertAsync(deal);

        // Act
        var result = await _repository.GetByIdAsync("D-001");

        // Assert
        result!.AccountName.Should().Be("Updated");
    }

    [Fact]
    public async Task UpsertManyAsync_AddsMultipleDeals()
    {
        // Arrange
        var deals = new List<Deal>
        {
            CreateTestDeal("D-001"),
            CreateTestDeal("D-002"),
            CreateTestDeal("D-003")
        };

        // Act
        await _repository.UpsertManyAsync(deals);
        var result = await _repository.GetAllAsync();

        // Assert
        result.Should().HaveCount(3);
    }

    [Fact]
    public async Task UpsertManyAsync_UpdatesExistingDeals()
    {
        // Arrange
        await _repository.UpsertAsync(CreateTestDeal("D-001"));

        var updatedDeal = CreateTestDeal("D-001");
        updatedDeal.AccountName = "Updated";

        await _repository.UpsertManyAsync(new[] { updatedDeal, CreateTestDeal("D-002") });

        // Act
        var all = await _repository.GetAllAsync();
        var updated = await _repository.GetByIdAsync("D-001");

        // Assert
        all.Should().HaveCount(2);
        updated!.AccountName.Should().Be("Updated");
    }

    #endregion

    #region G2-010 to G2-011: DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_RemovesDeal_WhenExists()
    {
        // Arrange
        await _repository.UpsertAsync(CreateTestDeal("D-001"));
        await _repository.UpsertAsync(CreateTestDeal("D-002"));

        // Act
        await _repository.DeleteAsync("D-001");
        var result = await _repository.GetAllAsync();

        // Assert
        result.Should().HaveCount(1);
        result[0].DealId.Should().Be("D-002");
    }

    [Fact]
    public async Task DeleteAsync_DoesNotThrow_WhenNotExists()
    {
        // Act
        var act = async () => await _repository.DeleteAsync("NONEXISTENT");

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task DeleteAsync_IsCaseInsensitive()
    {
        // Arrange
        await _repository.UpsertAsync(CreateTestDeal("D-ABC-123"));

        // Act
        await _repository.DeleteAsync("d-abc-123");
        var result = await _repository.GetAllAsync();

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region G2-012: SaveChangesAsync Tests

    [Fact]
    public async Task SaveChangesAsync_CompletesSuccessfully()
    {
        // Act
        var act = async () => await _repository.SaveChangesAsync();

        // Assert
        await act.Should().NotThrowAsync();
    }

    #endregion

    #region G2-013 to G2-014: Clear and LoadDeals Tests

    [Fact]
    public async Task Clear_RemovesAllDeals()
    {
        // Arrange
        _repository.LoadDeals(new[] { CreateTestDeal("D-001"), CreateTestDeal("D-002") });

        // Act
        _repository.Clear();
        var result = await _repository.GetAllAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task LoadDeals_ReplacesExistingDeals()
    {
        // Arrange
        _repository.LoadDeals(new[] { CreateTestDeal("D-OLD") });

        // Act
        _repository.LoadDeals(new[] { CreateTestDeal("D-NEW-1"), CreateTestDeal("D-NEW-2") });
        var result = await _repository.GetAllAsync();

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(d => d.DealId == "D-NEW-1");
        result.Should().Contain(d => d.DealId == "D-NEW-2");
        result.Should().NotContain(d => d.DealId == "D-OLD");
    }

    [Fact]
    public async Task LoadDeals_CreatesClones_NotReferences()
    {
        // Arrange
        var original = CreateTestDeal("D-001");
        original.AccountName = "Original";
        _repository.LoadDeals(new[] { original });

        // Act
        original.AccountName = "Modified";
        var result = await _repository.GetAllAsync();

        // Assert
        result[0].AccountName.Should().Be("Original");
    }

    #endregion

    #region Additional Repository Pattern Tests

    [Fact]
    public async Task Repository_SupportsFiltering_ByRegion()
    {
        // Arrange
        var deal1 = CreateTestDeal("D-001");
        deal1.Region = "London";
        var deal2 = CreateTestDeal("D-002");
        deal2.Region = "Scotland";

        await _repository.UpsertAsync(deal1);
        await _repository.UpsertAsync(deal2);

        var filter = new DealFilter { Region = "London" };

        // Act
        var result = await _repository.QueryAsync(filter, DateTime.Today);

        // Assert
        result.Should().HaveCount(1);
        result[0].Region.Should().Be("London");
    }

    [Fact]
    public async Task Repository_SupportsFiltering_ByAmountRange()
    {
        // Arrange
        var deal1 = CreateTestDeal("D-001");
        deal1.AmountGBP = 10000;
        var deal2 = CreateTestDeal("D-002");
        deal2.AmountGBP = 50000;
        var deal3 = CreateTestDeal("D-003");
        deal3.AmountGBP = 100000;

        await _repository.UpsertAsync(deal1);
        await _repository.UpsertAsync(deal2);
        await _repository.UpsertAsync(deal3);

        var filter = new DealFilter { MinAmount = 20000, MaxAmount = 80000 };

        // Act
        var result = await _repository.QueryAsync(filter, DateTime.Today);

        // Assert
        result.Should().HaveCount(1);
        result[0].AmountGBP.Should().Be(50000);
    }

    [Fact]
    public async Task Repository_SupportsFiltering_BySearchText()
    {
        // Arrange
        var deal1 = CreateTestDeal("D-001");
        deal1.AccountName = "Apex Technologies";
        var deal2 = CreateTestDeal("D-002");
        deal2.AccountName = "Beta Solutions";

        await _repository.UpsertAsync(deal1);
        await _repository.UpsertAsync(deal2);

        var filter = new DealFilter { SearchText = "apex" };

        // Act
        var result = await _repository.QueryAsync(filter, DateTime.Today);

        // Assert
        result.Should().HaveCount(1);
        result[0].AccountName.Should().Be("Apex Technologies");
    }

    #endregion

    #region Helper Methods

    private static Deal CreateTestDeal(string dealId) => new()
    {
        DealId = dealId,
        AccountName = $"Test Account {dealId}",
        DealName = $"Test Deal {dealId}",
        Stage = DealStage.Lead,
        Probability = 10,
        AmountGBP = 25000,
        Owner = "Test Owner",
        Region = "London",
        CreatedDate = DateTime.Today
    };

    #endregion
}
