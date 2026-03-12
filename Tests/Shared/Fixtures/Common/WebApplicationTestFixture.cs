using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace VoucherService.Tests.Shared.Fixtures.Common
{
    public abstract class WebApplicationTestFixture<TStartup> : IAsyncLifetime where TStartup : class
    {
        public HttpClient Client { get; private set; } = null!;
        public WebApplicationFactory<TStartup> Factory { get; private set; } = null!;

        public async Task InitializeAsync()
        {
            await OnInitializeAsync();
            Factory = CreateWebApplicationFactory();
            await ConfigureHostAsync();
            Client = Factory.CreateClient(GetWebApplicationFactoryClientOptions());
            await SetupTestDataAsync();
        }

        public async Task DisposeAsync()
        {
            await CleanupTestDataAsync();
            Client?.Dispose();
            await Factory.DisposeAsync();
            await OnDisposeAsync();
        }

        protected abstract void ConfigureAppConfiguration(IConfigurationBuilder configurationManager);
        protected virtual Task ConfigureTestServicesAsync(IServiceCollection services) => Task.CompletedTask;
        protected virtual Task ConfigureHostAsync() => Task.CompletedTask;
        protected virtual Task SetupTestDataAsync() => Task.CompletedTask;
        protected virtual Task CleanupTestDataAsync() => Task.CompletedTask;
        protected virtual Task OnInitializeAsync() => Task.CompletedTask;
        protected virtual Task OnDisposeAsync() => Task.CompletedTask;

        private WebApplicationFactory<TStartup> CreateWebApplicationFactory()
        {
            return new WebApplicationFactory<TStartup>().WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                var configBuilder = new ConfigurationBuilder();
                ConfigureAppConfiguration(configBuilder);
                builder.UseConfiguration(configBuilder.Build());
                builder.ConfigureLogging(ConfigureLogging);
                builder.ConfigureTestServices(async services => await ConfigureTestServicesAsync(services));
            });
        }

        protected virtual void ConfigureLogging(ILoggingBuilder logging)
        {
            logging.ClearProviders();
        }

        protected virtual WebApplicationFactoryClientOptions GetWebApplicationFactoryClientOptions()
        {
            return new WebApplicationFactoryClientOptions
            {
                BaseAddress = Factory.ClientOptions.BaseAddress,
                HandleCookies = Factory.ClientOptions.HandleCookies,
                AllowAutoRedirect = false,
                MaxAutomaticRedirections = Factory.ClientOptions.MaxAutomaticRedirections
            };
        }
    }
}