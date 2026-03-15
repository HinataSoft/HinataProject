using System.Text.Json.Serialization;

namespace HinataProject.Api.Mcp.JsonRpc;

public class JsonRpcError
{
    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("message")]
    public required string Message { get; set; }

    [JsonPropertyName("data")]
    public object? Data { get; set; }

    // Standard JSON-RPC error codes
    public static readonly JsonRpcError InvalidRequest = new() { Code = -32600, Message = "Invalid Request" };
    public static readonly JsonRpcError MethodNotFound = new() { Code = -32601, Message = "Method not found" };
    public static readonly JsonRpcError InvalidParams = new() { Code = -32602, Message = "Invalid params" };
    public static readonly JsonRpcError InternalError = new() { Code = -32603, Message = "Internal error" };

    // Application error codes
    public static JsonRpcError AppError(int code, string message, object? data = null)
        => new() { Code = code, Message = message, Data = data };

    public static JsonRpcError NodeNotFound => new() { Code = -31998, Message = "Node not found" };
    public static JsonRpcError GeneralAppError => new() { Code = -31999, Message = "General application error" };
    public static JsonRpcError Unauthorized(string reason) => new() { Code = -31997, Message = "Unauthorized", Data = new { reason } };
    public static JsonRpcError InvalidStateTransition => new() { Code = -31996, Message = "Invalid state transition" };
}
