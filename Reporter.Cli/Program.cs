using CommandLine;
using dotenv.net;
using Kontur.TestAnalytics.Reporter.Cli;
using Vostok.Logging.Console;

DotEnv.Fluent().WithProbeForEnv(10).Load();
var options = Parser.Default.ParseArguments<JunitReporterOptions>(args).GetOptionsOrThrow();

var reporter = new JunitReporter(options, new ConsoleLog());
await reporter.DoAsync();

ConsoleLog.Flush();