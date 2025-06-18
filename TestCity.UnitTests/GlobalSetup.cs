using dotenv.net;
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
        return XUnitLoggerProvider.ConfigureTestLogger(output);
    }
}

[CollectionDefinition("Global")]
public class GlobalCollection : ICollectionFixture<GlobalSetup>
{
}
