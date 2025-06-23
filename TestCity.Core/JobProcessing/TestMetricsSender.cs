using TestCity.Core.GitLab.Models;
using TestCity.Core.Graphite;
using TestCity.Core.JUnit;
using TestCity.Core.Logging;
using Microsoft.Extensions.Logging;
using NGitLab.Models;

namespace TestCity.Core.JobProcessing;

public sealed class TestMetricsSender(IGraphiteClient graphiteClient)
{
    public async Task SendAsync(Project project, string? refId, GitLabJob job, TestReportData? data)
    {
        try
        {
            var tags = new Dictionary<string, string>
            {
                { "project", "TestAnalytics" },
                { "tc-namespace", project.Namespace.Name },
                { "tc-project", project.Name },
                { "tc-ref", refId ?? "<empty>" },
                { "tc-job", job.Name }
            };

            var dateTime = job.FinishedAt;

            var metrics = new List<MetricPoint>();
            if (data != null)
            {
                metrics.Add(new MetricPoint("TotalTests", data.Counters.Total, tags, dateTime));
                metrics.Add(new MetricPoint("FailedTests", data.Counters.Failed, tags, dateTime));
                metrics.Add(new MetricPoint("SkippedTests", data.Counters.Skipped, tags, dateTime));
                metrics.Add(new MetricPoint("SuccessTests", data.Counters.Success, tags, dateTime));
                metrics.Add(new MetricPoint("TestSumDuration", data.Runs.Sum(x => x.Duration), tags, dateTime));
            }

            if (job.Duration.HasValue)
            {
                metrics.Add(new MetricPoint("JobDuration", job.Duration.Value, tags, dateTime));
            }

            if (job.Coverage.HasValue)
            {
                metrics.Add(new MetricPoint("Coverage", job.Coverage.Value, tags, dateTime));
            }

            if (job.Artifacts != null)
            {
                metrics.Add(new MetricPoint("ArtifactSize",
                    job.ArtifactsFile?.Size ?? job.Artifacts?.Sum(x => x.Size) ?? 0, tags, dateTime));
            }

            if (job.QueuedDuration.HasValue)
            {
                metrics.Add(new MetricPoint("QueuedDuration", job.QueuedDuration.Value, tags, dateTime));
            }

            await graphiteClient.SendAsync(metrics);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error while sending metrics: {ErrorMessage}", ex.Message);
        }
    }

    private readonly ILogger logger = Log.GetLog<TestMetricsSender>();
}
