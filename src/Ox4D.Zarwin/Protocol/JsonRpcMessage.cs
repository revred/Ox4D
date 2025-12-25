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

public class JsonRpcError
{
    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Data { get; set; }

    public static JsonRpcError ParseError(string message) => new() { Code = -32700, Message = message };
    public static JsonRpcError InvalidRequest(string message) => new() { Code = -32600, Message = message };
    public static JsonRpcError MethodNotFound(string method) => new() { Code = -32601, Message = $"Method not found: {method}" };
    public static JsonRpcError InvalidParams(string message) => new() { Code = -32602, Message = message };
    public static JsonRpcError InternalError(string message) => new() { Code = -32603, Message = message };
    public static JsonRpcError ApplicationError(string message) => new() { Code = -32000, Message = message };
}
