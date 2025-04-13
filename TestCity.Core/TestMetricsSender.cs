using System.Diagnostics.Metrics;
using Kontur.TestCity.Core.GitLab.Models;
using Kontur.TestCity.Core.Graphite;
using Microsoft.Extensions.Logging;
using NGitLab.Models;

namespace Kontur.TestCity.GitLabJobsCrawler;

public sealed class TestMetricsSender
{
    public TestMetricsSender(IGraphiteClient graphiteClient, ILogger<TestMetricsSender> logger)
    {
        this.graphiteClient = graphiteClient;
        this.logger = logger;
    }

    public async Task SendAsync(Project project, string refId, GitLabJob job, TestReportData data)
    {
        try
        {
            var tags = new Dictionary<string, string>
            {
                { "project", "TestAnalytics" },
                { "tc-namespace", project.Namespace.Name },
                { "tc-project", project.Name },
                { "tc-ref", refId },
                { "tc-job", job.Name },
            };

            var metrics = new List<MetricPoint>
            {
                new ("TotalTests", data.Counters.Total, tags),
                new ("FailedTests", data.Counters.Failed, tags),
                new ("SkippedTests", data.Counters.Skipped, tags),
                new ("SuccessTests", data.Counters.Success, tags),
                new ("TestSumDuration", data.Runs.Sum(x => x.Duration), tags),
            };

            if (job.Duration.HasValue)
            {
                metrics.Add(new MetricPoint("JobDuration", job.Duration.Value, tags));
            }

            if (job.Coverage.HasValue)
            {
                metrics.Add(new MetricPoint("Coverage", job.Coverage.Value, tags));
            }

            if (job.Artifacts != null)
            {
                metrics.Add(new MetricPoint("ArtifactSize", job.ArtifactsFile?.Size ?? job.Artifacts?.Sum(x => x.Size) ?? 0, tags));
            }

            if (job.QueuedDuration.HasValue)
            {
                metrics.Add(new MetricPoint("QueuedDuration", job.QueuedDuration.Value, tags));
            }

            await graphiteClient.SendAsync(metrics);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error while sending metrics: {ErrorMessage}", ex.Message);
        }
    }

    private readonly IGraphiteClient graphiteClient;
    private readonly ILogger<TestMetricsSender> logger;
}
