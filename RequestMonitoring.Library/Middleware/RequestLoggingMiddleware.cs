using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using RequestMonitoring.Library.Enitites;
using RequestMonitoring.Library.Middleware.Services.OpenSearchLog;
using System.Diagnostics;

namespace RequestMonitoring.Library.Middleware;

/// <summary>
/// Middleware для логирования HTTP-запросов в OpenSearch
/// </summary>
public class RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
{
    private static readonly ActivitySource ActivitySource = new("RequestMonitoring.RequestLogging");

    /// <summary>
    /// Обрабатывает HTTP-запрос, логирует информацию о запросе и передает управление следующему middleware
    /// </summary>
    /// <param name="context">Контекст HTTP-запроса</param>
    /// <param name="openSearchLogService">Сервис логирования в OpenSearch</param>
    public async Task InvokeAsync(HttpContext context, IOpenSearchLogService openSearchLogService)
    {
        using var activity = ActivitySource.StartActivity("LogRequest", ActivityKind.Server);

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

        activity?.SetTag("http.method", log.Method);
        activity?.SetTag("http.path", log.Path);
        activity?.SetTag("http.status_code", statusCode);
        activity?.SetTag("http.duration_ms", log.DurationMs);

        logger.LogInformation(
            "HTTP {Method} {Path} responded {StatusCode} in {DurationMs}ms",
            log.Method, log.Path, statusCode, log.DurationMs);

        _ = openSearchLogService.IndexAsync(log);
    }
}
