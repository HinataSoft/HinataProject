using System.Security.Claims;
using System.Text.Json;
using HinataProject.Api.Mcp.Tools;
using HinataProject.Api.Services;
using HinataProject.Domain;
using HinataProject.Persistence;
using HinataProject.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace HinataProject.Api.Mcp.Tools.NodeTools;

public class SetNodeStateTool : IToolHandler
{
    private readonly IDbContextFactory<HinataProjectDataContext> _dbFactory;
    private readonly NodeChangeSignal _nodeChangeSignal;

    public SetNodeStateTool(IDbContextFactory<HinataProjectDataContext> dbFactory, NodeChangeSignal nodeChangeSignal)
    {
        _dbFactory = dbFactory;
        _nodeChangeSignal = nodeChangeSignal;
    }

    public string Name => "set_node_state";
    public string Description => "Change the workflow state of a node";

    public JsonElement? InputSchema => JsonSerializer.Deserialize<JsonElement>("""
        {
            "type": "object",
            "properties": {
                "nodeId": { "type": "string", "description": "Node UUID" },
                "targetStateId": { "type": "string", "description": "Target state UUID" }
            },
            "required": ["nodeId", "targetStateId"]
        }
        """);

    public async Task<ToolResult> ExecuteAsync(JsonElement parameters, ClaimsPrincipal user, CancellationToken ct)
    {
        var subject = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(subject))
            return ToolResult.Error("Unauthorized: No subject claim");

        if (!parameters.TryGetProperty("nodeId", out var nodeIdElement) || !Guid.TryParse(nodeIdElement.GetString(), out var nodeId))
            return ToolResult.Error("Invalid or missing nodeId");
        if (!parameters.TryGetProperty("targetStateId", out var targetStateIdElement) || !Guid.TryParse(targetStateIdElement.GetString(), out var targetStateId))
            return ToolResult.Error("Invalid or missing targetStateId");

        using var db = await _dbFactory.CreateDbContextAsync(ct);

        var currentUser = await db.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .ThenInclude(r => r!.States)
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Subject == subject, ct);

        if (currentUser == null)
            return ToolResult.Error("User not found");

        // Check if user is admin
        var isAdmin = currentUser.Rights == Domain.Rights.Admin;

        var node = await db.Nodes
            .Include(n => n.Type)
            .Include(n => n.Workflow)
            .ThenInclude(w => w!.States)
            .Include(n => n.State)
            .Include(n => n.Assignees)
            .ThenInclude(a => a.Role)
            .FirstOrDefaultAsync(n => n.Id == nodeId, ct);

        if (node == null)
            return ToolResult.Error("NodeNotFound");

        if (node.Type?.Kind != TypeKind.Stateful)
            return ToolResult.Error("NodeNotStateful");

        var targetState = node.Workflow?.States.FirstOrDefault(s => s.Id == targetStateId);
        if (targetState == null)
            return ToolResult.Error("InvalidStateTransition");

        // Authorization: Admin can change state to anything
        // OR current user must be the current assignee (person the node is assigned to)
        if (!isAdmin)
        {
            // Load current state with its roles
            var currentState = await db.States
                .Include(s => s.Roles)
                .FirstOrDefaultAsync(s => s.Id == node.StateId, ct);

            if (currentState == null)
                return ToolResult.Error("Current state not found");

            // Find the role that corresponds to current state
            var currentStateRole = currentState.Roles.FirstOrDefault();
            if (currentStateRole == null)
                return ToolResult.Error("No role defined for current state");

            // Check if current user is assigned to this role on this node
            var isCurrentAssignee = node.Assignees.Any(a =>
                a.RoleId == currentStateRole.Id && a.UserId == currentUser.Id);

            if (!isCurrentAssignee)
                return ToolResult.Error("Unauthorized: Only admin or current assignee can change state");
        }

        node.StateId = targetStateId;
        await db.SaveChangesAsync(ct);

        // Notify all assignees that the node changed
        _ = _nodeChangeSignal.NotifyForNode(nodeId);

        return ToolResult.Success(new
        {
            node.Id,
            node.StateId,
            NewState = new { targetState.Id, targetState.Name }
        });
    }
}
