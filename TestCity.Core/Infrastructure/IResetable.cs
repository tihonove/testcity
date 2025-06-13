namespace TestCity.Core.Infrastructure;

/// <summary>
/// Интерфейс для объектов, поддерживающих сброс кэшей
/// </summary>
public interface IResetable
{
    /// <summary>
    /// Сбросить все кэши
    /// </summary>
    void Reset();
}
