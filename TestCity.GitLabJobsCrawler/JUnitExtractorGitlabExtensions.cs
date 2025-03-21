using System.IO.Compression;

namespace Kontur.TestCity.GitLabJobsCrawler;

public static class JUnitExtractorGitlabExtensions
{
    public static TestReportData? TryExtractTestRunsFromGitlabArtifact(this JUnitExtractor jUnitExtractor, byte[] artifactContent)
    {
        using (var zipStream = new MemoryStream(artifactContent))
        using (var archive = new ZipArchive(zipStream))
        {
            var xmlEntries = archive.Entries
                .Where(entry => entry.FullName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (xmlEntries.Count == 0)
            {
                return null;
            }

            IEnumerable<Stream> GetXmlStreams()
            {
                foreach (var entry in xmlEntries)
                {
                    using var stream = entry.Open();
                    using var reader = new StreamReader(stream);
                    var content = reader.ReadToEnd();

                    if (content.Contains("<testsuite"))
                    {
                        var contentStream = new MemoryStream();
                        using (var writer = new StreamWriter(contentStream, leaveOpen: true))
                        {
                            writer.Write(content);
                        }
                        contentStream.Position = 0;
                        yield return contentStream;

                        // Это глязный костыль, надо переписать
                        contentStream.Dispose();
                    }
                }
            }

            var result = jUnitExtractor.CollectTestsFromStreams(GetXmlStreams());
            if (result.counter.Total == 0)
            {
                return null;
            }
            return new TestReportData(result.counter, result.runs);
        }
    }
}
