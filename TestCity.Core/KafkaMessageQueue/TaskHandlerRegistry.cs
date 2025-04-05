using Kontur.TestCity.Core.Worker;
using Kontur.TestCity.Worker.Handlers.Base;

namespace Kontur.TestCity.Worker.Kafka;

public class TaskHandlerRegistry(IEnumerable<ITaskHandler> taskHandlers)
{
    public async Task DispatchTaskAsync(RawTask task, CancellationToken ct)
    {
        foreach (var handler in taskHandlers)
        {
            if (handler.CanHandle(task))
            {
                await handler.ExecuteAsync(task, ct);
                return;
            }
        }

        throw new Exception($"No handler found for task type: {task.Type}");
    }

    private readonly List<ITaskHandler> taskHandlers = [.. taskHandlers];
}
