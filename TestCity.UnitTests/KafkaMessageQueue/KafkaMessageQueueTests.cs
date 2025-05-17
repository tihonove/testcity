using Confluent.Kafka;
using Confluent.Kafka.Admin;
using TestCity.Core.KafkaMessageQueue;
using TestCity.Core.Worker;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using System.Collections.Concurrent;
using System.Diagnostics;
using TestCity.UnitTests.Utils;

namespace TestCity.UnitTests.KafkaMessageQueue;

[TestFixture]
class KafkaMessageQueueTests
{
    private const string TestTaskType = "test-task";
    private ILogger logger;

    [SetUp]
    public void SetUp()
    {
        logger = GlobalSetup.TestLoggerFactory.CreateLogger("KafkaMessageQueueTests");
    }

    [Test]
    public async Task TestEnqueueAndExecuteSingleTask()
    {
        var topicPrefix = "test-topic";
        await EnsureCleanTopicSet(topicPrefix);

        var count = 0;
        var testHandler = new DelegatedTaskHandler<string>(TestTaskType, _ =>
        {
            count++;
            GlobalSetup.TestLoggerFactory.CreateLogger("TestHandler").LogInformation("Task handled");
        });

        await using var consumerInstance = TestConsumerInstance.Run(topicPrefix, testHandler);
        await consumerInstance.Client.EnqueueTask(TestTaskType, "test-payload");
        await AssertEventually.That(() => count, Is.EqualTo(1));
    }

    [Test]
    public async Task TestMultipleTasksSequential()
    {
        var topicPrefix = "test-sequential-topic";
        await EnsureCleanTopicSet(topicPrefix);

        var processedPayloads = new ConcurrentBag<string>();
        var testHandler = new DelegatedTaskHandler<string>(TestTaskType, payload =>
        {
            processedPayloads.Add(payload);
            GlobalSetup.TestLoggerFactory.CreateLogger("TestHandler").LogInformation("Task handled: {Payload}", payload);
        });

        await using var consumerInstance = TestConsumerInstance.Run(topicPrefix, testHandler);

        // Send 10 tasks sequentially
        for (int i = 0; i < 10; i++)
        {
            await consumerInstance.Client.EnqueueTask(TestTaskType, $"payload-{i}");
        }

        // Verify all tasks are processed
        await AssertEventually.That(() => processedPayloads.Count, Is.EqualTo(10));

        // Verify all expected payloads were processed
        for (int i = 0; i < 10; i++)
        {
            Assert.That(processedPayloads, Contains.Item($"payload-{i}"));
        }
    }

    [Test]
    public async Task TestMultipleTasksParallel()
    {
        var topicPrefix = "test-parallel-topic";
        await EnsureCleanTopicSet(topicPrefix);

        var processedPayloads = new ConcurrentBag<string>();
        var testHandler = new DelegatedTaskHandler<string>(TestTaskType, payload =>
        {
            processedPayloads.Add(payload);
            GlobalSetup.TestLoggerFactory.CreateLogger("TestHandler").LogInformation("Task handled: {Payload}", payload);
        });

        await using var consumerInstance = TestConsumerInstance.Run(topicPrefix, testHandler);

        // Send 50 tasks in parallel
        var enqueueTasks = Enumerable.Range(0, 50)
            .Select(i => consumerInstance.Client.EnqueueTask(TestTaskType, $"parallel-{i}"))
            .ToArray();

        await Task.WhenAll(enqueueTasks);

        // Verify all tasks are processed
        await AssertEventually.That(() => processedPayloads.Count, Is.EqualTo(50));

        // Verify all expected payloads were processed
        for (int i = 0; i < 50; i++)
        {
            Assert.That(processedPayloads, Contains.Item($"parallel-{i}"));
        }
    }

    [Test]
    public async Task TestMultipleTaskTypesWithDifferentHandlers()
    {
        var topicPrefix = "test-multiple-types-topic";
        await EnsureCleanTopicSet(topicPrefix);

        var type1Payloads = new ConcurrentBag<string>();
        var type2Payloads = new ConcurrentBag<string>();

        var handler1 = new DelegatedTaskHandler<string>("type1-task", payload =>
        {
            type1Payloads.Add(payload);
            GlobalSetup.TestLoggerFactory.CreateLogger("Type1Handler").LogInformation("Type1 task handled: {Payload}", payload);
        });

        var handler2 = new DelegatedTaskHandler<string>("type2-task", payload =>
        {
            type2Payloads.Add(payload);
            GlobalSetup.TestLoggerFactory.CreateLogger("Type2Handler").LogInformation("Type2 task handled: {Payload}", payload);
        });

        await using var consumerInstance = TestConsumerInstance.Run(topicPrefix, handler1, handler2);

        // Send tasks of both types
        for (int i = 0; i < 5; i++)
        {
            await consumerInstance.Client.EnqueueTask("type1-task", $"type1-payload-{i}");
            await consumerInstance.Client.EnqueueTask("type2-task", $"type2-payload-{i}");
        }

        // Verify all tasks are processed
        await AssertEventually.That(() => type1Payloads.Count + type2Payloads.Count, Is.EqualTo(10));

        Assert.That(type1Payloads.Count, Is.EqualTo(5), "Expected 5 tasks of type1");
        Assert.That(type2Payloads.Count, Is.EqualTo(5), "Expected 5 tasks of type2");
    }

    [Test]
    public async Task TestTasksAreProcessedExactlyOnce()
    {
        var topicPrefix = "test-exactly-once-topic";
        await EnsureCleanTopicSet(topicPrefix);

        var processedCounts = new ConcurrentDictionary<string, int>();

        var testHandler = new DelegatedTaskHandler<string>(TestTaskType, payload =>
        {
            processedCounts.AddOrUpdate(payload, 1, (_, count) => count + 1);
            GlobalSetup.TestLoggerFactory.CreateLogger("TestHandler").LogInformation("Task handled: {Payload}", payload);
        });

        await using var consumerInstance = TestConsumerInstance.Run(topicPrefix, testHandler);

        // Send 10 tasks
        for (int i = 0; i < 10; i++)
        {
            await consumerInstance.Client.EnqueueTask(TestTaskType, $"unique-payload-{i}");
        }

        // Wait for all tasks to be processed
        await AssertEventually.That(() => processedCounts.Count, Is.EqualTo(10));

        // Verify each task was processed exactly once
        foreach (var kvp in processedCounts)
        {
            Assert.That(kvp.Value, Is.EqualTo(1), $"Task {kvp.Key} was processed {kvp.Value} times instead of exactly once");
        }
    }

    [Test]
    public async Task TestHandlerExceptionDoesNotBreakConsumer()
    {
        var topicPrefix = "test-exception-topic";
        await EnsureCleanTopicSet(topicPrefix);

        var successfullyProcessed = new ConcurrentBag<string>();
        var attemptedToProcess = new ConcurrentBag<string>();

        // Handler that throws exception on specific payloads
        var testHandler = new DelegatedTaskHandler<string>(TestTaskType, payload =>
        {
            attemptedToProcess.Add(payload);

            // Throw exception for even numbered payloads
            if (payload.Contains("even"))
            {
                throw new Exception($"Simulated failure processing {payload}");
            }

            successfullyProcessed.Add(payload);
        });

        await using var consumerInstance = TestConsumerInstance.Run(topicPrefix, testHandler);

        // Send both "good" and "bad" tasks
        for (int i = 0; i < 10; i++)
        {
            var payloadType = i % 2 == 0 ? "even" : "odd";
            await consumerInstance.Client.EnqueueTask(TestTaskType, $"payload-{payloadType}-{i}");
        }

        // Wait for all tasks to be attempted
        await AssertEventually.That(() => attemptedToProcess.Count, Is.EqualTo(10));

        // Verify odd-numbered tasks were processed successfully
        Assert.That(successfullyProcessed.Count, Is.EqualTo(5), "Expected 5 tasks to be processed successfully");

        // Verify the consumer continued running even after exceptions
        var additionalPayload = "after-exception";
        await consumerInstance.Client.EnqueueTask(TestTaskType, additionalPayload);

        await AssertEventually.That(() => successfullyProcessed, Contains.Item(additionalPayload));
    }

    [Test]
    public async Task TestServiceRestartPreservesUnprocessedTasks()
    {
        var topicPrefix = "test-restart-topic";
        await EnsureCleanTopicSet(topicPrefix);

        var delayFactor = 10000;
        var processedPayloads = new ConcurrentBag<int>();
        var testHandler = new DelegatedTaskHandler<int>(TestTaskType, async (payload, ct) =>
        {
            // Simulate slow processing
            if (payload > 5)
                await Task.Delay(delayFactor, ct);
            logger.LogInformation("Completed task: {Payload}", payload);
            processedPayloads.Add(payload);
        });

        // First consumer instance
        await using (var consumerInstance1 = TestConsumerInstance.Run(topicPrefix, testHandler))
        {
            // Enqueue 20 tasks
            for (int i = 0; i < 20; i++)
            {
                await consumerInstance1.Client.EnqueueTask(TestTaskType, i);
            }

            // Wait for some tasks to be processed, but not all
            while (processedPayloads.Count < 5)
            {
                await Task.Delay(100);
            }

            // Dispose/stop the first consumer
        }

        var firstConsumerCount = processedPayloads.Count;

        delayFactor = 0;
        // Start a new consumer with the same group id
        await using var consumerInstance2 = TestConsumerInstance.Run(topicPrefix, testHandler);

        // Wait for all remaining tasks to be processed
        await AssertEventually.That(() => processedPayloads.Count, Is.EqualTo(20));

        Assert.That(firstConsumerCount, Is.LessThan(20), "First consumer should not have processed all tasks");
        Assert.That(processedPayloads.Count, Is.EqualTo(20), "All tasks should be processed after restart");

        // Verify all expected payloads were processed
        for (int i = 0; i < 20; i++)
        {
            Assert.That(processedPayloads, Contains.Item(i));
        }
    }

    [Test]
    public async Task TestMultipleConsumersLoadBalancing()
    {
        var topicPrefix = "test-load-balancing-topic";
        await EnsureCleanTopicSet(topicPrefix, numPartitions: 3);  // Create topic with multiple partitions

        var consumer1Payloads = new ConcurrentBag<string>();
        var consumer2Payloads = new ConcurrentBag<string>();

        // Handler for first consumer
        var handler1 = new DelegatedTaskHandler<string>(TestTaskType, payload =>
        {
            consumer1Payloads.Add(payload);
            GlobalSetup.TestLoggerFactory.CreateLogger("Consumer1").LogInformation("Consumer 1 handled: {Payload}", payload);
        });

        // Handler for second consumer
        var handler2 = new DelegatedTaskHandler<string>(TestTaskType, payload =>
        {
            consumer2Payloads.Add(payload);
            GlobalSetup.TestLoggerFactory.CreateLogger("Consumer2").LogInformation("Consumer 2 handled: {Payload}", payload);
        });

        // Start two consumers with different consumer group instances but same group ID
        var consumerGroup = "test-group";
        await using var consumerInstance1 = TestConsumerInstance.Run(topicPrefix, consumerGroup, handler1);
        await using var consumerInstance2 = TestConsumerInstance.Run(topicPrefix, consumerGroup, handler2);

        // Use any of the clients to produce messages
        var client = consumerInstance1.Client;

        // Send a larger number of tasks
        for (int i = 0; i < 100; i++)
        {
            await client.EnqueueTask(TestTaskType, $"balanced-payload-{i}");
        }

        // Wait for all tasks to be processed
        await AssertEventually.That(() => consumer1Payloads.Count + consumer2Payloads.Count, Is.EqualTo(100));

        // Verify that both consumers received some tasks (load balancing)
        Assert.That(consumer1Payloads.Count, Is.GreaterThan(0), "Consumer 1 should have processed some tasks");
        Assert.That(consumer2Payloads.Count, Is.GreaterThan(0), "Consumer 2 should have processed some tasks");

        // Verify the total number of processed tasks
        Assert.That(consumer1Payloads.Count + consumer2Payloads.Count, Is.EqualTo(100), "Total tasks processed");

        // Verify no tasks were processed by both consumers
        var intersection = consumer1Payloads.Intersect(consumer2Payloads).ToList();
        Assert.That(intersection, Is.Empty, "No task should be processed by both consumers");
    }

    [Test]
    public async Task TestHighLoadWithLongRunningTasks()
    {
        var topicPrefix = "test-high-load-topic";
        await EnsureCleanTopicSet(topicPrefix, numPartitions: 4);

        var processedPayloads = new ConcurrentBag<string>();
        var completedTaskCounts = new ConcurrentDictionary<int, int>();

        // Handler that simulates varied processing times
        var testHandler = new DelegatedTaskHandler<string>(TestTaskType, async (payload, ct) =>
        {
            var parts = payload.Split('-');
            if (parts.Length >= 2 && int.TryParse(parts[1], out var taskId))
            {
                // Simulate varying processing times based on task ID
                var processingTime = taskId % 5 == 0 ? 500 : // Some tasks take longer
                                     taskId % 3 == 0 ? 200 : // Medium tasks
                                     50;                     // Fast tasks

                await Task.Delay(processingTime, ct);

                processedPayloads.Add(payload);
                completedTaskCounts.AddOrUpdate(processingTime, 1, (_, count) => count + 1);
            }
        });

        // Start multiple consumers to handle the load
        var consumerGroup = "load-group";
        await using var consumer1 = TestConsumerInstance.Run(topicPrefix, consumerGroup, testHandler);
        await using var consumer2 = TestConsumerInstance.Run(topicPrefix, consumerGroup, testHandler);
        await using var consumer3 = TestConsumerInstance.Run(topicPrefix, consumerGroup, testHandler);

        var client = consumer1.Client;

        // Send a large number of tasks in parallel
        const int taskCount = 200;
        var stopwatch = Stopwatch.StartNew();

        var enqueueTasks = Enumerable.Range(0, taskCount)
            .Select(i => client.EnqueueTask(TestTaskType, $"load-{i}"))
            .ToArray();

        await Task.WhenAll(enqueueTasks);
        var enqueueTime = stopwatch.ElapsedMilliseconds;

        // Wait for all tasks to be processed
        await AssertEventually.That(() => processedPayloads.Count, Is.EqualTo(taskCount));
        var totalProcessingTime = stopwatch.ElapsedMilliseconds;

        GlobalSetup.TestLoggerFactory.CreateLogger("HighLoadTest").LogInformation(
            "Processed {TaskCount} tasks in {TotalTime}ms (enqueue: {EnqueueTime}ms). Task breakdown: {TaskBreakdown}",
            taskCount, totalProcessingTime, enqueueTime, string.Join(", ", completedTaskCounts.Select(kvp => $"{kvp.Value} tasks @{kvp.Key}ms")));

        // Verify all expected payloads were processed
        for (int i = 0; i < taskCount; i++)
        {
            Assert.That(processedPayloads, Contains.Item($"load-{i}"));
        }
    }

    [Test]
    public async Task TestUnhandledTasksRemainInQueue()
    {
        var topicPrefix = "test-unhandled-topic";
        await EnsureCleanTopicSet(topicPrefix);

        var handler1Processed = new ConcurrentBag<string>();
        var handler2Processed = new ConcurrentBag<string>();

        // Handler for first type of tasks
        var handler1 = new DelegatedTaskHandler<string>("type1", payload =>
        {
            handler1Processed.Add(payload);
        });

        // First consumer only handles type1 tasks
        await using (var consumer1 = TestConsumerInstance.Run(topicPrefix, handler1))
        {
            // Send both types of tasks
            await consumer1.Client.EnqueueTask("type1", "task1-for-handler1");
            await consumer1.Client.EnqueueTask("type2", "task2-should-remain-in-queue");

            // Verify type1 tasks are processed
            await AssertEventually.That(() => handler1Processed.Count, Is.EqualTo(1));
        }

        // Second handler handles type2 tasks
        var handler2 = new DelegatedTaskHandler<string>("type2", payload =>
        {
            handler2Processed.Add(payload);
        });

        // Second consumer handles type2 tasks
        await using var consumer2 = TestConsumerInstance.Run(topicPrefix, handler2);

        // Verify type2 tasks that were previously unhandled are now processed
        await AssertEventually.That(() => handler2Processed.Count, Is.EqualTo(1));

        Assert.That(handler2Processed, Contains.Item("task2-should-remain-in-queue"));
    }

    [Test]
    public async Task TestPayloadDeserialization()
    {
        var topicPrefix = "test-payload-topic";
        await EnsureCleanTopicSet(topicPrefix);

        var processedData = new ConcurrentBag<TestData>();

        var testHandler = new DelegatedTaskHandler<TestData>("complex-task", payload =>
        {
            processedData.Add(payload);
            GlobalSetup.TestLoggerFactory.CreateLogger("PayloadTest").LogInformation(
                "Processed complex payload: Id={Id}, Name={Name}", payload.Id, payload.Name);
        });

        await using var consumerInstance = TestConsumerInstance.Run(topicPrefix, testHandler);

        // Create complex test data
        var testData = new TestData
        {
            Id = 42,
            Name = "Test Object",
            CreatedAt = DateTime.UtcNow,
            Values = new List<int> { 1, 2, 3, 4, 5 }
        };

        // Send task with complex payload
        await consumerInstance.Client.EnqueueTask("complex-task", testData);

        // Verify the task is processed with proper deserialization
        await AssertEventually.That(() => processedData.Count, Is.EqualTo(1));

        var receivedData = processedData.First();
        Assert.That(receivedData.Id, Is.EqualTo(testData.Id));
        Assert.That(receivedData.Name, Is.EqualTo(testData.Name));
        Assert.That(receivedData.CreatedAt, Is.EqualTo(testData.CreatedAt).Within(TimeSpan.FromSeconds(1)));
        Assert.That(receivedData.Values, Is.EquivalentTo(testData.Values));
    }

    private async Task EnsureCleanTopicSet(string topicPrefix, int numPartitions = 1, short replicationFactor = 1)
    {
        var tasksTopic = topicPrefix + "-tasks";
        var delayedTasksTopic = topicPrefix + "-tasks-delayed-1";
        var bootstrapServers = Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP_SERVERS") ?? throw new Exception("KAFKA_BOOTSTRAP_SERVERS is not set");
        using var adminClient = new AdminClientBuilder(new AdminClientConfig { BootstrapServers = bootstrapServers }).Build();
        try
        {
            await adminClient.DeleteTopicsAsync(new[] { tasksTopic });
            await adminClient.DeleteTopicsAsync(new[] { delayedTasksTopic });
            await Task.Delay(500);
        }
        catch
        {
        }

        await adminClient.CreateTopicsAsync(
        [
            new TopicSpecification
            {
                Name = tasksTopic,
                NumPartitions = numPartitions,
                ReplicationFactor = replicationFactor
            },
            new TopicSpecification
            {
                Name = delayedTasksTopic,
                NumPartitions = numPartitions,
                ReplicationFactor = replicationFactor
            }
        ]);
    }
}

// Extended version of TestConsumerInstance to support multiple consumer configuration
internal class TestConsumerInstance : IAsyncDisposable
{
    private readonly Func<Task> dispose;

    private TestConsumerInstance(KafkaMessageQueueClient client, Func<Task> dispose)
    {
        Client = client;
        this.dispose = dispose;
    }

    public static TestConsumerInstance Run(string topicPrefix, params ITaskHandler[] testHandlers)
    {
        return Run(topicPrefix, "test-group", testHandlers);
    }

    public static TestConsumerInstance Run(string topicPrefix, string groupId, params ITaskHandler[] testHandlers)
    {
        var tasksTopic = topicPrefix + "-tasks";
        var delayedTasksTopic = topicPrefix + "-tasks-delayed-1";
        var bootstrapServers = Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP_SERVERS") ?? throw new Exception("KAFKA_BOOTSTRAP_SERVERS is not set");
        var (consumerService, client) = new KafkaMessageQueueConsumerBuilder()
            .WithSettings(new KafkaConsumerSettings
            {
                BootstrapServers = bootstrapServers,
                GroupId = groupId,
                TasksTopic = tasksTopic,
                DelayedTasksTopic = delayedTasksTopic,
                DelayedBase = TimeSpan.FromSeconds(10),
            })
            .WithTaskHandlers(testHandlers)
            .WithLoggerFactory(GlobalSetup.TestLoggerFactory)
            .BuildServiceAndClient();
        var tokenSource = new CancellationTokenSource();
        var task = consumerService.ExecuteAsync(tokenSource.Token);
        return new TestConsumerInstance(client, async () =>
        {
            tokenSource.Cancel();
            await task;
        });
    }

    public KafkaMessageQueueClient Client { get; }

    public async ValueTask DisposeAsync()
    {
        await dispose();
    }
}

internal class DelegatedTaskHandler<TPayload> : TaskHandler<TPayload>
{
    private readonly string taskType;
    private readonly Func<TPayload, CancellationToken, Task> handler;

    public DelegatedTaskHandler(string taskType, Func<TPayload, CancellationToken, Task> handler)
    {
        this.taskType = taskType;
        this.handler = handler;
    }

    public DelegatedTaskHandler(string taskType, Action<TPayload> handler)
    {
        this.taskType = taskType;
        this.handler = (p, ct) =>
        {
            handler(p);
            return Task.CompletedTask;
        };
    }

    public override bool CanHandle(RawTask task)
    {
        return task.Type == taskType;
    }

    public override async ValueTask EnqueueAsync(TPayload payload, CancellationToken ct)
    {
        await handler(payload, ct);
    }
}

public static class KafkaMessageQueueClientExtensions
{
    public static Task EnqueueTask<T>(this KafkaMessageQueueClient client, string taskType, T taskPayload)
    {
        return client.EnqueueTask(taskType, $"task-{Guid.NewGuid()}", taskPayload);
    }
}

internal class TestData
{
    public int Id { get; set; }
    public string Name { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<int> Values { get; set; }
}
