using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Kontur.TestCity.Core.Logging;

public static class Log
{
    private static ILoggerFactory loggerFactory = new NullLoggerFactory();

    public static void ConfigureGlobalLogProvider(ILoggerFactory globalLoggerFactory)
    {
        loggerFactory = globalLoggerFactory;
    }

    public static ILogger LogForMe<T>(this T _)
    {
        return loggerFactory.CreateLogger<T>();
    }
    public static ILogger GetLog<T>()
    {
        return loggerFactory.CreateLogger<T>();
    }
}
