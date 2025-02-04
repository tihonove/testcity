using System.Collections.Generic;

public class GitLabGroup
{
    public string Id { get; set; }
    public string Title { get; set; }
    public List<GitLabGroup> Groups { get; set; }
    public List<GitLabProject> Projects { get; set; }
}

public class GitLabProject
{
    public string Id { get; set; }
    public string Title { get; set; }
}
