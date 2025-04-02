using Kontur.TestCity.Core.Worker;
using Kontur.TestCity.Core.Worker.TaskPayloads;
using Kontur.TestCity.Worker.Handlers.Base;
using Microsoft.Extensions.Logging;

namespace Kontur.TestCity.Worker.Handlers;

public class ProcessJobRunTaskHandler(ILogger<ProcessJobRunTaskHandler> logger) : TaskHandler<ProcessJobRunTaskPayload>
{
    public override bool CanHandle(RawTask task)
    {
        return task.Type == ProcessJobRunTaskPayload.TaskType;
    }

    public override async ValueTask EnqueueAsync(ProcessJobRunTaskPayload task, CancellationToken ct)
    {
        await Task.Delay(100, ct);
        logger.LogInformation("Process job run {ProjectId}, job run id: {JobRunId}", task.ProjectId, task.JobRunId);
    }

    private readonly ILogger<ProcessJobRunTaskHandler> logger = logger;
}
