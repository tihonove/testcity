namespace TestCity.Core.GitLab;

using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using TestCity.Core.Logging;
using TestCity.Core.Storage;

public class ProjectJobTypesCache(TestCityDatabase testCityDatabase)
{
    public async Task<bool> JobTypeExistsAsync(string projectId, string jobType, CancellationToken ct)
    {
        if (!projectJobTypesCache.TryGetValue(projectId, out var entry) || IsExpired(entry.Timestamp))
        {
            await UpdateJobTypesForProjectAsync(projectId, ct);
            if (!projectJobTypesCache.TryGetValue(projectId, out entry))
            {
                return false;
            }
        }
        return entry.JobTypes.Contains(jobType);
    }

    private async Task UpdateJobTypesForProjectAsync(string projectId, CancellationToken ct)
    {
        var cacheLock = projectJobTypesLocks.GetOrAdd(projectId, _ => new SemaphoreSlim(1, 1));
        await cacheLock.WaitAsync(ct);
        try
        {
            if (projectJobTypesCache.TryGetValue(projectId, out var existingEntry) && !IsExpired(existingEntry.Timestamp))
            {
                return;
            }
            logger.LogInformation("Обновление кэша типов задач для проекта {ProjectId}", projectId);
            var jobTypes = new HashSet<string>();
            await foreach (var jt in testCityDatabase.JobInfo.GetAllJonRunIdsAsync(long.Parse(projectId), ct))
            {
                jobTypes.Add(jt);
            }
            logger.LogInformation("Получено {Count} типов задач для проекта {ProjectId}", jobTypes.Count, projectId);
            projectJobTypesCache[projectId] = new ProjectJobTypesEntry
            {
                JobTypes = jobTypes,
                Timestamp = DateTime.UtcNow
            };
        }
        finally
        {
            cacheLock.Release();
        }
    }

    private static bool IsExpired(DateTime timestamp)
    {
        // Кэш считается устаревшим после 1 часа
        return (DateTime.UtcNow - timestamp).TotalHours > 1;
    }

    private class ProjectJobTypesEntry
    {
        public HashSet<string> JobTypes { get; set; } = new();
        public DateTime Timestamp { get; set; }
    }

    private readonly ILogger logger = Log.GetLog<ProjectJobTypesCache>();
    private static readonly ConcurrentDictionary<string, ProjectJobTypesEntry> projectJobTypesCache = new();
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> projectJobTypesLocks = new();

}
