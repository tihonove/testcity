using Kontur.TestCity.Core.Worker;

namespace Kontur.TestCity.Core.KafkaMessageQueue;

public interface ITaskHandler
{
    bool CanHandle(RawTask type);

    ValueTask ExecuteAsync(RawTask payload, CancellationToken ct);
}
