using Confluent.Kafka;
using Confluent.Kafka.Admin;

namespace Kontur.TestAnalytics.ActualizeDb.Cli;

public static class KafkaTopicActualizer
{
    public static async Task EnsureKafkaTopicExists()
    {
        var tasksTopic = Environment.GetEnvironmentVariable("KAFKA_TASKS_TOPIC") ?? "tasks";
        var bootstrapServers = Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP_SERVERS") ?? throw new Exception("KAFKA_BOOTSTRAP_SERVERS is not set");

        Console.WriteLine($"Waiting for Kafka to be available at {bootstrapServers}...");

        await WaitForKafkaAvailability(bootstrapServers, TimeSpan.FromMinutes(5), TimeSpan.FromSeconds(10));

        Console.WriteLine($"Verifying Kafka topic: {tasksTopic}");
        using var adminClient = new AdminClientBuilder(new AdminClientConfig
        {
            BootstrapServers = bootstrapServers,
        }).Build();

        try
        {
            var metadata = adminClient.GetMetadata(TimeSpan.FromSeconds(10));
            var topicExists = metadata.Topics.Any(t => t.Topic == tasksTopic);

            if (!topicExists)
            {
                Console.WriteLine($"Creating Kafka topic: {tasksTopic}");

                await adminClient.CreateTopicsAsync(
                [
                    new TopicSpecification
                    {
                        Name = tasksTopic,
                        ReplicationFactor = 1,
                        NumPartitions = 16,
                        Configs = new Dictionary<string, string>
                        {
                            { "retention.ms", TimeSpan.FromDays(14).TotalMilliseconds.ToString() },
                        },
                    },
                ]);
                Console.WriteLine($"Kafka topic '{tasksTopic}' created successfully with 14 days retention period");
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
