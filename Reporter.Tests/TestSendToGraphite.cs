using Kontur.TestAnalytics.Core.Graphite;
using NUnit.Framework;

namespace Kontur.TestAnalytics.Reporter.Tests;

[Explicit]
public class TestSendToGraphite
{
    private const string GraphiteHost = "graphite-relay.skbkontur.ru";
    private const int GraphitePort = 2003;

    [Test]
    public async Task TestSend()
    {
        var client = new GraphiteClient(GraphiteHost, GraphitePort);
        var metricPath = "TestCount.TCP";
        var value = 42.0;
        var timestamp = DateTime.UtcNow;

        await client.SendAsync(metricPath, value, timestamp);
    }

    [Test]
    public async Task TestSendWithTags()
    {
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
