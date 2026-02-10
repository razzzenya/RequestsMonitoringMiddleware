using Microsoft.AspNetCore.Http;
using RequestMonitoringLibrary.Enitites.Domain;
using RequestMonitoringLibrary.Middleware.Services.OpenSearchLog;
using System.Diagnostics;

namespace RequestMonitoringLibrary.Middleware;

public class RequestLogging(RequestDelegate next, IOpenSearchLogService openSearchLogService)
{
    //public async Task InvokeAsync(HttpContext context)
    //{
    //    Console.WriteLine($"Request: {context.Request.Method} {context.Request.Path}");
    //    await next(context);
    //    Console.WriteLine($"Response: {context.Response.StatusCode}");
    //}

    public async Task InvokeAsync(HttpContext context)
    {
        var sw = Stopwatch.StartNew();

        int? statusCode = null;
        context.Response.OnStarting(state =>
        {
            var httpContext = (HttpContext)state!;
            statusCode = httpContext.Response?.StatusCode;
            return Task.CompletedTask;
        }, context);

        await next(context);
        sw.Stop();

        var log = new RequestLog
        {
            Method = context.Request.Method,
            Path = context.Request.Path,
            QueryString = context.Request.QueryString.HasValue ? context.Request.QueryString.Value! : "",
            RemoteIp = context.Connection.RemoteIpAddress?.ToString() ?? "",
            StatusCode = statusCode,
            DurationMs = sw.ElapsedMilliseconds,
        };

        foreach (var h in context.Request.Headers)
        {
            log.Headers[h.Key] = h.Value.ToString();
        }

        _ = openSearchLogService.IndexAsync(log);
    }
}
