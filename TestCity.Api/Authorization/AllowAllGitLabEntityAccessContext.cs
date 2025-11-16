using TestCity.Core.GitlabProjects.AccessChecking;

namespace TestCity.Api.Authorization;

public class AllowAllGitLabEntityAccessContext : IGitLabEntityAccessContext
{
    public Task<List<AccessControlEntry>> ListAccessEntries()
    {
        return Task.FromResult(new List<AccessControlEntry> { new AccessControlEntry(Array.Empty<string>(), true) });
    }
}
