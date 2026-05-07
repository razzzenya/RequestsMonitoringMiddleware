# C4 · Уровень 4 — Code: ключевые классы

Диаграмма классов раскрывает основные типы из `RequestMonitoring.Library`,
участвующие в проверке домена, квот и логировании. Не все поля и методы
показаны — только наиболее важные для понимания архитектуры.

```mermaid
classDiagram
    direction LR

    class RequestLoggingMiddleware {
        +InvokeAsync(HttpContext, IOpenSearchLogService) Task
    }
    class RequestMonitoringMiddleware {
        +InvokeAsync(HttpContext, IDomainCheckService, IQuotaService) Task
    }

    class IDomainCheckService {
        <<interface>>
        +IsDomainAllowedAsync(HttpContext) Task~DomainStatusType~
    }
    class DomainCheckService {
        -IDistributedCache cache
        -DomainListsContext dbcontext
        -int cacheExpirationMinutes
        +IsDomainAllowedAsync(HttpContext) Task~DomainStatusType~
        -GetDomainStatusFromDatabaseAsync(string) Task~DomainStatusType~
        -TryCacheResultAsync(string, DomainStatusType) Task
    }

    class IDomainCacheService {
        <<interface>>
        +InvalidateDomainAsync(string) Task
    }
    class DomainCacheService

    class IQuotaService {
        <<interface>>
        +CheckAndIncrementAsync(string) Task~QuotaCheckResult~
    }
    class QuotaService {
        -IConnectionMultiplexer redis
        -DomainListsContext dbContext
        -IDomainCacheService domainCacheService
        -int _syncEveryNRequests
        +CheckAndIncrementAsync(string) Task~QuotaCheckResult~
        -MoveToGreylistedAsync(Domain) Task
    }

    class IQuotaCacheService {
        <<interface>>
    }
    class QuotaCacheService

    class QuotaPolicy {
        <<abstract>>
        +ExecuteAsync(Quota, IDatabase, DomainListsContext, int) Task~QuotaCheckResult~
        +Create(QuotaType)$ QuotaPolicy
        #IncrementInRedisAsync(Quota, IDatabase) Task~long~
        #SaveCounterAsync(Quota, DomainListsContext, long, int) Task
    }
    class UnlimitedQuotaPolicy
    class TotalQuotaPolicy
    class PeriodicQuotaPolicy
    class ExpiringUnlimitedQuotaPolicy
    class ExpiringTotalQuotaPolicy
    class ExpiringPeriodicQuotaPolicy

    class IOpenSearchLogService {
        <<interface>>
        +IndexAsync(RequestLog) Task
    }
    class OpenSearchLogService

    class DomainListsContext {
        <<DbContext>>
        +DbSet~Domain~ Domains
        +DbSet~DomainStatusType~ DomainStatusTypes
        +DbSet~Quota~ Quotas
    }

    class Domain {
        +int Id
        +string Host
        +int DomainStatusTypeId
        +DomainStatusType DomainStatusType
    }
    class DomainStatusType {
        +int Id
        +string Name
    }
    class Quota {
        +int Id
        +int DomainId
        +Domain Domain
        +QuotaType Type
        +int? MaxRequests
        +int? PeriodSeconds
        +DateTime? ExpiresAt
        +long RequestCount
    }
    class RequestLog {
        +string Method
        +string Path
        +string QueryString
        +string RemoteIp
        +int? StatusCode
        +long DurationMs
        +Dictionary~string,string~ Headers
    }

    class QuotaCheckResult {
        <<enumeration>>
        Allowed
        Exceeded
        TemporarilyExceeded
        NoQuota
    }
    class QuotaType {
        <<enumeration>>
        Unlimited
        Periodic
        Total
        ExpiringUnlimited
        ExpiringTotal
        ExpiringPeriodic
    }

    IDomainCheckService <|.. DomainCheckService
    IDomainCacheService <|.. DomainCacheService
    IQuotaService       <|.. QuotaService
    IQuotaCacheService  <|.. QuotaCacheService
    IOpenSearchLogService <|.. OpenSearchLogService

    QuotaPolicy <|-- UnlimitedQuotaPolicy
    QuotaPolicy <|-- TotalQuotaPolicy
    QuotaPolicy <|-- PeriodicQuotaPolicy
    QuotaPolicy <|-- ExpiringUnlimitedQuotaPolicy
    QuotaPolicy <|-- ExpiringTotalQuotaPolicy
    QuotaPolicy <|-- ExpiringPeriodicQuotaPolicy

    RequestMonitoringMiddleware ..> IDomainCheckService : uses
    RequestMonitoringMiddleware ..> IQuotaService       : uses
    RequestLoggingMiddleware    ..> IOpenSearchLogService : uses

    DomainCheckService ..> DomainListsContext
    QuotaService ..> DomainListsContext
    QuotaService ..> QuotaPolicy : QuotaPolicy.Create(quota.Type)
    QuotaService ..> IDomainCacheService

    Domain "1" --> "1" DomainStatusType
    Quota  "*" --> "1" Domain
```

## Заметки

- Все сервисы регистрируются в DI как `Scoped` (см. `Test.Api/Program.cs` и
  `AdminApi/Program.cs`).
- `QuotaService` использует **`IConnectionMultiplexer`** напрямую (а не
  `IDistributedCache`), потому что нужен атомарный `StringIncrementAsync` —
  это важная архитектурная деталь.
- `DomainListsContext` сконфигурирован с `[SetsRequiredMembers]` на
  конструкторе — это необходимо, чтобы корректно создавать его в юнит-тестах
  с `Microsoft.EntityFrameworkCore.InMemory`.
- DTO в `RequestMonitoring.AdminApi/DTO` следуют соглашению ABP:
  `*Dto` для чтения, `*CreateUpdateDto` для команд.
