# C4 · Уровень 4 — Dynamic / Sequence: обработка HTTP-запроса

Динамическая диаграмма показывает поток обработки одного HTTP-запроса от
клиента до контроллера через `RequestLoggingMiddleware` и
`RequestMonitoringMiddleware`, со всеми проверками статуса домена и квоты.

```mermaid
sequenceDiagram
    autonumber
    participant Client as Внешний клиент
    participant LogMW as RequestLoggingMiddleware
    participant MonMW as RequestMonitoringMiddleware
    participant DomSvc as DomainCheckService
    participant Cache as Redis (IDistributedCache)
    participant DB as DomainListsContext (SQLite)
    participant QSvc as QuotaService
    participant QPol as QuotaPolicy
    participant Redis as Redis (IConnectionMultiplexer)
    participant DomCache as DomainCacheService
    participant Ctrl as API Controller
    participant OS as OpenSearchLogService
    participant OSidx as OpenSearch index

    Client->>LogMW: HTTP request
    activate LogMW
    LogMW->>LogMW: Stopwatch.StartNew()
    LogMW->>MonMW: await next(context)

    activate MonMW
    MonMW->>DomSvc: IsDomainAllowedAsync(context)

    activate DomSvc
    DomSvc->>Cache: GetStringAsync("Domain_{host}")
    alt Cache hit
        Cache-->>DomSvc: cached DomainStatusType
    else Cache miss
        Cache-->>DomSvc: null
        DomSvc->>DB: Domains.Include(DomainStatusType).FirstOrDefault(host)
        DB-->>DomSvc: Domain или null
        alt Не найден
            DomSvc->>DB: DomainStatusTypes.First(Id=3) "Unauthorized"
            DB-->>DomSvc: Unauthorized
        end
        DomSvc->>Cache: SetStringAsync(key, status, TTL)
    end
    DomSvc-->>MonMW: DomainStatusType
    deactivate DomSvc

    alt status.Id == 3 (Unauthorized)
        MonMW-->>Client: 401 "This domain is forbidden."
    else status.Id == 2 (Greylisted)
        MonMW-->>Client: 402 "This domain is greylisted."
    else status.Id == 1 (Allowed)
        MonMW->>QSvc: CheckAndIncrementAsync(host)

        activate QSvc
        QSvc->>DB: Quotas.Include(Domain).FirstOrDefault(host)
        DB-->>QSvc: Quota или null

        alt Quota == null
            QSvc-->>MonMW: NoQuota (запрос пропускается дальше)
        else Quota найден
            QSvc->>QPol: QuotaPolicy.Create(quota.Type).ExecuteAsync(...)
            QPol->>Redis: StringSet(key, RequestCount, NotExists)
            QPol->>Redis: StringIncrementAsync(key)
            Redis-->>QPol: новый счётчик
            QPol->>DB: SaveChanges() каждые N запросов
            QPol-->>QSvc: Allowed | TemporarilyExceeded | Exceeded
            alt Exceeded
                QSvc->>DB: Domain.DomainStatusTypeId = 2 (Greylisted), SaveChanges
                QSvc->>DomCache: InvalidateDomainAsync(host)
                DomCache->>Cache: RemoveAsync("Domain_{host}")
            end
            QSvc-->>MonMW: QuotaCheckResult
        end
        deactivate QSvc

        alt result == Exceeded
            MonMW-->>Client: 402 "Quota exceeded ... greylisted."
        else result == TemporarilyExceeded
            MonMW-->>Client: 429 "Too many requests."
        else result == Allowed (или NoQuota — пропуск без ограничений)
            MonMW->>Ctrl: await next(context)
            Ctrl-->>MonMW: ответ
            MonMW-->>LogMW: ответ
        end
    end
    deactivate MonMW

    LogMW->>LogMW: Stopwatch.Stop(); собрать RequestLog
    LogMW-->>Client: HTTP response
    LogMW-)OS: IndexAsync(log) (fire-and-forget)
    OS-)OSidx: Index<RequestLog>
    deactivate LogMW
```

## Ключевые моменты

- **Порядок middleware** в `Test.Api/Program.cs`:
  `UseMiddleware<RequestLoggingMiddleware>()` → `UseMiddleware<RequestMonitoringMiddleware>()`,
  поэтому логирование измеряет время с учётом проверок домена/квоты.
- **Telemetry**: каждый middleware создаёт собственный `ActivitySource`
  и навешивает свой набор тегов:
  - `RequestMonitoringMiddleware` (`RequestMonitoring.DomainCheck`) —
    `domain.name`, `domain.status`, `domain.status.id`, `domain.quota`;
  - `RequestLoggingMiddleware` (`RequestMonitoring.RequestLogging`) —
    `http.method`, `http.path`, `http.status_code`, `http.duration_ms`.
- **Fire-and-forget** для логов в OpenSearch (`_ = openSearchLogService.IndexAsync(log)`)
  — клиент не ждёт записи лога.
- **Атомарность счётчиков** обеспечивается `StringIncrementAsync` в Redis;
  периодическая синхронизация с SQLite — раз в `QuotaSettings:SyncEveryNRequests`
  инкрементов, чтобы не нагружать БД.
- **Перевод в Greylisted** при превышении квоты сразу инвалидирует кэш
  статуса домена, чтобы следующие запросы получали `402` без задержки.
