namespace RequestMonitoring.AdminPanel.Models;

public static class DomainStatusHelper
{
    public static readonly (int Id, string Name)[] StatusOptions =
    [
        (1, "Белый список"),
        (2, "Серый список"),
        (3, "Неавторизован")
    ];
}
