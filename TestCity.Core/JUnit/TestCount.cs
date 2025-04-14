namespace Kontur.TestCity.Core.JUnit;

public class TestCount
{
    public int Total { get; set; }

    public int Success { get; set; }

    public int Failed { get; set; }

    public int Skipped { get; set; }

    public override string ToString() => $"Total: {Total}, Success: {Success}, Failed: {Failed}, Skipped: {Skipped}";

    public static TestCount operator +(TestCount a, TestCount b) => new ()
    {
        Total = a.Total + b.Total,
        Success = a.Success + b.Success,
        Failed = a.Failed + b.Failed,
        Skipped = a.Skipped + b.Skipped,
    };
}
