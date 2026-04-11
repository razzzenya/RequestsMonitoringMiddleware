using Microsoft.Extensions.Caching.Distributed;
using RequestMonitoring.Library.Context;
using RequestMonitoring.Library.Enitites;

namespace RequestMonitoring.Library.Middleware.Services.QuotaCheck.Policies;

/// <summary>
/// Полный безлимит - всегда пропускает без инкремента
/// </summary>
public class UnlimitedQuotaPolicy : QuotaPolicy
{
    public override Task<QuotaCheckResult> ExecuteAsync(Quota quota, IDistributedCache cache, DomainListsContext dbContext, int syncEveryNRequests) =>
        Task.FromResult(QuotaCheckResult.Allowed);
}