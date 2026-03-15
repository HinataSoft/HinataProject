using System.Security.Claims;
using System.Text.Json;
using HinataProject.Api.Mcp.Tools;
using HinataProject.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HinataProject.Api.Mcp.Tools.NodeTools;

public class ListChildrenTool : IToolHandler
{
    private readonly IDbContextFactory<HinataProjectDataContext> _dbFactory;

    public ListChildrenTool(IDbContextFactory<HinataProjectDataContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public string Name => "list_children";
    public string Description => "List child nodes of a parent node";

    public JsonElement? InputSchema => JsonSerializer.Deserialize<JsonElement>("""
        {
            "type": "object",
            "properties": {
                "parentId": { "type": "string", "description": "Parent node UUID" },
                "skip": { "type": "integer", "default": 0 },
                "take": { "type": "integer", "default": 20 }
            },
            "required": ["parentId"]
        }
        """);

    public async Task<ToolResult> ExecuteAsync(JsonElement parameters, ClaimsPrincipal user, CancellationToken ct)
    {
        if (!parameters.TryGetProperty("parentId", out var parentIdElement) || !Guid.TryParse(parentIdElement.GetString(), out var parentId))
            return ToolResult.Error("Invalid or missing parentId");

        var skip = 0;
        var take = 20;
        if (parameters.TryGetProperty("skip", out var skipElement))
            skip = skipElement.GetInt32();
        if (parameters.TryGetProperty("take", out var takeElement))
            take = takeElement.GetInt32();

        using var db = await _dbFactory.CreateDbContextAsync(ct);
        var children = await db.Nodes
            .Where(n => n.ParentId == parentId)
            .Include(n => n.Type)
            .Include(n => n.State)
            .AsNoTracking()
            .OrderBy(n => n.CreatedAt)
            .Skip(skip)
            .Take(take)
            .Select(n => new
            {
                n.Id,
                n.Caption,
                n.Description,
                Type = new { n.Type!.Id, n.Type.Name },
                State = n.State != null ? new { n.State.Id, n.State.Name } : null
            })
            .ToListAsync(ct);

        return ToolResult.Success(children);
    }
}
