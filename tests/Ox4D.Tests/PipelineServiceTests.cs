using FluentAssertions;
using Ox4D.Core.Models;
using Ox4D.Core.Models.Config;
using Ox4D.Core.Services;
using Ox4D.Store;
using Xunit;

namespace Ox4D.Tests;

/// <summary>
/// Tests for PipelineService - Epic G1: AI-Ready, Epic G3: Intelligence
/// Stories: S1.2.1-S1.2.5 (CRUD), S1.3.1-S1.3.3 (Reports), S3.5.1-S3.5.2 (Stats)
/// </summary>
public class PipelineServiceTests
{
    private readonly PipelineService _service;
    private readonly InMemoryDealRepository _repository;
    private readonly LookupTables _lookups;
    private readonly PipelineSettings _settings;

    public PipelineServiceTests()
    {
        _repository = new InMemoryDealRepository();
        _lookups = LookupTables.CreateDefault();
        _settings = new PipelineSettings();
        _service = new PipelineService(_repository, _lookups, _settings);
    }

    #region G1-001 to G1-003: ListDeals Tests

    [Fact]
    public async Task ListDeals_ReturnsAllDeals_WhenNoFilter()
    {
        // Arrange
        await SeedDeals(5);

        // Act
        var result = await _service.ListDealsAsync();

        // Assert
        result.Should().HaveCount(5);
    }

    [Fact]
    public async Task ListDeals_AppliesFilter_WhenFilterProvided()
    {
        // Arrange
        await _repository.UpsertAsync(CreateDeal("D-001", owner: "Alice"));
        await _repository.UpsertAsync(CreateDeal("D-002", owner: "Bob"));
        await _repository.UpsertAsync(CreateDeal("D-003", owner: "Alice"));

        var filter = new DealFilter { Owner = "Alice" };

        // Act
        var result = await _service.ListDealsAsync(filter);

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(d => d.Owner == "Alice");
    }

    [Fact]
    public async Task ListDeals_ReturnsEmpty_WhenNoDeals()
    {
        // Act
        var result = await _service.ListDealsAsync();

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region G1-010 to G1-013: Filter Variations

    [Fact]
    public async Task ListDeals_FiltersByOwner_Correctly()
    {
        // Arrange
        await _repository.UpsertAsync(CreateDeal("D-001", owner: "Alice"));
        await _repository.UpsertAsync(CreateDeal("D-002", owner: "Bob"));

        // Act
        var result = await _service.ListDealsAsync(new DealFilter { Owner = "Alice" });

        // Assert
        result.Should().HaveCount(1);
        result[0].Owner.Should().Be("Alice");
    }

    [Fact]
    public async Task ListDeals_FiltersByStage_Correctly()
    {
        // Arrange
        await _repository.UpsertAsync(CreateDeal("D-001", stage: DealStage.Lead));
        await _repository.UpsertAsync(CreateDeal("D-002", stage: DealStage.Proposal));

        // Act
        var result = await _service.ListDealsAsync(new DealFilter { Stages = new List<DealStage> { DealStage.Lead } });

        // Assert
        result.Should().HaveCount(1);
        result[0].Stage.Should().Be(DealStage.Lead);
    }

    [Fact]
    public async Task ListDeals_FiltersByRegion_Correctly()
    {
        // Arrange
        await _repository.UpsertAsync(CreateDeal("D-001", region: "London"));
        await _repository.UpsertAsync(CreateDeal("D-002", region: "Scotland"));

        // Act
        var result = await _service.ListDealsAsync(new DealFilter { Region = "London" });

        // Assert
        result.Should().HaveCount(1);
        result[0].Region.Should().Be("London");
    }

    [Fact]
    public async Task ListDeals_FiltersByMinAmount_Correctly()
    {
        // Arrange
        await _repository.UpsertAsync(CreateDeal("D-001", amount: 25000));
        await _repository.UpsertAsync(CreateDeal("D-002", amount: 75000));

        // Act
        var result = await _service.ListDealsAsync(new DealFilter { MinAmount = 50000 });

        // Assert
        result.Should().HaveCount(1);
        result[0].AmountGBP.Should().Be(75000);
    }

    #endregion

    #region G1-014 to G1-015: GetDeal Tests

    [Fact]
    public async Task GetDeal_ReturnsNull_WhenDealNotFound()
    {
        // Act
        var result = await _service.GetDealAsync("NONEXISTENT");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetDeal_ReturnsCorrectDeal_ById()
    {
        // Arrange
        await _repository.UpsertAsync(CreateDeal("D-001"));
        await _repository.UpsertAsync(CreateDeal("D-002"));

        // Act
        var result = await _service.GetDealAsync("D-001");

        // Assert
        result.Should().NotBeNull();
        result!.DealId.Should().Be("D-001");
    }

    [Fact]
    public async Task GetDeal_IsCaseInsensitive_ForDealId()
    {
        // Arrange
        await _repository.UpsertAsync(CreateDeal("D-ABC-123"));

        // Act
        var result = await _service.GetDealAsync("d-abc-123");

        // Assert
        result.Should().NotBeNull();
        result!.DealId.Should().Be("D-ABC-123");
    }

    #endregion

    #region G1-016 to G1-019: UpsertDeal Tests

    [Fact]
    public async Task UpsertDeal_CreatesNewDeal_WhenNotExists()
    {
        // Arrange
        var deal = new Deal
        {
            AccountName = "New Account",
            DealName = "New Deal",
            Stage = DealStage.Lead
        };

        // Act
        var result = await _service.UpsertDealAsync(deal);

        // Assert
        result.DealId.Should().NotBeNullOrEmpty();
        var saved = await _service.GetDealAsync(result.DealId);
        saved.Should().NotBeNull();
    }

    [Fact]
    public async Task UpsertDeal_UpdatesExisting_WhenExists()
    {
        // Arrange
        await _repository.UpsertAsync(CreateDeal("D-001"));
        var updated = CreateDeal("D-001");
        updated.AccountName = "Updated Account";

        // Act
        var result = await _service.UpsertDealAsync(updated);

        // Assert
        var saved = await _service.GetDealAsync("D-001");
        saved!.AccountName.Should().Be("Updated Account");
    }

    [Fact]
    public async Task UpsertDeal_NormalizesData_BeforeSave()
    {
        // Arrange
        var deal = new Deal
        {
            AccountName = "Test",
            DealName = "Test Deal",
            Stage = DealStage.Proposal,
            Probability = 0,
            Postcode = "SW1A 1AA"
        };

        // Act
        var result = await _service.UpsertDealAsync(deal);

        // Assert
        result.Probability.Should().Be(60); // Default for Proposal
        result.PostcodeArea.Should().Be("SW");
        result.Region.Should().Be("London");
    }

    [Fact]
    public async Task UpsertDeal_GeneratesDealId_WhenMissing()
    {
        // Arrange
        var deal = new Deal
        {
            AccountName = "Test",
            DealName = "Test Deal"
        };

        // Act
        var result = await _service.UpsertDealAsync(deal);

        // Assert
        result.DealId.Should().NotBeNullOrEmpty();
        result.DealId.Should().StartWith("D-");
    }

    #endregion

    #region G1-020 to G1-024: PatchDeal Tests

    [Fact]
    public async Task PatchDeal_ReturnsNull_WhenDealNotFound()
    {
        // Arrange
        var patch = new Dictionary<string, object?> { ["AccountName"] = "Updated" };

        // Act
        var result = await _service.PatchDealAsync("NONEXISTENT", patch);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task PatchDeal_UpdatesSingleField_Correctly()
    {
        // Arrange
        await _repository.UpsertAsync(CreateDeal("D-001", accountName: "Original"));
        var patch = new Dictionary<string, object?> { ["AccountName"] = "Updated" };

        // Act
        var result = await _service.PatchDealAsync("D-001", patch);

        // Assert
        result!.AccountName.Should().Be("Updated");
    }

    [Fact]
    public async Task PatchDeal_UpdatesMultipleFields_Correctly()
    {
        // Arrange
        await _repository.UpsertAsync(CreateDeal("D-001"));
        var patch = new Dictionary<string, object?>
        {
            ["AccountName"] = "New Account",
            ["AmountGBP"] = 99999m,
            ["Owner"] = "New Owner"
        };

        // Act
        var result = await _service.PatchDealAsync("D-001", patch);

        // Assert
        result!.AccountName.Should().Be("New Account");
        result.AmountGBP.Should().Be(99999m);
        result.Owner.Should().Be("New Owner");
    }

    [Fact]
    public async Task PatchDeal_ParsesDateField_Correctly()
    {
        // Arrange
        await _repository.UpsertAsync(CreateDeal("D-001"));
        var patch = new Dictionary<string, object?> { ["CloseDate"] = "2025-06-15" };

        // Act
        var result = await _service.PatchDealAsync("D-001", patch);

        // Assert
        result!.CloseDate.Should().Be(new DateTime(2025, 6, 15));
    }

    [Fact]
    public async Task PatchDeal_ParsesAmountField_Correctly()
    {
        // Arrange
        await _repository.UpsertAsync(CreateDeal("D-001"));
        var patch = new Dictionary<string, object?> { ["AmountGBP"] = "£50,000" };

        // Act
        var result = await _service.PatchDealAsync("D-001", patch);

        // Assert
        result!.AmountGBP.Should().Be(50000m);
    }

    [Fact]
    public async Task PatchDeal_ParsesStageField_Correctly()
    {
        // Arrange
        await _repository.UpsertAsync(CreateDeal("D-001"));
        var patch = new Dictionary<string, object?> { ["Stage"] = "Proposal" };

        // Act
        var result = await _service.PatchDealAsync("D-001", patch);

        // Assert
        result!.Stage.Should().Be(DealStage.Proposal);
    }

    #endregion

    #region G1-025 to G1-026: DeleteDeal Tests

    [Fact]
    public async Task DeleteDeal_RemovesDeal_FromRepository()
    {
        // Arrange
        await _repository.UpsertAsync(CreateDeal("D-001"));
        await _repository.UpsertAsync(CreateDeal("D-002"));

        // Act
        await _service.DeleteDealAsync("D-001");
        var allDeals = await _service.ListDealsAsync();

        // Assert
        allDeals.Should().HaveCount(1);
        allDeals[0].DealId.Should().Be("D-002");
    }

    [Fact]
    public async Task DeleteDeal_DoesNotThrow_WhenDealNotFound()
    {
        // Act
        var act = async () => await _service.DeleteDealAsync("NONEXISTENT");

        // Assert
        await act.Should().NotThrowAsync();
    }

    #endregion

    #region G1-027 to G1-029: Report Tools Tests

    [Fact]
    public async Task DailyBrief_ViaService_ReturnsValidReport()
    {
        // Arrange
        await SeedDeals(10);

        // Act
        var result = await _service.GetDailyBriefAsync();

        // Assert
        result.Should().NotBeNull();
        result.ReferenceDate.Should().Be(DateTime.Today);
    }

    [Fact]
    public async Task HygieneReport_ViaService_ReturnsValidReport()
    {
        // Arrange
        await SeedDeals(10);

        // Act
        var result = await _service.GetHygieneReportAsync();

        // Assert
        result.Should().NotBeNull();
        result.TotalDeals.Should().Be(10);
    }

    [Fact]
    public async Task ForecastSnapshot_ViaService_ReturnsValidReport()
    {
        // Arrange
        await SeedDeals(10);

        // Act
        var result = await _service.GetForecastSnapshotAsync();

        // Assert
        result.Should().NotBeNull();
        result.TotalDeals.Should().Be(10);
    }

    #endregion

    #region G1-030: Synthetic Data Generation

    [Fact]
    public async Task GenerateSyntheticData_CreatesDeals_WithCount()
    {
        // Act
        var count = await _service.GenerateSyntheticDataAsync(50, 42);
        var deals = await _service.ListDealsAsync();

        // Assert
        count.Should().Be(50);
        deals.Should().HaveCount(50);
    }

    [Fact]
    public async Task GenerateSyntheticData_IsDeterministic_WithSameSeed()
    {
        // Arrange
        var service1 = new PipelineService(new InMemoryDealRepository(), _lookups, _settings);
        var service2 = new PipelineService(new InMemoryDealRepository(), _lookups, _settings);

        // Act
        await service1.GenerateSyntheticDataAsync(10, 42);
        await service2.GenerateSyntheticDataAsync(10, 42);

        var deals1 = await service1.ListDealsAsync();
        var deals2 = await service2.ListDealsAsync();

        // Assert
        for (int i = 0; i < 10; i++)
        {
            deals1[i].AccountName.Should().Be(deals2[i].AccountName);
            deals1[i].AmountGBP.Should().Be(deals2[i].AmountGBP);
        }
    }

    #endregion

    #region G3-056 to G3-065: Pipeline Statistics

    [Fact]
    public async Task GetStats_ReturnsTotalDeals_Count()
    {
        // Arrange
        await SeedDeals(15);

        // Act
        var stats = await _service.GetStatsAsync();

        // Assert
        stats.TotalDeals.Should().Be(15);
    }

    [Fact]
    public async Task GetStats_ReturnsOpenDeals_Count()
    {
        // Arrange
        await _repository.UpsertAsync(CreateDeal("D-001", stage: DealStage.Lead));
        await _repository.UpsertAsync(CreateDeal("D-002", stage: DealStage.Proposal));
        await _repository.UpsertAsync(CreateDeal("D-003", stage: DealStage.ClosedWon));

        // Act
        var stats = await _service.GetStatsAsync();

        // Assert
        stats.OpenDeals.Should().Be(2);
    }

    [Fact]
    public async Task GetStats_ReturnsClosedWonDeals_Count()
    {
        // Arrange
        await _repository.UpsertAsync(CreateDeal("D-001", stage: DealStage.Lead));
        await _repository.UpsertAsync(CreateDeal("D-002", stage: DealStage.ClosedWon));
        await _repository.UpsertAsync(CreateDeal("D-003", stage: DealStage.ClosedWon));

        // Act
        var stats = await _service.GetStatsAsync();

        // Assert
        stats.ClosedWonDeals.Should().Be(2);
    }

    [Fact]
    public async Task GetStats_ReturnsClosedLostDeals_Count()
    {
        // Arrange
        await _repository.UpsertAsync(CreateDeal("D-001", stage: DealStage.Lead));
        await _repository.UpsertAsync(CreateDeal("D-002", stage: DealStage.ClosedLost));

        // Act
        var stats = await _service.GetStatsAsync();

        // Assert
        stats.ClosedLostDeals.Should().Be(1);
    }

    [Fact]
    public async Task GetStats_ReturnsTotalPipeline_Value()
    {
        // Arrange
        await _repository.UpsertAsync(CreateDeal("D-001", stage: DealStage.Lead, amount: 10000));
        await _repository.UpsertAsync(CreateDeal("D-002", stage: DealStage.Proposal, amount: 20000));
        await _repository.UpsertAsync(CreateDeal("D-003", stage: DealStage.ClosedWon, amount: 50000));

        // Act
        var stats = await _service.GetStatsAsync();

        // Assert
        stats.TotalPipeline.Should().Be(30000); // Only open deals
    }

    [Fact]
    public async Task GetStats_ReturnsWeightedPipeline_Value()
    {
        // Arrange
        await _repository.UpsertAsync(CreateDeal("D-001", stage: DealStage.Lead, amount: 10000, probability: 10));
        await _repository.UpsertAsync(CreateDeal("D-002", stage: DealStage.Proposal, amount: 20000, probability: 60));

        // Act
        var stats = await _service.GetStatsAsync();

        // Assert
        stats.WeightedPipeline.Should().Be(1000 + 12000); // 10000*0.1 + 20000*0.6 = 13000
    }

    [Fact]
    public async Task GetStats_ReturnsClosedWonValue()
    {
        // Arrange
        await _repository.UpsertAsync(CreateDeal("D-001", stage: DealStage.ClosedWon, amount: 50000));
        await _repository.UpsertAsync(CreateDeal("D-002", stage: DealStage.ClosedWon, amount: 30000));
        await _repository.UpsertAsync(CreateDeal("D-003", stage: DealStage.Lead, amount: 10000));

        // Act
        var stats = await _service.GetStatsAsync();

        // Assert
        stats.ClosedWonValue.Should().Be(80000);
    }

    [Fact]
    public async Task GetStats_ReturnsAverageDealsValue()
    {
        // Arrange
        await _repository.UpsertAsync(CreateDeal("D-001", stage: DealStage.Lead, amount: 10000));
        await _repository.UpsertAsync(CreateDeal("D-002", stage: DealStage.Proposal, amount: 30000));

        // Act
        var stats = await _service.GetStatsAsync();

        // Assert
        stats.AverageDealsValue.Should().Be(20000);
    }

    [Fact]
    public async Task GetStats_ReturnsUniqueOwners()
    {
        // Arrange
        await _repository.UpsertAsync(CreateDeal("D-001", owner: "Alice"));
        await _repository.UpsertAsync(CreateDeal("D-002", owner: "Bob"));
        await _repository.UpsertAsync(CreateDeal("D-003", owner: "Alice"));

        // Act
        var stats = await _service.GetStatsAsync();

        // Assert
        stats.Owners.Should().HaveCount(2);
        stats.Owners.Should().Contain("Alice");
        stats.Owners.Should().Contain("Bob");
    }

    [Fact]
    public async Task GetStats_ReturnsUniqueRegions()
    {
        // Arrange
        await _repository.UpsertAsync(CreateDeal("D-001", region: "London"));
        await _repository.UpsertAsync(CreateDeal("D-002", region: "Scotland"));
        await _repository.UpsertAsync(CreateDeal("D-003", region: "London"));

        // Act
        var stats = await _service.GetStatsAsync();

        // Assert
        stats.Regions.Should().HaveCount(2);
        stats.Regions.Should().Contain("London");
        stats.Regions.Should().Contain("Scotland");
    }

    #endregion

    #region Helper Methods

    private async Task SeedDeals(int count)
    {
        for (int i = 0; i < count; i++)
        {
            await _repository.UpsertAsync(CreateDeal($"D-{i:D4}"));
        }
    }

    private static Deal CreateDeal(
        string dealId,
        string owner = "Test Owner",
        DealStage stage = DealStage.Lead,
        string region = "London",
        decimal amount = 50000,
        int probability = 0,
        string accountName = "Test Account")
    {
        var deal = new Deal
        {
            DealId = dealId,
            AccountName = accountName,
            DealName = $"Deal {dealId}",
            Owner = owner,
            Stage = stage,
            Region = region,
            AmountGBP = amount,
            Probability = probability > 0 ? probability : stage.GetDefaultProbability(),
            CreatedDate = DateTime.Today.AddDays(-30),
            LastContactedDate = DateTime.Today.AddDays(-5)
        };
        return deal;
    }

    #endregion
}
