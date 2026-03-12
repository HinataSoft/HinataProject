using System.Reflection;
using Microsoft.OpenApi.Models;

namespace HinataProject.Api.Extensions;

public static class WebApplicationExtensions
{
    public static void UseConfiguredSwagger(this WebApplication app)
    {
        bool initializeSwagger =
            app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("Swagger:ForceSwagger");
        var logger = app.Services.GetRequiredService<ILogger<WebApplication>>();

        logger.LogInformation("Initialize Swagger: {initializeSwagger}", initializeSwagger);

        if (initializeSwagger is false)
        {
            logger.LogInformation("Skipping Swagger initialization based on configuration and environment");
            return;
        }

        string? swaggerUrl = null;
        
        app.UseSwagger(c =>
        {
            c.PreSerializeFilters.Add((swaggerDoc, httpReq) =>
            {
                swaggerUrl = app.Configuration.GetValue<string>("Swagger:OpenApiServer");
                swaggerDoc.Servers = new List<OpenApiServer>
                {
                    new OpenApiServer
                    {
                        Url = swaggerUrl is { Length: > 0 }
                            ? swaggerUrl
                            : $"{httpReq.Scheme}://{httpReq.Host.Value}"
                    }
                };
            });
        });

        app.UseSwaggerUI(q => { q.RoutePrefix = "swagger"; });

        logger.LogInformation("Swagger initialized on url: {url}", swaggerUrl);
    }

    public static void LogApplicationInfo(this WebApplication app)
    {
        var logger = app.Services.GetRequiredService<ILogger<WebApplication>>();
        logger.LogApplicationInfo(app.Environment);
    }

    public static void LogApplicationInfo(this ILogger<WebApplication> logger, IWebHostEnvironment webHostEnvironment)
    {
        logger.LogInformation("Assembly version: {assemblyVersion}", Assembly.GetEntryAssembly()?.GetName().Version?.ToString());
        logger.LogInformation("Environment: {EnvironmentName}", webHostEnvironment.EnvironmentName);
        logger.LogInformation("ContentRootPath: {EnvironmentContentRootPath}", webHostEnvironment.ContentRootPath);
        logger.LogInformation("WebRootPath: {WebRootPath}", webHostEnvironment.WebRootPath);
        logger.LogInformation("WorkingDirectory: {CurrentDirectory}", Directory.GetCurrentDirectory());
        logger.LogInformation("LoggerLevel: {LogLevel}", logger.GetEnabledLogLevel());
    }

    private static LogLevel GetEnabledLogLevel(this ILogger logger)
    {
        var levels = Enum.GetValues(typeof(LogLevel))
            .Cast<LogLevel>()
            .Where(q=> q is not LogLevel.None);
        
        foreach (var logLevel in levels)
        {
            if (logger.IsEnabled(logLevel))
                return logLevel;
        }
        
        return LogLevel.None;
    }
}