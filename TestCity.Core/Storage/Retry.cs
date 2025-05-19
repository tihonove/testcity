using Microsoft.Extensions.Logging;

namespace TestCity.Core.Storage;

public static class Retry
{
    public static async Task Action(Func<Task> action, TimeSpan timeBudget, ILogger? logger = null)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var attemptCount = 0;

        while (stopwatch.Elapsed < timeBudget)
        {
            attemptCount++;
            try
            {
                await action();
                return;
            }
            catch (Exception ex)
            {
                if (stopwatch.Elapsed + TimeSpan.FromSeconds(1) >= timeBudget)
                {
                    throw new Exception($"Не удалось выполнить действие после {attemptCount} попыток в течение {timeBudget.TotalSeconds} секунд. Последняя ошибка: {ex.Message}", ex);
                }
                logger?.LogWarning("Не удалось выполнить действие с {attemptCount} попытки. Пробуем ещё раз. {message}", attemptCount, ex.Message);
                await Task.Delay(TimeSpan.FromSeconds(1));
            }
        }
    }
    
}
