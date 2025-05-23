namespace TestCity.Core.GitlabProjects;

public static class GitLabGroupExtensions
{
    public static async Task TraverseRecursiveAsync(this GitLabGroup group, Func<GitLabGroup, Task> groupAction, Func<GitLabProject, Task> projectAction, CancellationToken cancellationToken = default)
    {
        await groupAction(group);
        foreach (var project in group.Projects)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await projectAction(project);
        }
        await group.Groups.TraverseRecursiveAsync(groupAction, projectAction, cancellationToken);
    }
    public static async Task TraverseRecursiveAsync(this IEnumerable<GitLabGroup> groups, Func<GitLabGroup, Task> groupAction, Func<GitLabProject, Task> projectAction, CancellationToken cancellationToken = default)
    {
        foreach (var project in groups)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await project.TraverseRecursiveAsync(groupAction, projectAction, cancellationToken);
        }
    }

}
