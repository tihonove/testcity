using Microsoft.Extensions.Logging;
using TestCity.Core.Logging;
using Xunit.Abstractions;

namespace TestCity.UnitTests.Utils;

public sealed class XUnitLoggerProvider(ITestOutputHelper output) : ILoggerProvider
{
    public static void ConfigureTestLogger(ITestOutputHelper output)
    {
        Log.ConfigureGlobalLogProvider(LoggerFactory.Create(builder => builder.AddProvider(new XUnitLoggerProvider(output)).SetMinimumLevel(LogLevel.Debug)));
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new NUnitLogger(output, categoryName);
    }

    public void Dispose()
    {
    }

    private class NUnitLogger(ITestOutputHelper output, string categoryName) : ILogger
    {
        private readonly string categoryName = categoryName.Split(".").Last();

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => default!;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            var message = formatter(state, exception);
            var logLevelString = logLevel switch
            {
                LogLevel.Trace => "trace",
                LogLevel.Debug => "debug",
                LogLevel.Information => "info ",
                LogLevel.Warning => "warn ",
                LogLevel.Error => "ERROR",
                LogLevel.Critical => "CRITL",
                _ => "UNKWN"
            };
            output.WriteLine($"{logLevelString} [{categoryName}] {message}");

            if (exception != null)
            {
                output.WriteLine(exception.ToString());
            }
        }
    }
}
