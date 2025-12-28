using System.Text.Json;
using System.Text.Json.Serialization;

namespace Ox4D.Zarwin.Protocol;

public class JsonRpcRequest
{
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; set; } = "2.0";

    [JsonPropertyName("id")]
    public JsonElement? Id { get; set; }

    [JsonPropertyName("method")]
    public string Method { get; set; } = string.Empty;

    [JsonPropertyName("params")]
    public JsonElement? Params { get; set; }
}

public class JsonRpcResponse
{
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; set; } = "2.0";

    [JsonPropertyName("id")]
    public JsonElement? Id { get; set; }

    [JsonPropertyName("result")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Result { get; set; }

    [JsonPropertyName("error")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public JsonRpcError? Error { get; set; }
}

/// <summary>
/// Standard JSON-RPC error with extended envelope for structured error data
/// </summary>
public class JsonRpcError
{
    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ErrorData? Data { get; set; }

    // Standard JSON-RPC error codes
    public static JsonRpcError ParseError(string message) =>
        new() { Code = -32700, Message = message, Data = ErrorData.Create("PARSE_ERROR", message) };

    public static JsonRpcError InvalidRequest(string message) =>
        new() { Code = -32600, Message = message, Data = ErrorData.Create("INVALID_REQUEST", message) };

    public static JsonRpcError MethodNotFound(string method) =>
        new() { Code = -32601, Message = $"Method not found: {method}", Data = ErrorData.Create("METHOD_NOT_FOUND", $"Method '{method}' is not available", method) };

    public static JsonRpcError InvalidParams(string message) =>
        new() { Code = -32602, Message = message, Data = ErrorData.Create("INVALID_PARAMS", message) };

    public static JsonRpcError InternalError(string message) =>
        new() { Code = -32603, Message = message, Data = ErrorData.Create("INTERNAL_ERROR", message) };

    // Application-level error codes (-32000 to -32099)
    public static JsonRpcError ApplicationError(string message) =>
        new() { Code = -32000, Message = message, Data = ErrorData.Create("APPLICATION_ERROR", message) };

    public static JsonRpcError NotFound(string resource, string? id = null) =>
        new() { Code = -32001, Message = $"{resource} not found" + (id != null ? $": {id}" : ""), Data = ErrorData.Create("NOT_FOUND", $"{resource} not found", id) };

    public static JsonRpcError ValidationError(string message, IEnumerable<string>? details = null) =>
        new() { Code = -32002, Message = message, Data = ErrorData.Create("VALIDATION_ERROR", message, details: details?.ToList()) };

    public static JsonRpcError DataIntegrityError(string message) =>
        new() { Code = -32003, Message = message, Data = ErrorData.Create("DATA_INTEGRITY_ERROR", message) };

    public static JsonRpcError OperationFailed(string operation, string reason) =>
        new() { Code = -32004, Message = $"{operation} failed: {reason}", Data = ErrorData.Create("OPERATION_FAILED", reason, operation) };
}

/// <summary>
/// Structured error data envelope for machine-readable error information
/// </summary>
public class ErrorData
{
    [JsonPropertyName("errorType")]
    public string ErrorType { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("identifier")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Identifier { get; set; }

    [JsonPropertyName("details")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? Details { get; set; }

    [JsonPropertyName("timestamp")]
    public string Timestamp { get; set; } = DateTime.UtcNow.ToString("o");

    [JsonPropertyName("serverVersion")]
    public string ServerVersion { get; set; } = McpServerInfo.Version;

    public static ErrorData Create(string errorType, string description, string? identifier = null, List<string>? details = null) =>
        new()
        {
            ErrorType = errorType,
            Description = description,
            Identifier = identifier,
            Details = details
        };
}
