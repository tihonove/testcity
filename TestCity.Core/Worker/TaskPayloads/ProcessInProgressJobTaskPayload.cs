namespace TestCity.Core.Worker.TaskPayloads;

public class ProcessInProgressJobTaskPayload
{
    public const string TaskType = "ProcessInProgressJob";

    public long ProjectId { get; set; }
    public long JobRunId { get; set; }
}
