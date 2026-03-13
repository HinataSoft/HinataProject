using System.Text.Json;
using HinataProject.Domain.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HinataProject.Api.Endpoints;

public class OAuthEndpoint : IDiscoverableEndpoint
{
    public Task MapEndpoint(IEndpointRouteBuilder routeBuilder)
    {
        var group = routeBuilder.MapGroup("/api/oauth");

        // Get OAuth settings for frontend
        group.MapGet("/settings", GetOAuthSettings)
            .AllowAnonymous();

        // Exchange authorization code for tokens
        group.MapPost("/token", ExchangeToken)
            .AllowAnonymous();

        return Task.CompletedTask;
    }

    private Task<IResult> GetOAuthSettings(
        [FromServices] IConfiguration configuration,
        CancellationToken ct)
    {
        var oauthConfig = configuration.GetSection("Frontend:OAuth");
        
        var settings = new OAuthSettingsDto
        {
            AuthorizationEndpoint = oauthConfig["AuthorizationEndpoint"] ?? "",
            TokenEndpoint = oauthConfig["TokenEndpoint"] ?? "",
            ClientId = oauthConfig["ClientId"] ?? "",
            RedirectUri = oauthConfig["RedirectUri"] ?? "",
            Scope = oauthConfig["Scope"] ?? ""
        };

        return Task.FromResult<IResult>(Results.Ok(settings));
    }

    private async Task<IResult> ExchangeToken(
        [FromBody] TokenRequestDto request,
        [FromServices] IConfiguration configuration,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var oauthConfig = configuration.GetSection("Frontend:OAuth");

        // Get token endpoint from config
        var tokenEndpoint = oauthConfig["TokenEndpoint"];
        if (string.IsNullOrEmpty(tokenEndpoint))
        {
            return Results.BadRequest(new { error = "Token endpoint not configured" });
        }

        // Note: In production, the client_secret should be stored securely (not in config)
        var clientId = oauthConfig["ClientId"] ?? "";
        var clientSecret = oauthConfig["ClientSecret"] ?? "";

        // Determine grant type
        string grantType;
        Dictionary<string, string> formData;

        if (!string.IsNullOrEmpty(request.RefreshToken))
        {
            // Refresh token grant
            grantType = "refresh_token";
            formData = new Dictionary<string, string>
            {
                { "grant_type", grantType },
                { "refresh_token", request.RefreshToken },
                { "client_id", clientId },
                { "client_secret", clientSecret }
            };
        }
        else if (!string.IsNullOrEmpty(request.Code))
        {
            // Authorization code grant
            var redirectUri = oauthConfig["RedirectUri"] ?? "";
            grantType = "authorization_code";
            formData = new Dictionary<string, string>
            {
                { "grant_type", grantType },
                { "code", request.Code },
                { "redirect_uri", redirectUri },
                { "client_id", clientId },
                { "client_secret", clientSecret }
            };
        }
        else
        {
            return Results.BadRequest(new { error = "Missing required parameter: either 'code' or 'refresh_token' must be provided" });
        }

        try
        {
            var formContent = new FormUrlEncodedContent(formData);

            var httpClient = new HttpClient();
            var response = await httpClient.PostAsync(tokenEndpoint, formContent, ct);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(ct);
                return Results.BadRequest(new { error = "Token exchange failed", details = errorContent });
            }

            var content = await response.Content.ReadAsStringAsync(ct);
            var tokenResponse = JsonSerializer.Deserialize<JsonElement>(content);

            // Return the token response to the frontend
            return Results.Ok(new
            {
                access_token = tokenResponse.TryGetProperty("access_token", out var at) ? at.GetString() : null,
                token_type = tokenResponse.TryGetProperty("token_type", out var tt) ? tt.GetString() : null,
                expires_in = tokenResponse.TryGetProperty("expires_in", out var ei) ? ei.GetInt32() : (int?)null,
                refresh_token = tokenResponse.TryGetProperty("refresh_token", out var rt) ? rt.GetString() : null,
                scope = tokenResponse.TryGetProperty("scope", out var sc) ? sc.GetString() : null
            });
        }
        catch (Exception ex)
        {
            return Results.BadRequest(new { error = "Token exchange failed", details = ex.Message });
        }
    }
}

public class TokenRequestDto
{
    public string? Code { get; set; } // For authorization_code grant
    public string? RefreshToken { get; set; } // For refresh_token grant
    public string? RedirectUri { get; set; }
}
