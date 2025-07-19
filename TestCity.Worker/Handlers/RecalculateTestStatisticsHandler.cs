using TestCity.Core.KafkaMessageQueue;
using TestCity.Core.Storage;
using TestCity.Core.Worker;
using TestCity.Core.Worker.TaskPayloads;

namespace TestCity.Worker.Handlers;

public sealed class RecalculateTestStatisticsHandler(TestCityDatabase testCityDatabase) : TaskHandler<RecalculateTestStatisticsTaskPayload>
{
    public override bool CanHandle(RawTask task)
    {
        return task.Type == RecalculateTestStatisticsTaskPayload.TaskType;
    }

    public override async ValueTask EnqueueAsync(RecalculateTestStatisticsTaskPayload task, CancellationToken ct)
    {
        await testCityDatabase.TestDashboardWeekly.ActualizeAsync(task.ProjectId.ToString(), task.JobId, task.BranchName);
    }
}
