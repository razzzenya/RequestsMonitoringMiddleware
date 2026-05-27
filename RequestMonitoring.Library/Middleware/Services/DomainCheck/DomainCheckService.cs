using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RequestMonitoring.Library.Context;
using RequestMonitoring.Library.Enitites;
using System.Text;

namespace RequestMonitoring.Library.Middleware.Services.DomainCheck;

/// <summary>
/// Сервис проверки статуса домена
/// </summary>
public class DomainCheckService(IConfiguration configuration, HybridCache cache, DomainListsContext dbcontext, ILogger<DomainCheckService> logger) : IDomainCheckService
{
    private readonly int cacheExpirationMinutes = configuration.GetValue("CacheSettings:ExpirationMinutes", 10);

    /// <summary>
    /// Проверяет статус домена из контекста запроса
    /// </summary>
    public async Task<DomainStatusType> IsDomainAllowedAsync(HttpContext context)
    {
        var domain = context.Request.Headers["X-Test-Host"].FirstOrDefault() ?? context.Request.Host.Host;
        var safeDomainForLog = SanitizeForLog(domain);
        var cacheKey = $"Domain_{domain}";

        var cancellationToken = context.RequestAborted;

        var status = await cache.GetOrCreateAsync(
            cacheKey,
            async ct => await GetDomainStatusFromDatabaseAsync(domain, ct),
            new HybridCacheEntryOptions
            {
                Expiration = TimeSpan.FromMinutes(cacheExpirationMinutes),
                LocalCacheExpiration = TimeSpan.FromMinutes(cacheExpirationMinutes)
            },
            cancellationToken: cancellationToken
        );

        logger.LogInformation("Domain {Domain} status: {Status}", safeDomainForLog, status.Name);
        return status;
    }

    /// <summary>
    /// Получает статус домена из базы данных
    /// </summary>
    private async Task<DomainStatusType> GetDomainStatusFromDatabaseAsync(string domain, CancellationToken ct = default)
    {
        var safeDomainForLog = SanitizeForLog(domain);
        var domainEntity = await dbcontext.Domains
            .Include(d => d.DomainStatusType)
            .FirstOrDefaultAsync(d => d.Host == domain, ct);

        if (domainEntity?.DomainStatusType != null)
        {
            logger.LogInformation("Domain {Domain} found in database with status {Status}",
                safeDomainForLog, domainEntity.DomainStatusType.Name);
            return domainEntity.DomainStatusType;
        }

        logger.LogWarning("Domain {Domain} not found in database. Returning blocked status", safeDomainForLog);
        return new DomainStatusType { Id = 3, Name = "Unauthorized" };
    }

    private static string SanitizeForLog(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var sb = new StringBuilder(value.Length);
        foreach (var ch in value)
        {
            if (!char.IsControl(ch))
            {
                sb.Append(ch);
            }
        }

        return sb.ToString();
    }
}
