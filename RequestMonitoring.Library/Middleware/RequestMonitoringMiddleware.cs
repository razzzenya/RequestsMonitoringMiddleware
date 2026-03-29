using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using RequestMonitoring.Library.Middleware.Services.DomainCheck;
using RequestMonitoring.Library.Middleware.Services.QuotaCheck;
using System.Diagnostics;

namespace RequestMonitoring.Library.Middleware;

/// <summary>
/// Middleware для проверки доступа домена к ресурсам
/// </summary>
public class RequestMonitoringMiddleware(RequestDelegate next, ILogger<RequestMonitoringMiddleware> logger)
{
    private static readonly ActivitySource ActivitySource = new("RequestMonitoring.DomainCheck");

    /// <summary>
    /// Проверяет статус домена и разрешает или блокирует запрос
    /// </summary>
    public async Task InvokeAsync(HttpContext context, IDomainCheckService domainCheckService, IQuotaService quotaService)
    {
        using var activity = ActivitySource.StartActivity("DomainCheck", ActivityKind.Server);

        var domain = context.Request.Headers["X-Test-Host"].FirstOrDefault() ?? context.Request.Host.Host;
        activity?.SetTag("domain.name", domain);

        logger.LogInformation("Checking domain access for {Domain}", domain);

        var domainStatus = await domainCheckService.IsDomainAllowedAsync(context);

        activity?.SetTag("domain.status", domainStatus.Name);
        activity?.SetTag("domain.status.id", domainStatus.Id);

        switch (domainStatus.Id)
        {
            case 1:
                var quotaResult = await quotaService.CheckAndIncrementAsync(domain);
                activity?.SetTag("domain.quota", quotaResult.ToString());

                if (quotaResult == QuotaCheckResult.Exceeded)
                {
                    logger.LogWarning("Domain {Domain} quota exceeded", domain);
                    activity?.SetStatus(ActivityStatusCode.Error, "Quota exceeded");
                    context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                    await context.Response.WriteAsync("Quota exceeded for this domain.");
                    return;
                }

                logger.LogInformation("Domain {Domain} is whitelisted - allowing access", domain);
                await next(context);
                return;

            case 2:
                logger.LogWarning("Domain {Domain} is greylisted - payment required", domain);
                activity?.SetStatus(ActivityStatusCode.Error, "Domain greylisted");
                context.Response.StatusCode = StatusCodes.Status402PaymentRequired;
                await context.Response.WriteAsync("This domain is greylisted.");
                return;

            case 3:
                logger.LogWarning("Domain {Domain} is unknown", domain);
                activity?.SetStatus(ActivityStatusCode.Error, "Unknown Domain");
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync("This domain is forbidden.");
                return;
        }
    }
}
