using System.IO.Compression;

namespace Kontur.TestCity.GitLabJobsCrawler;

public static class JUnitExtractorGitlabExtensions
{
    public static ArtifactsContentsInfo TryExtractTestRunsFromGitlabArtifact(this JUnitExtractor jUnitExtractor, byte[] artifactContent)
    {
        var result = TestReportData.CreateEmpty();
        var hasCodeQualityReport = false;
        using (var zipStream = new MemoryStream(artifactContent))
        using (var archive = new ZipArchive(zipStream))
        {
            foreach (var entry in archive.Entries)
            {
                if (entry.FullName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
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
                else if (entry.FullName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                {
                    using var stream = entry.Open();
                    using var reader = new StreamReader(stream);
                    var jsonFileContent = reader.ReadToEnd();
                    hasCodeQualityReport = jsonFileContent.Contains("\"fingerprint\"") && jsonFileContent.Contains("\"check_name\"") && jsonFileContent.Contains("\"severity\"");
                }
            }
        }

        return new ArtifactsContentsInfo
        {
            TestReportData = result.Counters.Total == 0 ? null : result,
            HasCodeQualityReport = hasCodeQualityReport,
        };
    }
}
