namespace TestCity.Core.GitlabProjects.AccessChecking;

public record AccessControlEntry(string[] PathSlug, bool HasAccess);
