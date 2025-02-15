namespace Kontur.TestAnalytics.Reporter.Cli;

public static class ReportPathResolver
{
    public static IEnumerable<string> GetReportPaths(IEnumerable<string> pathTemplates)
    {
        foreach (var path in pathTemplates.SelectMany(ResolveDirectoriesByPattern))
        {
            var root = Path.GetPathRoot(path);
            foreach (var filePath in Directory.GetFiles(root ?? ".", path[(root?.Length ?? 0) ..]))
            {
                yield return filePath;
            }
        }
    }

    private static IEnumerable<string> ResolveDirectoriesByPattern(string template)
    {
        var splitResult = template.Split("**/", 2);
        if (splitResult.Length == 1 || !Directory.Exists(splitResult[0]))
        {
            return Directory.Exists(Path.GetDirectoryName(template)) ? new[] { template } : Array.Empty<string>();
        }

        var dirs = Directory.GetDirectories(splitResult[0], "*", SearchOption.TopDirectoryOnly);
        var combinedDirs = dirs.Select(d => Path.Combine(d, splitResult[1])).ToList();

        return combinedDirs.SelectMany(ResolveDirectoriesByPattern);
    }
}
