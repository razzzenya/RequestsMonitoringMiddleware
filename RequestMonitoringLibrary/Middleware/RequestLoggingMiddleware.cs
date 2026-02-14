using Microsoft.AspNetCore.Http;
using RequestMonitoring.Library.Middleware.Services.OpenSearchLog;
using RequestMonitoring.Library.Enitites.Domain;
using System.Diagnostics;

namespace RequestMonitoring.Library.Middleware;

/// <summary>
/// Middleware для логирования HTTP-запросов в OpenSearch
/// </summary>
public class RequestLoggingMiddleware(RequestDelegate next)
{
    /// <summary>
    /// Обрабатывает HTTP-запрос, логирует информацию о запросе и передает управление следующему middleware
    /// </summary>
    /// <param name="context">Контекст HTTP-запроса</param>
    /// <param name="openSearchLogService">Сервис логирования в OpenSearch</param>
    public async Task InvokeAsync(HttpContext context, IOpenSearchLogService openSearchLogService)
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
