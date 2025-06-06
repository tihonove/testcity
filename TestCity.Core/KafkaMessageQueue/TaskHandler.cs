using System.Text.Json;
using TestCity.Core.Worker;

namespace TestCity.Core.KafkaMessageQueue;

public abstract class TaskHandler<TPayload> : ITaskHandler
{
    public abstract bool CanHandle(RawTask task);

    public abstract ValueTask EnqueueAsync(TPayload payload, CancellationToken ct);

    public async ValueTask ExecuteAsync(RawTask task, CancellationToken ct)
    {
        var payload = Deserialize(task.Payload);
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
