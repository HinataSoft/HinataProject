using System.Security.Claims;
using System.Text.Json;

namespace HinataProject.Api.Mcp.Tools;

public interface IToolHandler
{
    string Name { get; }
    string Description { get; }
    JsonElement? InputSchema { get; }
    Task<ToolResult> ExecuteAsync(JsonElement parameters, ClaimsPrincipal user, CancellationToken ct);
}
