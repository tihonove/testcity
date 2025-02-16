using NGitLab.Models;
using Vostok.Hosting.Abstractions;
using Vostok.Metrics;
using Vostok.Metrics.Models;

namespace Kontur.TestAnalytics.GitLabJobsCrawler;

public class TestMetricsSender
{
    private readonly IMetricContext rootMetricContext;

    public TestMetricsSender(IVostokApplicationMetrics metrics)
    {
        rootMetricContext = metrics.Root;
    }

    public void Send(Project project, string refId, Job job, TestReportData data)
    {
        var tags = new[]
        {
            ("component", "TestMetricsSender"),
            ("namespace", project.Namespace.Name),
            ("project", project.Name),
            ("ref", refId),
            ("job", job.Name),
        };

        rootMetricContext.Send(new MetricDataPoint(data.Counters.Total, "TotalTests", tags));
        rootMetricContext.Send(new MetricDataPoint(data.Counters.Failed, "FailedTests", tags));
        rootMetricContext.Send(new MetricDataPoint(data.Counters.Skipped, "SkippedTests", tags));
        rootMetricContext.Send(new MetricDataPoint(data.Counters.Success, "SuccessTests", tags));
        rootMetricContext.Send(new MetricDataPoint(data.Runs.Sum(x => x.Duration), "TestSumDuration", tags));

        if (job.Duration.HasValue)
        {
            rootMetricContext.Send(new MetricDataPoint(job.Duration.Value, "JobDuration", tags));
        }

        if (job.Coverage.HasValue)
        {
            rootMetricContext.Send(new MetricDataPoint(job.Coverage.Value, "Coverage", tags));
        }

        if (job.Artifacts != null)
        {
            rootMetricContext.Send(new MetricDataPoint(job.Artifacts.Size, "ArtifactSize", tags));
        }

        if (job.QueuedDuration.HasValue)
        {
            rootMetricContext.Send(new MetricDataPoint(job.QueuedDuration.Value, "QueuedDuration", tags));
        }
    }
}
