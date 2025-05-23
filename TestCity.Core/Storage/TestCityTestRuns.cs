using ClickHouse.Client.Copy;
using TestCity.Core.Clickhouse;
using TestCity.Core.Extensions;
using TestCity.Core.Storage.DTO;
using System.Runtime.CompilerServices;
using TestCity.Core.Logging;
using Microsoft.Extensions.Logging;

namespace TestCity.Core.Storage;

public class TestCityTestRuns(ConnectionFactory connectionFactory)
{
    public Task InsertBatchAsync(JobRunInfo jobRunInfo, IEnumerable<TestRun> lines)
    {
        return InsertBatchAsync(jobRunInfo, lines.ToAsyncEnumerable());
    }

    public async Task InsertBatchAsync(JobRunInfo info, IAsyncEnumerable<TestRun> lines)
    {
        await using var connection = connectionFactory.CreateConnection();
        using var bulkCopyInterface = new ClickHouseBulkCopy(connection)
        {
            DestinationTableName = "TestRuns",
            BatchSize = 1000,
            ColumnNames = Fields,
        };
        await bulkCopyInterface.InitAsync();
        await foreach (var testRuns in lines.Batches(1000))
        {
            var values = testRuns.Select(x =>
                new object?[]
                {
                    info.JobId, info.JobRunId, info.ProjectId, info.BranchName, x.TestId, (int)x.TestResult, x.Duration,
                    x.StartDateTime.ToUniversalTime(), info.AgentName, info.AgentOSName, info.JobUrl,
                    x.JUnitFailureMessage, x.JUnitFailureOutput, x.JUnitSystemOutput,
                });
            await bulkCopyInterface.WriteToServerAsync(values);
        }
    }

    public async Task InsertBatchAsync(IAsyncEnumerable<(JobRunInfo, TestRun)> lines)
    {
        await using var connection = connectionFactory.CreateConnection();
        using var bulkCopyInterface = new ClickHouseBulkCopy(connection)
        {
            DestinationTableName = "TestRuns",
            BatchSize = 1000,
            ColumnNames = Fields,
        };
        await bulkCopyInterface.InitAsync();
        await foreach (var testRunLine in lines.Batches(5000))
        {
            var values = testRunLine.Select(x =>
                new object?[]
                {
                    x.Item1.JobId, x.Item1.JobRunId, x.Item1.ProjectId, x.Item1.BranchName, x.Item2.TestId, (int)x.Item2.TestResult, x.Item2.Duration,
                    x.Item2.StartDateTime.ToUniversalTime(), x.Item1.AgentName, x.Item1.AgentOSName, x.Item1.JobUrl,
                    x.Item2.JUnitFailureMessage, x.Item2.JUnitFailureOutput, x.Item2.JUnitSystemOutput,
                });
            await bulkCopyInterface.WriteToServerAsync(values);
        }
    }

    public async IAsyncEnumerable<(JobRunInfo, TestRun)> GetAllAsync([EnumeratorCancellation] CancellationToken ct = default)
    {
        const int batchSize = 10000;
        var offset = 0;
        var timeoutBudget = TimeSpan.FromMinutes(10);
        var hasMoreRecords = true;
        await using var connection = connectionFactory.CreateConnection();

        while (hasMoreRecords && !ct.IsCancellationRequested)
        {
            // Define batch query with pagination
            var batchQuery = $@"
                SELECT
                    tr.JobId,
                    tr.JobRunId,
                    tr.ProjectId,
                    tr.BranchName,
                    tr.TestId,
                    tr.State,
                    tr.Duration,
                    tr.StartDateTime,
                    tr.AgentName,
                    tr.AgentOSName,
                    tr.JobUrl,
                    tr.JUnitFailureMessage,
                    tr.JUnitFailureOutput,
                    tr.JUnitSystemOutput
                FROM TestRuns tr
                LIMIT {batchSize}
                OFFSET {offset}
            ";

            // Process this batch with retry logic
            var currentBatchResults = new List<(JobRunInfo, TestRun)>();
            await Retry.Action(async () =>
            {
                currentBatchResults.Clear();
                var reader = await connection.ExecuteQueryAsync(batchQuery, ct);
                while (await reader.ReadAsync(ct))
                {
                    var jobRunInfo = new JobRunInfo
                    {
                        JobId = reader.GetString(0),
                        JobRunId = reader.GetString(1),
                        ProjectId = reader.GetString(2),
                        BranchName = reader.GetString(3),
                        AgentName = reader.GetString(8),
                        AgentOSName = reader.GetString(9),
                        JobUrl = reader.GetString(10),
                        PipelineId = string.Empty // Дополнительное поле, требуемое для JobRunInfo
                    };

                    var testRun = new TestRun
                    {
                        TestId = reader.GetString(4),
                        TestResult = Enum.Parse<TestResult>(reader.GetString(5)),
                        Duration = (long)reader.GetDecimal(6),
                        StartDateTime = reader.GetDateTime(7),
                        JUnitFailureMessage = reader.IsDBNull(11) ? null : reader.GetString(11),
                        JUnitFailureOutput = reader.IsDBNull(12) ? null : reader.GetString(12),
                        JUnitSystemOutput = reader.IsDBNull(13) ? null : reader.GetString(13)
                    };

                    currentBatchResults.Add((jobRunInfo, testRun));
                }
                hasMoreRecords = currentBatchResults.Count == batchSize;

            }, timeoutBudget, logger);
            offset += currentBatchResults.Count;
            foreach (var result in currentBatchResults)
            {
                yield return result;
            }
        }
    }

    public async IAsyncEnumerable<(JobRunInfo, TestRun)> GetByDateAsync(DateTime date, [EnumeratorCancellation] CancellationToken ct = default)
    {
        var startOfDay = new DateTime(date.Year, date.Month, date.Day, 0, 0, 0, DateTimeKind.Utc);
        var endOfDay = startOfDay.AddDays(1);

        var timeoutBudget = TimeSpan.FromMinutes(60);
        await using var connection = connectionFactory.CreateConnection();

        // Define query for specific date
        var query = $@"
            SELECT
                tr.JobId,
                tr.JobRunId,
                tr.ProjectId,
                tr.BranchName,
                tr.TestId,
                tr.State,
                tr.Duration,
                tr.StartDateTime,
                tr.AgentName,
                tr.AgentOSName,
                tr.JobUrl,
                tr.JUnitFailureMessage,
                tr.JUnitFailureOutput,
                tr.JUnitSystemOutput
            FROM TestRuns tr
            WHERE tr.StartDateTime >= '{startOfDay:yyyy-MM-dd HH:mm:ss}'
              AND tr.StartDateTime < '{endOfDay:yyyy-MM-dd HH:mm:ss}'
        ";

        // Process results with retry logic
        var results = new List<(JobRunInfo, TestRun)>();
        await Retry.Action(async () =>
        {
            results.Clear();
            var reader = await connection.ExecuteQueryAsync(query, ct);
            while (await reader.ReadAsync(ct))
            {
                var jobRunInfo = new JobRunInfo
                {
                    JobId = reader.GetString(0),
                    JobRunId = reader.GetString(1),
                    ProjectId = reader.GetString(2),
                    BranchName = reader.GetString(3),
                    AgentName = reader.GetString(8),
                    AgentOSName = reader.GetString(9),
                    JobUrl = reader.GetString(10),
                    PipelineId = string.Empty // Дополнительное поле, требуемое для JobRunInfo
                };

                var testRun = new TestRun
                {
                    TestId = reader.GetString(4),
                    TestResult = Enum.Parse<TestResult>(reader.GetString(5)),
                    Duration = (long)reader.GetDecimal(6),
                    StartDateTime = reader.GetDateTime(7),
                    JUnitFailureMessage = reader.IsDBNull(11) ? null : reader.GetString(11),
                    JUnitFailureOutput = reader.IsDBNull(12) ? null : reader.GetString(12),
                    JUnitSystemOutput = reader.IsDBNull(13) ? null : reader.GetString(13)
                };

                results.Add((jobRunInfo, testRun));
            }
        }, timeoutBudget);

        foreach (var result in results)
        {
            yield return result;
        }
    }

    public async IAsyncEnumerable<(JobRunInfo, TestRun)> GetByHourAsync(DateTime dateTime, [EnumeratorCancellation] CancellationToken ct = default)
    {
        var startOfHour = new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour, 0, 0, DateTimeKind.Utc);
        var endOfHour = startOfHour.AddHours(1);

        var timeoutBudget = TimeSpan.FromMinutes(30);

        var query = $@"
            SELECT
                tr.JobId,
                tr.JobRunId,
                tr.ProjectId,
                tr.BranchName,
                tr.TestId,
                tr.State,
                tr.Duration,
                tr.StartDateTime,
                tr.AgentName,
                tr.AgentOSName,
                tr.JobUrl,
                tr.JUnitFailureMessage,
                tr.JUnitFailureOutput,
                tr.JUnitSystemOutput
            FROM TestRuns tr
            WHERE tr.StartDateTime >= '{startOfHour:yyyy-MM-dd HH:mm:ss}'
              AND tr.StartDateTime < '{endOfHour:yyyy-MM-dd HH:mm:ss}'
        ";

        // Process results with retry logic
        var results = new List<(JobRunInfo, TestRun)>();
        await Retry.Action(async () =>
        {
            results.Clear();
            await using var connection = connectionFactory.CreateConnection();
            await using var reader = await connection.ExecuteQueryAsync(query, ct);
            while (await reader.ReadAsync(ct))
            {
                var jobRunInfo = new JobRunInfo
                {
                    JobId = reader.GetString(0),
                    JobRunId = reader.GetString(1),
                    ProjectId = reader.GetString(2),
                    BranchName = reader.GetString(3),
                    AgentName = reader.GetString(8),
                    AgentOSName = reader.GetString(9),
                    JobUrl = reader.GetString(10),
                    PipelineId = string.Empty // Дополнительное поле, требуемое для JobRunInfo
                };

                var testRun = new TestRun
                {
                    TestId = reader.GetString(4),
                    TestResult = Enum.Parse<TestResult>(reader.GetString(5)),
                    Duration = (long)reader.GetDecimal(6),
                    StartDateTime = reader.GetDateTime(7),
                    JUnitFailureMessage = reader.IsDBNull(11) ? null : reader.GetString(11),
                    JUnitFailureOutput = reader.IsDBNull(12) ? null : reader.GetString(12),
                    JUnitSystemOutput = reader.IsDBNull(13) ? null : reader.GetString(13)
                };

                results.Add((jobRunInfo, testRun));

                if (results.Count % 1000 == 0)
                {
                    logger.LogInformation("  Read {Count} record", results.Count);
                }
            }
        }, timeoutBudget, logger);

        foreach (var result in results)
        {
            yield return result;
        }
    }

    private static readonly string[] Fields =
    [
        "JobId",
        "JobRunId",
        "ProjectId",
        "BranchName",
        "TestId",
        "State",
        "Duration",
        "StartDateTime",
        "AgentName",
        "AgentOSName",
        "JobUrl",
        "JUnitFailureMessage",
        "JUnitFailureOutput",
        "JUnitSystemOutput",
    ];
    private readonly ILogger logger = Log.GetLog<TestCityTestRuns>();
}
