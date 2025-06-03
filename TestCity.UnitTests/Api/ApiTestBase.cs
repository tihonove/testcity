using Microsoft.Extensions.Logging;
using TestCity.Core.GitLab;
using TestCity.UnitTests.Api.ApiClient;
using Xunit.Abstractions;

namespace TestCity.UnitTests.Api;

public abstract class ApiTestBase : IDisposable
{
    protected ApiTestBase(ITestOutputHelper output)
    {
        Output = output;
        LoggerFactory = GlobalSetup.TestLoggerFactory(output);
        HttpClient = CreateHttpClient();
        GitLabClientProvider = new SkbKonturGitLabClientProvider(GitLabSettings.Default);

        GroupsApiClient = new GroupsApiClient(HttpClient);
        GitlabApiClient = new GitlabApiClient(HttpClient);
        ResetCacheApiClient = new ResetCacheApiClient(HttpClient);
    }

    protected ITestOutputHelper Output { get; }
    protected ILoggerFactory LoggerFactory { get; }
    protected HttpClient HttpClient { get; }
    protected SkbKonturGitLabClientProvider GitLabClientProvider { get; }
    internal GroupsApiClient GroupsApiClient { get; }
    internal GitlabApiClient GitlabApiClient { get; }
    internal ResetCacheApiClient ResetCacheApiClient { get; }

    protected virtual HttpClient CreateHttpClient()
    {
        return new HttpClient
        {
            BaseAddress = new Uri(Environment.GetEnvironmentVariable("TESTCITY_API_URL") ?? throw new InvalidOperationException("TESTCITY_API_URL environment variable is not set.")),
        };
    }

    public virtual void Dispose()
    {
        HttpClient.Dispose();
        GC.SuppressFinalize(this);
    }
}
