using System.Diagnostics;
using Xunit;

namespace TestCity.UnitTests.Utils;

internal static class AssertEventually
{
    internal static async Task AssertEqual(Func<int> getActualValue, int expected)
    {
        var sw = Stopwatch.StartNew();
        while (sw.Elapsed < TimeSpan.FromSeconds(60))
        {
            try
            {
                var actualValue = getActualValue();
                Assert.Equal(expected, getActualValue());
                return;
                // if (actualValue != expected)
                // {
                //     await Task.Delay(1000);
                // }
                // else
                // {
                //     return;
                // }
            }
            catch
            {
                await Task.Delay(1000);
            }
        }

        // Если после всех попыток значение не равно ожидаемому, вызываем финальный ассерт, который выбросит исключение
        Assert.Equal(expected, getActualValue());
    }

    internal static async Task AssertContains<T>(Func<IEnumerable<T>> getActualValue, T expected)
    {
        var sw = Stopwatch.StartNew();
        while (sw.Elapsed < TimeSpan.FromSeconds(60))
        {
            try
            {
                var actualValue = getActualValue();
                Assert.Contains(expected, getActualValue());
                return;
                // if (actualValue != expected)
                // {
                //     await Task.Delay(1000);
                // }
                // else
                // {
                //     return;
                // }
            }
            catch
            {
                await Task.Delay(1000);
            }
        }

        Assert.Contains(expected, getActualValue());
    }

    internal static async Task That<T>(Func<T> value, Func<T, bool> assertion, string? message = null)
    {
        var sw = Stopwatch.StartNew();
        while (sw.Elapsed < TimeSpan.FromSeconds(60))
        {
            try
            {
                var result = assertion(value());
                if (!result)
                {
                    await Task.Delay(1000);
                }
                else
                {
                    return;
                }
            }
            catch
            {
                await Task.Delay(1000);
            }
        }

        // Если после всех попыток условие не выполнено, вызываем финальный ассерт, который выбросит исключение
        if (message != null)
        {
            Assert.True(assertion(value()), message);
        }
        else
        {
            Assert.True(assertion(value()));
        }
    }

    // Вспомогательные методы для удобства использования в тестах
    internal static Task That<T>(Func<T> value, T expected)
        => That(value, actual => EqualityComparer<T>.Default.Equals(actual, expected));

    internal static Task That<T>(Func<T> value, T expected, string message)
        => That(value, actual => EqualityComparer<T>.Default.Equals(actual, expected), message);

    internal static Task That<T, TElement>(Func<T> value, TElement item) where T : IEnumerable<TElement>
        => That(value, collection => collection.Contains(item));
}
