namespace Kontur.TestCity.Core.Storage.DTO;

public class JobRunInfo
{
    public required string JobId { get; init; }
    public required string ProjectId { get; init; }
    public required string? PipelineId { get; init; }
    public required string JobRunId { get; init; }
    public required string? BranchName { get; init; }
    public required string AgentName { get; init; }
    public required string AgentOSName { get; init; }
    public required string? JobUrl { get; init; }
}
