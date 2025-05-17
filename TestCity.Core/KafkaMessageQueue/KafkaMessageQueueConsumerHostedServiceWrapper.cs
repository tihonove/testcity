using Microsoft.Extensions.Hosting;

namespace TestCity.Core.KafkaMessageQueue;

public class KafkaMessageQueueConsumerHostedServiceWrapper(KafkaMessageQueueConsumer messageQueueConsumer) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return messageQueueConsumer.ExecuteAsync(stoppingToken);
    }
}
