using System.IO;
using System.IO.Compression;
using NUnit.Framework;
using Kontur.TestAnalytics.Reporter.Tests;
using Kontur.TestAnalytics.Reporter.Tests.Utils;
using Microsoft.Extensions.Logging;

namespace Kontur.TestCity.GitLabJobsCrawler.Tests
{
    [TestFixture]
    public class JUnitExtractorGitlabExtensionsTest
    {
        private JUnitExtractor junitExtractor;

        [SetUp]
        public void SetUp()
        {
            junitExtractor = new JUnitExtractor(GlobalSetup.TestLoggerFactory.CreateLogger<JUnitExtractor>());
        }

        [Test]
        public void TryExtractTestRunsFromGitlabArtifact_ValidArchiveWithJunitReports_ReturnsTestData()
        {
            var artifactFileName = "remote-banking-run-tests-artifacts.zip";
            var artifactPath = TestFilesAccessor.GetTestFile(Path.Combine("test-data", artifactFileName));
            var artifactContent = File.ReadAllBytes(artifactPath.Path);
            var result = junitExtractor.TryExtractTestRunsFromGitlabArtifact(artifactContent);
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Counters.Total, Is.EqualTo(374));
        }
    }
}
