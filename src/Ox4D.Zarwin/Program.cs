// =============================================================================
// Ox4D.Zarwin - MCP (Model Context Protocol) Server
// =============================================================================
// PURPOSE:
//   Zarwin is a local stdio JSON-RPC server that exposes the Sales Pipeline
//   operations as MCP tools. This allows LLMs and AI assistants to interact
//   with the pipeline data through a standardized protocol.
//
// HOW IT WORKS:
//   - Reads JSON-RPC requests from stdin, one per line
//   - Dispatches to appropriate tool handlers (list deals, create deal, etc.)
//   - Returns JSON-RPC responses to stdout
//   - Designed to be spawned as a child process by Claude or other AI hosts
//
// USAGE:
//   dotnet run                                    # Uses default data/SalesPipelineV1.0.xlsx
//   dotnet run -- "path/to/custom.xlsx"          # Use custom Excel file
//
// MCP TOOLS EXPOSED:
//   - pipeline.list_deals, pipeline.get_deal, pipeline.upsert_deal
//   - pipeline.patch_deal, pipeline.delete_deal
//   - pipeline.hygiene_report, pipeline.daily_brief, pipeline.forecast_snapshot
//   - pipeline.generate_synthetic, pipeline.get_stats
// =============================================================================

using Ox4D.Zarwin;

var excelPath = args.Length > 0
    ? args[0]
    : Path.Combine(Directory.GetCurrentDirectory(), "data", "SalesPipelineV1.0.xlsx");

var server = new ZarwinServer(excelPath);

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

await server.RunAsync(Console.In, Console.Out, cts.Token);
