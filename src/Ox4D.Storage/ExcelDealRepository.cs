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
// THREAD SAFETY:
//   Not thread-safe. For concurrent access, use external locking or switch to
//   a database-backed implementation.
// =============================================================================

using ClosedXML.Excel;
using Ox4D.Core.Models;
using Ox4D.Core.Models.Config;
using Ox4D.Core.Services;

namespace Ox4D.Storage;

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
    private List<Deal> _deals = new();
    private bool _loaded = false;
    private bool _isDirty = false;

    public const string DealsSheetName = "Deals";
    public const string LookupsSheetName = "Lookups";
    public const string MetadataSheetName = "Metadata";

    public ExcelDealRepository(string filePath, LookupTables lookups)
    {
        _filePath = filePath;
        _lookups = lookups;
        _normalizer = new DealNormalizer(lookups);
    }

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
            _loaded = true;
            return Task.CompletedTask;
        }

        using var workbook = new XLWorkbook(_filePath);

        // Load deals from Deals sheet
        var dealsSheet = workbook.Worksheets.FirstOrDefault(ws =>
            ws.Name.Equals(DealsSheetName, StringComparison.OrdinalIgnoreCase))
            ?? workbook.Worksheets.FirstOrDefault();

        if (dealsSheet != null)
        {
            _deals = ReadDealsFromSheet(dealsSheet);
        }

        _loaded = true;
        return Task.CompletedTask;
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
                Tags = ParseTags(GetCellValue(worksheetRow, headers, "Tags"))
            };

            deals.Add(_normalizer.Normalize(deal));
        }

        return deals;
    }

    public async Task<IReadOnlyList<Deal>> GetAllAsync(CancellationToken ct = default)
    {
        await EnsureLoadedAsync(ct);
        return _deals.AsReadOnly();
    }

    public async Task<Deal?> GetByIdAsync(string dealId, CancellationToken ct = default)
    {
        await EnsureLoadedAsync(ct);
        return _deals.FirstOrDefault(d => d.DealId.Equals(dealId, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<IReadOnlyList<Deal>> QueryAsync(DealFilter filter, DateTime referenceDate, CancellationToken ct = default)
    {
        await EnsureLoadedAsync(ct);
        return _deals.Where(d => filter.Matches(d, referenceDate)).ToList().AsReadOnly();
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

        var dir = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        using var workbook = new XLWorkbook();

        // Create Deals sheet (main table)
        var dealsSheet = workbook.Worksheets.Add(DealsSheetName);
        WriteDealsSheet(dealsSheet, _deals);

        // Create Lookups sheet (configuration table)
        var lookupsSheet = workbook.Worksheets.Add(LookupsSheetName);
        WriteLookupsSheet(lookupsSheet, _lookups);

        // Create Metadata sheet (version/audit table)
        var metadataSheet = workbook.Worksheets.Add(MetadataSheetName);
        WriteMetadataSheet(metadataSheet);

        workbook.SaveAs(_filePath);
        _isDirty = false;

        return Task.CompletedTask;
    }

    private void WriteDealsSheet(IXLWorksheet sheet, List<Deal> deals)
    {
        var headers = new[]
        {
            "DealId", "OrderNo", "UserId", "AccountName", "ContactName", "Email", "Phone",
            "Postcode", "PostcodeArea", "InstallationLocation", "Region", "MapLink",
            "LeadSource", "ProductLine", "DealName", "Stage", "Probability", "AmountGBP", "WeightedAmountGBP",
            "Owner", "CreatedDate", "LastContactedDate", "NextStep", "NextStepDueDate", "CloseDate",
            "ServicePlan", "LastServiceDate", "NextServiceDueDate", "Comments", "Tags"
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
        sheet.Cell(2, 2).Value = "1.0";
        sheet.Cell(3, 1).Value = "LastModified";
        sheet.Cell(3, 2).Value = DateTime.UtcNow;
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
