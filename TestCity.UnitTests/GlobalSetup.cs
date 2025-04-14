using dotenv.net;
using Kontur.TestCity.Core.Logging;
using Kontur.TestCity.UnitTests.Utils;
using Microsoft.Extensions.Logging;
using NUnit.Framework;

namespace Kontur.TestCity.UnitTests;

[SetUpFixture]
public class GlobalSetup
{
    private static ILoggerFactory? loggerFactory;

    [OneTimeSetUp]
    public void LoadEnv()
    {
        DotEnv.Fluent().WithProbeForEnv(10).Load();
        Log.ConfigureGlobalLogProvider(TestLoggerFactory);
    }

    public static ILoggerFactory TestLoggerFactory
    {
        get
        {
            loggerFactory ??= LoggerFactory.Create(builder => builder.AddProvider(new NUnitLoggerProvider()).SetMinimumLevel(LogLevel.Debug));
            return loggerFactory;
        }
    }
}
