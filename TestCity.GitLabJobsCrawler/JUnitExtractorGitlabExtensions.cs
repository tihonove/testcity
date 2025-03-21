using System.IO.Compression;

namespace Kontur.TestCity.GitLabJobsCrawler;

public static class JUnitExtractorGitlabExtensions
{
    public static TestReportData? TryExtractTestRunsFromGitlabArtifact(this JUnitExtractor jUnitExtractor, byte[] artifactContent)
    {
        var result = TestReportData.CreateEmpty();
        using (var zipStream = new MemoryStream(artifactContent))
        using (var archive = new ZipArchive(zipStream))
        {
            foreach (var entry in archive.Entries.Where(entry => entry.FullName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase)))
            {
                using var stream = entry.Open();
                using var memoryStream = new MemoryStream();
                stream.CopyTo(memoryStream);
                memoryStream.Position = 0;
                using var reader = new StreamReader(memoryStream);
                var content = reader.ReadToEnd();

                if (content.Contains("<testsuite"))
                {
                    memoryStream.Position = 0;
                    result = result.Merge(jUnitExtractor.CollectTestRunsFromJunit(memoryStream));
                }
            }
        }
        if (result.Counters.Total == 0)
        {
            return null;
        }
        return result;
    }
}
