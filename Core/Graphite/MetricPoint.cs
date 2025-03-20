namespace Kontur.TestAnalytics.Core.Graphite;

public class MetricPoint(string name, double value, Dictionary<string, string>? tags = null, DateTime? timestamp = null)
{
    public string Name { get; } = name;
    public double Value { get; } = value;
    public Dictionary<string, string> Tags { get; } = tags ?? new Dictionary<string, string>();
    public DateTime? Timestamp { get; } = timestamp;
}
