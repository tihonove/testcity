using CommandLine;
using dotenv.net;
using Kontur.TestAnalytics.Reporter.Cli;
using Vostok.Logging.Abstractions;
using Vostok.Logging.Console;

DotEnv.Fluent().WithProbeForEnv(10).Load();
var options = Parser.Default.ParseArguments<JunitReporterOptions>(args).GetOptionsOrThrow();
LogProvider.Configure(new ConsoleLog(), true);

var reporter = new JunitReporter(options);
await reporter.DoAsync();

ConsoleLog.Flush();