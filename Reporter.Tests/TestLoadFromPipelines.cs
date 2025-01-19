using ClickHouse.Client.ADO;
using ClickHouse.Client.Copy;
using ClickHouse.Client.Utility;
using NUnit.Framework;
using NGitLab;
using System.IO.Compression;
using System.Threading.Tasks;
using System.Net.Mime;
using System.Reflection;

namespace Kontur.TestAnalytics.Reporter.Tests;

public class TestsLoadFromGitlab
{
    [Test]
    public async Task Test01()
    {
        var client = new GitLabClient("https://git.skbkontur.ru", "glpat-JpY7zGgBbJqpD5Vff9qd");
        // const int projectId = 17358;
        const int projectId = 182;
        IPipelineClient pipelineClient = client.GetPipelines(projectId);
        var pipelines = pipelineClient.All.Take(30).ToArray();

        foreach (var pipeline in pipelines)
        {
            var jobs = pipelineClient.GetJobsAsync(new NGitLab.Models.PipelineJobQuery { PipelineId = pipeline.Id }).ToArray();
            foreach (var job in jobs)
            {
                if (job.Artifacts != null)
                {
                    var artifactContents = client.GetJobs(projectId).GetJobArtifacts(job.Id);
                    var t = Path.Combine("/", "home", "tihonove", "workspace", "tmp", "Artifacts");
                    Directory.CreateDirectory(t);
                    string path = Path.Combine(t, $"{job.Id}_" + job.Artifacts.Filename);
                    Console.WriteLine(path);
                    File.WriteAllBytes(
                        path,
                        artifactContents
                    );

                    var extractor = new JUnitExtractor();
                    var r = extractor.TryExtractTestRunsFromGitlabArtifact(artifactContents);
                    if (r != null) {
                        Console.WriteLine($"Counters: {r.Counters.Total}");
                        foreach (var run in r.Runs)
                        {
                            Console.WriteLine($"Test Run: {run.TestId}");
                        }
                    }
                    // var artifacts = client.GetRepository(project.Id).Jobs.GetArtifactsAsync(job.Id).Result;
                    // var tempPath = Path.Combine(Path.GetTempPath(), job.Name);
                    // Directory.CreateDirectory(tempPath);
                    // var artifactPath = Path.Combine(tempPath, $"{job.Name}.zip");
                    // File.WriteAllBytes(artifactPath, artifacts);
                    // ZipFile.ExtractToDirectory(artifactPath, tempPath);
                }
            }
        }
    }
}

public record TestReportData(
    Cli.TestCount Counters,
    List<Client.TestRun> Runs
);

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
                                    .Where(file => File.ReadAllText(file).Contains("<testsuites"))
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
