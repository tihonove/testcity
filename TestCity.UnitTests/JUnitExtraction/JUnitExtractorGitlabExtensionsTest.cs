using TestCity.Core.JUnit;
using TestCity.Core.Storage.DTO;
using TestCity.UnitTests.Utils;
using NUnit.Framework;

namespace TestCity.UnitTests.JUnitExtraction
{
    [TestFixture]
    public class JUnitExtractorGitlabExtensionsTest
    {
        private JUnitExtractor junitExtractor;

        [SetUp]
        public void SetUp()
        {
            junitExtractor = new JUnitExtractor();
        }

        [Test]
        public void TryExtractTestRunsFromGitlabArtifact_ValidArchiveWithJunitReports_ReturnsTestData()
        {
            var artifactFileName = "remote-banking-run-tests-artifacts.zip";
            var artifactPath = TestFilesAccessor.GetTestFile(Path.Combine("TestData", "artifacts", artifactFileName));
            var artifactContent = File.ReadAllBytes(artifactPath.Path);
            var result = junitExtractor.TryExtractTestRunsFromGitlabArtifact(artifactContent).TestReportData;
            Assert.That(result, Is.Not.Null);
            Assert.That(result?.Counters.Total, Is.EqualTo(374));
            Assert.That(result?.Counters.Failed, Is.EqualTo(0));
        }

        [Test]
        public void CheckTestOutput()
        {
            var artifactFileName = "diadoc-screenshot-tests-artifacts.zip";
            var artifactPath = TestFilesAccessor.GetTestFile(Path.Combine("TestData", "artifacts", artifactFileName));
            var artifactContent = File.ReadAllBytes(artifactPath.Path);
            var result = junitExtractor.TryExtractTestRunsFromGitlabArtifact(artifactContent).TestReportData;
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Counters.Total, Is.EqualTo(402));
            Assert.That(result!.Counters.Failed, Is.EqualTo(42));

            foreach (var test in result!.Runs.Where(x => x.TestResult == TestResult.Failed))
            {
                TestContext.WriteLine(test.JUnitFailureMessage);
                TestContext.WriteLine(test.JUnitFailureOutput);
                TestContext.WriteLine(test.JUnitSystemOutput);
            }
        }
    }
}
