using System.Text.Json;
using Confluent.Kafka;
using Kontur.TestCity.Core.Worker;
using Microsoft.Extensions.Logging;

namespace Kontur.TestCity.Core.KafkaMessageQueue;

public class KafkaMessageQueueClient
{
    public static KafkaMessageQueueClient CreateDefault(ILogger<KafkaMessageQueueClient> logger)
    {
        return new KafkaMessageQueueClient(
            Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP_SERVERS") ?? throw new Exception("KAFKA_BOOTSTRAP_SERVERS is not set"),
            Environment.GetEnvironmentVariable("KAFKA_TASKS_TOPIC") ?? "tasks",
            logger);
    }

    public KafkaMessageQueueClient(string bootstrapServers, string tasksTopic, ILogger<KafkaMessageQueueClient> logger)
    {
        this.tasksTopic = tasksTopic;
        var config = new ProducerConfig
        {
            BootstrapServers = bootstrapServers,
            Acks = Acks.Leader,
        };
        this.logger = logger;
        producer = new ProducerBuilder<string, string>(config).Build();
    }

    public async Task EnqueueTask<T>(string taskType, string key, T taskPayload)
    {
        var payloadJson = JsonSerializer.Serialize(taskPayload);
        var rawTask = new RawTask
        {
            Type = taskType,
            Payload = JsonDocument.Parse(payloadJson).RootElement,
        };
        var rawTaskJson = JsonSerializer.Serialize(rawTask);
        await producer.ProduceAsync(tasksTopic, new Message<string, string>
        {
            Key = key,
            Value = rawTaskJson,
        });
    }

    private readonly string tasksTopic;
    private readonly ILogger logger;
    private readonly IProducer<string, string> producer;
}
