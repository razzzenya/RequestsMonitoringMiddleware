using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace RequestMonitoring.Library.Extensions;

/// <summary>
/// Методы расширения для настройки Aspire Service Defaults
/// </summary>
public static class ServiceDefaultsExtensions
{
    extension(IHostApplicationBuilder builder)
    {
        /// <summary>
        /// Добавляет стандартные настройки сервисов Aspire (телеметрия, health checks, service discovery)
        /// </summary>
        public IHostApplicationBuilder AddServiceDefaults()
        {
            builder.ConfigureOpenTelemetry();

            builder.AddDefaultHealthChecks();

            builder.Services.AddServiceDiscovery();

            builder.Services.ConfigureHttpClientDefaults(http =>
            {
                http.AddStandardResilienceHandler();
                http.AddServiceDiscovery();
            });

            return builder;
        }

        /// <summary>
        /// Настраивает OpenTelemetry для логирования, метрик и трассировок
        /// </summary>
        public IHostApplicationBuilder ConfigureOpenTelemetry()
        {
            builder.Logging.AddOpenTelemetry(logging =>
            {
                logging.IncludeFormattedMessage = true;
                logging.IncludeScopes = true;
            });

            builder.Services.AddOpenTelemetry()
                .WithMetrics(metrics =>
                {
                    metrics
                        .AddAspNetCoreInstrumentation()
                        .AddHttpClientInstrumentation()
                        .AddRuntimeInstrumentation()
                        .AddProcessInstrumentation();
                })
                .WithTracing(tracing =>
                {
                    tracing
                        .AddAspNetCoreInstrumentation()
                        .AddHttpClientInstrumentation()
                        .AddSource("Microsoft.EntityFrameworkCore")
                        .AddSource("StackExchange.Redis")
                        .AddSource("RequestMonitoring.DomainCheck")
                        .AddSource("RequestMonitoring.RequestLogging");
                });

            builder.AddOpenTelemetryExporters();

            return builder;
        }

        /// <summary>
        /// Добавляет экспортеры OpenTelemetry
        /// </summary>
        public IHostApplicationBuilder AddOpenTelemetryExporters()
        {
            var useOtlpExporter = !string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);

            if (useOtlpExporter)
            {
                builder.Services.AddOpenTelemetry().UseOtlpExporter();
            }

            return builder;
        }

        /// <summary>
        /// Добавляет стандартные health checks
        /// </summary>
        public IHostApplicationBuilder AddDefaultHealthChecks()
        {
            builder.Services.AddHealthChecks()
                .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);

            return builder;
        }
    }

    extension(WebApplication app)
    {
        /// <summary>
        /// Регистрирует стандартные endpoints для health checks
        /// </summary>
        public WebApplication MapDefaultEndpoints()
        {
            app.MapHealthChecks("/health");

            app.MapHealthChecks("/alive", new HealthCheckOptions
            {
                Predicate = r => r.Tags.Contains("live")
            });

            return app;
        }
    }
}
