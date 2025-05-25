using Xunit.Sdk;

namespace TestCity.UnitTests.Utils;

public static class CIUtils
{
    public static void SkipOnGitHubActions()
    {
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GITHUB_ACTIONS")))
            throw SkipException.ForSkip("Skip on github actions");
    }
}
