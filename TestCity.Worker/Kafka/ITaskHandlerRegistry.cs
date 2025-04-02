using Kontur.TestCity.Core.Worker;

namespace TestCity.Worker.Kafka;

public interface ITaskHandlerRegistry
{
    Task<bool> DispatchTaskAsync(RawTask task, CancellationToken ct);
}
