// =============================================================================
// ZarwinServer - Core MCP JSON-RPC Server Implementation
// =============================================================================
// Handles the JSON-RPC message loop and dispatches tool calls to handlers.
// Implements the MCP (Model Context Protocol) specification for AI tool use.
// =============================================================================

using System.Text.Json;
using Ox4D.Core.Models.Config;
using Ox4D.Core.Services;
using Ox4D.Zarwin.Handlers;
using Ox4D.Zarwin.Protocol;
using Ox4D.Store;

namespace Ox4D.Zarwin;

/// <summary>
/// MCP server that exposes Sales Pipeline operations via JSON-RPC over stdio.
/// </summary>
public class ZarwinServer
{
    private readonly ToolHandler _toolHandler;
    private readonly JsonSerializerOptions _jsonOptions;

    /// <summary>
    /// Creates a new Zarwin server instance backed by an Excel workbook.
    /// </summary>
    /// <param name="excelPath">Path to the Excel workbook (sheet-per-table design)</param>
    public ZarwinServer(string excelPath)
    {
        var lookups = LookupTables.CreateDefault();
        var settings = new PipelineSettings();

        // Use Excel as the primary data store (sheet-per-table design)
        var repository = new ExcelDealRepository(excelPath, lookups);
        var pipelineService = new PipelineService(repository, lookups, settings);
        var promoterService = new PromoterService(repository, settings);

        _toolHandler = new ToolHandler(pipelineService, promoterService, repository, lookups, excelPath);

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    /// <summary>
    /// Runs the JSON-RPC message loop, reading from input and writing to output.
    /// </summary>
    public async Task RunAsync(TextReader input, TextWriter output, CancellationToken ct = default)
    {
        while (!ct.IsCancellationRequested)
        {
            var line = await input.ReadLineAsync(ct);
            if (line == null) break;
            if (string.IsNullOrWhiteSpace(line)) continue;

            var response = await ProcessMessageAsync(line);
            await output.WriteLineAsync(response);
            await output.FlushAsync();
        }
    }

    private async Task<string> ProcessMessageAsync(string message)
    {
        JsonRpcRequest? request = null;
        try
        {
            request = JsonSerializer.Deserialize<JsonRpcRequest>(message, _jsonOptions);
        }
        catch (JsonException ex)
        {
            return SerializeResponse(new JsonRpcResponse
            {
                Error = JsonRpcError.ParseError(ex.Message)
            });
        }

        if (request == null)
        {
            return SerializeResponse(new JsonRpcResponse
            {
                Error = JsonRpcError.InvalidRequest("Invalid request format")
            });
        }

        return await HandleRequestAsync(request);
    }

    private async Task<string> HandleRequestAsync(JsonRpcRequest request)
    {
        var response = new JsonRpcResponse { Id = request.Id };

        try
        {
            response.Result = request.Method switch
            {
                "initialize" => HandleInitialize(request.Params),
                "tools/list" => HandleListTools(),
                "tools/call" => await HandleToolCall(request.Params),
                "shutdown" => new { success = true },
                _ when request.Method.StartsWith("pipeline.") => await _toolHandler.HandleAsync(request.Method, request.Params),
                _ when request.Method.StartsWith("promoter.") => await _toolHandler.HandleAsync(request.Method, request.Params),
                _ => throw new InvalidOperationException($"Unknown method: {request.Method}")
            };
        }
        catch (InvalidOperationException ex)
        {
            response.Error = JsonRpcError.ApplicationError(ex.Message);
        }
        catch (Exception ex)
        {
            response.Error = JsonRpcError.InternalError(ex.Message);
        }

        return SerializeResponse(response);
    }

    private object HandleInitialize(JsonElement? parameters)
    {
        return new
        {
            protocolVersion = McpServerInfo.ProtocolVersion,
            capabilities = new
            {
                tools = new { }
            },
            serverInfo = McpServerInfo.GetServerInfo()
        };
    }

    private object HandleListTools()
    {
        return new
        {
            tools = PipelineTools.GetAllTools()
        };
    }

    private async Task<object> HandleToolCall(JsonElement? parameters)
    {
        if (!parameters.HasValue)
            throw new InvalidOperationException("Tool call parameters required");

        var p = parameters.Value;

        if (!p.TryGetProperty("name", out var nameProp))
            throw new InvalidOperationException("Tool name required");

        var toolName = nameProp.GetString();
        if (string.IsNullOrEmpty(toolName))
            throw new InvalidOperationException("Tool name required");

        JsonElement? arguments = null;
        if (p.TryGetProperty("arguments", out var argsProp))
            arguments = argsProp;

        var result = await _toolHandler.HandleAsync(toolName, arguments);

        return new
        {
            content = new[]
            {
                new
                {
                    type = "text",
                    text = JsonSerializer.Serialize(result, _jsonOptions)
                }
            }
        };
    }

    private string SerializeResponse(JsonRpcResponse response)
    {
        return JsonSerializer.Serialize(response, _jsonOptions);
    }
}
