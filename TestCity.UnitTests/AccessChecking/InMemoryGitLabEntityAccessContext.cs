using TestCity.Core.GitlabProjects.AccessChecking;

namespace TestCity.UnitTests.AccessChecking;

public class InMemoryGitLabEntityAccessContext : IGitLabEntityAccessContext
{
    private readonly Dictionary<string, AccessControlEntry> entries = new();

    public void AddEntry(string[] pathSlug, bool hasAccess)
    {
        var key = string.Join("/", pathSlug);
        entries[key] = new AccessControlEntry(pathSlug, hasAccess);
    }

    public Task<List<AccessControlEntry>> ListAccessEntries()
    {
        return Task.FromResult(entries.Values.ToList());
    }
}
