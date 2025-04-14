using Kontur.TestCity.Core.Graphite;
using NUnit.Framework;

namespace Kontur.TestCity.UnitTests.Graphite;

public class TestSendToGraphite
{
    [Test]
    public async Task TestSend()
    {
        var GraphiteHost = Environment.GetEnvironmentVariable("GRAPHITE_RELAY_HOST") ?? throw new InvalidOperationException("GRAPHITE_RELAY_HOST is not set");
        var GraphitePort = int.Parse(Environment.GetEnvironmentVariable("GRAPHITE_RELAY_PORT") ?? throw new InvalidOperationException("GRAPHITE_RELAY_PORT is not set"));
        var client = new GraphiteClient(GraphiteHost, GraphitePort);
        const string metricPath = "TestCount.TCP";
        const double value = 42.0;
        var timestamp = DateTime.UtcNow;

        await client.SendAsync(metricPath, value, timestamp);
    }

    [Test]
    public async Task TestSendWithTags()
    {
        var GraphiteHost = Environment.GetEnvironmentVariable("GRAPHITE_RELAY_HOST") ?? throw new InvalidOperationException("GRAPHITE_RELAY_HOST is not set");
        var GraphitePort = int.Parse(Environment.GetEnvironmentVariable("GRAPHITE_RELAY_PORT") ?? throw new InvalidOperationException("GRAPHITE_RELAY_PORT is not set"));
        // Arrange
        var client = new GraphiteClient(GraphiteHost, GraphitePort);
        var timestamp = DateTime.UtcNow;

        var metric = new MetricPoint(
            "TestCount",
            100,
            new Dictionary<string, string>
            {
                { "tc-project", "TestAnalytics" },
                { "tc-namespace", "TestAnalytics" },
                { "project", "TestAnalytics" },
                { "environment", "test" },
                { "protocol", "tcp" },
            },
            timestamp);

        // Act
        await client.SendAsync(metric);

        // Дополнительный тест - отправка нескольких метрик по TCP с тегами
        var metrics = new List<MetricPoint>
        {
            new ("test.analytics.batch.tcp.metric1", 1.1, new Dictionary<string, string> { { "type", "test" }, { "protocol", "tcp" } }, timestamp),
            new ("test.analytics.batch.tcp.metric2", 2.2, new Dictionary<string, string> { { "type", "prod" }, { "protocol", "tcp" } }, timestamp),
            new ("test.analytics.batch.tcp.metric3", 3.3, new Dictionary<string, string> { { "type", "dev" }, { "protocol", "tcp" } }, timestamp),
        };

        await client.SendAsync(metrics);
    }
}
