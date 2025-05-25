using Microsoft.Extensions.Logging;
using TestCity.Core.JUnit;
using TestCity.Core.Storage.DTO;
using TestCity.UnitTests.Utils;
using Xunit;
using Xunit.Abstractions;

namespace TestCity.UnitTests.JUnitExtraction;

[Collection("Global")]
public class JUnitExtractorGitlabExtensionsTest(ITestOutputHelper output)
{
    [Fact]
    public void TryExtractTestRunsFromGitlabArtifact_ValidArchiveWithJunitReports_ReturnsTestData()
    {
        var artifactFileName = "remote-banking-run-tests-artifacts.zip";
        var artifactPath = TestFilesAccessor.GetTestFile(Path.Combine("TestData", "artifacts", artifactFileName));
        var artifactContent = File.ReadAllBytes(artifactPath.Path);
        var result = junitExtractor.TryExtractTestRunsFromGitlabArtifact(artifactContent).TestReportData;
        Assert.NotNull(result);
        Assert.Equal(374, result?.Counters.Total);
        Assert.Equal(0, result?.Counters.Failed);
    }

    [Fact]
    public void CheckTestOutput()
    {
        var artifactFileName = "diadoc-screenshot-tests-artifacts.zip";
        var artifactPath = TestFilesAccessor.GetTestFile(Path.Combine("TestData", "artifacts", artifactFileName));
        var artifactContent = File.ReadAllBytes(artifactPath.Path);
        var result = junitExtractor.TryExtractTestRunsFromGitlabArtifact(artifactContent).TestReportData;
        Assert.NotNull(result);
        Assert.Equal(402, result!.Counters.Total);
        Assert.Equal(42, result!.Counters.Failed);

        foreach (var test in result!.Runs.Where(x => x.TestResult == TestResult.Failed))
        {
            logger.LogInformation("JUnitFailureMessage: {JUnitFailureMessage}", test.JUnitFailureMessage);
            logger.LogInformation("JUnitFailureOutput: {JUnitFailureOutput}", test.JUnitFailureOutput);
            logger.LogInformation("JUnitSystemOutput: {JUnitSystemOutput}", test.JUnitSystemOutput);
        }
    }

    private readonly JUnitExtractor junitExtractor = new();
    private readonly ILogger logger = GlobalSetup.TestLoggerFactory(output).CreateLogger<JUnitExtractorGitlabExtensionsTest>();
}
