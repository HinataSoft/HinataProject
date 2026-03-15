using System.Text.Json.Serialization;

namespace HinataProject.Api.Mcp.JsonRpc;

public class JsonRpcRequest
{
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; set; } = "2.0";

    [JsonPropertyName("method")]
    public required string Method { get; set; }

    [JsonPropertyName("id")]
    public object Id { get; set; } = null!;

    [JsonPropertyName("params")]
    public object? Params { get; set; }
}
