using TestCity.Core.Clickhouse;
using TestCity.Core.Storage.DTO;

namespace TestCity.Core.Storage;

public class TestCityDatabase(ConnectionFactory connectionFactory)
{
    public TestCityTestRuns TestRuns { get; } = new TestCityTestRuns(connectionFactory);
    public TestCityCommitParents CommitParents { get; } = new TestCityCommitParents(connectionFactory);
    public TestCityJobInfo JobInfo { get; } = new TestCityJobInfo(connectionFactory);
    public TestCityInProgressJobInfo InProgressJobInfo { get; } = new TestCityInProgressJobInfo(connectionFactory);
    public TestCityGitLabEntities GitLabEntities { get; } = new TestCityGitLabEntities(connectionFactory);
    public TestCityTestDashboardWeekly TestDashboardWeekly { get; } = new TestCityTestDashboardWeekly(connectionFactory);

    public async Task<List<CommitParentsChangesEntry>> GetCommitChangesAsync(string commitSha, string jobId, string branchName, CancellationToken ct = default)
    {
        return await CommitParents.GetChangesAsync(commitSha, jobId, branchName, ct);
    }
}
