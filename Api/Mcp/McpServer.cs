using System.Security.Claims;
using System.Text.Json;
using HinataProject.Api.Mcp.JsonRpc;
using HinataProject.Api.Mcp.Tools;

namespace HinataProject.Api.Mcp;

public class McpServer
{
    private readonly ToolRegistry _toolRegistry;

    public McpServer(ToolRegistry toolRegistry, IEnumerable<IToolHandler> tools)
    {
        _toolRegistry = toolRegistry;
        foreach (var tool in tools)
        {
            _toolRegistry.Register(tool);
        }
    }

    public Task<JsonRpcResponse?> HandleRequestAsync(JsonRpcRequest request, ClaimsPrincipal user, CancellationToken ct)
    {
        try
        {
            // Notifications don't expect a response
            if (request.Id is null)
            {
                return HandleNotificationAsync(request, ct);
            }

            return request.Method switch
            {
                "initialize" => HandleInitializeAsync(request, ct),
                "tools/list" => HandleToolsListAsync(request, ct),
                "tools/call" => HandleToolsCallAsync(request, user, ct),
                _ => Task.FromResult<JsonRpcResponse?>(JsonRpcResponse.FromError(request.Id, JsonRpcError.MethodNotFound))
            };
        }
        catch (Exception)
        {
            return Task.FromResult<JsonRpcResponse?>(JsonRpcResponse.FromError(request.Id, JsonRpcError.InternalError));
        }
    }

    private Task<JsonRpcResponse?> HandleNotificationAsync(JsonRpcRequest request, CancellationToken ct)
    {
        // Handle notifications - these don't expect a response
        // Common MCP notifications: "notifications/initialized"
        if (request.Method == "notifications/initialized")
        {
            // Return null to indicate no response should be sent
            return Task.FromResult<JsonRpcResponse?>(null);
        }

        // Unknown notifications are ignored
        return Task.FromResult<JsonRpcResponse?>(null);
    }

    private static Task<JsonRpcResponse?> HandleInitializeAsync(JsonRpcRequest request, CancellationToken ct)
    {
        var result = new
        {
            protocolVersion = "2024-11-05",
            capabilities = new { tools = new { } },
            serverInfo = new { name = "HinataProject MCP Server", version = "1.0.0" }
        };
        return Task.FromResult<JsonRpcResponse?>(JsonRpcResponse.Success(request.Id, result));
    }

    private Task<JsonRpcResponse?> HandleToolsListAsync(JsonRpcRequest request, CancellationToken ct)
    {
        var tools = _toolRegistry.GetAll();
        return Task.FromResult<JsonRpcResponse?>(JsonRpcResponse.Success(request.Id, new { tools }));
    }

    private async Task<JsonRpcResponse?> HandleToolsCallAsync(JsonRpcRequest request, ClaimsPrincipal user, CancellationToken ct)
    {
        if (request.Params is not JsonElement paramsElement)
            return JsonRpcResponse.FromError(request.Id, JsonRpcError.InvalidParams);

        if (!paramsElement.TryGetProperty("name", out var nameElement))
            return JsonRpcResponse.FromError(request.Id, JsonRpcError.InvalidParams);

        var toolName = nameElement.GetString();
        if (string.IsNullOrEmpty(toolName))
            return JsonRpcResponse.FromError(request.Id, JsonRpcError.InvalidParams);

        var tool = _toolRegistry.Get(toolName);
        if (tool == null)
            return JsonRpcResponse.FromError(request.Id, JsonRpcError.MethodNotFound);

        JsonElement? args = null;
        if (paramsElement.TryGetProperty("arguments", out var argsElement))
            args = argsElement;

        var result = await tool.ExecuteAsync(args ?? JsonSerializer.Deserialize<JsonElement>("{}"), user, ct);

        return JsonRpcResponse.Success(request.Id, new
        {
            content = result.Content,
            isError = result.IsError
        });
    }
}
