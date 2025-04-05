using Kontur.TestCity.Core.KafkaMessageQueue;
using Kontur.TestCity.Core.Worker.TaskPayloads;

namespace Kontur.TestCity.Core.Worker;

public class WorkerClient(KafkaMessageQueueClient messageQueueClient)
{
    public async Task Enqueue(ProcessJobRunTaskPayload taskPayload)
    {
        await messageQueueClient.EnqueueTask(ProcessJobRunTaskPayload.TaskType, taskPayload.ProjectId.ToString(), taskPayload);
    }
}
