using System.Globalization;
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Kontur.TestCity.Core.KafkaMessageQueue;

namespace Kontur.TestCity.Core.Worker;

public static class KafkaTopicActualizer
{
    public static async Task EnsureKafkaTopicExists()
    {
        var settings = KafkaConsumerSettings.Default;

        Console.WriteLine($"Waiting for Kafka to be available at {settings.BootstrapServers}...");

        await WaitForKafkaAvailability(settings.BootstrapServers, TimeSpan.FromMinutes(5), TimeSpan.FromSeconds(10));

        Console.WriteLine($"Verifying Kafka topics: {settings.TasksTopic}, {settings.DelayedTasksTopic}");
        using var adminClient = new AdminClientBuilder(new AdminClientConfig
        {
            BootstrapServers = settings.BootstrapServers,
        }).Build();

        try
        {
            var metadata = adminClient.GetMetadata(TimeSpan.FromSeconds(10));

            if (!metadata.Topics.Any(t => t.Topic == settings.TasksTopic))
            {
                Console.WriteLine($"Creating Kafka topic: {settings.TasksTopic}");

                await adminClient.CreateTopicsAsync(
                [
                    new TopicSpecification
                    {
                        Name = settings.TasksTopic,
                        ReplicationFactor = 1,
                        NumPartitions = 16,
                        Configs = new Dictionary<string, string>
                        {
                            { "retention.ms", TimeSpan.FromDays(14).TotalMilliseconds.ToString(CultureInfo.InvariantCulture) },
                        },
                    },
                ]);
                Console.WriteLine($"Kafka topic '{settings.TasksTopic}' created successfully with 14 days retention period");
            }
            if (!metadata.Topics.Any(t => t.Topic == settings.DelayedTasksTopic))
            {
                Console.WriteLine($"Creating Kafka topic: {settings.DelayedTasksTopic}");

                await adminClient.CreateTopicsAsync(
                [
                    new TopicSpecification
                    {
                        Name = settings.DelayedTasksTopic,
                        ReplicationFactor = 1,
                        NumPartitions = 16,
                        Configs = new Dictionary<string, string>
                        {
                            { "retention.ms", TimeSpan.FromDays(60).TotalMilliseconds.ToString(CultureInfo.InvariantCulture) },
                        },
                    },
                ]);
                Console.WriteLine($"Kafka topic '{settings.DelayedTasksTopic}' created successfully with 14 days retention period");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error working with Kafka topics: {ex.Message}");
            throw;
        }
    }

    private static async Task WaitForKafkaAvailability(string bootstrapServers, TimeSpan timeout, TimeSpan retryInterval)
    {
        var startTime = DateTime.UtcNow;
        var endTime = startTime + timeout;

        while (DateTime.UtcNow < endTime)
        {
            try
            {
                using var adminClient = new AdminClientBuilder(new AdminClientConfig
                {
                    BootstrapServers = bootstrapServers,
                    SocketTimeoutMs = (int)TimeSpan.FromSeconds(5).TotalMilliseconds,
                }).Build();

                // Try to get metadata to check if Kafka is available
                adminClient.GetMetadata(TimeSpan.FromSeconds(5));
                Console.WriteLine("Successfully connected to Kafka");
                return;
            }
            catch (Exception ex)
            {
                var remainingTime = endTime - DateTime.UtcNow;
                if (remainingTime <= TimeSpan.Zero)
                {
                    throw new TimeoutException($"Timeout waiting for Kafka to be available: {ex.Message}", ex);
                }

                Console.WriteLine($"Kafka not yet available: {ex.Message}. Retrying in {retryInterval.TotalSeconds} seconds... (Timeout in {Math.Ceiling(remainingTime.TotalMinutes)} minutes)");
                await Task.Delay(retryInterval);
            }
        }

        throw new TimeoutException("Timeout waiting for Kafka to be available");
    }
}
