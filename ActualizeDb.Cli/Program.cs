using Kontur.TestAnalytics.ActualizeDb.Cli;
using Vostok.Hosting.Houston;
using Vostok.Hosting.Houston.Abstractions;
using Vostok.Hosting.Setup;

[assembly: HoustonEntryPoint(typeof(ActualizeDatabaseApplication))]

var houstonHost = new HoustonHost(
    new ActualizeDatabaseApplication(),
    config => config.Everywhere.SetupEnvironment(builder => builder.DisableServiceBeacon()));
await houstonHost.RunAsync();
