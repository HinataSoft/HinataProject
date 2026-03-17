using System.Security.Claims;
using System.Text;
using System.Text.Json;

using Microsoft.AspNetCore.Mvc;

using HinataProject.Api.Mcp;
using HinataProject.Api.Mcp.JsonRpc;

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
            .RequireAuthorization("Passive");

        group.MapPost("/orig", HandleJsonRpc);

        group.MapPost("/", async delegate (HttpContext context, ClaimsPrincipal user, CancellationToken ct)
        {
            string jsonstring;
            using (StreamReader reader = new StreamReader(context.Request.Body, Encoding.UTF8))
            { jsonstring = await reader.ReadToEndAsync(ct); }
            Console.WriteLine(jsonstring);
            var request = JsonSerializer.Deserialize<JsonRpcRequest>(jsonstring);
            if (request is null)
                return Results.BadRequest();
            var response = await HandleJsonRpc(request, user, ct);
            if (response is Microsoft.AspNetCore.Http.HttpResults.JsonHttpResult<JsonRpcResponse> typedResponse)
                Console.WriteLine(JsonSerializer.Serialize(typedResponse.Value));
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

        return Results.Json(response);
    }
}
