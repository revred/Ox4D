using System.Text.Json.Serialization;

namespace Ox4D.Zarwin.Protocol;

public class ToolDefinition
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("inputSchema")]
    public ToolInputSchema InputSchema { get; set; } = new();
}

public class ToolInputSchema
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "object";

    [JsonPropertyName("properties")]
    public Dictionary<string, ToolProperty> Properties { get; set; } = new();

    [JsonPropertyName("required")]
    public List<string> Required { get; set; } = new();
}

public class ToolProperty
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "string";

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("enum")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? Enum { get; set; }

    [JsonPropertyName("items")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ToolProperty? Items { get; set; }
}

public static class PipelineTools
{
    public static List<ToolDefinition> GetAllTools() => new()
    {
        new ToolDefinition
        {
            Name = "pipeline.list_deals",
            Description = "List all deals in the pipeline, optionally filtered by various criteria",
            InputSchema = new ToolInputSchema
            {
                Properties = new Dictionary<string, ToolProperty>
                {
                    ["searchText"] = new() { Type = "string", Description = "Free-text search across deal name, account, contact, and owner" },
                    ["stages"] = new() { Type = "array", Description = "Filter by stages", Items = new() { Type = "string", Enum = new List<string> { "Lead", "Qualified", "Discovery", "Proposal", "Negotiation", "Closed Won", "Closed Lost", "On Hold" } } },
                    ["owner"] = new() { Type = "string", Description = "Filter by deal owner" },
                    ["region"] = new() { Type = "string", Description = "Filter by region" },
                    ["productLine"] = new() { Type = "string", Description = "Filter by product line" },
                    ["minAmount"] = new() { Type = "number", Description = "Minimum deal amount" },
                    ["maxAmount"] = new() { Type = "number", Description = "Maximum deal amount" }
                }
            }
        },
        new ToolDefinition
        {
            Name = "pipeline.get_deal",
            Description = "Get a single deal by its ID",
            InputSchema = new ToolInputSchema
            {
                Properties = new Dictionary<string, ToolProperty>
                {
                    ["dealId"] = new() { Type = "string", Description = "The deal ID to retrieve" }
                },
                Required = new List<string> { "dealId" }
            }
        },
        new ToolDefinition
        {
            Name = "pipeline.upsert_deal",
            Description = "Create or update a deal in the pipeline",
            InputSchema = new ToolInputSchema
            {
                Properties = new Dictionary<string, ToolProperty>
                {
                    ["dealId"] = new() { Type = "string", Description = "Deal ID (auto-generated if not provided)" },
                    ["accountName"] = new() { Type = "string", Description = "Account/company name" },
                    ["dealName"] = new() { Type = "string", Description = "Name of the deal/opportunity" },
                    ["contactName"] = new() { Type = "string", Description = "Primary contact name" },
                    ["email"] = new() { Type = "string", Description = "Contact email" },
                    ["phone"] = new() { Type = "string", Description = "Contact phone" },
                    ["postcode"] = new() { Type = "string", Description = "Postcode" },
                    ["stage"] = new() { Type = "string", Description = "Deal stage", Enum = new List<string> { "Lead", "Qualified", "Discovery", "Proposal", "Negotiation", "Closed Won", "Closed Lost", "On Hold" } },
                    ["probability"] = new() { Type = "integer", Description = "Win probability (0-100)" },
                    ["amountGBP"] = new() { Type = "number", Description = "Deal value in GBP" },
                    ["owner"] = new() { Type = "string", Description = "Deal owner/sales rep" },
                    ["nextStep"] = new() { Type = "string", Description = "Next action step" },
                    ["nextStepDueDate"] = new() { Type = "string", Description = "Due date for next step (ISO format)" },
                    ["closeDate"] = new() { Type = "string", Description = "Expected close date (ISO format)" },
                    ["productLine"] = new() { Type = "string", Description = "Product line" },
                    ["leadSource"] = new() { Type = "string", Description = "Lead source" },
                    ["comments"] = new() { Type = "string", Description = "Notes/comments" },
                    ["tags"] = new() { Type = "array", Description = "Tags", Items = new() { Type = "string" } }
                },
                Required = new List<string> { "accountName", "dealName" }
            }
        },
        new ToolDefinition
        {
            Name = "pipeline.patch_deal",
            Description = "Update specific fields of an existing deal",
            InputSchema = new ToolInputSchema
            {
                Properties = new Dictionary<string, ToolProperty>
                {
                    ["dealId"] = new() { Type = "string", Description = "The deal ID to update" },
                    ["updates"] = new() { Type = "object", Description = "Key-value pairs of fields to update" }
                },
                Required = new List<string> { "dealId", "updates" }
            }
        },
        new ToolDefinition
        {
            Name = "pipeline.delete_deal",
            Description = "Delete a deal from the pipeline",
            InputSchema = new ToolInputSchema
            {
                Properties = new Dictionary<string, ToolProperty>
                {
                    ["dealId"] = new() { Type = "string", Description = "The deal ID to delete" }
                },
                Required = new List<string> { "dealId" }
            }
        },
        new ToolDefinition
        {
            Name = "pipeline.hygiene_report",
            Description = "Generate a data hygiene report showing deals with missing or inconsistent data",
            InputSchema = new ToolInputSchema()
        },
        new ToolDefinition
        {
            Name = "pipeline.daily_brief",
            Description = "Generate a daily brief showing actions due today, overdue items, and deals at risk",
            InputSchema = new ToolInputSchema
            {
                Properties = new Dictionary<string, ToolProperty>
                {
                    ["referenceDate"] = new() { Type = "string", Description = "Reference date for the brief (ISO format, defaults to today)" }
                }
            }
        },
        new ToolDefinition
        {
            Name = "pipeline.forecast_snapshot",
            Description = "Generate a forecast snapshot with pipeline breakdown by stage, owner, region, and close month",
            InputSchema = new ToolInputSchema
            {
                Properties = new Dictionary<string, ToolProperty>
                {
                    ["referenceDate"] = new() { Type = "string", Description = "Reference date for the forecast (ISO format, defaults to today)" }
                }
            }
        },
        new ToolDefinition
        {
            Name = "pipeline.generate_synthetic",
            Description = "Generate synthetic demo data for testing",
            InputSchema = new ToolInputSchema
            {
                Properties = new Dictionary<string, ToolProperty>
                {
                    ["count"] = new() { Type = "integer", Description = "Number of deals to generate (default: 100)" },
                    ["seed"] = new() { Type = "integer", Description = "Random seed for reproducibility" }
                }
            }
        },
        new ToolDefinition
        {
            Name = "pipeline.import_excel",
            Description = "Import deals from an Excel file",
            InputSchema = new ToolInputSchema
            {
                Properties = new Dictionary<string, ToolProperty>
                {
                    ["path"] = new() { Type = "string", Description = "Path to the Excel file" }
                },
                Required = new List<string> { "path" }
            }
        },
        new ToolDefinition
        {
            Name = "pipeline.export_excel",
            Description = "Export deals to an Excel file",
            InputSchema = new ToolInputSchema
            {
                Properties = new Dictionary<string, ToolProperty>
                {
                    ["path"] = new() { Type = "string", Description = "Path for the output Excel file" }
                },
                Required = new List<string> { "path" }
            }
        },
        new ToolDefinition
        {
            Name = "pipeline.get_stats",
            Description = "Get summary statistics for the pipeline",
            InputSchema = new ToolInputSchema()
        }
    };
}
