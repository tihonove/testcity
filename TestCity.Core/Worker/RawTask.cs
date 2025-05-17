using System.Text.Json;

namespace TestCity.Core.Worker;

public class RawTask
{
    public required string Type { get; set; }
    public required JsonElement Payload { get; set; }
    public int? ExecuteCount { get; set; }
}
