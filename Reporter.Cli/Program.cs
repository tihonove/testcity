using CommandLine;
using dotenv.net;
using Kontur.TestAnalytics.Reporter.Cli;
using Vostok.Logging.Abstractions;
using Vostok.Logging.Console;

DotEnv.Fluent().WithProbeForEnv(10).Load();
if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("TESTANALYTICS_CLICKHOUSE_HOST")))
{
    Environment.SetEnvironmentVariable("TESTANALYTICS_CLICKHOUSE_HOST", "vm-ch2-stg.dev.kontur.ru");
    Environment.SetEnvironmentVariable("TESTANALYTICS_CLICKHOUSE_PORT", "8123");
    Environment.SetEnvironmentVariable("TESTANALYTICS_CLICKHOUSE_DB", "test_analytics");
    Environment.SetEnvironmentVariable("TESTANALYTICS_CLICKHOUSE_USER", "tihonove");
    Environment.SetEnvironmentVariable("TESTANALYTICS_CLICKHOUSE_PASSWORD", "12487562");
}

var options = Parser.Default.ParseArguments<JunitReporterOptions>(args).GetOptionsOrThrow();
LogProvider.Configure(new ConsoleLog(), true);

var reporter = new JunitReporter(options);
await reporter.DoAsync();

ConsoleLog.Flush();
