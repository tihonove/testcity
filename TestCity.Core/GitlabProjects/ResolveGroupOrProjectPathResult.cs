namespace TestCity.Core.GitlabProjects;

public record ResolveGroupOrProjectPathResult(GitLabEntity[] PathSlug, GitLabEntity ResolvedEntity);
