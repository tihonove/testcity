namespace TestCity.Api.Models;
public class ManualJobRunInfo
{
    public string JobId { get; set; }
    public string JobRunId { get; set; }
    public ManualJobRunStatus Status { get; set; }
}
