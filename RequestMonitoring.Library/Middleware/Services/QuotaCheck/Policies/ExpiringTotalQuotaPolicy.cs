using RequestMonitoring.Library.Enitites;

namespace RequestMonitoring.Library.Middleware.Services.QuotaCheck.Policies;

/// <summary>
/// N запросов всего до определённой даты
/// </summary>
public class ExpiringTotalQuotaPolicy : QuotaPolicy
{
    public override async Task<QuotaCheckResult> ExecuteAsync(Quota quota, IQuotaCounter counter)
    {
        if (DateTime.UtcNow >= quota.ExpiresAt!.Value)
            return QuotaCheckResult.Exceeded;

        var count = await counter.IncrementTotalAsync(quota.DomainId, quota.RequestCount);

        return count > quota.MaxRequests!.Value
            ? QuotaCheckResult.Exceeded
            : QuotaCheckResult.Allowed;
    }
}
