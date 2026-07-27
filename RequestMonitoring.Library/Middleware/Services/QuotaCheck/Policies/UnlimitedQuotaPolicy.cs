using RequestMonitoring.Library.Enitites;

namespace RequestMonitoring.Library.Middleware.Services.QuotaCheck.Policies;

/// <summary>
/// Полный безлимит - всегда пропускает без инкремента
/// </summary>
public class UnlimitedQuotaPolicy : QuotaPolicy
{
    public override Task<QuotaCheckResult> ExecuteAsync(Quota quota, IQuotaCounter counter) =>
        Task.FromResult(QuotaCheckResult.Allowed);
}
