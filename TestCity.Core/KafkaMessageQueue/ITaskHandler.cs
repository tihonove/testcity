using TestCity.Core.Worker;

namespace TestCity.Core.KafkaMessageQueue;

public interface ITaskHandler
{
    bool CanHandle(RawTask type);

    ValueTask ExecuteAsync(RawTask payload, CancellationToken ct);
}
