using RequestMonitoring.Library.Enitites;
using RequestMonitoring.Library.Shared;

namespace RequestMonitoring.Library.Middleware.Services.QuotaCheck.Policies;

/// <summary>
/// Базовый класс политики квоты
/// </summary>
public abstract class QuotaPolicy
{
    private static readonly QuotaPolicy[] Policies =
    [
        new UnlimitedQuotaPolicy(),
        new PeriodicQuotaPolicy(),
        new TotalQuotaPolicy(),
        new ExpiringUnlimitedQuotaPolicy(),
        new ExpiringTotalQuotaPolicy(),
        new ExpiringPeriodicQuotaPolicy()
    ];

    /// <summary>
    /// Инкрементирует счётчик и проверяет квоту
    /// </summary>
    public abstract Task<QuotaCheckResult> ExecuteAsync(Quota quota, IQuotaCounter counter);

    /// <summary>
    /// Создаёт политику по типу квоты (возвращает закэшированный singleton-экземпляр)
    /// </summary>
    public static QuotaPolicy Create(QuotaType type)
    {
        var index = (int)type;
        if (index < 0 || index >= Policies.Length)
            throw new ArgumentOutOfRangeException(nameof(type), $"Unknown quota type: {type}");

        return Policies[index];
    }
}
