using FluentAssertions;
using Kontur.TestAnalytics.Core;
using Kontur.TestAnalytics.Reporter.Client;
using Kontur.TestCity.GitLabJobsCrawler;
using Microsoft.Extensions.Logging;
using NUnit.Framework;

namespace Kontur.TestAnalytics.Reporter.Tests
{
    [TestFixture]
    public class GitLabJobProcessorTests
    {
        private ILogger<GitLabJobProcessor> logger;
        private GitLabSettings settings;

        [OneTimeSetUp]
        public void Setup()
        {
            logger = GlobalSetup.TestLoggerFactory.CreateLogger<GitLabJobProcessor>();
            settings = GitLabSettings.Default;
        }

        [Test]
        public async Task PrintJobData_ForSpecificProjectAndJob()
        {
            var projectId = 17358;
            var jobId = 37359127;

            var gitLabClientProvider = new SkbKonturGitLabClientProvider(settings);
            var client = gitLabClientProvider.GetClient();
            var extractor = new JUnitExtractor(GlobalSetup.TestLoggerFactory.CreateLogger<JUnitExtractor>());
            var jobProcessor = new GitLabJobProcessor(client, extractor, logger);

            var processingResult = await jobProcessor.ProcessJobAsync(projectId, jobId);

            processingResult.JobInfo!.State.Should().Be(JobStatus.Failed);
            processingResult.TestReportData!.Runs.Should().HaveCount(421);
            processingResult.JobInfo.CustomStatusMessage.Should().Be("Не прошли тесты на подхватывание ресурсов после релиза (exitCode 1)");
        }
    }
}
