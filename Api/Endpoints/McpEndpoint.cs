using System.Security.Claims;
using System.Text;
using System.Text.Json;
using HinataProject.Api.Mcp;
using HinataProject.Api.Mcp.JsonRpc;
using HinataProject.Api.Mcp.Tools;
using HinataProject.Api.Mcp.Tools.NodeTools;
using HinataProject.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HinataProject.Api.Endpoints;

public class McpEndpoint : IDiscoverableEndpoint
{
    private readonly McpServer _mcpServer;

    public McpEndpoint(McpServer mcpServer)
    {
        _mcpServer = mcpServer;
    }

    public Task MapEndpoint(IEndpointRouteBuilder routeBuilder)
    {
        var group = routeBuilder.MapGroup("/mcp")
            ;//.RequireAuthorization("Passive");

        //group.MapPost("/", HandleJsonRpc);
        group.MapPost("/", async delegate (HttpContext context, ClaimsPrincipal user, CancellationToken ct)
        {
            string jsonstring;
            using (StreamReader reader = new StreamReader(context.Request.Body, Encoding.UTF8))
            { jsonstring = await reader.ReadToEndAsync(ct); }

            Console.WriteLine(jsonstring);

            var request = JsonSerializer.Deserialize<JsonRpcRequest>(jsonstring);

            var response = await HandleJsonRpc(request, user, ct);

            return response;

        });


        return Task.CompletedTask;
    }

    private async Task<IResult> HandleJsonRpc(
        [FromBody] JsonRpcRequest request,
        ClaimsPrincipal user,
        CancellationToken ct)
    {
        var response = await _mcpServer.HandleRequestAsync(request, user, ct);

        // Notifications don't expect a response
        if (response is null)
            return Results.NoContent();

        Console.WriteLine(JsonSerializer.Serialize(response));
        return Results.Json(response);
    }
}
