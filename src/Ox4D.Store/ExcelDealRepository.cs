// =============================================================================
// ExcelDealRepository - Excel-Based Storage with Sheet-Per-Table Design
// =============================================================================
// PURPOSE:
//   Provides persistent storage for pipeline data in Excel format. Each worksheet
//   acts as a database table, making future migration to Supabase straightforward.
//
// WORKSHEET STRUCTURE:
//   - Deals: Primary pipeline deals table (row per deal, column per field)
//   - Lookups: Configuration tables (postcode→region, stage→probability)
//   - Metadata: Version info, timestamps, settings
//
// DESIGN FOR SUPABASE MIGRATION:
//   When migrating to Supabase, each sheet maps to a PostgreSQL table:
//   - Deals sheet → deals table
//   - Lookups sheet → lookups table (or split into postcode_regions, stage_probabilities)
//   - Replace this class with SupabaseDealRepository implementing IDealRepository
//
// ATOMIC SAVE STRATEGY:
//   1. Write to temp file
//   2. Validate temp file integrity
//   3. Create backup of existing file
//   4. Replace original with temp file
//
// THREAD SAFETY:
//   Uses file-based locking for concurrent access. For high-concurrency scenarios,
//   switch to a database-backed implementation.
// =============================================================================

using ClosedXML.Excel;
using Ox4D.Core.Models;
using Ox4D.Core.Models.Config;
using Ox4D.Core.Services;

namespace Ox4D.Store;

/// <summary>
/// Result of Excel file validation
/// </summary>
public class ExcelValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public int DealCount { get; set; }
    public bool HasDealsSheet { get; set; }
    public bool HasLookupsSheet { get; set; }
    public bool HasMetadataSheet { get; set; }
    public string? SchemaVersion { get; set; }
    public bool RequiresMigration { get; set; }
}

/// <summary>
/// Excel-based repository that treats each worksheet as a database table.
/// This design mirrors the future Supabase schema where each sheet becomes a table.
///
/// Worksheet structure:
/// - Deals: Main pipeline deals (primary table)
/// - Lookups: Configuration data (postcode→region, stage→probability)
/// - Metadata: Version info, last modified, etc.
/// </summary>
public class ExcelDealRepository : IDealRepository
{
    private readonly string _filePath;
    private readonly LookupTables _lookups;
    private readonly DealNormalizer _normalizer;
    private readonly IClock _clock;
    private readonly object _fileLock = new();
    private readonly int _maxBackups;
    private List<Deal> _deals = new();
    private bool _loaded = false;
    private bool _isDirty = false;
    private string? _loadedSchemaVersion;

    public const string DealsSheetName = "Deals";
    public const string LookupsSheetName = "Lookups";
    public const string MetadataSheetName = "Metadata";
    public const string BackupExtension = ".bak";
    public const string LockFileExtension = ".lock";

    // Schema versioning
    public const string CurrentSchemaVersion = "1.2";
    public static readonly string[] SupportedSchemaVersions = { "1.0", "1.1", "1.2" };

    // Required columns for validation
    private static readonly string[] RequiredDealColumns = new[]
    {
        "DealId", "AccountName", "DealName", "Stage"
    };

    public ExcelDealRepository(string filePath, LookupTables lookups, int maxBackups = 5)
        : this(filePath, lookups, SystemClock.Instance, maxBackups) { }

    public ExcelDealRepository(string filePath, LookupTables lookups, IClock clock, int maxBackups = 5)
    {
        _filePath = filePath;
        _lookups = lookups;
        _clock = clock;
        _normalizer = new DealNormalizer(lookups);
        _maxBackups = maxBackups;
    }

    /// <summary>
    /// Gets the schema version of the currently loaded file
    /// </summary>
    public string? LoadedSchemaVersion => _loadedSchemaVersion;

    /// <summary>
    /// Reloads data from the Excel file, discarding any unsaved changes.
    /// </summary>
    public void Reload()
    {
        _loaded = false;
        _isDirty = false;
        _deals = new List<Deal>();
        LoadAsync().Wait();
    }

    public string FilePath => _filePath;

    private async Task EnsureLoadedAsync(CancellationToken ct)
    {
        if (_loaded) return;
        await LoadAsync(ct);
    }

    public Task LoadAsync(CancellationToken ct = default)
    {
        if (!File.Exists(_filePath))
        {
            _deals = new List<Deal>();
            _loadedSchemaVersion = CurrentSchemaVersion;
            _loaded = true;
            return Task.CompletedTask;
        }

        lock (_fileLock)
        {
            // Validate file integrity before loading
            var validation = ValidateExcelFile(_filePath);
            if (!validation.IsValid)
            {
                // Try to restore from backup if validation fails
                if (RestoreFromBackup())
                {
                    validation = ValidateExcelFile(_filePath);
                    if (!validation.IsValid)
                    {
                        throw new InvalidOperationException(
                            $"Excel file is corrupted and backup restoration failed: {string.Join("; ", validation.Errors)}");
                    }
                }
                else
                {
                    throw new InvalidOperationException(
                        $"Excel file validation failed: {string.Join("; ", validation.Errors)}");
                }
            }

            // Store schema version from validation
            _loadedSchemaVersion = validation.SchemaVersion ?? "1.0";

            // Check for unsupported future versions
            if (!string.IsNullOrEmpty(validation.SchemaVersion) &&
                !SupportedSchemaVersions.Contains(validation.SchemaVersion))
            {
                throw new InvalidOperationException(
                    $"Unsupported schema version: {validation.SchemaVersion}. " +
                    $"This version of Ox4D supports versions: {string.Join(", ", SupportedSchemaVersions)}. " +
                    $"Please update Ox4D to open this file.");
            }

            using var workbook = new XLWorkbook(_filePath);

            // Apply migrations if needed
            if (validation.RequiresMigration)
            {
                MigrateWorkbook(workbook, validation.SchemaVersion ?? "1.0");
            }

            // Load deals from Deals sheet
            var dealsSheet = workbook.Worksheets.FirstOrDefault(ws =>
                ws.Name.Equals(DealsSheetName, StringComparison.OrdinalIgnoreCase))
                ?? workbook.Worksheets.FirstOrDefault();

            if (dealsSheet != null)
            {
                _deals = ReadDealsFromSheet(dealsSheet);
            }

            _loaded = true;

            // If migration was needed, save the updated schema
            if (validation.RequiresMigration)
            {
                _isDirty = true;
            }
        }
        return Task.CompletedTask;
    }

    /// <summary>
    /// Migrates workbook from an older schema version to current
    /// </summary>
    private void MigrateWorkbook(XLWorkbook workbook, string fromVersion)
    {
        // Migration chain: 1.0 → 1.1 → 1.2
        if (fromVersion == "1.0")
        {
            MigrateFrom_1_0_To_1_1(workbook);
            fromVersion = "1.1";
        }

        if (fromVersion == "1.1")
        {
            MigrateFrom_1_1_To_1_2(workbook);
        }

        _loadedSchemaVersion = CurrentSchemaVersion;
    }

    /// <summary>
    /// Migration from schema 1.0 to 1.1
    /// 1.0 had no explicit schema version in metadata
    /// </summary>
    private void MigrateFrom_1_0_To_1_1(XLWorkbook workbook)
    {
        // Ensure Metadata sheet exists with Version property
        var metadataSheet = workbook.Worksheets.FirstOrDefault(ws =>
            ws.Name.Equals(MetadataSheetName, StringComparison.OrdinalIgnoreCase));

        if (metadataSheet == null)
        {
            metadataSheet = workbook.Worksheets.Add(MetadataSheetName);
            metadataSheet.Cell(1, 1).Value = "Property";
            metadataSheet.Cell(1, 2).Value = "Value";
        }

        // Add version row if missing
        var lastRow = metadataSheet.LastRowUsed()?.RowNumber() ?? 1;
        bool hasVersion = false;
        for (int row = 2; row <= lastRow; row++)
        {
            if (metadataSheet.Cell(row, 1).GetString().Equals("Version", StringComparison.OrdinalIgnoreCase))
            {
                hasVersion = true;
                break;
            }
        }

        if (!hasVersion)
        {
            var newRow = lastRow + 1;
            metadataSheet.Cell(newRow, 1).Value = "Version";
            metadataSheet.Cell(newRow, 2).Value = "1.1";
        }
    }

    /// <summary>
    /// Migration from schema 1.1 to 1.2
    /// 1.2 adds SchemaVersion as a distinct property from Version
    /// </summary>
    private void MigrateFrom_1_1_To_1_2(XLWorkbook workbook)
    {
        var metadataSheet = workbook.Worksheets.FirstOrDefault(ws =>
            ws.Name.Equals(MetadataSheetName, StringComparison.OrdinalIgnoreCase));

        if (metadataSheet == null) return;

        // Update version to 1.2
        var lastRow = metadataSheet.LastRowUsed()?.RowNumber() ?? 1;
        for (int row = 2; row <= lastRow; row++)
        {
            if (metadataSheet.Cell(row, 1).GetString().Equals("Version", StringComparison.OrdinalIgnoreCase))
            {
                metadataSheet.Cell(row, 2).Value = "1.2";
                break;
            }
        }
    }

    /// <summary>
    /// Validates the current Excel file without loading it
    /// </summary>
    public ExcelValidationResult Validate()
    {
        return ValidateExcelFile(_filePath);
    }

    private List<Deal> ReadDealsFromSheet(IXLWorksheet sheet)
    {
        var deals = new List<Deal>();
        var headers = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        // Read headers from row 1
        var headerRow = sheet.Row(1);
        var lastCol = sheet.LastColumnUsed()?.ColumnNumber() ?? 0;

        for (int col = 1; col <= lastCol; col++)
        {
            var header = headerRow.Cell(col).GetString().Trim();
            if (!string.IsNullOrEmpty(header))
            {
                var normalized = header.Replace(" ", "").Replace("_", "");
                headers[normalized] = col;
            }
        }

        // Read data rows
        var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 1;
        for (int row = 2; row <= lastRow; row++)
        {
            var worksheetRow = sheet.Row(row);
            if (worksheetRow.IsEmpty()) continue;

            var deal = new Deal
            {
                DealId = GetCellValue(worksheetRow, headers, "DealId", "ID") ?? DealNormalizer.GenerateDealId(),
                OrderNo = GetCellValue(worksheetRow, headers, "OrderNo", "OrderNumber"),
                UserId = GetCellValue(worksheetRow, headers, "UserId"),
                AccountName = GetCellValue(worksheetRow, headers, "AccountName", "Account", "Company") ?? "Unknown",
                ContactName = GetCellValue(worksheetRow, headers, "ContactName", "Contact"),
                Email = GetCellValue(worksheetRow, headers, "Email", "E-mail"),
                Phone = GetCellValue(worksheetRow, headers, "Phone", "Telephone", "Tel"),
                Postcode = GetCellValue(worksheetRow, headers, "Postcode", "PostCode", "Zip"),
                PostcodeArea = GetCellValue(worksheetRow, headers, "PostcodeArea"),
                InstallationLocation = GetCellValue(worksheetRow, headers, "InstallationLocation", "Address"),
                Region = GetCellValue(worksheetRow, headers, "Region"),
                MapLink = GetCellValue(worksheetRow, headers, "MapLink", "Map"),
                LeadSource = GetCellValue(worksheetRow, headers, "LeadSource", "Source"),
                ProductLine = GetCellValue(worksheetRow, headers, "ProductLine", "Product"),
                DealName = GetCellValue(worksheetRow, headers, "DealName", "Deal", "Opportunity") ?? "Unnamed Deal",
                Stage = DealStageExtensions.ParseStage(GetCellValue(worksheetRow, headers, "Stage")),
                Probability = ParseInt(GetCellValue(worksheetRow, headers, "Probability", "Prob")),
                AmountGBP = DealNormalizer.ParseAmount(GetCellValue(worksheetRow, headers, "AmountGBP", "Amount", "Value")),
                Owner = GetCellValue(worksheetRow, headers, "Owner", "SalesRep", "Rep"),
                CreatedDate = DealNormalizer.ParseDate(GetCellValue(worksheetRow, headers, "CreatedDate", "Created")),
                LastContactedDate = DealNormalizer.ParseDate(GetCellValue(worksheetRow, headers, "LastContactedDate", "LastContact")),
                NextStep = GetCellValue(worksheetRow, headers, "NextStep"),
                NextStepDueDate = DealNormalizer.ParseDate(GetCellValue(worksheetRow, headers, "NextStepDueDate", "NextStepDue")),
                CloseDate = DealNormalizer.ParseDate(GetCellValue(worksheetRow, headers, "CloseDate", "ExpectedClose")),
                ServicePlan = GetCellValue(worksheetRow, headers, "ServicePlan"),
                LastServiceDate = DealNormalizer.ParseDate(GetCellValue(worksheetRow, headers, "LastServiceDate")),
                NextServiceDueDate = DealNormalizer.ParseDate(GetCellValue(worksheetRow, headers, "NextServiceDueDate")),
                Comments = GetCellValue(worksheetRow, headers, "Comments", "Notes"),
                Tags = ParseTags(GetCellValue(worksheetRow, headers, "Tags")),
                // Promoter fields
                PromoterId = GetCellValue(worksheetRow, headers, "PromoterId"),
                PromoCode = GetCellValue(worksheetRow, headers, "PromoCode"),
                PromoterCommission = DealNormalizer.ParseAmount(GetCellValue(worksheetRow, headers, "PromoterCommission", "Commission")),
                CommissionPaid = ParseBool(GetCellValue(worksheetRow, headers, "CommissionPaid")),
                CommissionPaidDate = DealNormalizer.ParseDate(GetCellValue(worksheetRow, headers, "CommissionPaidDate"))
            };

            deals.Add(_normalizer.Normalize(deal));
        }

        return deals;
    }

    public async Task<IReadOnlyList<Deal>> GetAllAsync(CancellationToken ct = default)
    {
        await EnsureLoadedAsync(ct);
        // Return clones to prevent external modification of internal state
        return _deals.Select(d => d.Clone()).ToList().AsReadOnly();
    }

    public async Task<Deal?> GetByIdAsync(string dealId, CancellationToken ct = default)
    {
        await EnsureLoadedAsync(ct);
        var deal = _deals.FirstOrDefault(d => d.DealId.Equals(dealId, StringComparison.OrdinalIgnoreCase));
        // Return clone to prevent external modification of internal state
        return deal?.Clone();
    }

    public async Task<IReadOnlyList<Deal>> QueryAsync(DealFilter filter, DateTime referenceDate, CancellationToken ct = default)
    {
        await EnsureLoadedAsync(ct);
        // Return clones to prevent external modification of internal state
        return _deals.Where(d => filter.Matches(d, referenceDate)).Select(d => d.Clone()).ToList().AsReadOnly();
    }

    public async Task UpsertAsync(Deal deal, CancellationToken ct = default)
    {
        await EnsureLoadedAsync(ct);

        var existing = _deals.FindIndex(d => d.DealId.Equals(deal.DealId, StringComparison.OrdinalIgnoreCase));
        if (existing >= 0)
        {
            _deals[existing] = deal;
        }
        else
        {
            _deals.Add(deal);
        }
        _isDirty = true;
    }

    public async Task UpsertManyAsync(IEnumerable<Deal> deals, CancellationToken ct = default)
    {
        foreach (var deal in deals)
        {
            await UpsertAsync(deal, ct);
        }
    }

    public async Task DeleteAsync(string dealId, CancellationToken ct = default)
    {
        await EnsureLoadedAsync(ct);
        _deals.RemoveAll(d => d.DealId.Equals(dealId, StringComparison.OrdinalIgnoreCase));
        _isDirty = true;
    }

    public Task SaveChangesAsync(CancellationToken ct = default)
    {
        if (!_isDirty && File.Exists(_filePath)) return Task.CompletedTask;

        lock (_fileLock)
        {
            var dir = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            // Cross-process lock using lock file
            var lockFilePath = _filePath + LockFileExtension;
            using (var lockFile = AcquireCrossProcessLock(lockFilePath))
            {
                // Step 1: Write to temp file with unique name (avoid collisions between processes)
                var uniqueId = Guid.NewGuid().ToString("N")[..8];
                var tempFile = Path.Combine(
                    Path.GetDirectoryName(_filePath) ?? ".",
                    $"~${Path.GetFileName(_filePath)}.{uniqueId}.tmp.xlsx");
                try
                {
                    using (var workbook = new XLWorkbook())
                    {
                        // Create Deals sheet (main table)
                        var dealsSheet = workbook.Worksheets.Add(DealsSheetName);
                        WriteDealsSheet(dealsSheet, _deals);

                        // Create Lookups sheet (configuration table)
                        var lookupsSheet = workbook.Worksheets.Add(LookupsSheetName);
                        WriteLookupsSheet(lookupsSheet, _lookups);

                        // Create Metadata sheet (version/audit table)
                        var metadataSheet = workbook.Worksheets.Add(MetadataSheetName);
                        WriteMetadataSheet(metadataSheet);

                        workbook.SaveAs(tempFile);
                    }

                    // Step 2: Validate temp file integrity
                    var validation = ValidateExcelFile(tempFile);
                    if (!validation.IsValid)
                    {
                        throw new InvalidOperationException(
                            $"Validation failed for temp file: {string.Join("; ", validation.Errors)}");
                    }

                    // Step 3: Create backup of existing file
                    if (File.Exists(_filePath))
                    {
                        CreateBackup();
                    }

                    // Step 4: Replace original with temp file (atomic on most filesystems)
                    File.Move(tempFile, _filePath, overwrite: true);
                    _isDirty = false;
                }
                catch
                {
                    // Clean up temp file on failure
                    if (File.Exists(tempFile))
                    {
                        try { File.Delete(tempFile); } catch { /* ignore cleanup errors */ }
                    }
                    throw;
                }
            }
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Acquires a cross-process lock using a lock file with retry logic
    /// </summary>
    private FileStream AcquireCrossProcessLock(string lockFilePath, int maxRetries = 10, int retryDelayMs = 100)
    {
        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            try
            {
                // FileShare.None ensures exclusive access across processes
                return new FileStream(
                    lockFilePath,
                    FileMode.OpenOrCreate,
                    FileAccess.ReadWrite,
                    FileShare.None,
                    bufferSize: 1,
                    FileOptions.DeleteOnClose);
            }
            catch (IOException) when (attempt < maxRetries - 1)
            {
                // Another process has the lock - wait and retry
                Thread.Sleep(retryDelayMs * (attempt + 1)); // Exponential backoff
            }
        }

        throw new InvalidOperationException(
            $"Could not acquire file lock after {maxRetries} attempts. Another process may be writing to the file.");
    }

    /// <summary>
    /// Validates an Excel file for structural integrity
    /// </summary>
    public static ExcelValidationResult ValidateExcelFile(string filePath)
    {
        var result = new ExcelValidationResult();

        if (!File.Exists(filePath))
        {
            result.Errors.Add($"File does not exist: {filePath}");
            return result;
        }

        try
        {
            using var workbook = new XLWorkbook(filePath);

            // Check for required sheets
            result.HasDealsSheet = workbook.Worksheets.Any(ws =>
                ws.Name.Equals(DealsSheetName, StringComparison.OrdinalIgnoreCase));
            result.HasLookupsSheet = workbook.Worksheets.Any(ws =>
                ws.Name.Equals(LookupsSheetName, StringComparison.OrdinalIgnoreCase));
            result.HasMetadataSheet = workbook.Worksheets.Any(ws =>
                ws.Name.Equals(MetadataSheetName, StringComparison.OrdinalIgnoreCase));

            if (!result.HasDealsSheet)
            {
                result.Errors.Add($"Missing required sheet: {DealsSheetName}");
            }
            else
            {
                // Validate Deals sheet structure
                var dealsSheet = workbook.Worksheets.First(ws =>
                    ws.Name.Equals(DealsSheetName, StringComparison.OrdinalIgnoreCase));

                var headers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var lastCol = dealsSheet.LastColumnUsed()?.ColumnNumber() ?? 0;

                for (int col = 1; col <= lastCol; col++)
                {
                    var header = dealsSheet.Cell(1, col).GetString().Trim();
                    if (!string.IsNullOrEmpty(header))
                    {
                        headers.Add(header.Replace(" ", "").Replace("_", ""));
                    }
                }

                // Check for required columns
                foreach (var required in RequiredDealColumns)
                {
                    var normalized = required.Replace(" ", "").Replace("_", "");
                    if (!headers.Contains(normalized))
                    {
                        result.Errors.Add($"Missing required column: {required}");
                    }
                }

                // Count deals
                var lastRow = dealsSheet.LastRowUsed()?.RowNumber() ?? 1;
                result.DealCount = Math.Max(0, lastRow - 1);
            }

            if (!result.HasLookupsSheet)
            {
                result.Warnings.Add($"Missing optional sheet: {LookupsSheetName}");
            }

            if (!result.HasMetadataSheet)
            {
                result.Warnings.Add($"Missing optional sheet: {MetadataSheetName}");
                result.SchemaVersion = "1.0"; // No metadata = legacy 1.0 file
                result.RequiresMigration = true;
            }
            else
            {
                // Read schema version from Metadata sheet
                var metadataSheet = workbook.Worksheets.First(ws =>
                    ws.Name.Equals(MetadataSheetName, StringComparison.OrdinalIgnoreCase));

                var lastRow = metadataSheet.LastRowUsed()?.RowNumber() ?? 1;
                for (int row = 2; row <= lastRow; row++)
                {
                    var propName = metadataSheet.Cell(row, 1).GetString().Trim();
                    if (propName.Equals("Version", StringComparison.OrdinalIgnoreCase))
                    {
                        result.SchemaVersion = metadataSheet.Cell(row, 2).GetString().Trim();
                        break;
                    }
                }

                // Default to 1.0 if no version found
                if (string.IsNullOrEmpty(result.SchemaVersion))
                {
                    result.SchemaVersion = "1.0";
                    result.RequiresMigration = true;
                }
                else if (result.SchemaVersion != CurrentSchemaVersion &&
                         SupportedSchemaVersions.Contains(result.SchemaVersion))
                {
                    // Older supported version - needs migration
                    result.RequiresMigration = true;
                }
            }

            result.IsValid = result.Errors.Count == 0;
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Failed to read Excel file: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// Creates a timestamped backup of the current file
    /// </summary>
    private void CreateBackup()
    {
        if (!File.Exists(_filePath)) return;

        var dir = Path.GetDirectoryName(_filePath) ?? ".";
        var fileName = Path.GetFileNameWithoutExtension(_filePath);
        var ext = Path.GetExtension(_filePath);

        // Create backup with timestamp (using injected clock for testability)
        var timestamp = _clock.Now.ToString("yyyyMMdd_HHmmss");
        var backupPath = Path.Combine(dir, $"{fileName}_{timestamp}{ext}{BackupExtension}");
        File.Copy(_filePath, backupPath, overwrite: true);

        // Rotate backups - keep only the most recent N
        RotateBackups(dir, fileName, ext);
    }

    /// <summary>
    /// Removes old backups keeping only the most recent ones
    /// </summary>
    private void RotateBackups(string dir, string fileName, string ext)
    {
        var pattern = $"{fileName}_*{ext}{BackupExtension}";
        var backups = Directory.GetFiles(dir, pattern)
            .OrderByDescending(f => f)
            .Skip(_maxBackups)
            .ToList();

        foreach (var oldBackup in backups)
        {
            try { File.Delete(oldBackup); } catch { /* ignore deletion errors */ }
        }
    }

    /// <summary>
    /// Restores from the most recent backup
    /// </summary>
    public bool RestoreFromBackup()
    {
        var dir = Path.GetDirectoryName(_filePath) ?? ".";
        var fileName = Path.GetFileNameWithoutExtension(_filePath);
        var ext = Path.GetExtension(_filePath);

        var pattern = $"{fileName}_*{ext}{BackupExtension}";
        var latestBackup = Directory.GetFiles(dir, pattern)
            .OrderByDescending(f => f)
            .FirstOrDefault();

        if (latestBackup == null) return false;

        lock (_fileLock)
        {
            File.Copy(latestBackup, _filePath, overwrite: true);
            _loaded = false;
            _isDirty = false;
        }

        return true;
    }

    /// <summary>
    /// Gets a list of available backup files
    /// </summary>
    public IReadOnlyList<string> GetBackups()
    {
        var dir = Path.GetDirectoryName(_filePath) ?? ".";
        var fileName = Path.GetFileNameWithoutExtension(_filePath);
        var ext = Path.GetExtension(_filePath);

        var pattern = $"{fileName}_*{ext}{BackupExtension}";
        return Directory.GetFiles(dir, pattern)
            .OrderByDescending(f => f)
            .ToList()
            .AsReadOnly();
    }

    private void WriteDealsSheet(IXLWorksheet sheet, List<Deal> deals)
    {
        var headers = new[]
        {
            "DealId", "OrderNo", "UserId", "AccountName", "ContactName", "Email", "Phone",
            "Postcode", "PostcodeArea", "InstallationLocation", "Region", "MapLink",
            "LeadSource", "ProductLine", "DealName", "Stage", "Probability", "AmountGBP", "WeightedAmountGBP",
            "Owner", "CreatedDate", "LastContactedDate", "NextStep", "NextStepDueDate", "CloseDate",
            "ServicePlan", "LastServiceDate", "NextServiceDueDate", "Comments", "Tags",
            // Promoter columns
            "PromoterId", "PromoCode", "PromoterCommission", "CommissionPaid", "CommissionPaidDate"
        };

        // Write headers with formatting
        for (int i = 0; i < headers.Length; i++)
        {
            var cell = sheet.Cell(1, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.LightSteelBlue;
        }

        // Write data rows
        for (int row = 0; row < deals.Count; row++)
        {
            var deal = deals[row];
            var r = row + 2;

            sheet.Cell(r, 1).Value = deal.DealId;
            sheet.Cell(r, 2).Value = deal.OrderNo ?? "";
            sheet.Cell(r, 3).Value = deal.UserId ?? "";
            sheet.Cell(r, 4).Value = deal.AccountName;
            sheet.Cell(r, 5).Value = deal.ContactName ?? "";
            sheet.Cell(r, 6).Value = deal.Email ?? "";
            sheet.Cell(r, 7).Value = deal.Phone ?? "";
            sheet.Cell(r, 8).Value = deal.Postcode ?? "";
            sheet.Cell(r, 9).Value = deal.PostcodeArea ?? "";
            sheet.Cell(r, 10).Value = deal.InstallationLocation ?? "";
            sheet.Cell(r, 11).Value = deal.Region ?? "";
            sheet.Cell(r, 12).Value = deal.MapLink ?? "";
            sheet.Cell(r, 13).Value = deal.LeadSource ?? "";
            sheet.Cell(r, 14).Value = deal.ProductLine ?? "";
            sheet.Cell(r, 15).Value = deal.DealName;
            sheet.Cell(r, 16).Value = deal.Stage.ToDisplayString();
            sheet.Cell(r, 17).Value = deal.Probability;
            sheet.Cell(r, 18).Value = deal.AmountGBP ?? 0;
            sheet.Cell(r, 19).Value = deal.WeightedAmountGBP ?? 0;
            sheet.Cell(r, 20).Value = deal.Owner ?? "";
            if (deal.CreatedDate.HasValue) sheet.Cell(r, 21).Value = deal.CreatedDate.Value;
            if (deal.LastContactedDate.HasValue) sheet.Cell(r, 22).Value = deal.LastContactedDate.Value;
            sheet.Cell(r, 23).Value = deal.NextStep ?? "";
            if (deal.NextStepDueDate.HasValue) sheet.Cell(r, 24).Value = deal.NextStepDueDate.Value;
            if (deal.CloseDate.HasValue) sheet.Cell(r, 25).Value = deal.CloseDate.Value;
            sheet.Cell(r, 26).Value = deal.ServicePlan ?? "";
            if (deal.LastServiceDate.HasValue) sheet.Cell(r, 27).Value = deal.LastServiceDate.Value;
            if (deal.NextServiceDueDate.HasValue) sheet.Cell(r, 28).Value = deal.NextServiceDueDate.Value;
            sheet.Cell(r, 29).Value = deal.Comments ?? "";
            sheet.Cell(r, 30).Value = string.Join(", ", deal.Tags);
            // Promoter fields
            sheet.Cell(r, 31).Value = deal.PromoterId ?? "";
            sheet.Cell(r, 32).Value = deal.PromoCode ?? "";
            sheet.Cell(r, 33).Value = deal.PromoterCommission ?? 0;
            sheet.Cell(r, 34).Value = deal.CommissionPaid ? "Yes" : "No";
            if (deal.CommissionPaidDate.HasValue) sheet.Cell(r, 35).Value = deal.CommissionPaidDate.Value;
        }

        // Format as table with auto-filter
        if (deals.Any())
        {
            var tableRange = sheet.Range(1, 1, deals.Count + 1, headers.Length);
            tableRange.SetAutoFilter();
        }

        sheet.Columns().AdjustToContents();
    }

    private void WriteLookupsSheet(IXLWorksheet sheet, LookupTables lookups)
    {
        // Postcode to Region mapping (Table: PostcodeRegions)
        sheet.Cell(1, 1).Value = "PostcodeArea";
        sheet.Cell(1, 2).Value = "Region";
        sheet.Cell(1, 1).Style.Font.Bold = true;
        sheet.Cell(1, 2).Style.Font.Bold = true;
        sheet.Cell(1, 1).Style.Fill.BackgroundColor = XLColor.LightSteelBlue;
        sheet.Cell(1, 2).Style.Fill.BackgroundColor = XLColor.LightSteelBlue;

        var postcodeRows = lookups.PostcodeToRegion.OrderBy(kvp => kvp.Key).ToList();
        for (int i = 0; i < postcodeRows.Count; i++)
        {
            sheet.Cell(i + 2, 1).Value = postcodeRows[i].Key;
            sheet.Cell(i + 2, 2).Value = postcodeRows[i].Value;
        }

        // Stage to Probability mapping (Table: StageProbabilities)
        sheet.Cell(1, 4).Value = "Stage";
        sheet.Cell(1, 5).Value = "DefaultProbability";
        sheet.Cell(1, 4).Style.Font.Bold = true;
        sheet.Cell(1, 5).Style.Font.Bold = true;
        sheet.Cell(1, 4).Style.Fill.BackgroundColor = XLColor.LightSteelBlue;
        sheet.Cell(1, 5).Style.Fill.BackgroundColor = XLColor.LightSteelBlue;

        var stageRows = lookups.StageProbabilities.OrderBy(kvp => kvp.Key).ToList();
        for (int i = 0; i < stageRows.Count; i++)
        {
            sheet.Cell(i + 2, 4).Value = stageRows[i].Key.ToDisplayString();
            sheet.Cell(i + 2, 5).Value = stageRows[i].Value;
        }

        sheet.Columns().AdjustToContents();
    }

    private void WriteMetadataSheet(IXLWorksheet sheet)
    {
        sheet.Cell(1, 1).Value = "Property";
        sheet.Cell(1, 2).Value = "Value";
        sheet.Cell(1, 1).Style.Font.Bold = true;
        sheet.Cell(1, 2).Style.Font.Bold = true;
        sheet.Cell(1, 1).Style.Fill.BackgroundColor = XLColor.LightSteelBlue;
        sheet.Cell(1, 2).Style.Fill.BackgroundColor = XLColor.LightSteelBlue;

        sheet.Cell(2, 1).Value = "Version";
        sheet.Cell(2, 2).Value = CurrentSchemaVersion;
        sheet.Cell(3, 1).Value = "LastModified";
        sheet.Cell(3, 2).Value = _clock.UtcNow;
        sheet.Cell(4, 1).Value = "DealCount";
        sheet.Cell(4, 2).Value = _deals.Count;
        sheet.Cell(5, 1).Value = "GeneratedBy";
        sheet.Cell(5, 2).Value = "Ox4D Sales Pipeline Manager";

        sheet.Columns().AdjustToContents();
    }

    private string? GetCellValue(IXLRow row, Dictionary<string, int> headers, params string[] possibleNames)
    {
        foreach (var name in possibleNames)
        {
            var normalized = name.Replace(" ", "").Replace("_", "");
            if (headers.TryGetValue(normalized, out var col))
            {
                var value = row.Cell(col).GetString().Trim();
                if (!string.IsNullOrEmpty(value)) return value;
            }
        }
        return null;
    }

    private int ParseInt(string? value) =>
        int.TryParse(value?.Replace("%", "").Trim(), out var result) ? result : 0;

    private bool ParseBool(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return false;
        return value.Trim().Equals("Yes", StringComparison.OrdinalIgnoreCase)
            || value.Trim().Equals("True", StringComparison.OrdinalIgnoreCase)
            || value.Trim() == "1";
    }

    private List<string> ParseTags(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return new List<string>();
        return value.Split(new[] { ',', ';', '|' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(t => t.Trim())
                    .Where(t => !string.IsNullOrEmpty(t))
                    .ToList();
    }

    /// <summary>
    /// Import lookups from an existing Excel file's Lookups sheet
    /// </summary>
    public LookupTables ImportLookupsFromExcel()
    {
        var lookups = LookupTables.CreateDefault();

        if (!File.Exists(_filePath)) return lookups;

        using var workbook = new XLWorkbook(_filePath);
        var lookupsSheet = workbook.Worksheets.FirstOrDefault(ws =>
            ws.Name.Equals(LookupsSheetName, StringComparison.OrdinalIgnoreCase));

        if (lookupsSheet == null) return lookups;

        var lastRow = lookupsSheet.LastRowUsed()?.RowNumber() ?? 1;

        // Read postcode mappings (columns A:B)
        for (int row = 2; row <= lastRow; row++)
        {
            var postcode = lookupsSheet.Cell(row, 1).GetString().Trim();
            var region = lookupsSheet.Cell(row, 2).GetString().Trim();
            if (!string.IsNullOrEmpty(postcode) && !string.IsNullOrEmpty(region))
            {
                lookups.PostcodeToRegion[postcode] = region;
            }
        }

        // Read stage probabilities (columns D:E)
        for (int row = 2; row <= lastRow; row++)
        {
            var stageName = lookupsSheet.Cell(row, 4).GetString().Trim();
            var probStr = lookupsSheet.Cell(row, 5).GetString().Trim();

            if (!string.IsNullOrEmpty(stageName) && int.TryParse(probStr, out var prob))
            {
                var stage = DealStageExtensions.ParseStage(stageName);
                lookups.StageProbabilities[stage] = prob;
            }
        }

        return lookups;
    }
}
