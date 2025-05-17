using TestCity.Core.JobProcessing;
using TestCity.Core.KafkaMessageQueue;
using TestCity.Core.Worker;
using TestCity.Core.Worker.TaskPayloads;

namespace TestCity.Worker.Handlers;

public sealed class BuildCommitParentsHandler(CommitParentsBuilderService commitParentsBuilder) : TaskHandler<BuildCommitParentsTaskPayload>
{
    public override bool CanHandle(RawTask task)
    {
        return task.Type == BuildCommitParentsTaskPayload.TaskType;
    }

    public override async ValueTask EnqueueAsync(BuildCommitParentsTaskPayload task, CancellationToken ct)
    {
        await commitParentsBuilder.BuildCommitParent(task.ProjectId, task.CommitSha, ct);
    }
}
