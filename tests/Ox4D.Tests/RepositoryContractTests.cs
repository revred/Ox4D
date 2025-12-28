using FluentAssertions;
using Ox4D.Core.Models;
using Ox4D.Core.Services;
using Xunit;

namespace Ox4D.Tests;

/// <summary>
/// Abstract contract tests for IDealRepository implementations.
/// All repository implementations must pass these tests to ensure behavioral consistency.
/// Based on Review/TestReview.md requirement for shared repository contract tests.
/// </summary>
public abstract class RepositoryContractTests<TRepository> where TRepository : IDealRepository
{
    protected abstract TRepository CreateRepository();
    protected abstract Task CleanupRepository(TRepository repository);

    #region GetAllAsync Contract

    [Fact]
    public async Task Contract_GetAllAsync_ReturnsEmptyList_WhenEmpty()
    {
        var repo = CreateRepository();
        try
        {
            var result = await repo.GetAllAsync();
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }
        finally
        {
            await CleanupRepository(repo);
        }
    }

    [Fact]
    public async Task Contract_GetAllAsync_ReturnsAllDeals()
    {
        var repo = CreateRepository();
        try
        {
            var deal1 = CreateDeal("CONTRACT-001");
            var deal2 = CreateDeal("CONTRACT-002");
            await repo.UpsertAsync(deal1);
            await repo.UpsertAsync(deal2);

            var result = await repo.GetAllAsync();

            result.Should().HaveCount(2);
            result.Select(d => d.DealId).Should().Contain("CONTRACT-001");
            result.Select(d => d.DealId).Should().Contain("CONTRACT-002");
        }
        finally
        {
            await CleanupRepository(repo);
        }
    }

    [Fact]
    public async Task Contract_GetAllAsync_ReturnsDefensiveCopies()
    {
        var repo = CreateRepository();
        try
        {
            var deal = CreateDeal("CONTRACT-001");
            deal.AccountName = "Original";
            await repo.UpsertAsync(deal);

            var result1 = await repo.GetAllAsync();
            result1[0].AccountName = "Modified";

            var result2 = await repo.GetAllAsync();
            result2[0].AccountName.Should().Be("Original", "modifications to returned deals should not affect stored data");
        }
        finally
        {
            await CleanupRepository(repo);
        }
    }

    #endregion

    #region GetByIdAsync Contract

    [Fact]
    public async Task Contract_GetByIdAsync_ReturnsNull_WhenNotFound()
    {
        var repo = CreateRepository();
        try
        {
            var result = await repo.GetByIdAsync("NONEXISTENT-ID");
            result.Should().BeNull();
        }
        finally
        {
            await CleanupRepository(repo);
        }
    }

    [Fact]
    public async Task Contract_GetByIdAsync_ReturnsDeal_WhenFound()
    {
        var repo = CreateRepository();
        try
        {
            var deal = CreateDeal("CONTRACT-001");
            await repo.UpsertAsync(deal);

            var result = await repo.GetByIdAsync("CONTRACT-001");

            result.Should().NotBeNull();
            result!.DealId.Should().Be("CONTRACT-001");
        }
        finally
        {
            await CleanupRepository(repo);
        }
    }

    [Fact]
    public async Task Contract_GetByIdAsync_IsCaseInsensitive()
    {
        var repo = CreateRepository();
        try
        {
            var deal = CreateDeal("CONTRACT-ABC-123");
            await repo.UpsertAsync(deal);

            var result = await repo.GetByIdAsync("contract-abc-123");

            result.Should().NotBeNull();
            result!.DealId.Should().Be("CONTRACT-ABC-123");
        }
        finally
        {
            await CleanupRepository(repo);
        }
    }

    [Fact]
    public async Task Contract_GetByIdAsync_ReturnsDefensiveCopy()
    {
        var repo = CreateRepository();
        try
        {
            var deal = CreateDeal("CONTRACT-001");
            deal.AccountName = "Original";
            await repo.UpsertAsync(deal);

            var result1 = await repo.GetByIdAsync("CONTRACT-001");
            result1!.AccountName = "Modified";

            var result2 = await repo.GetByIdAsync("CONTRACT-001");
            result2!.AccountName.Should().Be("Original");
        }
        finally
        {
            await CleanupRepository(repo);
        }
    }

    #endregion

    #region UpsertAsync Contract

    [Fact]
    public async Task Contract_UpsertAsync_CreatesNewDeal()
    {
        var repo = CreateRepository();
        try
        {
            var deal = CreateDeal("CONTRACT-NEW");

            await repo.UpsertAsync(deal);

            var result = await repo.GetByIdAsync("CONTRACT-NEW");
            result.Should().NotBeNull();
        }
        finally
        {
            await CleanupRepository(repo);
        }
    }

    [Fact]
    public async Task Contract_UpsertAsync_UpdatesExistingDeal()
    {
        var repo = CreateRepository();
        try
        {
            var deal = CreateDeal("CONTRACT-001");
            deal.AccountName = "Original";
            await repo.UpsertAsync(deal);

            deal.AccountName = "Updated";
            await repo.UpsertAsync(deal);

            var result = await repo.GetByIdAsync("CONTRACT-001");
            result!.AccountName.Should().Be("Updated");
        }
        finally
        {
            await CleanupRepository(repo);
        }
    }

    [Fact]
    public async Task Contract_UpsertAsync_UpdatesMatchesCaseInsensitively()
    {
        var repo = CreateRepository();
        try
        {
            var deal = CreateDeal("CONTRACT-ABC");
            deal.AccountName = "Original";
            await repo.UpsertAsync(deal);

            var updateDeal = CreateDeal("contract-abc");
            updateDeal.AccountName = "Updated";
            await repo.UpsertAsync(updateDeal);

            var all = await repo.GetAllAsync();
            all.Should().HaveCount(1, "should update existing deal, not create new one");
        }
        finally
        {
            await CleanupRepository(repo);
        }
    }

    #endregion

    #region UpsertManyAsync Contract

    [Fact]
    public async Task Contract_UpsertManyAsync_CreatesMultipleDeals()
    {
        var repo = CreateRepository();
        try
        {
            var deals = new[]
            {
                CreateDeal("CONTRACT-001"),
                CreateDeal("CONTRACT-002"),
                CreateDeal("CONTRACT-003")
            };

            await repo.UpsertManyAsync(deals);

            var result = await repo.GetAllAsync();
            result.Should().HaveCount(3);
        }
        finally
        {
            await CleanupRepository(repo);
        }
    }

    [Fact]
    public async Task Contract_UpsertManyAsync_UpdatesExistingAndCreatesNew()
    {
        var repo = CreateRepository();
        try
        {
            await repo.UpsertAsync(CreateDeal("CONTRACT-001"));

            var updated = CreateDeal("CONTRACT-001");
            updated.AccountName = "Updated";
            var newDeal = CreateDeal("CONTRACT-002");

            await repo.UpsertManyAsync(new[] { updated, newDeal });

            var all = await repo.GetAllAsync();
            all.Should().HaveCount(2);

            var first = await repo.GetByIdAsync("CONTRACT-001");
            first!.AccountName.Should().Be("Updated");
        }
        finally
        {
            await CleanupRepository(repo);
        }
    }

    #endregion

    #region DeleteAsync Contract

    [Fact]
    public async Task Contract_DeleteAsync_RemovesDeal()
    {
        var repo = CreateRepository();
        try
        {
            await repo.UpsertAsync(CreateDeal("CONTRACT-001"));
            await repo.UpsertAsync(CreateDeal("CONTRACT-002"));

            await repo.DeleteAsync("CONTRACT-001");

            var all = await repo.GetAllAsync();
            all.Should().HaveCount(1);
            all[0].DealId.Should().Be("CONTRACT-002");
        }
        finally
        {
            await CleanupRepository(repo);
        }
    }

    [Fact]
    public async Task Contract_DeleteAsync_DoesNotThrow_WhenNotExists()
    {
        var repo = CreateRepository();
        try
        {
            var act = async () => await repo.DeleteAsync("NONEXISTENT");
            await act.Should().NotThrowAsync();
        }
        finally
        {
            await CleanupRepository(repo);
        }
    }

    [Fact]
    public async Task Contract_DeleteAsync_IsCaseInsensitive()
    {
        var repo = CreateRepository();
        try
        {
            await repo.UpsertAsync(CreateDeal("CONTRACT-ABC"));

            await repo.DeleteAsync("contract-abc");

            var all = await repo.GetAllAsync();
            all.Should().BeEmpty();
        }
        finally
        {
            await CleanupRepository(repo);
        }
    }

    #endregion

    #region QueryAsync Contract

    [Fact]
    public async Task Contract_QueryAsync_FiltersByOwner()
    {
        var repo = CreateRepository();
        try
        {
            var deal1 = CreateDeal("CONTRACT-001");
            deal1.Owner = "Alice";
            var deal2 = CreateDeal("CONTRACT-002");
            deal2.Owner = "Bob";
            var deal3 = CreateDeal("CONTRACT-003");
            deal3.Owner = "Alice";

            await repo.UpsertManyAsync(new[] { deal1, deal2, deal3 });

            var filter = new DealFilter { Owner = "Alice" };
            var result = await repo.QueryAsync(filter, DateTime.Today);

            result.Should().HaveCount(2);
            result.Should().OnlyContain(d => d.Owner == "Alice");
        }
        finally
        {
            await CleanupRepository(repo);
        }
    }

    [Fact]
    public async Task Contract_QueryAsync_FiltersByStage()
    {
        var repo = CreateRepository();
        try
        {
            var deal1 = CreateDeal("CONTRACT-001");
            deal1.Stage = DealStage.Lead;
            var deal2 = CreateDeal("CONTRACT-002");
            deal2.Stage = DealStage.Proposal;
            var deal3 = CreateDeal("CONTRACT-003");
            deal3.Stage = DealStage.Lead;

            await repo.UpsertManyAsync(new[] { deal1, deal2, deal3 });

            var filter = new DealFilter { Stages = new List<DealStage> { DealStage.Lead } };
            var result = await repo.QueryAsync(filter, DateTime.Today);

            result.Should().HaveCount(2);
            result.Should().OnlyContain(d => d.Stage == DealStage.Lead);
        }
        finally
        {
            await CleanupRepository(repo);
        }
    }

    [Fact]
    public async Task Contract_QueryAsync_FiltersByAmountRange()
    {
        var repo = CreateRepository();
        try
        {
            var deal1 = CreateDeal("CONTRACT-001");
            deal1.AmountGBP = 10000;
            var deal2 = CreateDeal("CONTRACT-002");
            deal2.AmountGBP = 50000;
            var deal3 = CreateDeal("CONTRACT-003");
            deal3.AmountGBP = 100000;

            await repo.UpsertManyAsync(new[] { deal1, deal2, deal3 });

            var filter = new DealFilter { MinAmount = 20000, MaxAmount = 80000 };
            var result = await repo.QueryAsync(filter, DateTime.Today);

            result.Should().HaveCount(1);
            result[0].AmountGBP.Should().Be(50000);
        }
        finally
        {
            await CleanupRepository(repo);
        }
    }

    #endregion

    #region SaveChangesAsync Contract

    [Fact]
    public async Task Contract_SaveChangesAsync_CompletesWithoutError()
    {
        var repo = CreateRepository();
        try
        {
            await repo.UpsertAsync(CreateDeal("CONTRACT-001"));

            var act = async () => await repo.SaveChangesAsync();

            await act.Should().NotThrowAsync();
        }
        finally
        {
            await CleanupRepository(repo);
        }
    }

    #endregion

    #region Helper Methods

    protected static Deal CreateDeal(string dealId) => new()
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
