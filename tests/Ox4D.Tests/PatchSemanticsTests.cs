using FluentAssertions;
using Ox4D.Core.Models;
using Ox4D.Core.Models.Config;
using Ox4D.Core.Services;
using Ox4D.Store;
using Xunit;

namespace Ox4D.Tests;

/// <summary>
/// Tests for typed patch validation and result reporting.
/// These tests ensure that:
/// - Unknown fields are rejected with clear reasons
/// - Invalid values are rejected with clear reasons
/// - Valid patches are applied and reported
/// - No silent failures occur
/// </summary>
public class PatchSemanticsTests
{
    private readonly InMemoryDealRepository _repository;
    private readonly PipelineService _service;

    public PatchSemanticsTests()
    {
        _repository = new InMemoryDealRepository();
        var lookups = LookupTables.CreateDefault();
        var settings = new PipelineSettings();
        _service = new PipelineService(_repository, lookups, settings);
    }

    private static Deal CreateTestDeal(string id = "D-TEST-001") => new()
    {
        DealId = id,
        AccountName = "Test Account",
        DealName = "Test Deal",
        Stage = DealStage.Lead,
        AmountGBP = 10000m,
        Owner = "Original Owner"
    };

    // =========================================================================
    // Unknown Field Rejection Tests
    // =========================================================================

    [Fact]
    public async Task PatchDeal_RejectsUnknownField_WithClearReason()
    {
        // Arrange
        await _repository.UpsertAsync(CreateTestDeal());
        var patch = new Dictionary<string, object?> { ["InvalidField"] = "some value" };

        // Act
        var result = await _service.PatchDealWithResultAsync("D-TEST-001", patch);

        // Assert
        result.Success.Should().BeFalse();
        result.RejectedFields.Should().ContainSingle();
        result.RejectedFields[0].FieldName.Should().Be("InvalidField");
        result.RejectedFields[0].Reason.Should().Contain("Unknown field");
    }

    [Fact]
    public async Task PatchDeal_RejectsDerivedField_WithClearReason()
    {
        // Arrange
        await _repository.UpsertAsync(CreateTestDeal());
        var patch = new Dictionary<string, object?> { ["PostcodeArea"] = "SW" };

        // Act
        var result = await _service.PatchDealWithResultAsync("D-TEST-001", patch);

        // Assert
        result.RejectedFields.Should().ContainSingle();
        result.RejectedFields[0].FieldName.Should().Be("PostcodeArea");
        result.RejectedFields[0].Reason.Should().Contain("derived field");
    }

    [Fact]
    public async Task PatchDeal_RejectsDealIdPatch_WithClearReason()
    {
        // Arrange
        await _repository.UpsertAsync(CreateTestDeal());
        var patch = new Dictionary<string, object?> { ["DealId"] = "D-NEW-001" };

        // Act
        var result = await _service.PatchDealWithResultAsync("D-TEST-001", patch);

        // Assert
        result.RejectedFields.Should().ContainSingle();
        result.RejectedFields[0].FieldName.Should().Be("DealId");
        result.RejectedFields[0].Reason.Should().Contain("cannot be patched");
    }

    // =========================================================================
    // Invalid Value Rejection Tests
    // =========================================================================

    [Fact]
    public async Task PatchDeal_RejectsInvalidProbability_OutOfRange()
    {
        // Arrange
        await _repository.UpsertAsync(CreateTestDeal());
        var patch = new Dictionary<string, object?> { ["Probability"] = 150 };

        // Act
        var result = await _service.PatchDealWithResultAsync("D-TEST-001", patch);

        // Assert
        result.RejectedFields.Should().ContainSingle();
        result.RejectedFields[0].FieldName.Should().Be("Probability");
        result.RejectedFields[0].Reason.Should().Contain("between 0 and 100");
    }

    [Fact]
    public async Task PatchDeal_RejectsNegativeProbability()
    {
        // Arrange
        await _repository.UpsertAsync(CreateTestDeal());
        var patch = new Dictionary<string, object?> { ["Probability"] = -10 };

        // Act
        var result = await _service.PatchDealWithResultAsync("D-TEST-001", patch);

        // Assert
        result.RejectedFields.Should().ContainSingle();
        result.RejectedFields[0].FieldName.Should().Be("Probability");
    }

    [Fact]
    public async Task PatchDeal_RejectsNegativeAmount()
    {
        // Arrange
        await _repository.UpsertAsync(CreateTestDeal());
        var patch = new Dictionary<string, object?> { ["AmountGBP"] = -5000m };

        // Act
        var result = await _service.PatchDealWithResultAsync("D-TEST-001", patch);

        // Assert
        result.RejectedFields.Should().ContainSingle();
        result.RejectedFields[0].FieldName.Should().Be("AmountGBP");
        result.RejectedFields[0].Reason.Should().Contain("cannot be negative");
    }

    [Fact]
    public async Task PatchDeal_RejectsInvalidDateFormat()
    {
        // Arrange
        await _repository.UpsertAsync(CreateTestDeal());
        var patch = new Dictionary<string, object?> { ["CloseDate"] = "not-a-date" };

        // Act
        var result = await _service.PatchDealWithResultAsync("D-TEST-001", patch);

        // Assert
        result.RejectedFields.Should().ContainSingle();
        result.RejectedFields[0].FieldName.Should().Be("CloseDate");
        result.RejectedFields[0].Reason.Should().Contain("Invalid date");
    }

    // =========================================================================
    // Successful Patch Tests
    // =========================================================================

    [Fact]
    public async Task PatchDeal_AppliesValidField_ReportsChange()
    {
        // Arrange
        await _repository.UpsertAsync(CreateTestDeal());
        var patch = new Dictionary<string, object?> { ["Owner"] = "New Owner" };

        // Act
        var result = await _service.PatchDealWithResultAsync("D-TEST-001", patch);

        // Assert
        result.Success.Should().BeTrue();
        result.AppliedFields.Should().ContainSingle();
        result.AppliedFields[0].FieldName.Should().Be("Owner");
        result.AppliedFields[0].OldValue.Should().Be("Original Owner");
        result.AppliedFields[0].NewValue.Should().Be("New Owner");
        result.Deal!.Owner.Should().Be("New Owner");
    }

    [Fact]
    public async Task PatchDeal_AppliesMultipleFields_ReportsAllChanges()
    {
        // Arrange
        await _repository.UpsertAsync(CreateTestDeal());
        var patch = new Dictionary<string, object?>
        {
            ["Owner"] = "New Owner",
            ["Stage"] = "Proposal",
            ["AmountGBP"] = 25000m
        };

        // Act
        var result = await _service.PatchDealWithResultAsync("D-TEST-001", patch);

        // Assert
        result.Success.Should().BeTrue();
        result.AppliedFields.Should().HaveCount(3);
        result.AppliedFields.Select(f => f.FieldName).Should().Contain("Owner", "Stage", "AmountGBP");
    }

    [Fact]
    public async Task PatchDeal_ParsesCurrencyAmount_Correctly()
    {
        // Arrange
        await _repository.UpsertAsync(CreateTestDeal());
        var patch = new Dictionary<string, object?> { ["AmountGBP"] = "£75,000" };

        // Act
        var result = await _service.PatchDealWithResultAsync("D-TEST-001", patch);

        // Assert
        result.Success.Should().BeTrue();
        result.Deal!.AmountGBP.Should().Be(75000m);
    }

    // =========================================================================
    // Partial Success Tests
    // =========================================================================

    [Fact]
    public async Task PatchDeal_PartialSuccess_AppliesValidFields_RejectsInvalid()
    {
        // Arrange
        await _repository.UpsertAsync(CreateTestDeal());
        var patch = new Dictionary<string, object?>
        {
            ["Owner"] = "New Owner",      // Valid
            ["InvalidField"] = "value",    // Invalid - unknown
            ["Probability"] = 150          // Invalid - out of range
        };

        // Act
        var result = await _service.PatchDealWithResultAsync("D-TEST-001", patch);

        // Assert
        result.Success.Should().BeFalse(); // False because some fields rejected
        result.Deal.Should().NotBeNull();  // But deal was still updated
        result.AppliedFields.Should().ContainSingle(f => f.FieldName == "Owner");
        result.RejectedFields.Should().HaveCount(2);
    }

    // =========================================================================
    // Not Found Tests
    // =========================================================================

    [Fact]
    public async Task PatchDeal_DealNotFound_ReturnsNotFoundResult()
    {
        // Arrange
        var patch = new Dictionary<string, object?> { ["Owner"] = "New Owner" };

        // Act
        var result = await _service.PatchDealWithResultAsync("D-NONEXISTENT", patch);

        // Assert
        result.Success.Should().BeFalse();
        result.Deal.Should().BeNull();
        result.Error.Should().Contain("not found");
    }

    // =========================================================================
    // Normalization Integration Tests
    // =========================================================================

    [Fact]
    public async Task PatchDeal_TriggersNormalization_ReportsChanges()
    {
        // Arrange
        await _repository.UpsertAsync(CreateTestDeal());
        var patch = new Dictionary<string, object?> { ["Postcode"] = "SW1A 1AA" };

        // Act
        var result = await _service.PatchDealWithResultAsync("D-TEST-001", patch);

        // Assert
        result.Success.Should().BeTrue();
        result.NormalizationChanges.Should().NotBeEmpty();
        // Should have normalized PostcodeArea and Region
        result.NormalizationChanges.Should().Contain(c => c.FieldName == "PostcodeArea");
    }

    [Fact]
    public async Task PatchDeal_StageChange_UpdatesProbability()
    {
        // Arrange
        var deal = CreateTestDeal();
        deal.Probability = 0; // Will be auto-set based on stage
        await _repository.UpsertAsync(deal);
        var patch = new Dictionary<string, object?> { ["Stage"] = "Proposal" };

        // Act
        var result = await _service.PatchDealWithResultAsync("D-TEST-001", patch);

        // Assert
        result.Success.Should().BeTrue();
        result.Deal!.Probability.Should().Be(60); // Proposal = 60%
    }

    // =========================================================================
    // Case Insensitivity Tests
    // =========================================================================

    [Fact]
    public async Task PatchDeal_AcceptsCaseInsensitiveFieldNames()
    {
        // Arrange
        await _repository.UpsertAsync(CreateTestDeal());
        var patch = new Dictionary<string, object?>
        {
            ["OWNER"] = "New Owner",
            ["amountgbp"] = 50000m
        };

        // Act
        var result = await _service.PatchDealWithResultAsync("D-TEST-001", patch);

        // Assert
        result.Success.Should().BeTrue();
        result.AppliedFields.Should().HaveCount(2);
    }

    // =========================================================================
    // Tags Parsing Tests
    // =========================================================================

    [Fact]
    public async Task PatchDeal_ParsesCommaSeparatedTags()
    {
        // Arrange
        await _repository.UpsertAsync(CreateTestDeal());
        var patch = new Dictionary<string, object?> { ["Tags"] = "urgent, high-value, enterprise" };

        // Act
        var result = await _service.PatchDealWithResultAsync("D-TEST-001", patch);

        // Assert
        result.Success.Should().BeTrue();
        result.Deal!.Tags.Should().HaveCount(3);
        result.Deal.Tags.Should().Contain("urgent");
        result.Deal.Tags.Should().Contain("high-value");
        result.Deal.Tags.Should().Contain("enterprise");
    }

    // =========================================================================
    // Boolean Parsing Tests
    // =========================================================================

    [Theory]
    [InlineData("true", true)]
    [InlineData("false", false)]
    [InlineData("yes", true)]
    [InlineData("no", false)]
    [InlineData("1", true)]
    [InlineData("0", false)]
    public async Task PatchDeal_ParsesCommissionPaidVariants(string input, bool expected)
    {
        // Arrange
        await _repository.UpsertAsync(CreateTestDeal());
        var patch = new Dictionary<string, object?> { ["CommissionPaid"] = input };

        // Act
        var result = await _service.PatchDealWithResultAsync("D-TEST-001", patch);

        // Assert
        result.Success.Should().BeTrue();
        result.Deal!.CommissionPaid.Should().Be(expected);
    }
}
