namespace TestCity.Core.Worker.TaskPayloads;

public class RecalculateTestStatisticsTaskPayload
{
    public const string TaskType = "RecalculateTestStatistics";
    public long ProjectId { get; set; }
    public string JobId { get; set; }
    public string BranchName { get; set; }
}
