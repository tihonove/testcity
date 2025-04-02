using Kontur.TestCity.Core.Worker;
using Kontur.TestCity.Worker.Handlers.Base;
using Microsoft.Extensions.Logging;
using TestCity.Worker.Kafka;

namespace Kontur.TestCity.Worker.Kafka;

public class TaskHandlerRegistry(ILogger<TaskHandlerRegistry> logger, IEnumerable<ITaskHandler> taskHandlers) : ITaskHandlerRegistry
{
    public async Task<bool> DispatchTaskAsync(RawTask task, CancellationToken ct)
    {
        foreach (var handler in taskHandlers)
        {
            if (handler.CanHandle(task))
            {
                await handler.EnqueueAsync(task, ct);
                return true;
            }
        }

        logger.LogWarning("No handler found for task type: {TaskType}", task.Type);
        return false;
    }

    private readonly ILogger<TaskHandlerRegistry> logger = logger;
    private readonly List<ITaskHandler> taskHandlers = [.. taskHandlers];
}
