using Microsoft.Extensions.Logging;
using NUnit.Framework;

namespace Kontur.TestCity.UnitTests;

public sealed class NUnitLoggerProvider : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName)
    {
        return new NUnitLogger(categoryName);
    }

    public void Dispose()
    {
    }

    private class NUnitLogger(string categoryName) : ILogger
    {
        private readonly string categoryName = categoryName;

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
            TestContext.Out.WriteLine($"{logLevel}: {categoryName}: {message}");

            if (exception != null)
            {
                TestContext.Out.WriteLine(exception);
            }
        }
    }
}
