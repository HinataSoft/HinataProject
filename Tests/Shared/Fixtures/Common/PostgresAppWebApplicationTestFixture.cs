using Microsoft.Extensions.Configuration;
using VoucherService.Tests.Shared.DatabaseStrategies;
using Xunit.Abstractions;

namespace VoucherService.Tests.Shared.Fixtures.Common;

public class PostgresAppWebApplicationTestFixture<T>(IMessageSink messageSink)
    : DatabaseAppWebApplicationTestFixture<T>(messageSink) where T : class
{
    public override IDatabaseSetupStrategy GetStrategy(IConfiguration configuration) => new PostgresDatabaseStrategy();
}