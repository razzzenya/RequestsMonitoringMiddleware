namespace RequestMonitoring.Library.Shared;

/// <summary>
/// Тип квоты домена
/// </summary>
public enum QuotaType
{
    Unlimited,
    Periodic,
    Total,
    ExpiringUnlimited,
    ExpiringTotal,
    ExpiringPeriodic
}