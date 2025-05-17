using TestCity.Core.KafkaMessageQueue;
using TestCity.Core.Worker.TaskPayloads;

namespace TestCity.Core.Worker;

public class WorkerClient(KafkaMessageQueueClient messageQueueClient)
{
    public async Task Enqueue(ProcessJobRunTaskPayload taskPayload)
    {
        await messageQueueClient.EnqueueTask(ProcessJobRunTaskPayload.TaskType, taskPayload.ProjectId.ToString(), taskPayload);
    }
    
    public async Task Enqueue(ProcessInProgressJobTaskPayload taskPayload)
    {
        await messageQueueClient.EnqueueTask(ProcessInProgressJobTaskPayload.TaskType, taskPayload.ProjectId.ToString(), taskPayload);
    }
    
    public async Task Enqueue(BuildCommitParentsTaskPayload taskPayload)
    {
        await messageQueueClient.EnqueueTask(BuildCommitParentsTaskPayload.TaskType, taskPayload.ProjectId + "-" + taskPayload.CommitSha, taskPayload);
    }
}
