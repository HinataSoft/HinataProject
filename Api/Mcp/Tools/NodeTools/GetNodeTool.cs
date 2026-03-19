using System.Security.Claims;
using System.Text.Json;
using HinataProject.Api.Mcp.Tools;
using HinataProject.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HinataProject.Api.Mcp.Tools.NodeTools;

public class GetNodeTool : IToolHandler
{
    private readonly IDbContextFactory<HinataProjectDataContext> _dbFactory;

    public GetNodeTool(IDbContextFactory<HinataProjectDataContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public string Name => "get_node";
    public string Description => "Get a node by its ID";

    public JsonElement? InputSchema => JsonSerializer.Deserialize<JsonElement>("""
        {
            "type": "object",
            "properties": {
                "nodeId": { "type": "string", "description": "Node UUID" }
            },
            "required": ["nodeId"]
        }
        """);

    public async Task<ToolResult> ExecuteAsync(JsonElement parameters, ClaimsPrincipal user, CancellationToken ct)
    {
        if (!parameters.TryGetProperty("nodeId", out var nodeIdElement) || !Guid.TryParse(nodeIdElement.GetString(), out var nodeId))
            return ToolResult.Error("Invalid or missing nodeId");

        using var db = await _dbFactory.CreateDbContextAsync(ct);
        var node = await db.Nodes
            .Include(n => n.Type)
            .Include(n => n.Workflow).ThenInclude(w => w!.States)
            .Include(n => n.State)
            //.Include(n => n.Comments).ThenInclude(c => c.User)
            //.Include(n => n.AddedTypes).ThenInclude(at => at.Type)
            //.Include(n => n.AddedWorkflows).ThenInclude(aw => aw.Workflow)
            //.Include(n => n.AddedRoles).ThenInclude(ar => ar.Role)
            //.Include(n => n.Assignees).ThenInclude(a => a.Role)
            //.Include(n => n.Assignees).ThenInclude(a => a.User)
            .AsNoTracking()
            .FirstOrDefaultAsync(n => n.Id == nodeId, ct);

        if (node == null)
            return ToolResult.Error("NodeNotFound");

        return ToolResult.Success(new
        {
            node.Id,
            node.Caption,
            node.Description,
            node.Manifest,
            node.Guardrails,
            Type = node.Type != null ? new { node.Type.Id, node.Type.Name, node.Type.Kind } : null,
            Workflow = node.Workflow != null ? new { node.Workflow.Id, node.Workflow.Name, States = node.Workflow.States.Select(s => new { s.Id, s.Name }).ToList() } : null,
            State = node.State != null ? new { node.State.Id, node.State.Name } : null
        });
    }
}
