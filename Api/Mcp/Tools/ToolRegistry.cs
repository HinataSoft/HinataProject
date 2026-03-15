using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HinataProject.Api.Mcp.Tools;

public class ToolRegistry
{
    private readonly ConcurrentDictionary<string, IToolHandler> _tools = new();

    public void Register(IToolHandler tool)
    {
        if (string.IsNullOrEmpty(tool.Name))
            throw new ArgumentException("Tool name cannot be null or empty", nameof(tool));

        if (!_tools.TryAdd(tool.Name, tool))
            throw new InvalidOperationException($"Tool '{tool.Name}' is already registered");
    }

    public IToolHandler? Get(string name) => _tools.GetValueOrDefault(name);

    public IReadOnlyList<RegisteredTool> GetAll()
    {
        return _tools.Values.Select(t => new RegisteredTool
        {
            Name = t.Name,
            Description = t.Description,
            InputSchema = t.InputSchema
        }).ToList();
    }
}

public class RegisteredTool
{
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    [JsonPropertyName("description")]
    public required string Description { get; set; }

    [JsonPropertyName("inputSchema")]
    public JsonElement? InputSchema { get; set; }
}
