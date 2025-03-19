using CommandLine;
using dotenv.net;
using Kontur.TestAnalytics.Reporter.Cli;
using Microsoft.Extensions.Logging;

DotEnv.Fluent().WithProbeForEnv(10).Load();
var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug));
var logger = loggerFactory.CreateLogger<JunitReporter>();

if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("TESTANALYTICS_CLICKHOUSE_HOST")))
{
    Environment.SetEnvironmentVariable("TESTANALYTICS_CLICKHOUSE_HOST", "vm-ch2-stg.dev.kontur.ru");
    Environment.SetEnvironmentVariable("TESTANALYTICS_CLICKHOUSE_PORT", "8123");
    Environment.SetEnvironmentVariable("TESTANALYTICS_CLICKHOUSE_DB", "test_analytics");
    Environment.SetEnvironmentVariable("TESTANALYTICS_CLICKHOUSE_USER", "tihonove");
    Environment.SetEnvironmentVariable("TESTANALYTICS_CLICKHOUSE_PASSWORD", "12487562");
}

var options = Parser.Default.ParseArguments<JunitReporterOptions>(args).GetOptionsOrThrow();
var reporter = new JunitReporter(options, logger);
await reporter.DoAsync();
