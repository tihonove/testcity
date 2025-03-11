using System.Text;

namespace Kontur.TestAnalytics.Front;

public static class TestAnalyticsFrontContent
{
    private static string ResourcesPrefix => typeof(TestAnalyticsFrontContent).Namespace + ".dist";

    public static IEnumerable<string> EnumerateFiles()
    {
        return typeof(TestAnalyticsFrontContent).Assembly.GetManifestResourceNames().Select(x => x.Replace(ResourcesPrefix + ".", string.Empty));
    }

    public static Stream GetFile(string relativePath)
    {
        return typeof(TestAnalyticsFrontContent).Assembly.GetManifestResourceStream(
            ResourcesPrefix + "." + relativePath.Replace("/", ".").Replace("\\", "."))
        ?? throw new Exception($"Content file not found '{relativePath}'");
    }

    public static string GetFileContent(string relativePath)
    {
        using var stream = GetFile(relativePath);
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return reader.ReadToEnd();
    }
}
