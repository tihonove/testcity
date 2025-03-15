using System.IO.Compression;

namespace Kontur.TestCity.GitLabJobsCrawler;

public static class JUnitExtractorGitlabExtensions
{
    public static TestReportData? TryExtractTestRunsFromGitlabArtifact(this JUnitExtractor jUnitExtractor, byte[] artifactContent)
    {
        var tempPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempPath);

        try
        {
            using (var zipStream = new MemoryStream(artifactContent))
            using (var archive = new ZipArchive(zipStream))
            {
                archive.ExtractToDirectory(tempPath);
            }

            var xmlFiles = Directory.EnumerateFiles(tempPath, "*.xml", SearchOption.AllDirectories)
                                    .Where(file => File.ReadAllText(file).Contains("<testsuite"))
                                    .ToList();

            if (xmlFiles.Count == 0)
            {
                return null;
            }

            var result = jUnitExtractor.CollectTestsFromReports(xmlFiles);
            return new TestReportData(result.counter, result.runs);
        }
        finally
        {
            Directory.Delete(tempPath, true);
        }
    }
}
