namespace RequestMonitoring.Library.Enitites;

/// <summary>
/// Тип квоты домена
/// </summary>
public enum QuotaType
{
    /// <summary>
    /// Полный безлимит
    /// </summary>
    Unlimited,

    /// <summary>
    /// N запросов в период, бессрочно
    /// </summary>
    Periodic,

    /// <summary>
    /// N запросов всего, без сброса
    /// </summary>
    Total,

    /// <summary>
    /// Безлимит до определённой даты, потом Greylisted
    /// </summary>
    ExpiringUnlimited,

    /// <summary>
    /// N запросов всего до определённой даты
    /// </summary>
    ExpiringTotal,

    /// <summary>
    /// N запросов в период до определённой даты
    /// </summary>
    ExpiringPeriodic
}