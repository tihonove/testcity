using Vostok.Configuration;
using Vostok.Configuration.Abstractions.Attributes;
using Vostok.Configuration.Sources.Environment;

namespace Kontur.TestAnalytics.Core;

public class GitLabSettings
{
    [Alias("GITLAB_TOKEN")]
    public string GitLabToken { get; set; }

    public static GitLabSettings Default => DefaultInstance.Value;

    private static readonly Lazy<GitLabSettings> DefaultInstance = new (() =>
    {
        var source = new EnvironmentVariablesSource();
        var provider = new ConfigurationProvider();
        provider.SetupSourceFor<GitLabSettings>(source);
        return provider.Get<GitLabSettings>();
    });
}
