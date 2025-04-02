namespace TestCity.Worker.Kafka.Configuration
{
    public class KafkaConsumerSettings
    {
        public string BootstrapServers { get; set; }
        public string GroupId { get; set; }
        public string TasksTopic { get; set; }

        public static KafkaConsumerSettings Default => new KafkaConsumerSettings
        {
            BootstrapServers = Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP_SERVERS") ?? throw new Exception("KAFKA_BOOTSTRAP_SERVERS is not set"),
            TasksTopic = Environment.GetEnvironmentVariable("KAFKA_TASKS_TOPIC") ?? "tasks",
            GroupId = "test-city-tasks-consumer",
        };
    }
}
