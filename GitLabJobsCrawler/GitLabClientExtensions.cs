using System.Text.RegularExpressions;
using NGitLab;
using NGitLab.Models;

namespace Kontur.TestAnalytics.GitLabJobsCrawler;

public static partial class GitLabClientExtensions
{
    public static async Task<string> BranchOrRef(this GitLabClient client, int projectId, string refId)
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

    private static readonly Regex MergeRequestRef = MyRegex();
    private static readonly Dictionary<string, string> RefToBranch = new Dictionary<string, string>();

    [GeneratedRegex("^refs/merge-requests/(\\d+)/head$")]
    private static partial Regex MyRegex();
}
