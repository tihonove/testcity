namespace Kontur.TestCity.Core.Worker.TaskPayloads;

public class BuildCommitParentsTaskPayload
{
    public const string TaskType = "BuildCommitParents";

    public long ProjectId { get; set; }
    public string CommitSha { get; set; }
}
