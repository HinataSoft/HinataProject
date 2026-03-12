using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VoucherService.Tests.Shared.DatabaseStrategies;
using Xunit.Abstractions;

namespace VoucherService.Tests.Shared.Fixtures.Common;

public abstract class DatabaseAppWebApplicationTestFixture<T> : XUnitWebApplicationTestFixture<T> where T : class
{
    private IDatabaseSetupStrategy? databaseSetupStrategy;

    public DatabaseAppWebApplicationTestFixture(IMessageSink messageSink) : base(messageSink)
    {
    }

    public abstract IDatabaseSetupStrategy GetStrategy(IConfiguration configuration);

    protected override async Task OnInitializeAsync()
    {
        await base.OnInitializeAsync();
        
        var configuration = this.Factory?.Services?.GetService<IConfiguration>() ??
                            new ConfigurationManager().AddEnvironmentVariables().Build();
        
        databaseSetupStrategy = GetStrategy(configuration);
        await databaseSetupStrategy.InitializeAsync(configuration);
    }
    
    protected override async Task OnDisposeAsync()
    {
        await base.OnDisposeAsync();

        if (databaseSetupStrategy is not null)
            await databaseSetupStrategy.DisposeAsync();
    }

    protected override void ConfigureAppConfiguration(IConfigurationBuilder configurationManager)
    {
        base.ConfigureAppConfiguration(configurationManager);

        if (databaseSetupStrategy is null)
            throw new Exception("Database setup strategy cannot be null");
        
        configurationManager.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DbContext"] = databaseSetupStrategy.GetConnectionString()
        });
    }
}