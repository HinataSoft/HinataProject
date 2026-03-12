using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace VoucherService.Tests.Shared.DatabaseStrategies;

public class PostgresDatabaseStrategy : IDatabaseSetupStrategy
{
    private IDatabaseSetupStrategy databaseSetupStrategy = null!;

    public async Task InitializeAsync(IConfiguration configuration)
    {
        var useDbDirectly = configuration.GetValue<bool>("TESTS_USE_DB_DIRECTLY", false);

        databaseSetupStrategy =
            useDbDirectly ? new RandomPostgresDatabaseStrategy() : new PostgreSqlContainerStrategy();

        await databaseSetupStrategy.InitializeAsync(configuration);
    }

    public async Task DisposeAsync()
    {
        await databaseSetupStrategy.DisposeAsync();
    }

    public string GetConnectionString()
    {
        return databaseSetupStrategy.GetConnectionString();
    }
}