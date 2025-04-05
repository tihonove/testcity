using System.Collections.Concurrent;
using System.Text.Json;
using System.Threading.Channels;
using Confluent.Kafka;
using Kontur.TestCity.Core.Worker;
using Microsoft.Extensions.Logging;
using TestCity.Worker.Kafka.Configuration;

namespace Kontur.TestCity.Worker.Kafka;

public sealed class KafkaMessageQueueConsumer(TaskHandlerRegistry taskHandlerRegistry, KafkaConsumerSettings settings, ILogger<KafkaMessageQueueConsumer> logger)
{
    public Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.Run(async () =>
        {
            logger.LogInformation("Kafka consumer service started");

            var queueMap = new ConcurrentDictionary<(IConsumer<string, string>, long), ConcurrentQueue<TaskExecutionItem>>();
            var semaphore = new SemaphoreSlim(100, 100);
            var inputChannel = Channel.CreateBounded<(IConsumer<string, string>, ConsumeResult<string, string>)>(new BoundedChannelOptions(10000) { });

            var primaryConsumer = RunPrimiaryTasksConsumer(inputChannel, stoppingToken);
            var delayedConsumer = RunDelayedTasksConsumer(inputChannel, stoppingToken);

            stoppingToken.Register(() =>
            {
                inputChannel.Writer.Complete();
            });

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var consumeResult = await inputChannel.Reader.ReadAsync(stoppingToken);
                    await semaphore.WaitAsync(stoppingToken);

                    var executionItem = new TaskExecutionItem
                    {
                        State = TaskExecutionResult.Queued,
                        Message = consumeResult.Item2,
                    };
                    var queue = queueMap.GetOrAdd((consumeResult.Item1, consumeResult.Item2.Partition), (_) => new ConcurrentQueue<TaskExecutionItem>());
                    queue.Enqueue(executionItem);
                    RunTaskExecution(executionItem, semaphore, consumeResult.Item1, queue, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    // Нормальное завершение при отмене токена
                    break;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error consuming from input channel");
                    await Task.Delay(1000, stoppingToken);
                }
            }

            logger.LogInformation("Kafka consumer service stopped");
        }, stoppingToken);
    }

    private IConsumer<string, string> RunPrimiaryTasksConsumer(Channel<(IConsumer<string, string>, ConsumeResult<string, string>)> inputChannel, CancellationToken stoppingToken)
    {
        var primaryConsumer = new ConsumerBuilder<string, string>(new ConsumerConfig
        {
            BootstrapServers = settings.BootstrapServers,
            GroupId = settings.GroupId,
            EnableAutoCommit = false,
            AutoOffsetReset = AutoOffsetReset.Earliest,
        }).Build();
        Task.Run(async () =>
        {
            primaryConsumer.Subscribe(settings.TasksTopic);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var consumeResult = primaryConsumer.Consume(stoppingToken);
                    await inputChannel.Writer.WriteAsync((primaryConsumer, consumeResult), stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error consuming from Kafka");
                    await Task.Delay(1000, stoppingToken);
                }
            }

            try
            {
                primaryConsumer?.Unsubscribe();
                primaryConsumer?.Close();
                primaryConsumer?.Dispose();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error closing Kafka consumer");
            }

        }, stoppingToken);

        return primaryConsumer;
    }

    private IConsumer<string, string> RunDelayedTasksConsumer(Channel<(IConsumer<string, string>, ConsumeResult<string, string>)> inputChannel, CancellationToken stoppingToken)
    {
        var consumer = new ConsumerBuilder<string, string>(new ConsumerConfig
        {
            BootstrapServers = settings.BootstrapServers,
            GroupId = settings.GroupId,
            EnableAutoCommit = false,
            AutoOffsetReset = AutoOffsetReset.Earliest,
        }).Build();
        Task.Run(async () =>
        {
            consumer.Subscribe(settings.DelayedTasksTopic);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(settings.DelayedBase, stoppingToken);
                    var delayedTasksBuffer = new List<ConsumeResult<string, string>>();
                    while (!stoppingToken.IsCancellationRequested)
                    {
                        var consumeResult = consumer.Consume(TimeSpan.Zero);
                        if (consumeResult == null)
                        {
                            break;
                        }
                        await Task.Delay(settings.DelayedBase, stoppingToken);
                        delayedTasksBuffer.Add(consumeResult);
                    }
                    foreach (var delayedTask in delayedTasksBuffer)
                        await inputChannel.Writer.WriteAsync((consumer, delayedTask), stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error consuming from Kafka");
                    await Task.Delay(1000, stoppingToken);
                }
            }

            try
            {
                consumer?.Unsubscribe();
                consumer?.Close();
                consumer?.Dispose();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error closing Kafka consumer");
            }
        }, stoppingToken);

        return consumer;
    }

    private void CommitCompleted(IConsumer<string, string> consumer, ConcurrentQueue<TaskExecutionItem> queue)
    {
        lock (lockCommit)
        {
            List<TaskExecutionItem> completedItems = new List<TaskExecutionItem>();

            TaskExecutionItem? item = null;
            while (queue.TryPeek(out item) && item.State != TaskExecutionResult.Canceled && item.State != TaskExecutionResult.Executing && item.State != TaskExecutionResult.Queued)
            {
                logger.LogInformation("Peek message to commit Offset: {Offset}", item.Message.Offset);
                if (queue.TryDequeue(out var completedItem))
                {
                    completedItems.Add(completedItem);
                }
                else
                {
                    break;
                }
            }
            if (completedItems.Count > 0)
            {
                foreach (var partition in completedItems.GroupBy(x => x.Message.Partition))
                {
                    var partitionItems = partition.ToList();
                    logger.LogInformation("Commit {CommitCount} messages from partition {TasksPartition}. Offset: {Offset}",
                        partitionItems.Count,
                        partition.Key,
                        partitionItems.Last().Message.Offset);
                    consumer?.Commit(partitionItems.Last().Message);
                }
            }
            else
            {
                logger.LogInformation("Not found tasks to commit. Last task: {Offset}. State: {TaskState}", item?.Message.Offset, item?.State);
            }
        }
    }

    private void RunTaskExecution(TaskExecutionItem executionItem, SemaphoreSlim semaphore, IConsumer<string, string> item1, ConcurrentQueue<TaskExecutionItem> queue, CancellationToken ct)
    {
        var delayedQueueProducer = new ProducerBuilder<string, string>(new ProducerConfig
        {
            BootstrapServers = settings.BootstrapServers,
            Acks = Acks.Leader,
        }).Build();

        Task.Run(async () =>
        {
            try
            {
                executionItem.State = TaskExecutionResult.Executing;
                var message = executionItem.Message.Message.Value;
                RawTask? rawTask = null;
                try
                {
                    rawTask = JsonSerializer.Deserialize<RawTask>(message) ?? throw new JsonException("Failed to deserialize message");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to deserialize message: {Message}", message);
                    executionItem.State = TaskExecutionResult.Skipped;
                    semaphore.Release();
                    return;
                }

                try
                {
                    await taskHandlerRegistry.DispatchTaskAsync(rawTask, ct);
                    executionItem.State = TaskExecutionResult.Success;
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    executionItem.State = TaskExecutionResult.Canceled;
                }
                catch (Exception e)
                {
                    await EnqueueTaskToDelayedTopicWithRetriesOrSkip(delayedQueueProducer, executionItem, rawTask);
                    executionItem.State = TaskExecutionResult.Failure;
                    executionItem.ErrorMessage = e.Message;
                }
                finally
                {
                    semaphore.Release();
                }
            }
            finally
            {
                CommitCompleted(item1, queue);
            }
        }, ct);
    }

    private async Task EnqueueTaskToDelayedTopicWithRetriesOrSkip(IProducer<string, string> delayedQueueProducer, TaskExecutionItem executionItem, RawTask rawTask)
    {
        const int maxRetryAttempts = 5;
        const int initialRetryDelayMs = 1000;
        const int maxTotalRetryTimeMs = 30000; // 30 seconds total retry time

        var startTime = DateTime.UtcNow;
        var attempt = 0;
        Exception? lastException = null;

        try
        {
            rawTask.ExecuteCount ??= 0;
            rawTask.ExecuteCount++;
            var rawTaskJson = JsonSerializer.Serialize(rawTask);

            while (attempt < maxRetryAttempts && (DateTime.UtcNow - startTime).TotalMilliseconds < maxTotalRetryTimeMs)
            {
                attempt++;
                try
                {
                    await delayedQueueProducer.ProduceAsync(settings.DelayedTasksTopic, new Message<string, string>
                    {
                        Key = executionItem.Message.Message.Key,
                        Value = rawTaskJson,
                    });

                    // If we reached here, the message was produced successfully
                    logger.LogInformation("Successfully produced delayed task message on attempt {Attempt}", attempt);
                    return;
                }
                catch (Exception e)
                {
                    lastException = e;
                    logger.LogWarning(e, "Failed to produce delayed task message on attempt {Attempt}/{MaxAttempts}: {Message}",
                        attempt, maxRetryAttempts, rawTask);

                    // Calculate remaining time for retries
                    var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
                    var remainingTime = maxTotalRetryTimeMs - elapsed;

                    if (attempt < maxRetryAttempts && remainingTime > 0)
                    {
                        // Exponential backoff with jitter, but ensure we don't exceed the remaining time
                        var delayMs = Math.Min(
                            initialRetryDelayMs * Math.Pow(2, attempt - 1) * (0.8 + new Random().NextDouble() * 0.4),
                            remainingTime);

                        await Task.Delay((int)delayMs);
                    }
                }
            }

            // If we get here, all retry attempts failed
            throw new Exception($"Failed to produce delayed task message after {attempt} attempts within {maxTotalRetryTimeMs}ms", lastException);
        }
        catch (Exception e)
        {
            logger.LogError(e, "All attempts to produce delayed task message failed: {Message}", rawTask);
            executionItem.State = TaskExecutionResult.Skipped;
        }
    }

    enum TaskExecutionResult
    {
        Queued,
        Executing,
        Success,
        Skipped,
        Failure,
        Canceled,
    }

    class TaskExecutionItem
    {
        public required TaskExecutionResult State { get; set; }
        public ConsumeResult<string, string> Message { get; init; }
        public RawTask? RawTask { get; init; }

        public string? ErrorMessage { get; set; }
    }

    private readonly Lock lockCommit = new Lock();
}
