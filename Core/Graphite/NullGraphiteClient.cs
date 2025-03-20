namespace Kontur.TestAnalytics.Core.Graphite;

public class NullGraphiteClient : IGraphiteClient
{
    public Task SendAsync(string metricPath, double value, DateTime? timestamp = null)
    {
        return Task.CompletedTask;
    }

    public Task SendAsync(MetricPoint metric)
    {
        return Task.CompletedTask;
    }

    public Task SendAsync(IEnumerable<MetricPoint> metrics)
    {
        return Task.CompletedTask;
    }
}
