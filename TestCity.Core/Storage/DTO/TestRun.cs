namespace TestCity.Core.Storage.DTO;

public record TestRun
{
    public required string TestId { get; init; }
    public required TestResult TestResult { get; init; }
    public required long Duration { get; init; }
    public required DateTime StartDateTime { get; init; }
    public string? JUnitFailureMessage { get; init; }
    public string? JUnitFailureOutput { get; init; }
    public string? JUnitSystemOutput { get; init; }
}
