using Kontur.TestCity.Core.Worker;

namespace Kontur.TestCity.Worker.Handlers.Base;

public interface ITaskHandler
{
    bool CanHandle(RawTask type);

    ValueTask EnqueueAsync(RawTask payload, CancellationToken ct);
}
