using System.Text.Json;
using Confluent.Kafka;
using Kontur.TestCity.Core.Worker.TaskPayloads;
using Microsoft.Extensions.Logging;

namespace Kontur.TestCity.Core.Worker;

public class WorkerClient
{
    public WorkerClient(ILogger<WorkerClient> logger)
    {
        tasksTopic = Environment.GetEnvironmentVariable("KAFKA_TASKS_TOPIC") ?? "tasks";
        var config = new ProducerConfig
        {
            BootstrapServers = Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP_SERVERS") ?? throw new Exception("KAFKA_BOOTSTRAP_SERVERS is not set"),
            Acks = Acks.Leader,
        };
        this.logger = logger;
        producer = new ProducerBuilder<string, string>(config).Build();
    }

    public async Task Enqueue(ProcessJobRunTaskPayload taskPayload)
    {
        await EnqueueTask(ProcessJobRunTaskPayload.TaskType, taskPayload);
    }

    private async Task EnqueueTask<T>(string taskType, T taskPayload)
    {
        var payloadJson = JsonSerializer.Serialize(taskPayload);
        var rawTask = new RawTask
        {
            Type = taskType,
            Payload = JsonDocument.Parse(payloadJson).RootElement,
        };
        var rawTaskJson = JsonSerializer.Serialize(rawTask);
        await ProduceJsonMessage(tasksTopic, rawTaskJson);
    }

    private async Task ProduceJsonMessage(string topic, string jsonMessage)
    {
        await producer.ProduceAsync(topic, new Message<string, string>
        {
            Key = $"task-{Guid.NewGuid()}",
            Value = jsonMessage,
        });
    }

    private readonly string tasksTopic;
    private readonly ILogger logger;
    private readonly IProducer<string, string> producer;
}
