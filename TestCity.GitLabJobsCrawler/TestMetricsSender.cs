using System.Diagnostics.Metrics;
using Kontur.TestAnalytics.Core.Graphite;
using NGitLab.Models;

namespace Kontur.TestCity.GitLabJobsCrawler;

public sealed class TestMetricsSender : IDisposable
{
    public TestMetricsSender(IGraphiteClient graphiteClient, ILogger<TestMetricsSender> logger)
    {
        this.graphiteClient = graphiteClient;
        this.logger = logger;
        this.meter = new Meter("GitLabProjectTestsMetrics");
    }

    public async Task SendAsync(Project project, string refId, Job job, TestReportData data)
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
                metrics.Add(new MetricPoint("ArtifactSize", job.Artifacts.Size, tags));
            }

            if (job.QueuedDuration.HasValue)
            {
                metrics.Add(new MetricPoint("QueuedDuration", job.QueuedDuration.Value, tags));
            }

            await this.graphiteClient.SendAsync(metrics);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error while sending metrics: {ErrorMessage}", ex.Message);
        }
    }

    public void Dispose()
    {
        this.meter.Dispose();
    }

    private readonly IGraphiteClient graphiteClient;
    private readonly Meter meter;
    private readonly ILogger<TestMetricsSender> logger;
}
