namespace Kontur.TestAnalytics.Reporter.Cli;

public static class JUnitReportHelper
{
    public static string RemoveDuplicatePartInClassName(string className, string testCaseName)
    {
        var startIndex = -1;

        for (var i = 1; i < testCaseName.Length; i++)
        {
            if (testCaseName[i] == '.')
            {
                break;
            }

            var currentStartIndex = className.LastIndexOf(testCaseName[..i], StringComparison.Ordinal);

            if (currentStartIndex == -1)
            {
                break;
            }

            startIndex = currentStartIndex;
        }

        return startIndex == -1 ? className : className[..startIndex];
    }
}
