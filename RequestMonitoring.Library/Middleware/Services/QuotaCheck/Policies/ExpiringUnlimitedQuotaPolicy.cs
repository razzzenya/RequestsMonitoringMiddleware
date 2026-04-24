using RequestMonitoring.Library.Context;
using RequestMonitoring.Library.Enitites;
using StackExchange.Redis;

namespace RequestMonitoring.Library.Middleware.Services.QuotaCheck.Policies;

/// <summary>
/// Безлимит до определённой даты, потом Greylisted
/// </summary>
public class ExpiringUnlimitedQuotaPolicy : QuotaPolicy
{
    public override Task<QuotaCheckResult> ExecuteAsync(Quota quota, IDatabase db, DomainListsContext dbContext, int syncEveryNRequests)
    {
        var result = DateTime.UtcNow >= quota.ExpiresAt!.Value
            ? QuotaCheckResult.Exceeded
            : QuotaCheckResult.Allowed;

        return Task.FromResult(result);
    }
}