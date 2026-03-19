using System.Security.Claims;
using System.Text.Json;
using HinataProject.Api.Mcp.Tools;
using HinataProject.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HinataProject.Api.Mcp.Tools.NodeTools;

public class ListCommentsTool : IToolHandler
{
    private readonly IDbContextFactory<HinataProjectDataContext> _dbFactory;

    public ListCommentsTool(IDbContextFactory<HinataProjectDataContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public string Name => "list_comments";
    public string Description => "List comments for a node";

    public JsonElement? InputSchema => JsonSerializer.Deserialize<JsonElement>("""
        {
            "type": "object",
            "properties": {
                "nodeId": { "type": "string", "description": "Node UUID" },
                "skip": { "type": "integer", "default": 0 },
                "take": { "type": "integer", "default": 20 },
                "ordering": { "type": "string", "default": "asc", "description": "Sort by CreatedAt: asc or desc" }
            },
            "required": ["nodeId"]
        }
        """);

    public async Task<ToolResult> ExecuteAsync(JsonElement parameters, ClaimsPrincipal user, CancellationToken ct)
    {
        if (!parameters.TryGetProperty("nodeId", out var nodeIdElement) || !Guid.TryParse(nodeIdElement.GetString(), out var nodeId))
            return ToolResult.Error("Invalid or missing nodeId");

        var skip = 0;
        var take = 20;
        var ordering = "asc";
        if (parameters.TryGetProperty("skip", out var skipElement))
            skip = skipElement.GetInt32();
        if (parameters.TryGetProperty("take", out var takeElement))
            take = takeElement.GetInt32();
        if (parameters.TryGetProperty("ordering", out var orderingElement))
            ordering = orderingElement.GetString() ?? "asc";

        using var db = await _dbFactory.CreateDbContextAsync(ct);

        var nodeExists = await db.Nodes.AnyAsync(n => n.Id == nodeId, ct);
        if (!nodeExists)
            return ToolResult.Error("NodeNotFound");

        var baseQuery = db.Comments.Where(c => c.NodeId == nodeId);

        var totalCount = await baseQuery.CountAsync(ct);

        var comments = ordering == "desc"
            ? baseQuery.OrderByDescending(c => c.CreatedAt).Skip(skip).Take(take)
            : baseQuery.OrderBy(c => c.CreatedAt).Skip(skip).Take(take);

        var items = await comments.Include(c => c.User).AsNoTracking().ToListAsync(ct);

        return ToolResult.Success(new
        {
            items = items.Select(c => new
            {
                c.Id,
                c.Text,
                User = c.User != null ? new { c.User.Id, c.User.Name } : null,
                c.CreatedAt
            }),
            totalCount
        });
    }
}
