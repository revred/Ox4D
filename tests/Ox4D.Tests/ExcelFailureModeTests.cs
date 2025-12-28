using ClosedXML.Excel;
using FluentAssertions;
using Ox4D.Core.Models.Config;
using Ox4D.Store;
using Xunit;

namespace Ox4D.Tests;

/// <summary>
/// Tests for Excel failure modes - corrupted files, missing sheets, missing columns.
/// Based on Review/TestReview.md requirement for Excel failure mode tests.
/// </summary>
public class ExcelFailureModeTests : IDisposable
{
    private readonly string _testDir;
    private readonly LookupTables _lookups;

    public ExcelFailureModeTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), $"Ox4D_FailureTests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDir);
        _lookups = LookupTables.CreateDefault();
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_testDir))
            {
                Directory.Delete(_testDir, recursive: true);
            }
        }
        catch { }
    }

    private string GetTestFilePath(string name) => Path.Combine(_testDir, $"{name}.xlsx");

    #region Corrupted Workbook Tests

    [Fact]
    public void ValidateExcelFile_ReturnsInvalid_WhenFileDoesNotExist()
    {
        var filePath = GetTestFilePath("nonexistent");

        var result = ExcelDealRepository.ValidateExcelFile(filePath);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("does not exist"));
    }

    [Fact]
    public void ValidateExcelFile_ReturnsInvalid_WhenFileIsCorrupted()
    {
        var filePath = GetTestFilePath("corrupted");
        File.WriteAllText(filePath, "This is not a valid Excel file");

        var result = ExcelDealRepository.ValidateExcelFile(filePath);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Failed to read"));
    }

    [Fact]
    public void ValidateExcelFile_ReturnsInvalid_WhenFileIsEmpty()
    {
        var filePath = GetTestFilePath("empty");
        File.WriteAllBytes(filePath, Array.Empty<byte>());

        var result = ExcelDealRepository.ValidateExcelFile(filePath);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task LoadAsync_ThrowsException_WhenFileIsCorrupted()
    {
        var filePath = GetTestFilePath("corrupted_load");
        File.WriteAllText(filePath, "Not valid Excel content");

        var repo = new ExcelDealRepository(filePath, _lookups);

        var act = async () => await repo.LoadAsync();

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*validation failed*");
    }

    #endregion

    #region Missing Sheet Tests

    [Fact]
    public void ValidateExcelFile_ReturnsInvalid_WhenDealsSheetMissing()
    {
        var filePath = GetTestFilePath("no_deals_sheet");
        using (var workbook = new XLWorkbook())
        {
            // Create a workbook with only Lookups sheet, no Deals sheet
            var sheet = workbook.Worksheets.Add("OtherSheet");
            sheet.Cell(1, 1).Value = "SomeColumn";
            workbook.SaveAs(filePath);
        }

        var result = ExcelDealRepository.ValidateExcelFile(filePath);

        result.IsValid.Should().BeFalse();
        result.HasDealsSheet.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Deals"));
    }

    [Fact]
    public void ValidateExcelFile_ReturnsWarning_WhenLookupsSheetMissing()
    {
        var filePath = GetTestFilePath("no_lookups_sheet");
        using (var workbook = new XLWorkbook())
        {
            var sheet = workbook.Worksheets.Add("Deals");
            sheet.Cell(1, 1).Value = "DealId";
            sheet.Cell(1, 2).Value = "AccountName";
            sheet.Cell(1, 3).Value = "DealName";
            sheet.Cell(1, 4).Value = "Stage";
            workbook.SaveAs(filePath);
        }

        var result = ExcelDealRepository.ValidateExcelFile(filePath);

        result.IsValid.Should().BeTrue(); // Missing Lookups is a warning, not an error
        result.HasLookupsSheet.Should().BeFalse();
        result.Warnings.Should().Contain(w => w.Contains("Lookups"));
    }

    [Fact]
    public void ValidateExcelFile_ReturnsWarning_WhenMetadataSheetMissing()
    {
        var filePath = GetTestFilePath("no_metadata_sheet");
        using (var workbook = new XLWorkbook())
        {
            var sheet = workbook.Worksheets.Add("Deals");
            sheet.Cell(1, 1).Value = "DealId";
            sheet.Cell(1, 2).Value = "AccountName";
            sheet.Cell(1, 3).Value = "DealName";
            sheet.Cell(1, 4).Value = "Stage";
            workbook.SaveAs(filePath);
        }

        var result = ExcelDealRepository.ValidateExcelFile(filePath);

        result.IsValid.Should().BeTrue(); // Missing Metadata is a warning, not an error
        result.HasMetadataSheet.Should().BeFalse();
        result.Warnings.Should().Contain(w => w.Contains("Metadata"));
    }

    #endregion

    #region Missing Column Tests

    [Fact]
    public void ValidateExcelFile_ReturnsInvalid_WhenDealIdColumnMissing()
    {
        var filePath = GetTestFilePath("no_dealid_column");
        using (var workbook = new XLWorkbook())
        {
            var sheet = workbook.Worksheets.Add("Deals");
            // Missing DealId column
            sheet.Cell(1, 1).Value = "AccountName";
            sheet.Cell(1, 2).Value = "DealName";
            sheet.Cell(1, 3).Value = "Stage";
            workbook.SaveAs(filePath);
        }

        var result = ExcelDealRepository.ValidateExcelFile(filePath);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("DealId"));
    }

    [Fact]
    public void ValidateExcelFile_ReturnsInvalid_WhenAccountNameColumnMissing()
    {
        var filePath = GetTestFilePath("no_account_column");
        using (var workbook = new XLWorkbook())
        {
            var sheet = workbook.Worksheets.Add("Deals");
            sheet.Cell(1, 1).Value = "DealId";
            // Missing AccountName column
            sheet.Cell(1, 2).Value = "DealName";
            sheet.Cell(1, 3).Value = "Stage";
            workbook.SaveAs(filePath);
        }

        var result = ExcelDealRepository.ValidateExcelFile(filePath);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("AccountName"));
    }

    [Fact]
    public void ValidateExcelFile_ReturnsInvalid_WhenDealNameColumnMissing()
    {
        var filePath = GetTestFilePath("no_dealname_column");
        using (var workbook = new XLWorkbook())
        {
            var sheet = workbook.Worksheets.Add("Deals");
            sheet.Cell(1, 1).Value = "DealId";
            sheet.Cell(1, 2).Value = "AccountName";
            // Missing DealName column
            sheet.Cell(1, 3).Value = "Stage";
            workbook.SaveAs(filePath);
        }

        var result = ExcelDealRepository.ValidateExcelFile(filePath);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("DealName"));
    }

    [Fact]
    public void ValidateExcelFile_ReturnsInvalid_WhenStageColumnMissing()
    {
        var filePath = GetTestFilePath("no_stage_column");
        using (var workbook = new XLWorkbook())
        {
            var sheet = workbook.Worksheets.Add("Deals");
            sheet.Cell(1, 1).Value = "DealId";
            sheet.Cell(1, 2).Value = "AccountName";
            sheet.Cell(1, 3).Value = "DealName";
            // Missing Stage column
            workbook.SaveAs(filePath);
        }

        var result = ExcelDealRepository.ValidateExcelFile(filePath);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Stage"));
    }

    [Fact]
    public void ValidateExcelFile_ReturnsValid_WhenAllRequiredColumnsPresent()
    {
        var filePath = GetTestFilePath("valid_columns");
        using (var workbook = new XLWorkbook())
        {
            var sheet = workbook.Worksheets.Add("Deals");
            sheet.Cell(1, 1).Value = "DealId";
            sheet.Cell(1, 2).Value = "AccountName";
            sheet.Cell(1, 3).Value = "DealName";
            sheet.Cell(1, 4).Value = "Stage";
            workbook.SaveAs(filePath);
        }

        var result = ExcelDealRepository.ValidateExcelFile(filePath);

        result.IsValid.Should().BeTrue();
        result.HasDealsSheet.Should().BeTrue();
    }

    #endregion

    #region Backup and Recovery Tests

    [Fact]
    public async Task Repository_CreatesBackup_BeforeSave()
    {
        var filePath = GetTestFilePath("backup_test");
        var repo = new ExcelDealRepository(filePath, _lookups);

        // Create initial file
        await repo.UpsertAsync(new Ox4D.Core.Models.Deal
        {
            DealId = "D-001",
            AccountName = "Test",
            DealName = "Test Deal",
            Stage = Ox4D.Core.Models.DealStage.Lead
        });
        await repo.SaveChangesAsync();

        // Update and save again to trigger backup
        await repo.UpsertAsync(new Ox4D.Core.Models.Deal
        {
            DealId = "D-002",
            AccountName = "Test2",
            DealName = "Test Deal 2",
            Stage = Ox4D.Core.Models.DealStage.Lead
        });
        await repo.SaveChangesAsync();

        var backups = repo.GetBackups();
        backups.Should().NotBeEmpty("a backup should be created before save");
    }

    [Fact]
    public async Task Repository_RotatesBackups_AccordingToMaxBackups()
    {
        var filePath = GetTestFilePath("rotation_test");
        var maxBackups = 3;
        var repo = new ExcelDealRepository(filePath, _lookups, maxBackups: maxBackups);

        // Create initial file and several saves to create backups
        for (int i = 0; i < 5; i++)
        {
            await repo.UpsertAsync(new Ox4D.Core.Models.Deal
            {
                DealId = $"D-{i:D3}",
                AccountName = $"Test {i}",
                DealName = $"Test Deal {i}",
                Stage = Ox4D.Core.Models.DealStage.Lead
            });
            await repo.SaveChangesAsync();
            await Task.Delay(10); // Small delay to ensure unique timestamps
        }

        var backups = repo.GetBackups();
        backups.Should().HaveCountLessOrEqualTo(maxBackups,
            "old backups should be rotated out");
    }

    [Fact]
    public void GetBackups_ReturnsBackupsInDescendingOrder()
    {
        var filePath = GetTestFilePath("backup_order");
        var dir = _testDir;
        var fileName = Path.GetFileNameWithoutExtension(filePath);
        var ext = Path.GetExtension(filePath);

        // First create a valid xlsx file
        var tempXlsx = Path.Combine(dir, "temp_source.xlsx");
        using (var wb = new XLWorkbook())
        {
            var sheet = wb.Worksheets.Add("Deals");
            sheet.Cell(1, 1).Value = "DealId";
            sheet.Cell(1, 2).Value = "AccountName";
            sheet.Cell(1, 3).Value = "DealName";
            sheet.Cell(1, 4).Value = "Stage";
            wb.SaveAs(tempXlsx);
        }

        // Create backup files by copying (backups are just copies with .bak extension)
        var backup1 = Path.Combine(dir, $"{fileName}_20250101_100000{ext}{ExcelDealRepository.BackupExtension}");
        var backup2 = Path.Combine(dir, $"{fileName}_20250102_100000{ext}{ExcelDealRepository.BackupExtension}");
        var backup3 = Path.Combine(dir, $"{fileName}_20250103_100000{ext}{ExcelDealRepository.BackupExtension}");

        File.Copy(tempXlsx, backup1);
        File.Copy(tempXlsx, backup2);
        File.Copy(tempXlsx, backup3);

        var repo = new ExcelDealRepository(filePath, _lookups);
        var backups = repo.GetBackups();

        backups.Should().HaveCount(3);
        // Most recent first
        backups[0].Should().Contain("20250103");
        backups[1].Should().Contain("20250102");
        backups[2].Should().Contain("20250101");
    }

    [Fact]
    public void RestoreFromBackup_RestoresLatestBackup()
    {
        var filePath = GetTestFilePath("restore_test");
        var dir = _testDir;
        var fileName = Path.GetFileNameWithoutExtension(filePath);
        var ext = Path.GetExtension(filePath);

        // First create a valid xlsx file as a source
        var tempXlsx = Path.Combine(dir, "backup_source.xlsx");
        using (var wb = new XLWorkbook())
        {
            var sheet = wb.Worksheets.Add("Deals");
            sheet.Cell(1, 1).Value = "DealId";
            sheet.Cell(1, 2).Value = "AccountName";
            sheet.Cell(1, 3).Value = "DealName";
            sheet.Cell(1, 4).Value = "Stage";
            sheet.Cell(2, 1).Value = "D-BACKUP";
            sheet.Cell(2, 2).Value = "Backup Account";
            sheet.Cell(2, 3).Value = "Backup Deal";
            sheet.Cell(2, 4).Value = "Lead";
            wb.SaveAs(tempXlsx);
        }

        // Copy to backup location
        var backupPath = Path.Combine(dir, $"{fileName}_20250101_100000{ext}{ExcelDealRepository.BackupExtension}");
        File.Copy(tempXlsx, backupPath);

        var repo = new ExcelDealRepository(filePath, _lookups);
        var result = repo.RestoreFromBackup();

        result.Should().BeTrue();
        File.Exists(filePath).Should().BeTrue();

        // Validate restored file
        var validation = ExcelDealRepository.ValidateExcelFile(filePath);
        validation.IsValid.Should().BeTrue();
    }

    [Fact]
    public void RestoreFromBackup_ReturnsFalse_WhenNoBackupsExist()
    {
        var filePath = GetTestFilePath("no_backups");
        var repo = new ExcelDealRepository(filePath, _lookups);

        var result = repo.RestoreFromBackup();

        result.Should().BeFalse();
    }

    #endregion

    #region Validation Instance Method Tests

    [Fact]
    public async Task Validate_ReturnsValidResult_AfterSuccessfulSave()
    {
        var filePath = GetTestFilePath("validate_after_save");
        var repo = new ExcelDealRepository(filePath, _lookups);

        await repo.UpsertAsync(new Ox4D.Core.Models.Deal
        {
            DealId = "D-001",
            AccountName = "Test",
            DealName = "Test Deal",
            Stage = Ox4D.Core.Models.DealStage.Lead
        });
        await repo.SaveChangesAsync();

        var validation = repo.Validate();

        validation.IsValid.Should().BeTrue();
        validation.HasDealsSheet.Should().BeTrue();
        validation.HasLookupsSheet.Should().BeTrue();
        validation.HasMetadataSheet.Should().BeTrue();
        validation.DealCount.Should().Be(1);
    }

    [Fact]
    public void Validate_CountsDealsCorrectly()
    {
        var filePath = GetTestFilePath("count_deals");
        using (var workbook = new XLWorkbook())
        {
            var sheet = workbook.Worksheets.Add("Deals");
            sheet.Cell(1, 1).Value = "DealId";
            sheet.Cell(1, 2).Value = "AccountName";
            sheet.Cell(1, 3).Value = "DealName";
            sheet.Cell(1, 4).Value = "Stage";
            // Add 5 deals
            for (int i = 2; i <= 6; i++)
            {
                sheet.Cell(i, 1).Value = $"D-{i:D3}";
                sheet.Cell(i, 2).Value = $"Account {i}";
                sheet.Cell(i, 3).Value = $"Deal {i}";
                sheet.Cell(i, 4).Value = "Lead";
            }
            workbook.SaveAs(filePath);
        }

        var result = ExcelDealRepository.ValidateExcelFile(filePath);

        result.IsValid.Should().BeTrue();
        result.DealCount.Should().Be(5);
    }

    #endregion
}
