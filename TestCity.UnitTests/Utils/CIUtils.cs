namespace TestCity.UnitTests.Utils;

public static class CIUtils
{
    public static bool IsGitHubActions()
    {
        return !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GITHUB_ACTIONS"));
    }
}
