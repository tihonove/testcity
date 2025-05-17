namespace TestCity.Core.Storage.DTO;

public class InProgressJobInfo
{
    public string JobId { get; set; } = string.Empty;
    public string JobRunId { get; set; } = string.Empty;
    public string? JobUrl { get; set; }
    public DateTime StartDateTime { get; set; }
    public string? PipelineSource { get; set; }
    public string? Triggered { get; set; }
    public string? BranchName { get; set; }
    public string? CommitSha { get; set; }
    public string? CommitMessage { get; set; }
    public string? CommitAuthor { get; set; }
    public string? AgentName { get; set; }
    public string? AgentOSName { get; set; }
    public string ProjectId { get; set; } = string.Empty;
    public string? PipelineId { get; set; }
    public List<CommitParentsChangesEntry> ChangesSinceLastRun { get; set; } = new();
}
