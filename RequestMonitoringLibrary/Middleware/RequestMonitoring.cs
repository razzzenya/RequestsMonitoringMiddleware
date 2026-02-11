using Microsoft.AspNetCore.Http;
using RequestMonitoringLibrary.Enitites.Domain;
using RequestMonitoringLibrary.Middleware.Services.DomainCheck;

namespace RequestMonitoringLibrary.Middleware;

public class RequestMonitoring(RequestDelegate next, IDomainCheckService domainCheckService)
{   
    public async Task InvokeAsync(HttpContext context)
    {
        var domainStatus = await domainCheckService.IsDomainAllowedAsync(context);
        switch (domainStatus)
        {
            case DomainStatusType.Allowed:
                await next(context);
                return;

            case DomainStatusType.Forbidden:
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsync("This domain is forbidden.");
                return;

            case DomainStatusType.Greylisted:
                context.Response.StatusCode = StatusCodes.Status402PaymentRequired;
                await context.Response.WriteAsync("This domain is greylisted.");
                return;
        }
    }
}
