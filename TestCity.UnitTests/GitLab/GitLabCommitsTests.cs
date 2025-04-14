using Kontur.TestCity.Core;
using Kontur.TestCity.Core.GitLab;
using NUnit.Framework;

namespace Kontur.TestCity.UnitTests.GitLab;

[TestFixture]
public class GitLabCommitsTests
{
    private GitLabExtendedClient gitLabExtendedClient;

    [SetUp]
    public void Setup()
    {
        var provider = new SkbKonturGitLabClientProvider(GitLabSettings.Default);
        gitLabExtendedClient = provider.GetExtendedClient();
    }

    [Test]
    public async Task GetRepositoryCommitsAsync_WithSpecificRefName_ReturnsCommits()
    {
        const int projectId = 17358;
        const string refName = "2a9b75152e9ce789d47ca310952c4d160d005207";

        var response = await gitLabExtendedClient.GetRepositoryCommitsAsync(
            projectId,
            options => options.ForReference(refName));

        var commits = response.Result;
        Assert.That(commits, Is.Not.Null);

        Console.WriteLine($"Found {commits.Count} commits:");
        foreach (var commit in commits)
        {
            Console.WriteLine("-----------------------------------------------------");
            Console.WriteLine($"ID: {commit.Id}");
            Console.WriteLine($"Short ID: {commit.ShortId}");
            Console.WriteLine($"Title: {commit.Title}");
            Console.WriteLine($"Author: {commit.AuthorName} <{commit.AuthorEmail}>");
            Console.WriteLine($"Date: {commit.CreatedAt}");
            Console.WriteLine($"Message: {commit.Message}");
            Console.WriteLine($"Parent IDs: {string.Join(", ", commit.ParentIds)}");
            Console.WriteLine($"Web URL: {commit.WebUrl}");

            if (commit.Trailers.Any())
            {
                Console.WriteLine("Trailers:");
                foreach (var trailer in commit.Trailers)
                {
                    Console.WriteLine($"  {trailer.Key}: {trailer.Value}");
                }
            }

            if (commit.ExtendedTrailers.Any())
            {
                Console.WriteLine("Extended Trailers:");
                foreach (var trailer in commit.ExtendedTrailers)
                {
                    Console.WriteLine($"  {trailer.Key}: {string.Join(", ", trailer.Value)}");
                }
            }
        }

    }

    [Test]
    public async Task GetRepositoryCommitsAsync_WithKeysetPagination_ReturnsCommits()
    {
        const int projectId = 17358;
        const int perPage = 10;

        var response = await gitLabExtendedClient.GetRepositoryCommitsAsync(
            projectId,
            options => options.UseKeysetPagination(
                perPage: perPage,
                orderBy: "created_at",
                sort: "desc"));

        var commits = response.Result;
        Assert.That(commits, Is.Not.Null);
        Assert.That(commits.Count, Is.LessThanOrEqualTo(perPage));
        if (response.NextPageLink != null)
        {
            Console.WriteLine($"Next page link: {response.NextPageLink}");
        }

        Console.WriteLine($"Found {commits.Count} commits with keyset pagination:");
        foreach (var commit in commits.Take(3)) // Display just first 3 to keep output manageable
        {
            Console.WriteLine("-----------------------------------------------------");
            Console.WriteLine($"ID: {commit.Id}");
            Console.WriteLine($"Created At: {commit.CreatedAt}");
            Console.WriteLine($"Title: {commit.Title}");
        }
    }

    [Test]
    public async Task GetRepositoryCommitsAsync_WithComplexOptions_ReturnsFilteredCommits()
    {
        // Arrange
        const int projectId = 17358;

        // Act - using a more complex query with chained options
        var response = await gitLabExtendedClient.GetRepositoryCommitsAsync(
            projectId,
            new RepositoryCommitsQueryOptions()
                .WithDateRange(
                    since: DateTime.Now.AddMonths(-1),
                    until: DateTime.Now)
                .UseKeysetPagination(
                    perPage: 5,
                    orderBy: "created_at",
                    sort: "desc"));

        var commits = response.Result;

        // Assert
        Assert.That(commits, Is.Not.Null);
        Console.WriteLine($"Found {commits.Count} commits from the last month");
    }
}
