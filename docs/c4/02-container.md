# C4 · Уровень 2 — Containers

Диаграмма контейнеров детализирует систему `RequestsMonitoringMiddleware`
до уровня развёртываемых приложений и хранилищ. Оркестрация выполняется через
.NET Aspire (`RequestMonitoring.AppHost`).

```mermaid
C4Container
    title Containers — RequestsMonitoringMiddleware

    Person(admin, "Администратор")
    Person(client, "Внешний клиент / сервис")

    System_Boundary(rms, "RequestsMonitoringMiddleware") {
        Container(appHost, "AppHost", ".NET Aspire", "Оркестрация всех контейнеров (запуск, переменные окружения, привязки)")

        Container(adminPanel, "Admin Panel", "Blazor WebAssembly + MudBlazor", "SPA-интерфейс для администратора")
        Container(adminApi,   "Admin API",  "ASP.NET Core Web API",          "REST API для управления доменами, статусами и квотами; cookie-аутентификация")
        Container(testApi,    "Protected API (Test.Api)", "ASP.NET Core Web API + RequestMonitoring.Library", "Защищаемое API: подключает RequestLoggingMiddleware и RequestMonitoringMiddleware")

        ContainerDb(sqlite,     "Domains DB",   "SQLite",     "Домены, типы статусов, квоты")
        ContainerDb(redis,      "Redis",        "Redis 7",    "Кэш статусов доменов; атомарные счётчики квот (StringIncrementAsync)")
        ContainerDb(opensearch, "OpenSearch",   "OpenSearch 2.12", "Индекс request-logs с журналом HTTP-запросов")

        Container(osDashboard,    "OpenSearch Dashboards", "OpenSearch Dashboards 2.12", "UI для просмотра логов")
        Container(redisCommander, "Redis Commander",       "Web UI",                     "UI для просмотра содержимого Redis (dev)")
    }

    Rel(client, testApi, "HTTP-запросы", "HTTPS")
    Rel(admin,  adminPanel, "Использует UI", "HTTPS")
    Rel(admin,  osDashboard, "Просматривает логи", "HTTPS")

    Rel(adminPanel, adminApi, "Вызывает REST API (cookie-auth)", "JSON/HTTPS")

    Rel(adminApi, sqlite, "Читает/пишет домены и квоты", "EF Core")
    Rel(adminApi, redis,  "Инвалидация кэша доменов и квот", "StackExchange.Redis")

    Rel(testApi, sqlite, "Читает домены, статусы, квоты; синхронизирует счётчики", "EF Core")
    Rel(testApi, redis,  "Кэш статусов; атомарный инкремент счётчиков квот", "StackExchange.Redis / IDistributedCache")
    Rel(testApi, opensearch, "Индексирует RequestLog", "OpenSearch.Client (HTTP)")

    Rel(osDashboard, opensearch, "Читает индексы", "HTTP")
    Rel(redisCommander, redis,   "Читает данные",  "RESP")

    Rel(appHost, adminPanel, "запускает / связывает")
    Rel(appHost, adminApi,   "запускает / связывает")
    Rel(appHost, testApi,    "запускает / связывает")
    Rel(appHost, redis,      "запускает")
    Rel(appHost, opensearch, "запускает")
    Rel(appHost, osDashboard,"запускает")

    UpdateLayoutConfig($c4ShapeInRow="3", $c4BoundaryInRow="1")
```

## Контейнеры

| Контейнер | Технологии | Назначение |
|---|---|---|
| **AppHost** | .NET Aspire | Декларативное описание топологии (`AppHost.cs`): поднимает Redis, OpenSearch, OpenSearch Dashboards и три приложения с нужными переменными окружения. |
| **Admin Panel** | Blazor WebAssembly, MudBlazor | SPA для администратора. Авторизуется в Admin API через cookie (`CookieHandler`, `CookieAuthStateProvider`). |
| **Admin API** | ASP.NET Core Web API, EF Core, Mapster, Scalar | REST API для CRUD над доменами, статусами и квотами; логин/логаут через `AuthController` (cookie-аутентификация). |
| **Protected API (Test.Api)** | ASP.NET Core, `RequestMonitoring.Library` | Демонстрационное защищаемое API. В пайплайне подключены `RequestLoggingMiddleware` и `RequestMonitoringMiddleware`. |
| **Domains DB** | SQLite | Таблицы `domain`, `domain_status_type`, `quota`. |
| **Redis** | Redis | (1) Кэш `Domain_{host}` со статусом домена; (2) ключи счётчиков квот, инкрементируемые атомарно. |
| **OpenSearch** | OpenSearch 2.12 | Индекс `request-logs` с документами `RequestLog`. |
| **OpenSearch Dashboards** | OpenSearch Dashboards | UI для логов. |
| **Redis Commander** | Web UI | Dev-инструмент для просмотра Redis. |

## Ключевые потоки

- **Запрос клиента** идёт в `Test.Api` → проходит `RequestLoggingMiddleware`
  (стартует таймер, регистрирует логирование) → `RequestMonitoringMiddleware`
  (проверка домена и квоты) → контроллер. После завершения запроса лог
  асинхронно отправляется в OpenSearch.
- **Изменения администратора** через Admin Panel → Admin API → SQLite, после
  чего Admin API вызывает инвалидацию ключа в Redis (`IDomainCacheService`,
  `IQuotaCacheService`), чтобы `Test.Api` сразу увидел новое состояние.
