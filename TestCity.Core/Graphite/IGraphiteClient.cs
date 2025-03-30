namespace Kontur.TestCity.Core.Graphite;

public interface IGraphiteClient
{
    Task SendAsync(string metricPath, double value, DateTime? timestamp = null);
    Task SendAsync(MetricPoint metric);
    Task SendAsync(IEnumerable<MetricPoint> metrics);
}
