namespace TestCity.Core.Worker.TaskPayloads;

public class ProcessJobRunTaskPayload
{
    public const string TaskType = "ProcessJobRun";

    public long ProjectId { get; set; }
    public long JobRunId { get; set; }
}
