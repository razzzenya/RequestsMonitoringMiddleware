using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenSearch.Client;
using RequestMonitoring.Library.Enitites.Domain;

namespace RequestMonitoring.Library.Extensions;

/// <summary>
/// Методы расширения для регистрации сервисов в DI контейнере
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Регистрирует OpenSearch клиент как singleton и создает индекс при необходимости
    /// </summary>
    /// <param name="services">Коллекция сервисов</param>
    /// <param name="configuration">Конфигурация приложения</param>
    /// <returns>Коллекция сервисов для цепочки вызовов</returns>
    public static IServiceCollection AddOpenSearchClient(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IOpenSearchClient>(sp =>
        {
            var uriString = configuration["OpenSearch:Uri"] ?? "http://localhost:9200";
            var index = configuration["OpenSearch:Index"] ?? "request-logs";
            var uri = new Uri(uriString);

            var settings = new ConnectionSettings(uri)
                .DefaultIndex(index)
                .ThrowExceptions();

            var client = new OpenSearchClient(settings);

            var existsResp = client.Indices.Exists(index);
            if (!existsResp.Exists)
            {
                client.Indices.Create(index, c => c
                    .Map<RequestLog>(m => m
                        .AutoMap()
                    )
                );
            }

            return client;
        });

        return services;
    }
}
