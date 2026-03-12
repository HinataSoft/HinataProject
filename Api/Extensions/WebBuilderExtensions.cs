using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using HinataProject.Api.Initialization;
using MicroElements.Swashbuckle.NodaTime;
using Microsoft.Extensions.Logging.Console;
using Microsoft.OpenApi.Models;
using NodaTime;
using NodaTime.Serialization.SystemTextJson;

namespace HinataProject.Api.Extensions;

public static class WebBuilderExtensions
{
    public static IServiceCollection ConfigureJsonOptions(this IServiceCollection serviceCollection)
    {
        var jsonSerializerOptions = new JsonSerializerOptions();
        jsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        jsonSerializerOptions.PropertyNameCaseInsensitive = true;
        jsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        jsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        jsonSerializerOptions.TypeInfoResolver = new DefaultJsonTypeInfoResolver();
        jsonSerializerOptions.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);

        serviceCollection.AddSingleton<JsonSerializerOptions>(jsonSerializerOptions);

        serviceCollection.ConfigureHttpJsonOptions(jsonOptions =>
        {
            jsonSerializerOptions.CopyTo(jsonOptions.SerializerOptions);
        });

        serviceCollection.Configure<Microsoft.AspNetCore.Mvc.JsonOptions>(jsonOptions =>
        {
            jsonSerializerOptions.CopyTo(jsonOptions.JsonSerializerOptions);
        });

        return serviceCollection;
    }

    public static IServiceCollection ConfigureTimeAndZoneProviders(this IServiceCollection serviceCollection)
    {
        serviceCollection
            .AddSingleton<TimeProvider>(TimeProvider.System);

        return serviceCollection;
    }

    public static (ILogger configurationLogger, ILoggerFactory configurationLoggerFactory)
        InitializeTelemetryAndLogging(this WebApplicationBuilder builder)
    {
        ILoggerFactory? tempLoggerFactory;
        ILogger<Program>? logger;

        tempLoggerFactory = LoggerFactory.Create(q =>
        {
            q.AddSimpleConsole(q =>
            {
                q.ColorBehavior = LoggerColorBehavior.Enabled;
                q.SingleLine = true;
                q.IncludeScopes = false;
            });
        });
        logger = new Logger<Api.Program>(tempLoggerFactory);

        builder.Logging.AddSimpleConsole(q =>
        {
            q.SingleLine = true;
            q.IncludeScopes = false;
            q.ColorBehavior = LoggerColorBehavior.Enabled;
            q.TimestampFormat = "[HH:mm:ss.fff] ";
        });

        return (logger, tempLoggerFactory);
    }

    public static IServiceCollection AddConfiguredSwaggerGen(this IServiceCollection services,
        IConfiguration configuration)
    {
        return services.AddSwaggerGen(q =>
        {
            q.ConfigureForNodaTimeWithSystemTextJson();

            q.AddSecurityDefinition("client", new OpenApiSecurityScheme
            {
                Description =
                    "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.OpenIdConnect,
                OpenIdConnectUrl =
                    new Uri("https://playground.eposid.eu/openid/connect/.well-known/openid-configuration"),
                Scheme = "Bearer"
            });

            q.AddSecurityDefinition("identity", new OpenApiSecurityScheme
            {
                Description =
                    "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer"
            });

            var url = configuration.GetValue<string>("IdentityServer:ConnectUriPrefix");

            if (Uri.IsWellFormedUriString(url, UriKind.Absolute))
            {
                q.AddSecurityDefinition("openid", new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.OpenIdConnect,
                    OpenIdConnectUrl =
                        new Uri(url, UriKind.Absolute).Combine("connect/.well-known/openid-configuration"),
                    Description = "OpenID Connect",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Scheme = "Bearer",
                    BearerFormat = "JWT"
                });
            }


            q.OperationFilter<AuthorizeCheckOperationFilter>();
        });
    }

    private static Uri Combine(this Uri baseUri, params string[] segments)
    {
        if (baseUri is null)
            throw new ArgumentNullException(nameof(baseUri));
        
        string combinedPath = baseUri.AbsolutePath.TrimEnd('/');
        foreach (var segment in segments)
            combinedPath = $"{combinedPath}/{segment.Trim('/')}";

        var builder = new UriBuilder(baseUri)
        {
            Path = combinedPath,
            Query = baseUri.Query // Preserve the original query string
        };

        return builder.Uri;
    }
}