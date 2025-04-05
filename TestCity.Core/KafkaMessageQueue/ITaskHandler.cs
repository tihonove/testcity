using Kontur.TestCity.Core.Worker;

namespace Kontur.TestCity.Worker.Handlers.Base;

public interface ITaskHandler
{
    bool CanHandle(RawTask type);

    ValueTask ExecuteAsync(RawTask payload, CancellationToken ct);
}
