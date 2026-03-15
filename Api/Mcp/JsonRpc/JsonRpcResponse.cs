using System.Text.Json.Serialization;

namespace HinataProject.Api.Mcp.JsonRpc;

public class JsonRpcResponse
{
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; set; } = "2.0";

    [JsonPropertyName("id")]
    public required object Id { get; set; }

    [JsonPropertyName("result")]
    public object? Result { get; set; }

    [JsonPropertyName("error")]
    public JsonRpcError? Error { get; set; }

    public static JsonRpcResponse Success(object id, object result)
        => new() { Id = id, Result = result };

    public static JsonRpcResponse FromError(object id, JsonRpcError error)
        => new() { Id = id, Error = error };
}
