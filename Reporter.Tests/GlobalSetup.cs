using dotenv.net;
using Microsoft.Extensions.Logging;
using NUnit.Framework;

namespace Kontur.TestAnalytics.Reporter.Tests;

[SetUpFixture]
public class GlobalSetup
{
    private static ILoggerFactory? loggerFactory;

    [OneTimeSetUp]
    public void LoadEnv()
    {
        DotEnv.Fluent().WithProbeForEnv(10).Load();
    }

    public static ILoggerFactory TestLoggerFactory
    {
        get
        {
            loggerFactory ??= LoggerFactory.Create(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug));
            return loggerFactory;
        }
    }
}
