using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RequestMonitoring.Library.Context;
using RequestMonitoring.Library.Enitites;
using RequestMonitoring.Library.Middleware.Services.DomainCache;
using RequestMonitoring.Library.Middleware.Services.QuotaCheck.Policies;
using StackExchange.Redis;

namespace RequestMonitoring.Library.Middleware.Services.QuotaCheck;

public class QuotaService(IConnectionMultiplexer redis, DomainListsContext dbContext, IConfiguration configuration, IDomainCacheService domainCacheService, ILogger<QuotaService> logger) : IQuotaService
{
    private readonly int _syncEveryNRequests = configuration.GetValue("QuotaSettings:SyncEveryNRequests", 10);

    public async Task<QuotaCheckResult> CheckAndIncrementAsync(string host)
    {
        var quota = await dbContext.Quotas
            .Include(q => q.Domain)
            .FirstOrDefaultAsync(q => q.Domain.Host == host);

        if (quota is null)
            return QuotaCheckResult.NoQuota;

        var db = redis.GetDatabase();
        var policy = QuotaPolicy.Create(quota.Type);
        var result = await policy.ExecuteAsync(quota, db, dbContext, _syncEveryNRequests);

        if (result == QuotaCheckResult.Exceeded)
        {
            logger.LogWarning("Quota exceeded for domain {Host}, moving to Greylisted", host);
            await MoveToGreylistedAsync(quota.Domain);
        }
        else if (result == QuotaCheckResult.TemporarilyExceeded)
        {
            logger.LogWarning("Periodic quota temporarily exceeded for domain {Host}", host);
        }

        return result;
    }

    private async Task MoveToGreylistedAsync(Domain domain)
    {
        try
        {
            var greylistedStatus = await dbContext.DomainStatusTypes.FindAsync(2);
            if (greylistedStatus is null)
                return;

            domain.DomainStatusTypeId = 2;
            domain.DomainStatusType = greylistedStatus;
            await dbContext.SaveChangesAsync();

            await domainCacheService.InvalidateDomainAsync(domain.Host);

            logger.LogWarning("Domain {Host} moved to Greylisted due to quota exceeded", domain.Host);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to move domain {Host} to Greylisted", domain.Host);
        }
    }
}