using System.Security.Claims;
using System.Text.Json;
using HinataProject.Api.Mcp.Tools;
using HinataProject.Api.Services;
using HinataProject.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HinataProject.Api.Mcp.Tools.NodeTools;

public class UpdateNodeTool : IToolHandler
{
    private readonly IDbContextFactory<HinataProjectDataContext> _dbFactory;
    private readonly NodeChangeSignal _nodeChangeSignal;

    public UpdateNodeTool(IDbContextFactory<HinataProjectDataContext> dbFactory, NodeChangeSignal nodeChangeSignal)
    {
        _dbFactory = dbFactory;
        _nodeChangeSignal = nodeChangeSignal;
    }

    public string Name => "update_node";
    public string Description => "Update properties of a node";

    public JsonElement? InputSchema => JsonSerializer.Deserialize<JsonElement>("""
        {
            "type": "object",
            "properties": {
                "nodeId": { "type": "string", "description": "Node UUID" },
                "manifest": { "type": "string", "description": "Technical specification" },
                "caption": { "type": "string", "description": "One-line summary" },
                "description": { "type": "string", "description": "Short human-readable summary" }
            },
            "required": ["nodeId"]
        }
        """);

    public async Task<ToolResult> ExecuteAsync(JsonElement parameters, ClaimsPrincipal user, CancellationToken ct)
    {
        var subject = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(subject))
            return ToolResult.Error("Unauthorized: No subject claim");

        if (!parameters.TryGetProperty("nodeId", out var nodeIdElement) || !Guid.TryParse(nodeIdElement.GetString(), out var nodeId))
            return ToolResult.Error("Invalid or missing nodeId");

        using var db = await _dbFactory.CreateDbContextAsync(ct);

        var currentUser = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Subject == subject, ct);
        if (currentUser == null)
            return ToolResult.Error("User not found");

        // Check Active or Admin rights per MANIFEST.md
        if (currentUser.Rights != Domain.Rights.Admin && currentUser.Rights != Domain.Rights.Active)
            return ToolResult.Error("Unauthorized: Active rights required for this operation");

        var node = await db.Nodes.FirstOrDefaultAsync(n => n.Id == nodeId, ct);
        if (node == null)
            return ToolResult.Error("NodeNotFound");

        var hasChanges = false;
        if (parameters.TryGetProperty("manifest", out var manifestElement) && manifestElement.ValueKind != JsonValueKind.Null)
        {
            node.Manifest = manifestElement.GetString();
            hasChanges = true;
        }
        if (parameters.TryGetProperty("caption", out var captionElement) && captionElement.ValueKind != JsonValueKind.Null)
        {
            node.Caption = captionElement.GetString();
            hasChanges = true;
        }
        if (parameters.TryGetProperty("description", out var descElement) && descElement.ValueKind != JsonValueKind.Null)
        {
            node.Description = descElement.GetString();
            hasChanges = true;
        }
        if (!hasChanges)
            return ToolResult.Error("NoPropertiesToUpdate");

        await db.SaveChangesAsync(ct);

        // Notify all assignees that the node changed
        _ = _nodeChangeSignal.NotifyForNode(nodeId);

        return ToolResult.Success(new
        {
            node.Id,
            node.Manifest,
            node.Caption,
            node.Description,
            node.Guardrails
        });
    }
}
