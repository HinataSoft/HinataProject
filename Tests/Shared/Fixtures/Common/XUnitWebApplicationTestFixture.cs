using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace VoucherService.Tests.Shared.Fixtures.Common;

public abstract class XUnitWebApplicationTestFixture<TStartup> : WebApplicationTestFixture<TStartup> where TStartup : class
{
    public readonly IMessageSink MessageSink;

    protected XUnitWebApplicationTestFixture(IMessageSink messageSink)
    {
        MessageSink = messageSink;
    }

    protected override void ConfigureLogging(ILoggingBuilder logging)
    {
        base.ConfigureLogging(logging);
        if (MessageSink != null)
            logging.AddXUnit(MessageSink);
    }
        
        
    protected override void ConfigureAppConfiguration(IConfigurationBuilder configurationManager)
    {
        if (!Directory.Exists("TestsConfiguration"))
            throw new DirectoryNotFoundException("Configuration directory for tests not found");

        foreach (var file in Directory.GetFiles("TestsConfiguration", "*.json", SearchOption.AllDirectories))
            configurationManager.AddJsonFile(file);
        
        configurationManager.AddEnvironmentVariables();
    }
}