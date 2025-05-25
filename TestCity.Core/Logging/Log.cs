using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace TestCity.Core.Logging;

public static class Log
{
    public static ILoggerFactory LoggerFactory { get; private set; } = new NullLoggerFactory();

    public static void ConfigureGlobalLogProvider(ILoggerFactory globalLoggerFactory)
    {
        LoggerFactory = globalLoggerFactory;
    }

    public static ILogger LogForMe<T>(this T _)
    {
        return LoggerFactory.CreateLogger<T>();
    }
    public static ILogger GetLog<T>()
    {
        return LoggerFactory.CreateLogger<T>();
    }

    public static ILogger GetLog(string name)
    {
        return LoggerFactory.CreateLogger(name);
    }
}
