using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

namespace TestCity.Core.Graphite;

public class GraphiteClient(string host, int port = 2003) : IGraphiteClient
{
    public async Task SendAsync(string metricPath, double value, DateTime? timestamp = null)
    {
        using var tcpClient = new TcpClient();
        await tcpClient.ConnectAsync(host, port);

        await using var stream = tcpClient.GetStream();
        await using var writer = new StreamWriter(stream, Encoding.ASCII) { AutoFlush = true };

        var timestampEpoch = (timestamp ?? DateTime.UtcNow).ToEpochTime();
        var message = $"{metricPath} {value} {timestampEpoch}\n";

        await writer.WriteAsync(message);
    }

    public async Task SendAsync(MetricPoint metric)
    {
        using var tcpClient = new TcpClient();
        await tcpClient.ConnectAsync(host, port);

        using var stream = tcpClient.GetStream();
        using var writer = new StreamWriter(stream, Encoding.ASCII) { AutoFlush = true };

        var metricPath = FormatMetricPath(metric);
        var timestampEpoch = (metric.Timestamp ?? DateTime.UtcNow).ToEpochTime();
        var message = $"{metricPath} {metric.Value} {timestampEpoch}\n";

        await writer.WriteAsync(message);
    }

    public async Task SendAsync(IEnumerable<MetricPoint> metrics)
    {
        using var tcpClient = new TcpClient();
        await tcpClient.ConnectAsync(host, port);

        using var stream = tcpClient.GetStream();
        using var writer = new StreamWriter(stream, Encoding.ASCII) { AutoFlush = true };

        foreach (var metric in metrics)
        {
            var metricPath = FormatMetricPath(metric);
            var timestampEpoch = (metric.Timestamp ?? DateTime.UtcNow).ToEpochTime();
            var message = $"{metricPath} {metric.Value} {timestampEpoch}\n";
            await writer.WriteAsync(message);
        }
    }

    private static string FormatMetricPath(MetricPoint metric)
    {
        if (metric.Tags == null || metric.Tags.Count == 0)
        {
            return SanitizeString(metric.Name);
        }

        var tags = string.Join(";", metric.Tags.Select(kv => $"{SanitizeString(kv.Key)}={SanitizeString(kv.Value)}"));
        return $"{SanitizeString(metric.Name)};{tags}";
    }

    private static string SanitizeString(string input)
    {
        if (string.IsNullOrEmpty(input))
            return "_empty_";
        var transliterated = TransliterateRussianToEnglish(input);
        var sanitized = InvalidCharactersRegex.Replace(transliterated, "_");
        sanitized = MultipleUnderscoresRegex.Replace(sanitized, "_");
        if (string.IsNullOrEmpty(sanitized))
            return "_empty_";
        return sanitized;
    }

    private static string TransliterateRussianToEnglish(string input)
    {
        var ruToEn = new Dictionary<char, string>
        {
            {'а', "a"}, {'б', "b"}, {'в', "v"}, {'г', "g"}, {'д', "d"}, {'е', "e"}, {'ё', "e"},
            {'ж', "zh"}, {'з', "z"}, {'и', "i"}, {'й', "y"}, {'к', "k"}, {'л', "l"}, {'м', "m"},
            {'н', "n"}, {'о', "o"}, {'п', "p"}, {'р', "r"}, {'с', "s"}, {'т', "t"}, {'у', "u"},
            {'ф', "f"}, {'х', "h"}, {'ц', "ts"}, {'ч', "ch"}, {'ш', "sh"}, {'щ', "sch"},
            {'ъ', ""}, {'ы', "y"}, {'ь', ""}, {'э', "e"}, {'ю', "yu"}, {'я', "ya"},
            {'А', "A"}, {'Б', "B"}, {'В', "V"}, {'Г', "G"}, {'Д', "D"}, {'Е', "E"}, {'Ё', "E"},
            {'Ж', "Zh"}, {'З', "Z"}, {'И', "I"}, {'Й', "Y"}, {'К', "K"}, {'Л', "L"}, {'М', "M"},
            {'Н', "N"}, {'О', "O"}, {'П', "P"}, {'Р', "R"}, {'С', "S"}, {'Т', "T"}, {'У', "U"},
            {'Ф', "F"}, {'Х', "H"}, {'Ц', "Ts"}, {'Ч', "Ch"}, {'Ш', "Sh"}, {'Щ', "Sch"},
            {'Ъ', ""}, {'Ы', "Y"}, {'Ь', ""}, {'Э', "E"}, {'Ю', "Yu"}, {'Я', "Ya"}
        };

        var result = new StringBuilder();

        foreach (var c in input)
        {
            if (ruToEn.TryGetValue(c, out var latinChar))
                result.Append(latinChar);
            else
                result.Append(c);
        }

        return result.ToString();
    }

    private readonly string host = host;
    private readonly int port = port;
    private static readonly Regex InvalidCharactersRegex = new(@"[^a-zA-Z0-9_\-]", RegexOptions.Compiled);
    private static readonly Regex MultipleUnderscoresRegex = new(@"_{2,}", RegexOptions.Compiled);
}
