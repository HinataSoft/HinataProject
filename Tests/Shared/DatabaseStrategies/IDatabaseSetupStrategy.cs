using Microsoft.Extensions.Configuration;

namespace VoucherService.Tests.Shared.DatabaseStrategies;

public interface IDatabaseSetupStrategy
{
    Task InitializeAsync(IConfiguration configuration);
    Task DisposeAsync();
    string GetConnectionString();
}