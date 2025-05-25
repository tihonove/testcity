using dotenv.net;
using TestCity.Core.Logging;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;
using TestCity.UnitTests.Utils;

namespace TestCity.UnitTests;

public sealed class GlobalSetup
{
    public GlobalSetup()
    {
        DotEnv.Fluent().WithProbeForEnv(10).Load();
    }

    public static ILoggerFactory TestLoggerFactory(ITestOutputHelper output)
    {
        XUnitLoggerProvider.ConfigureTestLogger(output);
        return Log.LoggerFactory;
    }
}

[CollectionDefinition("Global")]
public class GlobalCollection : ICollectionFixture<GlobalSetup>
{
}
