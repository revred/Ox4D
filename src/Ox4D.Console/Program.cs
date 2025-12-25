// =============================================================================
// Ox4D.Console - Interactive Sales Pipeline Manager
// =============================================================================
// Purpose: Provides a rich terminal UI for sales managers to manage their
//          pipeline, generate reports, and maintain data quality.
//
// Features:
// - Import/Export Excel workbooks
// - Generate synthetic demo data
// - Daily brief with action items
// - Pipeline hygiene reporting
// - Forecast snapshots by stage/owner/region
// - CRUD operations on deals
//
// Data Storage: Excel workbook with sheets as tables (Supabase-ready design)
// =============================================================================

using Ox4D.Console;
using Ox4D.Core.Models.Config;
using Ox4D.Core.Services;
using Ox4D.Storage;
using Spectre.Console;

var dataDir = Path.Combine(Directory.GetCurrentDirectory(), "data");
var excelPath = Path.Combine(dataDir, "SalesPipelineV1.0.xlsx");

if (!Directory.Exists(dataDir))
    Directory.CreateDirectory(dataDir);

var lookups = LookupTables.CreateDefault();
var settings = new PipelineSettings();
var repository = new ExcelDealRepository(excelPath, lookups);
var pipelineService = new PipelineService(repository, lookups, settings);

var copilot = new CopilotMenu(pipelineService, repository, lookups, settings, excelPath);

AnsiConsole.Write(new FigletText("Ox4D Pipeline").Color(Color.Blue));
AnsiConsole.MarkupLine("[grey]Sales Pipeline Manager v1.0[/]");
AnsiConsole.WriteLine();

await copilot.RunAsync();
