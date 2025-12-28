using Ox4D.Core.Models.Config;
using Ox4D.Store;

namespace Ox4D.Tests;

/// <summary>
/// Contract tests for ExcelDealRepository.
/// Ensures ExcelDealRepository adheres to the IDealRepository contract.
/// Uses temporary files for isolation between tests.
/// </summary>
public class ExcelRepositoryContractTests : RepositoryContractTests<ExcelDealRepository>
{
    private string? _tempFilePath;

    protected override ExcelDealRepository CreateRepository()
    {
        // Create a unique temporary file path for each test
        _tempFilePath = Path.Combine(Path.GetTempPath(), $"Ox4D_ContractTest_{Guid.NewGuid()}.xlsx");
        var lookups = LookupTables.CreateDefault();
        return new ExcelDealRepository(_tempFilePath, lookups);
    }

    protected override async Task CleanupRepository(ExcelDealRepository repository)
    {
        // Clean up the temporary file after each test
        if (_tempFilePath != null && File.Exists(_tempFilePath))
        {
            try
            {
                File.Delete(_tempFilePath);
            }
            catch
            {
                // Ignore cleanup errors in tests
            }
        }

        // Also clean up any backup files
        if (_tempFilePath != null)
        {
            var dir = Path.GetDirectoryName(_tempFilePath) ?? ".";
            var fileName = Path.GetFileNameWithoutExtension(_tempFilePath);
            var pattern = $"{fileName}_*{ExcelDealRepository.BackupExtension}";

            foreach (var backup in Directory.GetFiles(dir, pattern))
            {
                try { File.Delete(backup); } catch { }
            }
        }

        await Task.CompletedTask;
    }
}
