using System.Security.Claims;
using System.Text.Json;
using HinataProject.Api.Mcp.Tools;
using HinataProject.Domain;
using HinataProject.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HinataProject.Api.Mcp.Tools.NodeTools;

public class GetAssignedToMeTool : IToolHandler
{
    private readonly IDbContextFactory<HinataProjectDataContext> _dbFactory;

    public GetAssignedToMeTool(IDbContextFactory<HinataProjectDataContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public string Name => "get_assigned_to_me";
    public string Description => "Get IDs of nodes assigned to the authenticated user";

    public JsonElement? InputSchema => JsonSerializer.Deserialize<JsonElement>("""
        {
            "type": "object",
            "properties": {
                "skip": { "type": "integer", "default": 0 },
                "take": { "type": "integer", "default": 20 }
            }
        }
        """);

    public async Task<ToolResult> ExecuteAsync(JsonElement parameters, ClaimsPrincipal user, CancellationToken ct)
    {
        var subject = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(subject))
            return ToolResult.Error("Unauthorized: No subject claim");

        var skip = 0;
        var take = 20;
        if (parameters.TryGetProperty("skip", out var skipElement))
            skip = skipElement.GetInt32();
        if (parameters.TryGetProperty("take", out var takeElement))
            take = takeElement.GetInt32();

        using var db = await _dbFactory.CreateDbContextAsync(ct);
        var currentUser = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Subject == subject, ct);
        if (currentUser == null)
            return ToolResult.Error("User not found");

        // Find all nodes where:
        // 1. Node is stateful (has a State)
        // 2. The current state's Role (State.Roles) has an assignee for this user
        var baseQuery = db.Nodes
            .Include(n => n.Type)
            .Include(n => n.State)
                .ThenInclude(s => s!.Roles)
            .Where(n => n.StateId != null)
            .Where(n => n.Type != null && n.Type.Kind == TypeKind.Stateful)
            .Where(n => n.State!.Roles.Any(role =>
                db.NodeAssignees.Any(na =>
                    na.NodeId == n.Id &&
                    na.RoleId == role.Id &&
                    na.UserId == currentUser.Id)));

        int nodeCount = await baseQuery.CountAsync(ct);

        var nodes = await baseQuery
            .OrderBy(n => n.Id)
            .Skip(skip).Take(take)
            .ToListAsync(ct);

        var items = nodes.Select(n => new
        {
            n.Id,
            n.ParentId,
            n.Caption,
            Type = n.Type != null ? new { n.Type.Id, n.Type.Name, n.Type.Kind } : null,
            State = n.State != null ? new { n.State.Id, n.State.Name } : null
        }).ToList();

        return ToolResult.Success(new { items, total = nodeCount });
    }
}
