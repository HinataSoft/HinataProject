using System.Security.Claims;
using System.Text.Json;
using HinataProject.Api.Mcp.Tools;
using HinataProject.Persistence;
using HinataProject.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace HinataProject.Api.Mcp.Tools.NodeTools;

public class AddCommentTool : IToolHandler
{
    private readonly IDbContextFactory<HinataProjectDataContext> _dbFactory;

    public AddCommentTool(IDbContextFactory<HinataProjectDataContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public string Name => "add_comment";
    public string Description => "Add a comment to a node";

    public JsonElement? InputSchema => JsonSerializer.Deserialize<JsonElement>("""
        {
            "type": "object",
            "properties": {
                "nodeId": { "type": "string", "description": "Node UUID" },
                "text": { "type": "string", "description": "Comment text" }
            },
            "required": ["nodeId", "text"]
        }
        """);

    public async Task<ToolResult> ExecuteAsync(JsonElement parameters, ClaimsPrincipal user, CancellationToken ct)
    {
        var subject = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(subject))
            return ToolResult.Error("Unauthorized: No subject claim");

        if (!parameters.TryGetProperty("nodeId", out var nodeIdElement) || !Guid.TryParse(nodeIdElement.GetString(), out var nodeId))
            return ToolResult.Error("Invalid or missing nodeId");
        if (!parameters.TryGetProperty("text", out var textElement))
            return ToolResult.Error("Missing text");

        var text = textElement.GetString();
        if (string.IsNullOrWhiteSpace(text))
            return ToolResult.Error("EmptyComment");

        using var db = await _dbFactory.CreateDbContextAsync(ct);

        var currentUser = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Subject == subject, ct);
        if (currentUser == null)
            return ToolResult.Error("User not found");

        var nodeExists = await db.Nodes.AnyAsync(n => n.Id == nodeId, ct);
        if (!nodeExists)
            return ToolResult.Error("NodeNotFound");

        var comment = new CommentEntity
        {
            NodeId = nodeId,
            UserId = currentUser.Id,
            Text = text
        };

        db.Comments.Add(comment);
        await db.SaveChangesAsync(ct);

        return ToolResult.Success(new
        {
            comment.Id,
            comment.Text,
            comment.CreatedAt,
            User = new { currentUser.Id, currentUser.Name }
        });
    }
}
