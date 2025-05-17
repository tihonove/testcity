namespace TestCity.Core.GitLab.Models;

public static class JobScopeExtensions
{
    public static IEnumerable<JobScope> GetIndividualScopes(this JobScope scope)
    {
        if (scope.HasFlag(JobScope.Pending))
            yield return JobScope.Pending;
        if (scope.HasFlag(JobScope.Running))
            yield return JobScope.Running;
        if (scope.HasFlag(JobScope.Failed))
            yield return JobScope.Failed;
        if (scope.HasFlag(JobScope.Created))
            yield return JobScope.Created;
        if (scope.HasFlag(JobScope.Success))
            yield return JobScope.Success;
        if (scope.HasFlag(JobScope.Canceled))
            yield return JobScope.Canceled;
        if (scope.HasFlag(JobScope.Skipped))
            yield return JobScope.Skipped;
        if (scope.HasFlag(JobScope.WaitingForResource))
            yield return JobScope.WaitingForResource;
        if (scope.HasFlag(JobScope.Manual))
            yield return JobScope.Manual;
    }

    public static string ToStringValue(this JobScope scope)
    {
        return scope switch
        {
            JobScope.Pending => "pending",
            JobScope.Running => "running",
            JobScope.Failed => "failed",
            JobScope.Created => "created",
            JobScope.Success => "success",
            JobScope.Canceled => "canceled",
            JobScope.Skipped => "skipped",
            JobScope.WaitingForResource => "waiting_for_resource",
            JobScope.Manual => "manual",
            _ => throw new ArgumentOutOfRangeException(nameof(scope), scope, null)
        };
    }
}
