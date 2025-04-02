using System.Text.Json;
using Confluent.Kafka;
using Kontur.TestCity.Core.Worker;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TestCity.Worker.Kafka;
using TestCity.Worker.Kafka.Configuration;

namespace Kontur.TestCity.Worker.Kafka;

public sealed class KafkaConsumerService(ITaskHandlerRegistry taskHandlerRegistry, KafkaConsumerSettings settings, ILogger<KafkaConsumerService> logger) : BackgroundService
{
    public override async Task StartAsync(CancellationToken ct)
    {
        consumer = new ConsumerBuilder<Ignore, string>(new ConsumerConfig
        {
            BootstrapServers = settings.BootstrapServers,
            GroupId = settings.GroupId,
            EnableAutoCommit = false,
            AutoOffsetReset = AutoOffsetReset.Earliest,
        }).Build();
        consumer.Subscribe(settings.TasksTopic);
        await base.StartAsync(ct);
    }

    public override async Task StopAsync(CancellationToken ct)
    {
        await base.StopAsync(ct);
        consumer?.Unsubscribe();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (consumer == null)
        {
            throw new Exception("Consumer is not initialized");
        }

        logger.LogInformation("Kafka consumer service started");

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var consumeResult = consumer.Consume(stoppingToken);
                    var message = consumeResult.Message.Value;
                    RawTask? rawTask = null;
                    try
                    {
                        rawTask = JsonSerializer.Deserialize<RawTask>(message) ?? throw new JsonException("Failed to deserialize message");
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Failed to deserialize message: {Message}", message);
                        consumer.Commit(consumeResult);
                        continue;
                    }

                    var dispatched = await taskHandlerRegistry.DispatchTaskAsync(rawTask, stoppingToken);
                    if (dispatched)
                    {
                        consumer.Commit(consumeResult);
                    }
                    else
                    {
                        consumer.Commit(consumeResult);
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    // Нормальное завершение при отмене токена
                    break;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error consuming from Kafka");
                    await Task.Delay(1000, stoppingToken); // Небольшая пауза перед повторной попыткой
                }
            }
        }
        finally
        {
            try
            {
                consumer.Close();
                consumer.Dispose();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error closing Kafka consumer");
            }

            logger.LogInformation("Kafka consumer service stopped");
        }
    }

    public override void Dispose()
    {
        consumer?.Dispose();
        base.Dispose();
    }

    private readonly ITaskHandlerRegistry taskHandlerRegistry = taskHandlerRegistry;
    private readonly KafkaConsumerSettings settings = settings;
    private readonly ILogger<KafkaConsumerService> logger = logger;
    private IConsumer<Ignore, string>? consumer;
}
