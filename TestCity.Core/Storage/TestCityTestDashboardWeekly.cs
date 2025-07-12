using TestCity.Core.Clickhouse;
using TestCity.Core.Storage.DTO;
using Microsoft.Extensions.Logging;
using TestCity.Core.Logging;

namespace TestCity.Core.Storage;

public class TestCityTestDashboardWeekly(ConnectionFactory connectionFactory)
{
    public async Task<List<TestDashboardWeeklyEntry>> GetTestsByProjectAndJobAsync(
        string projectId,
        string jobId,
        CancellationToken ct = default)
    {
        await using var connection = connectionFactory.CreateConnection();

        var query = $@"
            SELECT 
                ProjectId,
                JobId,
                TestId,
                RunCount,
                FailCount,
                Entropy
            FROM TestDashboardWeekly
            WHERE ProjectId = '{projectId}' AND JobId = '{jobId}'";

        var results = new List<TestDashboardWeeklyEntry>();

        await Retry.Action(async () =>
        {
            results.Clear();
            var reader = await connection.ExecuteQueryAsync(query, ct);
            while (await reader.ReadAsync(ct))
            {
                results.Add(new TestDashboardWeeklyEntry
                {
                    ProjectId = reader.GetString(0),
                    JobId = reader.GetString(1),
                    TestId = reader.GetString(2),
                    RunCount = Convert.ToUInt64(reader.GetValue(3)),
                    FailCount = Convert.ToUInt64(reader.GetValue(4)),
                    Entropy = reader.GetDouble(5),
                });
            }
        }, TimeSpan.FromMinutes(2), logger);

        return results;
    }

    public async Task<TestDashboardWeeklyEntry?> GetTestAsync(
        string projectId,
        string jobId,
        string testId,
        CancellationToken ct = default)
    {
        await using var connection = connectionFactory.CreateConnection();

        var query = $@"
            SELECT 
                ProjectId,
                JobId,
                TestId,
                RunCount,
                FailCount,
                Entropy
            FROM TestDashboardWeekly
            WHERE ProjectId = '{projectId}' 
              AND JobId = '{jobId}' 
              AND TestId = '{testId}'
            LIMIT 1";

        TestDashboardWeeklyEntry? result = null;

        await Retry.Action(async () =>
        {
            result = null;
            var reader = await connection.ExecuteQueryAsync(query, ct);
            if (await reader.ReadAsync(ct))
            {
                result = new TestDashboardWeeklyEntry
                {
                    ProjectId = reader.GetString(0),
                    JobId = reader.GetString(1),
                    TestId = reader.GetString(2),
                    RunCount = Convert.ToUInt64(reader.GetValue(3)),
                    FailCount = Convert.ToUInt64(reader.GetValue(4)),
                    Entropy = reader.GetDouble(5),
                };
            }
        }, TimeSpan.FromMinutes(2), logger);

        return result;
    }

    public async Task<List<TestDashboardWeeklyEntry>> GetFlakyTestsAsync(
        string? projectId = null,
        double minEntropy = 0.1,
        int limit = 100,
        CancellationToken ct = default)
    {
        await using var connection = connectionFactory.CreateConnection();

        var whereClause = projectId != null
            ? $"WHERE ProjectId = '{projectId}' AND Entropy >= {minEntropy.ToString(System.Globalization.CultureInfo.InvariantCulture)}"
            : $"WHERE Entropy >= {minEntropy.ToString(System.Globalization.CultureInfo.InvariantCulture)}";

        var query = $@"
            SELECT 
                ProjectId,
                JobId,
                TestId,
                RunCount,
                FailCount,
                Entropy
            FROM TestDashboardWeekly
            {whereClause}
            ORDER BY Entropy DESC
            LIMIT {limit}";

        var results = new List<TestDashboardWeeklyEntry>();

        await Retry.Action(async () =>
        {
            results.Clear();
            var reader = await connection.ExecuteQueryAsync(query, ct);
            while (await reader.ReadAsync(ct))
            {
                results.Add(new TestDashboardWeeklyEntry
                {
                    ProjectId = reader.GetString(0),
                    JobId = reader.GetString(1),
                    TestId = reader.GetString(2),
                    RunCount = Convert.ToUInt64(reader.GetValue(3)),
                    FailCount = Convert.ToUInt64(reader.GetValue(4)),
                    Entropy = reader.GetDouble(5),
                });
            }
        }, TimeSpan.FromMinutes(2), logger);

        return results;
    }

    private readonly ILogger logger = Log.GetLog<TestCityTestDashboardWeekly>();
}
