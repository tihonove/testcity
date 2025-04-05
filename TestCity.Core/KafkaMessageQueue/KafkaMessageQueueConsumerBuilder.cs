using Kontur.TestCity.Worker.Handlers.Base;
using Kontur.TestCity.Worker.Kafka;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TestCity.Worker.Kafka.Configuration;

namespace Kontur.TestCity.Core.KafkaMessageQueue;

public class KafkaMessageQueueConsumerBuilder
{
    public KafkaMessageQueueConsumerBuilder WithSettings(KafkaConsumerSettings settings)
    {
        this.settings = settings;
        return this;
    }

    public KafkaMessageQueueConsumerBuilder WithKafkaSettings(string bootstrapServers, string groupId, string tasksTopic)
    {
        settings = new KafkaConsumerSettings
        {
            BootstrapServers = bootstrapServers,
            GroupId = groupId,
            TasksTopic = tasksTopic
        };
        return this;
    }

    public KafkaMessageQueueConsumerBuilder WithTaskHandler(ITaskHandler handler)
    {
        taskHandlers.Add(handler);
        return this;
    }

    public KafkaMessageQueueConsumerBuilder WithTaskHandlers(IEnumerable<ITaskHandler> handlers)
    {
        taskHandlers.AddRange(handlers);
        return this;
    }

    public KafkaMessageQueueConsumerBuilder WithLoggerFactory(ILoggerFactory loggerFactory)
    {
        this.loggerFactory = loggerFactory;
        return this;
    }

    public KafkaMessageQueueConsumer Build()
    {
        if (loggerFactory == null)
        {
            throw new InvalidOperationException("Logger must be provided. Use WithLogger method to set a logger.");
        }

        var taskHandlerRegistry = new TaskHandlerRegistry(taskHandlers);
        return new KafkaMessageQueueConsumer(taskHandlerRegistry, settings, loggerFactory.CreateLogger<KafkaMessageQueueConsumer>());
    }
    public BackgroundService BuildBackgroundService()
    {
        return new KafkaMessageQueueConsumerHostedServiceWrapper(Build());
    }
    public (KafkaMessageQueueConsumer, KafkaMessageQueueClient) BuildServiceAndClient()
    {
        if (loggerFactory == null)
        {
            throw new InvalidOperationException("Logger must be provided. Use WithLogger method to set a logger.");
        }

        var taskHandlerRegistry = new TaskHandlerRegistry(taskHandlers);
        return (
            new KafkaMessageQueueConsumer(taskHandlerRegistry, settings, loggerFactory.CreateLogger<KafkaMessageQueueConsumer>()),
            new KafkaMessageQueueClient(settings.BootstrapServers, settings.TasksTopic, loggerFactory.CreateLogger<KafkaMessageQueueClient>())
        );
    }

    private readonly List<ITaskHandler> taskHandlers = new();
    private KafkaConsumerSettings settings = KafkaConsumerSettings.Default;
    private ILoggerFactory loggerFactory;
}
