using RequestMonitoring.Library.Enitites;

namespace RequestMonitoring.Library.Middleware.Services.QuotaCheck.Policies;

/// <summary>
/// N запросов в период до определённой даты
/// </summary>
public class ExpiringPeriodicQuotaPolicy : QuotaPolicy
{
    public override async Task<QuotaCheckResult> ExecuteAsync(Quota quota, IQuotaCounter counter)
    {
        if (DateTime.UtcNow >= quota.ExpiresAt!.Value)
            return QuotaCheckResult.Exceeded; // срок действия истёк - Greylisted

        var count = await counter.IncrementPeriodicAsync(quota.DomainId, quota.PeriodSeconds!.Value, quota.RequestCount);

        return count > quota.MaxRequests!.Value
            ? QuotaCheckResult.TemporarilyExceeded // лимит периода исчерпан - 429, сбросится в следующем периоде
            : QuotaCheckResult.Allowed;
    }
}
