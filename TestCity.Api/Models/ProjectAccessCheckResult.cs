namespace TestCity.Api.Models;

/// <summary>
/// Результат проверки доступа к проекту GitLab
/// </summary>
public class ProjectAccessCheckResult
{
    /// <summary>
    /// ID проекта GitLab
    /// </summary>
    public long ProjectId { get; set; }
    
    /// <summary>
    /// Название проекта (если доступ получен)
    /// </summary>
    public string? ProjectName { get; set; }
    
    /// <summary>
    /// Флаг общего результата проверки доступа
    /// </summary>
    public bool HasAccess { get; set; }
    
    /// <summary>
    /// Флаг успешного доступа к данным самого проекта
    /// </summary>
    public bool ProjectAccessible { get; set; }
    
    /// <summary>
    /// Флаг успешного доступа к данным джобов проекта
    /// </summary>
    public bool JobsAccessible { get; set; }
    
    /// <summary>
    /// Флаг успешного доступа к данным коммитов проекта
    /// </summary>
    public bool CommitsAccessible { get; set; }
    
    /// <summary>
    /// Сообщение с дополнительной информацией о результате проверки
    /// </summary>
    public string? Message { get; set; }
}
