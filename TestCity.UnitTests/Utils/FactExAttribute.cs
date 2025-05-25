using dotenv.net;
using Xunit;

namespace TestCity.UnitTests.Utils;

public class FactExAttribute : FactAttribute
{
    public bool Explicit { get; set; }

    public bool SkipOnCI { get; set; }

    public FactExAttribute()
    {
        if (Explicit)
        {
            DotEnv.Fluent().WithProbeForEnv(10).Load();
            if (Environment.GetEnvironmentVariable("RUN_EXPLICIT_TESTS") != "1")
                Skip = "Explicit test, run with RUN_EXPLICIT_TESTS=1";
        }
        if (SkipOnCI)
        {
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GITHUB_ACTIONS")))
                Skip = "Skip on github actions";
        }
    }
}
