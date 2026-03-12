using Microsoft.Extensions.Configuration;
using Testcontainers.PostgreSql;

namespace VoucherService.Tests.Shared.DatabaseStrategies;

public class PostgreSqlContainerStrategy : IDatabaseSetupStrategy
{
    private readonly PostgreSqlContainer postgreSqlContainer = new PostgreSqlBuilder().Build();

    public async Task InitializeAsync(IConfiguration configuration)
    {
        await postgreSqlContainer.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await postgreSqlContainer.StopAsync();
    }

    public string GetConnectionString()
    {
        return postgreSqlContainer.GetConnectionString();
    }
}