using System.Text.Json;
using Kontur.TestCity.Core.Worker;

namespace Kontur.TestCity.Worker.Handlers.Base;

public abstract class TaskHandler<TPayload> : ITaskHandler
{
    public abstract bool CanHandle(RawTask task);

    public abstract ValueTask EnqueueAsync(TPayload payload, CancellationToken ct);

    public async ValueTask ExecuteAsync(RawTask task, CancellationToken ct)
    {
        var payload = TaskHandler<TPayload>.Deserialize(task.Payload);
        await EnqueueAsync(payload, ct);
    }

    private static TPayload Deserialize(JsonElement element)
    {
        try
        {
            return element.Deserialize<TPayload>() ?? throw new InvalidOperationException($"Failed to deserialize payload to {typeof(TPayload).Name}");
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to deserialize payload to {typeof(TPayload).Name}", ex);
        }
    }
}
