using TestCity.Core.KafkaMessageQueue;
using TestCity.Core.Worker;
using TestCity.Core.Worker.TaskPayloads;

namespace TestCity.Worker.Handlers;

public sealed class RecalculateTestStatisticsHandler : TaskHandler<RecalculateTestStatisticsTaskPayload>
{
    public override bool CanHandle(RawTask task)
    {
        return task.Type == RecalculateTestStatisticsTaskPayload.TaskType;
    }

    public override async ValueTask EnqueueAsync(RecalculateTestStatisticsTaskPayload task, CancellationToken ct)
    {
        // TODO: Implement test statistics recalculation logic
        // Available fields:
        // - task.ProjectId (long)
        // - task.JobId (long) 
        // - task.BranchName (string)

        await Task.CompletedTask;
    }
}
