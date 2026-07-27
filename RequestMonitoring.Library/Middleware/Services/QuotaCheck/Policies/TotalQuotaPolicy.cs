using RequestMonitoring.Library.Enitites;

namespace RequestMonitoring.Library.Middleware.Services.QuotaCheck.Policies;

/// <summary>
/// N запросов всего, счётчик никогда не сбрасывается
/// </summary>
public class TotalQuotaPolicy : QuotaPolicy
{
    public override async Task<QuotaCheckResult> ExecuteAsync(Quota quota, IQuotaCounter counter)
    {
        var count = await counter.IncrementTotalAsync(quota.DomainId, quota.RequestCount);

        return count > quota.MaxRequests!.Value
            ? QuotaCheckResult.Exceeded
            : QuotaCheckResult.Allowed;
    }
}
