using Microsoft.AspNetCore.Http;
using RequestMonitoring.Library.Middleware.Services.DomainCheck;

namespace RequestMonitoring.Library.Middleware;

/// <summary>
/// Middleware для проверки доступа домена к ресурсам
/// </summary>
public class RequestMonitoringMiddleware(RequestDelegate next)
{
    /// <summary>
    /// Проверяет статус домена и разрешает или блокирует запрос
    /// </summary>
    /// <param name="context">Контекст HTTP-запроса</param>
    /// <param name="domainCheckService">Сервис проверки домена</param>
    public async Task InvokeAsync(HttpContext context, IDomainCheckService domainCheckService)
    {
        var domainStatus = await domainCheckService.IsDomainAllowedAsync(context);

        switch (domainStatus.Id)
        {
            case 1:
                await next(context);
                return;

            case 2:
                context.Response.StatusCode = StatusCodes.Status402PaymentRequired;
                await context.Response.WriteAsync("This domain is greylisted.");
                return;

            case 3:
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync("This domain is forbidden.");
                return;
        }
    }
}
