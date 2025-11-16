namespace TestCity.Core.GitlabProjects.AccessChecking;

public interface IGitLabEntityAccessContext
{
    Task<List<AccessControlEntry>> ListAccessEntries();
}
