using System.Text.RegularExpressions;
using NGitLab;
using NGitLab.Models;

namespace Kontur.TestAnalytics.GitLabJobsCrawler;

public static class GitLabClientExtensions
{
    public static async Task<string> BranchOrRef(this GitLabClient client, int projectId, string refId)
    {
        if (refToBranch.TryGetValue(refId, out var branch))
        {
            return branch;
        }

        var match = mergeRequestRef.Match(refId);
        if (!match.Success)
        {
            return refId;
        }

        var mrId = long.Parse(match.Groups[1].Value);
        var mr = await client.GetMergeRequest(projectId).GetByIidAsync(mrId, new SingleMergeRequestQuery());

        refToBranch[refId] = mr.SourceBranch;
        return mr.SourceBranch;
    }

    private static readonly Regex mergeRequestRef = new Regex("^refs/merge-requests/(\\d+)/head$");
    private static readonly Dictionary<string, string> refToBranch = new Dictionary<string, string>();
}
