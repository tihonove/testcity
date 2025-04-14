using System.Text.RegularExpressions;
using NGitLab;
using NGitLab.Models;

namespace Kontur.TestCity.Core.GitLab;

public static class GitLabClientExtensions
{
    public static async Task<string?> BranchOrRef(this IGitLabClient client, long projectId, string? refId)
    {
        if (refId == null)
        {
            return null;
        }
        if (RefToBranch.TryGetValue(refId, out var branch))
        {
            return branch;
        }

        var match = MergeRequestRef.Match(refId);
        if (!match.Success)
        {
            return refId;
        }

        var mrId = long.Parse(match.Groups[1].Value);
        var mr = await client.GetMergeRequest(projectId).GetByIidAsync(mrId, new SingleMergeRequestQuery());

        RefToBranch[refId] = mr.SourceBranch;
        return mr.SourceBranch;
    }

    public static byte[]? GetJobArtifactsOrNull(this IJobClient jobClient, long jobId)
    {
        try
        {
            return jobClient.GetJobArtifacts(jobId);
        }
        catch (GitLabException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    private static readonly Regex MergeRequestRef = new ("^refs/merge-requests/(\\d+)/head$", RegexOptions.Compiled);
    private static readonly Dictionary<string, string> RefToBranch = new Dictionary<string, string>();
}
