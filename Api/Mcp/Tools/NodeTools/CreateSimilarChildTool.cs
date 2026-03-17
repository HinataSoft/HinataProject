using System.Security.Claims;
using System.Text.Json;
using HinataProject.Api.Mcp.Tools;
using HinataProject.Api.Services;
using HinataProject.Persistence;
using HinataProject.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace HinataProject.Api.Mcp.Tools.NodeTools;

public class CreateSimilarChildTool : IToolHandler
{
    private readonly IDbContextFactory<HinataProjectDataContext> _dbFactory;
    private readonly NodeChangeSignal _nodeChangeSignal;

    public CreateSimilarChildTool(IDbContextFactory<HinataProjectDataContext> dbFactory, NodeChangeSignal nodeChangeSignal)
    {
        _dbFactory = dbFactory;
        _nodeChangeSignal = nodeChangeSignal;
    }

    public string Name => "create_similar_child";
    public string Description => "Create a new child node under a parent node. The new node will automatically inherit the parent's type, workflow, and assignees. The state will be set to the workflow's default state.";

    public JsonElement? InputSchema => JsonSerializer.Deserialize<JsonElement>("""
        {
            "type": "object",
            "properties": {
                "parentId": { "type": "string", "description": "UUID of the parent node" },
                "caption": { "type": "string", "description": "One-line summary for the new node" },
                "description": { "type": "string", "description": "Short human-readable summary" },
                "manifest": { "type": "string", "description": "Technical specification" }
            },
            "required": ["parentId", "caption"]
        }
        """);

    public async Task<ToolResult> ExecuteAsync(JsonElement parameters, ClaimsPrincipal user, CancellationToken ct)
    {
        var subject = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(subject))
            return ToolResult.Error("Unauthorized: No subject claim");

        if (!parameters.TryGetProperty("parentId", out var parentIdElement) || !Guid.TryParse(parentIdElement.GetString(), out var parentId))
            return ToolResult.Error("Invalid or missing parentId");

        if (!parameters.TryGetProperty("caption", out var captionElement) || string.IsNullOrWhiteSpace(captionElement.GetString()))
            return ToolResult.Error("Invalid or missing caption");

        var description = parameters.TryGetProperty("description", out var descElement) ? descElement.GetString() : null;
        var manifest = parameters.TryGetProperty("manifest", out var manifestElement) ? manifestElement.GetString() : null;

        using var db = await _dbFactory.CreateDbContextAsync(ct);

        var currentUser = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Subject == subject, ct);
        if (currentUser == null)
            return ToolResult.Error("User not found");

        // Check Active or Admin rights
        if (currentUser.Rights != Domain.Rights.Admin && currentUser.Rights != Domain.Rights.Active)
            return ToolResult.Error("Unauthorized: Active rights required for this operation");

        // Fetch parent with inherited properties
        var parent = await db.Nodes
            .Include(n => n.Type)
            .Include(n => n.Workflow)
                .ThenInclude(w => w!.DefaultState)
            .Include(n => n.State)
            .Include(n => n.Assignees)
                .ThenInclude(a => a.User)
            .FirstOrDefaultAsync(n => n.Id == parentId, ct);

        if (parent == null)
            return ToolResult.Error("ParentNodeNotFound");

        if (parent.WorkflowId == null)
            return ToolResult.Error("ParentHasNoWorkflow: Parent node has no workflow, cannot create child");

        // Get next PublicId
        var maxPublicId = await db.Nodes.MaxAsync(n => (int?)n.PublicId, ct) ?? 0;

        // Create child node inheriting from parent
        var child = new NodeEntity
        {
            Id = Guid.NewGuid(),
            ParentId = parentId,
            PublicId = maxPublicId + 1,
            Caption = captionElement.GetString()!,
            Description = description ?? string.Empty,
            Manifest = manifest ?? string.Empty,
            Summary = string.Empty,
            TypeId = parent.TypeId,
            WorkflowId = parent.WorkflowId,
            // Use workflow's default state, not parent's state
            StateId = parent.Workflow!.DefaultStateId
        };

        db.Nodes.Add(child);

        // Copy assignees from parent
        foreach (var assignee in parent.Assignees)
        {
            db.NodeAssignees.Add(new NodeAssigneeEntity
            {
                NodeId = child.Id,
                UserId = assignee.UserId,
                RoleId = assignee.RoleId
            });
        }

        await db.SaveChangesAsync(ct);

        // Notify assignees of the new child node
        _ = _nodeChangeSignal.NotifyForNode(child.Id);

        return ToolResult.Success(new
        {
            child.Id,
            child.PublicId,
            child.Caption,
            child.Description,
            child.Manifest,
            child.Summary,
            Type = parent.Type != null ? new { parent.Type.Id, parent.Type.Name } : null,
            Workflow = parent.Workflow != null ? new { parent.Workflow.Id, parent.Workflow.Name } : null,
            State = parent.Workflow?.DefaultState != null ? new { parent.Workflow.DefaultState.Id, parent.Workflow.DefaultState.Name } : null,
            Assignees = parent.Assignees.Select(a => new { a.UserId, UserName = a.User != null ? a.User.Name : null }).ToList()
        });
    }
}
