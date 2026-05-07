# C4 · Уровень 3 — Components: Protected API + RequestMonitoring.Library

Диаграмма раскрывает внутреннее устройство контейнера **Protected API
(Test.Api)** вместе с компонентами библиотеки `RequestMonitoring.Library`,
которые регистрируются в его DI-контейнере.

```mermaid
C4Component
    title Components — Test.Api (Protected API) и RequestMonitoring.Library

    Container_Ext(redis, "Redis", "Кэш + счётчики квот")
    ContainerDb_Ext(sqlite, "Domains DB", "SQLite")
    Container_Ext(opensearch, "OpenSearch", "Индекс request-logs")
    Person(client, "Внешний клиент")

    Container_Boundary(testApi, "Protected API (Test.Api)") {
        Component(controllers, "API Controllers", "ASP.NET Core MVC", "Бизнес-эндпоинты защищаемого API")

        Component(loggingMw,    "RequestLoggingMiddleware",    "Middleware", "Замеряет длительность и сохраняет RequestLog в OpenSearch")
        Component(monitoringMw, "RequestMonitoringMiddleware", "Middleware", "Проверяет статус домена и квоту, формирует ответы 401/402/429")

        Component(domainCheck, "DomainCheckService",    "Service (IDomainCheckService)",   "Возвращает DomainStatusType по домену; кэш через IDistributedCache")
        Component(domainCache, "DomainCacheService",    "Service (IDomainCacheService)",   "Инвалидация ключей Domain_{host} в кэше")
        Component(quotaSvc,    "QuotaService",          "Service (IQuotaService)",         "Проверка и инкремент квоты, перевод в Greylisted при превышении")
        Component(quotaPolicy, "QuotaPolicy + наследники", "Strategy",                     "Total/Periodic/Unlimited (+ Expiring* варианты)")
        Component(quotaCache,  "QuotaCacheService",     "Service (IQuotaCacheService)",    "Инвалидация ключей счётчиков квот в Redis")
        Component(osLog,       "OpenSearchLogService",  "Service (IOpenSearchLogService)", "Индексирует RequestLog в OpenSearch")

        ComponentDb(dbCtx, "DomainListsContext", "EF Core DbContext", "Таблицы domain, domain_status_type, quota")
    }

    Rel(client, loggingMw, "HTTP-запрос", "HTTPS")
    Rel(loggingMw, monitoringMw, "next(context)")
    Rel(monitoringMw, controllers, "next(context) если домен и квота OK")

    Rel(monitoringMw, domainCheck, "IsDomainAllowedAsync(context)")
    Rel(monitoringMw, quotaSvc,    "CheckAndIncrementAsync(host)")

    Rel(domainCheck, dbCtx, "Domains.Include(DomainStatusType)")
    Rel(domainCheck, redis, "Read/Write Domain_{host}", "IDistributedCache")

    Rel(quotaSvc, dbCtx,       "Quotas.Include(Domain), SaveChanges")
    Rel(quotaSvc, quotaPolicy, "policy.ExecuteAsync(...)")
    Rel(quotaSvc, domainCache, "InvalidateDomainAsync(host) при переводе в Greylisted")
    Rel(quotaPolicy, redis,    "StringIncrementAsync, KeyExpire", "StackExchange.Redis")

    Rel(loggingMw, osLog, "IndexAsync(log) — fire-and-forget")
    Rel(osLog, opensearch, "Index<RequestLog>", "OpenSearch.Client")

    Rel(dbCtx, sqlite, "EF Core / SQLite")

    UpdateLayoutConfig($c4ShapeInRow="3", $c4BoundaryInRow="1")
```

## Пояснения

- **Пайплайн middleware** в `Test.Api/Program.cs`:
  `RequestLoggingMiddleware` → `RequestMonitoringMiddleware` → контроллеры.
- **`RequestMonitoringMiddleware`** ветвится по `DomainStatusType.Id`:
  `1` Whitelisted (с проверкой квоты), `2` Greylisted → `402`, `3` Unknown → `401`.
- **Квоты** реализованы стратегией `QuotaPolicy.Create(quota.Type)`. Атомарность
  обеспечивается `StringIncrementAsync` Redis, в SQLite счётчики
  синхронизируются раз в `QuotaSettings:SyncEveryNRequests` запросов.
- **Логирование** в OpenSearch выполняется как fire-and-forget
  (`_ = openSearchLogService.IndexAsync(log)`), чтобы не задерживать ответ.
- **Кэширование статуса домена**: ключ `Domain_{host}` со временем жизни
  `CacheSettings:ExpirationMinutes` (по умолчанию 10 мин).
