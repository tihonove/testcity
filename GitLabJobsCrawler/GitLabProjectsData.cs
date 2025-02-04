using System.Collections.Generic;

public class GitLabGroup
{
    public string Id { get; set; }
    public string Title { get; set; }
    public List<GitLabGroup> Projects { get; set; }
    public List<GitLabProject> Groups { get; set; }
}

public class GitLabProject
{
    public string Id { get; set; }
    public string Title { get; set; }
}
