using System.Text.Json;

namespace Kontur.TestCity.Core.Worker;

public class RawTask
{
    public required string Type { get; set; }
    public required JsonElement Payload { get; set; }
    public int? ExecuteCount { get; set; }
}
