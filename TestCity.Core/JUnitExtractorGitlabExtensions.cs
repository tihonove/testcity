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
                    bool containsTestSuite = false;
                    var reader = new StreamReader(stream);
                    string? line;
                    int lineCount = 0;
                    const int maxLinesToCheck = 20;

                    while ((line = reader.ReadLine()) != null && lineCount < maxLinesToCheck)
                    {
                        lineCount++;
                        if (line.Contains("<testsuite"))
                        {
                            containsTestSuite = true;
                            break;
                        }
                    }
                    stream.Close();
                    using var stream2 = entry.Open();

                    if (containsTestSuite)
                    {
                        result = result.Merge(jUnitExtractor.CollectTestRunsFromJunit(stream2));
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
