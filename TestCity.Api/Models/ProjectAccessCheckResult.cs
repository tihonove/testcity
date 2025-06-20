namespace TestCity.Api.Models;

/// <summary>
/// Result of checking access to a GitLab project
/// </summary>
public class ProjectAccessCheckResult
{
    /// <summary>
    /// GitLab project ID
    /// </summary>
    public long ProjectId { get; set; }
    
    /// <summary>
    /// Project name (if access is granted)
    /// </summary>
    public string? ProjectName { get; set; }
    
    /// <summary>
    /// Flag of the overall access check result
    /// </summary>
    public bool HasAccess { get; set; }
    
    /// <summary>
    /// Flag of successful access to the project data itself
    /// </summary>
    public bool ProjectAccessible { get; set; }
    
    /// <summary>
    /// Flag of successful access to project jobs data
    /// </summary>
    public bool JobsAccessible { get; set; }
    
    /// <summary>
    /// Flag of successful access to project commits data
    /// </summary>
    public bool CommitsAccessible { get; set; }
    
    /// <summary>
    /// Message with additional information about the check result
    /// </summary>
    public string? Message { get; set; }
}
