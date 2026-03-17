using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

using HinataProject.Api;
using HinataProject.Api.Extensions;
using HinataProject.Api.Mcp;
using HinataProject.Api.Mcp.Tools;
using HinataProject.Api.Mcp.Tools.NodeTools;
using HinataProject.Api.Services;
using HinataProject.Persistence.Utils;

var builder = WebApplication.CreateBuilder(args);

var (_, configurationLoggerFactory) = builder.InitializeTelemetryAndLogging();

var authConfig = builder.Configuration.GetSection("Authentication");

builder.Services
    .AddEndpointsApiExplorer()
    .AddConfiguredSwaggerGen(builder.Configuration)
    .ConfigureJsonOptions()
    .ConfigureTimeAndZoneProviders()
    .AddEndpointsApiExplorer()
    .AddProblemDetails(options =>
    {
        options.CustomizeProblemDetails = ctx =>
        {
            ctx.ProblemDetails.Extensions.TryAdd("TraceIdentifier", ctx.HttpContext.TraceIdentifier);
        };
    })
    .AddExceptionHandler<ExceptionToProblemDetailsHandler>()
    .AddCors();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = authConfig["Authority"] ?? throw new InvalidOperationException("Authority is required");
        options.Audience = authConfig["Audience"];
        options.RequireHttpsMetadata = authConfig.GetValue<bool>("RequireHttpsMetadata");

        // Configure token validation parameters - DO NOT set ValidIssuer, let it be discovered automatically
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = authConfig.GetValue<bool>("ValidateIssuer"),
            ValidateAudience = authConfig.GetValue<bool>("ValidateAudience"),
            ValidateLifetime = authConfig.GetValue<bool>("ValidateLifetime"),
            ValidateIssuerSigningKey = authConfig.GetValue<bool>("ValidateIssuerSigningKey"),
            NameClaimType = authConfig["NameClaimType"] ?? "name",
        };
    });

builder.Services.AddRightsAuthorization();

// MCP Server
builder.Services.AddSingleton<ToolRegistry>();
builder.Services.AddSingleton<McpServer>();
builder.Services.AddSingleton<NodeChangeSignal>();
builder.Services.AddSingleton<IToolHandler, GetNodeTool>();
builder.Services.AddSingleton<IToolHandler, ListChildrenTool>();
builder.Services.AddSingleton<IToolHandler, GetAssignedToMeTool>();
builder.Services.AddSingleton<IToolHandler, SetNodeStateTool>();
builder.Services.AddSingleton<IToolHandler, AddCommentTool>();
builder.Services.AddSingleton<IToolHandler, UpdateNodeTool>();
builder.Services.AddSingleton<IToolHandler, CreateSimilarChildTool>();

builder.AddEndpoints(typeof(Program).Assembly);
builder.Services.ConfigurePersistence(builder.Configuration);

var app = builder.Build();

configurationLoggerFactory.Dispose();
app.LogApplicationInfo();

app.UseCors(q => q.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
app.UseRouting();

// Serve static files from "wwwroot" directory at root "/"
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(
        Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"))
});

// Fallback to index.html for SPA routing (when file is not found)
app.MapFallbackToFile("index.html");

app.UseAuthentication();
app.UseAuthorization();
app.UseConfiguredSwagger();

await app.Services.PrepareDataProviders();

await app.MapAllEndpoints();

app.UseExceptionHandler();

app.Run();

namespace HinataProject.Api
{
    public partial class Program
    {
    }
}
