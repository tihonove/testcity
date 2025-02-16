using System.Text.RegularExpressions;
using NGitLab;
using NGitLab.Models;

namespace Kontur.TestAnalytics.Core;

public static class GitLabClientExtensions
{
    public static async Task<string> BranchOrRef(this IGitLabClient client, int projectId, string refId)
    {
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

    private static readonly Regex MergeRequestRef = new ("^refs/merge-requests/(\\d+)/head$", RegexOptions.Compiled);
    private static readonly Dictionary<string, string> RefToBranch = new Dictionary<string, string>();
}
