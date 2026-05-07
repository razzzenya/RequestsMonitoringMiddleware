namespace RequestMonitoring.AdminApi.DTO;

/// <summary>
/// Результат запроса с пагинацией
/// </summary>
/// <typeparam name="T">Тип элементов</typeparam>
public record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize);
