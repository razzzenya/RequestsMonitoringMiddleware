using RequestMonitoring.Library.Shared;

namespace RequestMonitoring.AdminPanel.Models;

public static class QuotaTypeHelper
{
    public static string GetDisplayName(QuotaType type) => type switch
    {
        QuotaType.Unlimited         => "Безлимит",
        QuotaType.Periodic          => "Периодический",
        QuotaType.Total             => "Общий",
        QuotaType.ExpiringUnlimited => "Безлимит до даты",
        QuotaType.ExpiringTotal     => "Общий до даты",
        QuotaType.ExpiringPeriodic  => "Периодический до даты",
        _                           => type.ToString()
    };

    public static string GetDescription(QuotaType type) => type switch
    {
        QuotaType.Unlimited         => "Неограниченное количество запросов без каких-либо ограничений.",
        QuotaType.Periodic          => "N запросов в период. Счётчик сбрасывается по истечении периода.",
        QuotaType.Total             => "N запросов всего. После исчерпания домен блокируется.",
        QuotaType.ExpiringUnlimited => "Безлимит до указанной даты. После истечения домен блокируется.",
        QuotaType.ExpiringTotal     => "N запросов всего до указанной даты. После исчерпания или истечения домен блокируется.",
        QuotaType.ExpiringPeriodic  => "N запросов в период до указанной даты. После истечения даты домен блокируется.",
        _                           => string.Empty
    };
}
