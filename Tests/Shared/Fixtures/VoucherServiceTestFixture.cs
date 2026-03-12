using System.Net.Http.Json;
using System.Text.Json;
using HinataProject.Api;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;
using NodaTime.Serialization.SystemTextJson;
using VoucherService.Tests.Shared.Fixtures.Common;
using Xunit.Abstractions;

namespace VoucherService.Tests.Shared.Fixtures;

public class VoucherServiceTestFixture : PostgresAppWebApplicationTestFixture<Program>
{
    public JsonSerializerOptions JsonSerializerOptions { get; private set; }

    public VoucherServiceTestFixture(IMessageSink messageSink)
        : base(messageSink)
    {
        JsonSerializerOptions = new JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
        JsonSerializerOptions.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);
    }

    /// <summary>
    /// Represents the initial time value used in the test.
    /// </summary>
    Instant initialTime = Instant.FromUtc(2024, 01, 01, 15, 0);

    //public ConfigurableTimeProvider TimeProvider { get; private set; }

    /// <summary>
    /// Configures the services required for testing.
    /// </summary>
    /// <param name="services">The collection of services to be configured.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    protected override async Task ConfigureTestServicesAsync(IServiceCollection services)
    {
        await base.ConfigureTestServicesAsync(services);
    }

    protected override async Task SetupTestDataAsync()
    {
        await base.SetupTestDataAsync();
    }

    
    internal async Task<string> GetClientAccessToken(string clientId, string clientSecret, string connectUriPrefix)
    {
        var client = new HttpClient();
        client.DefaultRequestHeaders
            .Accept
            .Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

        var configuration = this.Factory.Services.GetService<IConfiguration>()
            ?? throw new Exception("IConfiguration missing");
        
        var content = new StringContent(
            $"grant_type=client_credentials&" +
            $"client_id={Uri.EscapeDataString(clientId)}&" +
            $"client_secret={Uri.EscapeDataString(clientSecret)}",
            System.Text.Encoding.UTF8, "application/x-www-form-urlencoded");

        HttpResponseMessage responseMessage = await client.PostAsync($"{connectUriPrefix}connect/token", content);
        string reponseString = await responseMessage.Content.ReadAsStringAsync();
        Dictionary<string, object> responseData = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(reponseString)
            ?? throw new Exception($"Cannot deserialize response {reponseString}");

        if (!responseData.TryGetValue("access_token", out object? accessToken) || accessToken is null)
            throw new Exception($"Cannot find access_token in response {reponseString}");

        return accessToken.ToString()
            ?? throw new Exception($"Empty access_token in response {reponseString}");
    }

    internal async Task<string> GetClientAccessToken()
    {
        
        var configuration = this.Factory.Services.GetService<IConfiguration>()
                            ?? throw new Exception("IConfiguration missing");
        
        string clientId = configuration.GetRequiredSection("IdentityServer").GetValue<string>("ClientId")
                          ?? throw new Exception("Configuration IdentityServer.ClientId not found");
        
        string clientSecret = configuration.GetRequiredSection("IdentityServer").GetValue<string>("ClientSecret")
                              ?? throw new Exception("Configuration IdentityServer.ClientSecret not found");
        
        
        string connectUriPrefix = configuration.GetRequiredSection("IdentityServer").GetValue<string>("ConnectUriPrefix")
                                  ?? throw new Exception("Configuration IdentityServer.ConnectUriPrefix not found");
        
        return await GetClientAccessToken(clientId, clientSecret, connectUriPrefix);
    }

    internal async Task<string> GetIdentityAccessToken(string identityKey = "SaleAdmin")
    {
        var configuration = this.Factory.Services.GetService<IConfiguration>()
                            ?? throw new Exception("IConfiguration missing");
        
        var identitySection = configuration.GetRequiredSection("IdentityServer:TestIdentities").GetRequiredSection(identityKey); //.Bind(new { Username = "", Password = "" });
        
        var username = identitySection.GetValue<string?>("Username") ?? throw new Exception("Configuration IdentityServer.TestIdentities.SaleAdmin.Username not found");
        var password = identitySection.GetValue<string?>("Password") ?? throw new Exception("Configuration IdentityServer.TestIdentities.SaleAdmin.Password not found");
        
        return await GetIdentityAccessToken(username, password);
    }

    internal async Task<string> GetIdentityAccessToken(string username, string password)
    {
        var configuration = this.Factory.Services.GetService<IConfiguration>()
                            ?? throw new Exception("IConfiguration missing");
        
        string clientId = configuration.GetRequiredSection("IdentityServer").GetValue<string>("ClientId")
                          ?? throw new Exception("Configuration IdentityServer.ClientId not found");
        
        string clientSecret = configuration.GetRequiredSection("IdentityServer").GetValue<string>("ClientSecret")
                              ?? throw new Exception("Configuration IdentityServer.ClientSecret not found");
        
        string connectUriPrefix = configuration.GetRequiredSection("IdentityServer").GetValue<string>("ConnectUriPrefix")
                                  ?? throw new Exception("Configuration IdentityServer.ConnectUriPrefix not found");
        
        
        return await GetIdentityAccessToken(clientId, clientSecret, username, password, connectUriPrefix);
    }

    internal async Task<string> GetIdentityAccessToken(string clientId, string clientSecret, string username, string password, string connectUriPrefix)
    {
        var client = new HttpClient();
        client.DefaultRequestHeaders
            .Accept
            .Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
        
        var content = new StringContent(
            $"grant_type=password&" +
            $"client_id={Uri.EscapeDataString(clientId)}&" +
            $"client_secret={Uri.EscapeDataString(clientSecret)}&" +
            $"username={Uri.EscapeDataString(username)}&" +
            $"password={Uri.EscapeDataString(password)}",
            System.Text.Encoding.UTF8, "application/x-www-form-urlencoded");

        HttpResponseMessage responseMessage = await client.PostAsync($"{connectUriPrefix}connect/token", content);
        string reponseString = await responseMessage.Content.ReadAsStringAsync();
        Dictionary<string, object> responseData = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(reponseString)
            ?? throw new Exception($"Cannot deserialize response {reponseString}");

        if (!responseData.TryGetValue("access_token", out object? accessToken) || accessToken is null)
            throw new Exception($"Cannot find access_token in response {reponseString}");

        return accessToken.ToString()
            ?? throw new Exception($"Empty access_token in response {reponseString}");
    }

    internal async Task<Tresponse?> SendApiRequest<Trequest, Tresponse>(ITestOutputHelper output, HttpMethod httpMethod, string endpoint, Trequest request, string? accessToken = null)
    {
        HttpRequestMessage requestMessage = new HttpRequestMessage(httpMethod, endpoint);

        if (!String.IsNullOrEmpty(accessToken))
            requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        if (typeof(Trequest) != typeof(NoRequest))
        {
            var content = JsonContent.Create(request, options: JsonSerializerOptions);
            requestMessage.Content = content;
            string? requestString = await content.ReadAsStringAsync();
            output.WriteLine($"API request {httpMethod} {endpoint}: {requestString}");
        }

        HttpResponseMessage responseMessage = await Client.SendAsync(requestMessage);
        string responseString = await responseMessage.Content.ReadAsStringAsync();
        if (!responseMessage.IsSuccessStatusCode)
        {
            output.WriteLine($"FAILED API request {httpMethod} {endpoint}: {responseMessage.StatusCode}, " +
                $"reason: {responseMessage.ReasonPhrase}, response: {responseString}");
            return default;
        }

        output.WriteLine($"API response {httpMethod} {endpoint}: {responseString}");

        Tresponse? result = JsonSerializer.Deserialize<Tresponse>(responseString, options: JsonSerializerOptions);

        return result;
    }
}
internal record NoRequest
{
    public static NoRequest NoValue = new NoRequest();
}