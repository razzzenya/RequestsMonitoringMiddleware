# C4 · Уровень 3 — Components: Admin API

Диаграмма раскрывает внутреннее устройство контейнера **Admin API**
(`RequestMonitoring.AdminApi`).

```mermaid
C4Component
    title Components — RequestMonitoring.AdminApi

    Container_Ext(adminPanel, "Admin Panel", "Blazor WASM")
    ContainerDb_Ext(sqlite, "Domains DB", "SQLite")
    Container_Ext(redis, "Redis", "Кэш / счётчики")

    Container_Boundary(adminApi, "Admin API (RequestMonitoring.AdminApi)") {
        Component(authCtrl,    "AuthController",              "ASP.NET Core MVC", "POST /api/auth/login|logout, GET /me — cookie-аутентификация")
        Component(domainsCtrl, "DomainsController",           "ASP.NET Core MVC", "CRUD доменов")
        Component(statusCtrl,  "DomainStatusTypesController", "ASP.NET Core MVC", "Чтение справочника статусов")
        Component(quotasCtrl,  "QuotasController",            "ASP.NET Core MVC", "CRUD квот")

        Component(authMw, "Cookie Authentication / Authorization", "ASP.NET Core", "Cookies-схема, OnRedirectToLogin → 401")
        Component(corsMw, "CORS (AllowAdminPanel)",                "ASP.NET Core", "Разрешает запросы с Admin Panel с куками")

        Component(mapster, "Mapster TypeAdapterConfig", "Mapster", "Domain → DomainDto (плоское поле DomainStatusName)")

        Component(domainCache, "DomainCacheService",  "IDomainCacheService", "Инвалидация Domain_{host} в Redis при изменениях")
        Component(quotaCache,  "QuotaCacheService",   "IQuotaCacheService",  "Инвалидация ключей счётчиков квот в Redis")

        ComponentDb(dbCtx, "DomainListsContext", "EF Core DbContext", "domain, domain_status_type, quota")
    }

    Rel(adminPanel, corsMw, "JSON/HTTPS, cookie", "fetch + credentials")
    Rel(corsMw, authMw, "пропуск")
    Rel(authMw, authCtrl,    "/api/auth/*")
    Rel(authMw, domainsCtrl, "/api/domains/* (Authorize)")
    Rel(authMw, statusCtrl,  "/api/domainstatustypes/* (Authorize)")
    Rel(authMw, quotasCtrl,  "/api/quotas/* (Authorize)")

    Rel(domainsCtrl, dbCtx,       "EF Core")
    Rel(domainsCtrl, mapster,     "Adapt<DomainDto>()")
    Rel(domainsCtrl, domainCache, "InvalidateDomainAsync(host)")

    Rel(statusCtrl, dbCtx, "EF Core (read)")

    Rel(quotasCtrl, dbCtx,      "EF Core")
    Rel(quotasCtrl, quotaCache, "Invalidate*")

    Rel(domainCache, redis, "KeyDelete Domain_{host}",     "IDistributedCache / IConnectionMultiplexer")
    Rel(quotaCache,  redis, "KeyDelete для счётчиков",     "IConnectionMultiplexer")

    Rel(dbCtx, sqlite, "SQLite")

    UpdateLayoutConfig($c4ShapeInRow="3", $c4BoundaryInRow="1")
```

## Пояснения

- **Аутентификация**: схема `Cookies` с `HttpOnly`, `SameSite=Strict`,
  `Secure` в продакшене. Контроллеры (`Domains`, `DomainStatusTypes`,
  `Quotas`) защищены атрибутом `[Authorize]`; `AuthController` — `[AllowAnonymous]`
  для `/login`.
- **CORS**: политика `AllowAdminPanel` разрешает запросы только с фронтенда
  Admin Panel, с поддержкой кук (`AllowCredentials`).
- **DTO-маппинг**: `Mapster` сконфигурирован в `Program.cs` так, что у
  `DomainDto` есть плоское поле `DomainStatusName` (по соглашению ABP-style:
  `*Dto` для чтения, `*CreateUpdateDto` для записи).
- **Согласованность кэша и БД**: после любого изменения домена / квоты
  контроллер вызывает соответствующий `*CacheService`, который удаляет ключи
  в Redis, чтобы `Test.Api` сразу подхватил новое состояние.
- **Документация API**: в Development подключаются OpenAPI и Scalar
  (`MapOpenApi`, `MapScalarApiReference`).
