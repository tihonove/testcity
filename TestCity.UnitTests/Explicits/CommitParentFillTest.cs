using FluentAssertions;
using Kontur.TestAnalytics.Reporter.Client;
using Kontur.TestCity.Core;
using Kontur.TestCity.Core.GitLab;
using Microsoft.Extensions.Logging;
using NGitLab.Models;
using NUnit.Framework;

namespace Kontur.TestCity.UnitTests.Explicits;

[TestFixture]
[Explicit]
public class CommitParentFillTest
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
    public async Task FillParents()
    {
        long projectId = 24783;
        var gitLabClientProvider = new SkbKonturGitLabClientProvider(settings);

        var client = gitLabClientProvider.GetExtendedClient();
        // const string startCommit = "c62700b6216b26cf2a30a73a32484fea0b8dc911";
        await foreach (var startCommit in client.GetAllProjectJobsAsync(projectId).Take(200).Select(x => x.Commit?.Id!).Where(x => x is not null).Distinct())
        {
            var commits = client.GetAllRepositoryCommitsAsync(projectId, x => x.ForReference(startCommit));

            var entries = await commits.Take(200).Select((x, i) => new CommitParentsEntry
            {
                ProjectId = projectId,
                CommitSha = startCommit,
                ParentCommitSha = x.Id,
                Depth = i,
                AuthorName = x.AuthorName,
                AuthorEmail = x.AuthorEmail,
                MessagePreview = GetMessagePreview(x),
            }).ToListAsync();

            await TestRunsUploader.UploadCommitParents(entries);

        }
    }

    private static string? GetMessagePreview(GitLabCommit x)
    {
        var firstLine = x.Message?.Split('\n').FirstOrDefault();
        if (firstLine is null)
            return null;

        return firstLine[..Math.Min(100, firstLine.Length)];
    }
}
