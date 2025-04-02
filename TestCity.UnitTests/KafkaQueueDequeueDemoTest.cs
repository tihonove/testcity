using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Kontur.TestCity.Core.Worker;
using Kontur.TestCity.Core.Worker.TaskPayloads;
using Microsoft.Extensions.Logging;
using NUnit.Framework;

namespace Kontur.TestCity.UnitTests
{
    [TestFixture]
    public class KafkaQueueDequeueDemoTest
    {
        private readonly ILogger logger;
        public KafkaQueueDequeueDemoTest()
        {
            logger = GlobalSetup.TestLoggerFactory.CreateLogger<KafkaQueueDequeueDemoTest>();
        }

        [Test]
        public async Task EmailTaskSerializationTest()
        {
            var emailTask = new ProcessJobRunTaskPayload
            {
                ProjectId = 100,
                JobRunId = 200,
            };
            var workerClient = new WorkerClient(GlobalSetup.TestLoggerFactory.CreateLogger<WorkerClient>());
            await workerClient.Enqueue(emailTask);
        }

        private async Task CreateTopic(string topic)
        {
            using var adminClient = new AdminClientBuilder(new AdminClientConfig
            {
                BootstrapServers = Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP_SERVERS") ?? throw new Exception("KAFKA_BOOTSTRAP_SERVERS is not set"),
            }).Build();

            try
            {
                await adminClient.CreateTopicsAsync(
                [
                    new TopicSpecification
                    {
                        Name = topic,
                        ReplicationFactor = 1,
                        NumPartitions = 1,
                    },
                ]);
                logger.LogInformation($"Создана тема: {topic}");
            }
            catch (CreateTopicsException ex)
            {
                logger.LogError($"Ошибка создания темы: {ex.Message}");
            }
        }

        private async Task DeleteTopic(string topic)
        {
            using var adminClient = new AdminClientBuilder(new AdminClientConfig
            {
                BootstrapServers = Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP_SERVERS") ?? throw new Exception("KAFKA_BOOTSTRAP_SERVERS is not set"),
            }).Build();

            try
            {
                await adminClient.DeleteTopicsAsync([topic]);
                logger.LogInformation($"Удалена тема: {topic}");
            }
            catch (DeleteTopicsException ex)
            {
                logger.LogWarning($"Ошибка удаления темы (возможно не существует): {ex.Message}");
            }
        }
    }
}
