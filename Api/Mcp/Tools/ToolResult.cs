using System.Text.Json;
using System.Text.Json.Serialization;

namespace HinataProject.Api.Mcp.Tools;

public class ToolResult
{
    [JsonPropertyName("content")]
    public required List<ToolContent> Content { get; set; }

    [JsonPropertyName("isError")]
    public bool IsError { get; set; }

    public static ToolResult Success(object data)
        => new() { Content = [new ToolContent { Type = "text", Text = JsonSerializer.Serialize(data) }] };

    public static ToolResult Error(string message)
        => new() { Content = [new ToolContent { Type = "text", Text = message }], IsError = true };
}

public class ToolContent
{
    [JsonPropertyName("type")]
    public required string Type { get; set; }

    [JsonPropertyName("text")]
    public required string Text { get; set; }
}
