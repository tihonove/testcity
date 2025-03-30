using System.Net.Sockets;
using System.Text;

namespace Kontur.TestCity.Core.Graphite;

public class GraphiteClient(string host, int port = 2003) : IGraphiteClient
{
    public async Task SendAsync(string metricPath, double value, DateTime? timestamp = null)
    {
        using var tcpClient = new TcpClient();
        await tcpClient.ConnectAsync(host, port);

        using var stream = tcpClient.GetStream();
        using var writer = new StreamWriter(stream, Encoding.ASCII) { AutoFlush = true };

        var timestampEpoch = (timestamp ?? DateTime.UtcNow).ToEpochTime();
        var message = $"{metricPath} {value} {timestampEpoch}\n";

        await writer.WriteAsync(message);
    }

    public async Task SendAsync(MetricPoint metric)
    {
        using var tcpClient = new TcpClient();
        await tcpClient.ConnectAsync(host, port);

        using var stream = tcpClient.GetStream();
        using var writer = new StreamWriter(stream, Encoding.ASCII) { AutoFlush = true };

        var metricPath = FormatMetricPath(metric);
        var timestampEpoch = (metric.Timestamp ?? DateTime.UtcNow).ToEpochTime();
        var message = $"{metricPath} {metric.Value} {timestampEpoch}\n";

        await writer.WriteAsync(message);
    }

    public async Task SendAsync(IEnumerable<MetricPoint> metrics)
    {
        using var tcpClient = new TcpClient();
        await tcpClient.ConnectAsync(host, port);

        using var stream = tcpClient.GetStream();
        using var writer = new StreamWriter(stream, Encoding.ASCII) { AutoFlush = true };

        foreach (var metric in metrics)
        {
            var metricPath = FormatMetricPath(metric);
            var timestampEpoch = (metric.Timestamp ?? DateTime.UtcNow).ToEpochTime();
            var message = $"{metricPath} {metric.Value} {timestampEpoch}\n";
            await writer.WriteAsync(message);
        }
    }

    private static string FormatMetricPath(MetricPoint metric)
    {
        if (metric.Tags == null || metric.Tags.Count == 0)
        {
            return metric.Name;
        }

        var tags = string.Join(";", metric.Tags.Select(kv => $"{kv.Key}={kv.Value}"));
        return $"{metric.Name};{tags}";
    }

    private readonly string host = host;
    private readonly int port = port;
}
