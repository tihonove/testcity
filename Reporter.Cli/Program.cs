using CommandLine;
using dotenv.net;
using Kontur.TestAnalytics.Reporter.Cli;
using Vostok.Logging.Console;

DotEnv.Fluent().WithProbeForEnv(10).Load();

await Parser.Default.ParseArguments<JunitRepoterOptions>(args)
    .MapResult( 
        (JunitRepoterOptions options) => new JunitReporter(options, new ConsoleLog()).DoAsync(),
        errs => throw new ArgumentException());