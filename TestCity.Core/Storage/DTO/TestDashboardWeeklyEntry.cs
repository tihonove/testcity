namespace TestCity.Core.Storage.DTO;

public record TestDashboardWeeklyEntry
{
    public required string ProjectId { get; init; }
    public required string JobId { get; init; }
    public required string TestId { get; init; }
    public required ulong RunCount { get; init; }
    public required ulong FailCount { get; init; }
    public required ulong FlipCount { get; init; }
}
