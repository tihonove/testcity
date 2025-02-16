using dotenv.net;
using NUnit.Framework;

namespace Kontur.TestAnalytics.Reporter.Tests;

[SetUpFixture]
public class GlobalSetup
{
    [OneTimeSetUp]
    public void LoadEnv()
    {
        DotEnv.Fluent().WithProbeForEnv(10).Load();
    }
}
