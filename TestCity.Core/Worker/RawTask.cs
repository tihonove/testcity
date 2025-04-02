using System.Text.Json;

namespace Kontur.TestCity.Core.Worker;

public class RawTask
{
    public string Type { get; set; }
    public JsonElement Payload { get; set; }
}
