using NUnit.Framework;

namespace TestCity.UnitTests.Utils;

public static class CIUtils
{
    public static void SkipOnGitHubActions()
    {
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GITHUB_ACTIONS")))
            Assert.Ignore("Skip on github actions");
    }
}
