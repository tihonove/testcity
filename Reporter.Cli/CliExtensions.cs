using CommandLine;

namespace Kontur.TestAnalytics.Reporter.Cli;

public static class CliExtensions
{
    public static T GetOptionsOrThrow<T>(this ParserResult<T> parserResult)
    {
        var result = default(T);
        parserResult.WithParsed(c => result = c).WithNotParsed(_ => throw new ArgumentException("Ошибка при чтении параметров"));
        return result ?? throw new ArgumentException("Ошибка при чтении параметров");
    }
}
