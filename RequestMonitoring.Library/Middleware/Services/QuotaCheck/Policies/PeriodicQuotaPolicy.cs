using RequestMonitoring.Library.Enitites;

namespace RequestMonitoring.Library.Middleware.Services.QuotaCheck.Policies;

/// <summary>
/// N запросов в период, бессрочно
/// </summary>
public class PeriodicQuotaPolicy : QuotaPolicy
{
    public override async Task<QuotaCheckResult> ExecuteAsync(Quota quota, IQuotaCounter counter)
    {
        var count = await counter.IncrementPeriodicAsync(quota.DomainId, quota.PeriodSeconds!.Value, quota.RequestCount);

        return count > quota.MaxRequests!.Value ? QuotaCheckResult.TemporarilyExceeded : QuotaCheckResult.Allowed;
    }
}
