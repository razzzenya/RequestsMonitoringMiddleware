using Microsoft.AspNetCore.Http;
using RequestMonitoringLibrary.Middleware.Services.DomainCheck;

namespace RequestMonitoringLibrary.Middleware;

public class RequestMonitoring(RequestDelegate next)
{
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
