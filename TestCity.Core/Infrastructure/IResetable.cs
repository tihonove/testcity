namespace TestCity.Core.Infrastructure;

/// <summary>
/// Interface for objects that support cache reset
/// </summary>
public interface IResetable
{
    /// <summary>
    /// Reset all caches
    /// </summary>
    void Reset();
}
