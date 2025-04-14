namespace Kontur.TestCity.Core.KafkaMessageQueue
{
    public class KafkaConsumerSettings
    {
        public string BootstrapServers { get; set; }
        public string GroupId { get; set; }
        public string TasksTopic { get; set; }
        public string DelayedTasksTopic { get; set; }
        public TimeSpan DelayedBase { get; set; } = TimeSpan.FromMinutes(1);
        public int MaxParallelTasksCount { get; set; } = 100;

        public static KafkaConsumerSettings Default => new KafkaConsumerSettings
        {
            BootstrapServers = Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP_SERVERS") ?? throw new Exception("KAFKA_BOOTSTRAP_SERVERS is not set"),
            TasksTopic = Environment.GetEnvironmentVariable("KAFKA_TASKS_TOPIC") ?? "tasks",
            DelayedTasksTopic = Environment.GetEnvironmentVariable("KAFKA_TASKS_TOPIC_DELAYED") ?? "tasks-delayed-1",
            GroupId = "test-city-tasks-consumer",
        };
    }
}
