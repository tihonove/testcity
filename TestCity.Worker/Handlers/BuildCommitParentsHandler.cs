using Kontur.TestCity.Core.JobProcessing;
using Kontur.TestCity.Core.KafkaMessageQueue;
using Kontur.TestCity.Core.Worker;
using Kontur.TestCity.Core.Worker.TaskPayloads;

namespace Kontur.TestCity.Worker.Handlers;

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
