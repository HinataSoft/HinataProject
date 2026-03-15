using System.Security.Claims;
using System.Text.Json;
using HinataProject.Api.Mcp.Tools;
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

        var nodeQuery = db.NodeAssignees
            .Where(na => na.UserId == currentUser.Id)
            .Include(na => na.Node).ThenInclude(n => n.Type)
            .Include(na => na.Node).ThenInclude(n => n.Workflow).ThenInclude(w => w!.States)
            .Include(na => na.Node).ThenInclude(n => n.State)
            .AsNoTracking()
            .Select(na => new
            {
                na.Node!.Id,
                na.Node.Caption,
                na.Node.Summary,
                Type = na.Node.Type != null ? new { na.Node.Type.Id, na.Node.Type.Name, na.Node.Type.Kind } : null,
                Workflow = na.Node.Workflow != null ? new { na.Node.Workflow.Id, na.Node.Workflow.Name, States = na.Node.Workflow.States.Select(s => new { s.Id, s.Name }).ToList() } : null,
                State = na.Node.State != null ? new { na.Node.State.Id, na.Node.State.Name } : null
            });

        int nodeCount = await nodeQuery.CountAsync();

        var nodeIds = nodeQuery
            .Skip(skip).Take(take)
            .ToListAsync(ct);

        return ToolResult.Success(new { items = nodeIds, total = nodeCount });
    }
}
