using ClickHouse.Client.ADO;
using ClickHouse.Client.Utility;
using Kontur.TestAnalytics.Reporter.Client.Impl;

namespace Kontur.TestAnalytics.Reporter.Client;

public static class TestRunsUploader
{
    public static async Task UploadAsync(JobRunInfo jobRunInfo, IAsyncEnumerable<TestRun> lines)
    {
        await using var connection = CreateConnection();
        var uploader = new TestRunsUploaderInternal(connection);
        await uploader.UploadAsync(jobRunInfo, lines);
    }

    public static Task UploadAsync(JobRunInfo jobRunInfo, IEnumerable<TestRun> lines)
    {
        return UploadAsync(jobRunInfo, lines.ToAsyncEnumerable());
    }

    public static async Task<bool> IsJobRunIdExists(string jobId)
    {
        await using var connection = CreateConnection();
        var result = await connection.ExecuteScalarAsync($"Select count(JobRunId) > 0 from JobInfo where JobInfo.JobRunId == '{jobId}'");
        return (byte)result == 1;
    }

    public static async Task JobInfoUploadAsync(FullJobInfo jobInfo)
    {
        await using var connection = CreateConnection();
        var uploader = new JobInfoUploaderInternal(connection);
        await uploader.UploadAsync(jobInfo);
    }

    private static ClickHouseConnection CreateConnection()
    {
        return new ClickHouseConnection(
            "Host=vm-ch2-stg.dev.kontur.ru;Port=8123;Username=tihonove;password=12487562;Database=test_analytics");
    }
}
