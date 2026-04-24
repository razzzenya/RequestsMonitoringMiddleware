namespace RequestMonitoring.Library.Middleware.Services.DomainCache;

/// <summary>
/// Сервис для управления кэшем доменов
/// </summary>
public interface IDomainCacheService
{
    /// <summary>
    /// Удаляет из кэша конкретный домен
    /// </summary>
    public Task InvalidateDomainAsync(string host);

    /// <summary>
    /// Удаляет все записи доменов из кэша
    /// </summary>
    public Task InvalidateAllDomainsAsync();
}
