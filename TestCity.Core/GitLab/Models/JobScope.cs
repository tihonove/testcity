namespace TestCity.Core.GitLab.Models;

[Flags]
public enum JobScope
{
    None = 0,
    Pending = 1 << 0,
    Running = 1 << 1,
    Failed = 1 << 2,
    Created = 1 << 3,
    Success = 1 << 4,
    Canceled = 1 << 5,
    Skipped = 1 << 6,
    WaitingForResource = 1 << 7,
    Manual = 1 << 8,
    All = Pending | Running | Failed | Created | Success | Canceled | Skipped | WaitingForResource | Manual
}
