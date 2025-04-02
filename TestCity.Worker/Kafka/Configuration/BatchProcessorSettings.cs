namespace TestCity.Worker.Kafka.Configuration
{
    public class BatchProcessorSettings
    {
        public int MaxBatchSize { get; set; } = 100;
        public TimeSpan MaxWaitTime { get; set; } = TimeSpan.FromSeconds(5);
        public int MaxChannelCapacity { get; set; } = 1000;

        public static BatchProcessorSettings Default => new BatchProcessorSettings();
    }
}
