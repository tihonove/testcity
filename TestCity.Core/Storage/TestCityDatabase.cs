using Kontur.TestCity.Core.Clickhouse;

namespace Kontur.TestCity.Core.Storage;

public class TestCityDatabase(ConnectionFactory connectionFactory)
{
    public TestCityTestRuns TestRuns { get; } = new TestCityTestRuns(connectionFactory);
    public TestCityCommitParents CommitParents { get; } = new TestCityCommitParents(connectionFactory);
    public TestCityJobInfo JobInfo { get; } = new TestCityJobInfo(connectionFactory);
}
