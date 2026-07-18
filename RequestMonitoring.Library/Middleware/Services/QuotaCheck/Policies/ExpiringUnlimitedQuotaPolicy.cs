using RequestMonitoring.Library.Enitites;

namespace RequestMonitoring.Library.Middleware.Services.QuotaCheck.Policies;

/// <summary>
/// Безлимит до определённой даты, потом Greylisted
/// </summary>
public class ExpiringUnlimitedQuotaPolicy : QuotaPolicy
{
    public override Task<QuotaCheckResult> ExecuteAsync(Quota quota, IQuotaCounter counter)
    {
        var result = DateTime.UtcNow >= quota.ExpiresAt!.Value
            ? QuotaCheckResult.Exceeded
            : QuotaCheckResult.Allowed;

        return Task.FromResult(result);
    }
}
