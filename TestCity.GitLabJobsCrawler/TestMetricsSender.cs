using System.Diagnostics;
using System.Diagnostics.Metrics;
using NGitLab.Models;

namespace Kontur.TestCity.GitLabJobsCrawler;

public sealed class TestMetricsSender : IDisposable
{
    public TestMetricsSender()
    {
        this.meter = new Meter("GitLabProjectTestsMetrics");
        this.totalTestsCounter = this.meter.CreateCounter<long>("TotalTests", "tests", "Total number of tests");
        this.failedTestsCounter = this.meter.CreateCounter<long>("FailedTests", "tests", "Number of failed tests");
        this.skippedTestsCounter = this.meter.CreateCounter<long>("SkippedTests", "tests", "Number of skipped tests");
        this.successTestsCounter = this.meter.CreateCounter<long>("SuccessTests", "tests", "Number of successful tests");
        this.testSumDurationCounter = this.meter.CreateCounter<double>("TestSumDuration", "ms", "Sum of all test durations");
        this.jobDurationCounter = this.meter.CreateCounter<double>("JobDuration", "ms", "Duration of the job");
        this.coverageCounter = this.meter.CreateCounter<double>("Coverage", "%", "Code coverage percentage");
        this.artifactSizeCounter = this.meter.CreateCounter<double>("ArtifactSize", "bytes", "Size of artifacts");
        this.queuedDurationCounter = this.meter.CreateCounter<double>("QueuedDuration", "ms", "Time spent in queue");
    }

    public void Send(Project project, string refId, Job job, TestReportData data)
    {
        var tags = new TagList
        {
            { "tc-namespace", project.Namespace.Name },
            { "tc-project", project.Name },
            { "tc-ref", refId },
            { "tc-job", job.Name },
        };

        this.totalTestsCounter.Add(data.Counters.Total, tags);
        this.failedTestsCounter.Add(data.Counters.Failed, tags);
        this.skippedTestsCounter.Add(data.Counters.Skipped, tags);
        this.successTestsCounter.Add(data.Counters.Success, tags);
        this.testSumDurationCounter.Add(data.Runs.Sum(x => x.Duration), tags);

        if (job.Duration.HasValue)
        {
            this.jobDurationCounter.Add(job.Duration.Value, tags);
        }

        if (job.Coverage.HasValue)
        {
            this.coverageCounter.Add(job.Coverage.Value, tags);
        }

        if (job.Artifacts != null)
        {
            this.artifactSizeCounter.Add(job.Artifacts.Size, tags);
        }

        if (job.QueuedDuration.HasValue)
        {
            this.queuedDurationCounter.Add(job.QueuedDuration.Value, tags);
        }
    }

    private readonly Meter meter;
    private readonly Counter<long> totalTestsCounter;
    private readonly Counter<long> failedTestsCounter;
    private readonly Counter<long> skippedTestsCounter;
    private readonly Counter<long> successTestsCounter;
    private readonly Counter<double> testSumDurationCounter;
    private readonly Counter<double> jobDurationCounter;
    private readonly Counter<double> coverageCounter;
    private readonly Counter<double> artifactSizeCounter;
    private readonly Counter<double> queuedDurationCounter;

    public void Dispose()
    {
        this.meter.Dispose();
    }
}
