namespace TestCity.UnitTests.Utils;

internal class TestFile(string path) : IDisposable
{
    public string Path { get; } = path;

    public void Dispose()
    {
        // noop
    }
}

internal class TestFileReference(string path)
{
    public TestFile AcquireFile()
    {
        return new TestFile(path);
    }
}
