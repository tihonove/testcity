namespace Kontur.TestCity.UnitTests.Utils;

internal static class TestFilesAccessor
{
    private static string? FindFilePathUpwards(string relativePath, string startDirectory)
    {
        var baseDir = startDirectory;
        var directory = new DirectoryInfo(baseDir);

        while (directory != null)
        {
            var candidate = Path.Combine(directory.FullName, relativePath);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        return null;
    }

    private static string? FindDirectoryPathUpwards(string relativePath, string startDirectory)
    {
        var baseDir = startDirectory;
        var directory = new DirectoryInfo(baseDir);

        while (directory != null)
        {
            var candidate = Path.Combine(directory.FullName, relativePath);
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        return null;
    }

    public static TestFile GetTestFile(string path)
    {
        var fullPath =
            FindFilePathUpwards(path, Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? Environment.CurrentDirectory) ??
            FindFilePathUpwards(path, Environment.CurrentDirectory)
            ?? throw new Exception($"File '{path}' not found");
        return new TestFile(fullPath);
    }

    public static IEnumerable<TestFileReference> EnumerateTestFiles(string path, string mask)
    {
        var fullPath =
            FindDirectoryPathUpwards(path, Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? Environment.CurrentDirectory) ??
            FindDirectoryPathUpwards(path, Environment.CurrentDirectory)
            ?? throw new Exception($"File '{path}' not found");
        return Directory.EnumerateFiles(fullPath, mask).Select(file => new TestFileReference(file));
    }
}
