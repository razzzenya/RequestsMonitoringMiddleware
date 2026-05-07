# RequestsMonitoringMiddleware

Распределённая система мониторинга и контроля HTTP‑запросов на базе ASP.NET Core. Предоставляет middleware для проверки доступа доменов к защищаемому API, ограничения количества запросов по квотам, а также логирования и наблюдения за трафиком в реальном времени.

## Что это за код

Проект реализует решение для управления доступом и нагрузкой со стороны внешних доменов‑клиентов. Основные возможности:

- **Контроль доступа доменов** — проверка хоста входящего запроса по белому, серому и неизвестному спискам. Запросы из неизвестных или серых доменов блокируются с соответствующими HTTP‑статусами (`401`, `402`).
- **Квоты на запросы** — ограничение количества обращений каждого домена. Поддерживаются как абсолютные, так и периодические (например, в час/сутки) квоты. При исчерпании абсолютной квоты домен автоматически переводится в greylist; при исчерпании периодической — возвращается `429 Too Many Requests`.
- **Логирование запросов** — каждый HTTP‑запрос (метод, путь, query, IP, заголовки, статус, длительность) индексируется в OpenSearch для последующего анализа и визуализации в OpenSearch Dashboards.
- **Админ‑API и админ‑панель** — REST‑сервис и Blazor WebAssembly интерфейс для управления списком доменов, статусами и квотами.
- **Наблюдаемость (observability)** — интеграция с OpenTelemetry: трассировки операций middleware (`RequestMonitoring.DomainCheck`, `RequestMonitoring.RequestLogging`), метрики ASP.NET Core, Runtime и Process.

## На чём написан

- **Платформа:** .NET 10 (ASP.NET Core).
- **Язык:** C#.
- **Оркестрация:** [.NET Aspire](https://learn.microsoft.com/dotnet/aspire/) — единая точка запуска всех сервисов и контейнеров (`RequestMonitoring.AppHost`).
- **Хранилище данных:**
  - SQLite через Entity Framework Core — список доменов, статусов и квот.
  - Redis (StackExchange.Redis) — распределённый кэш доменов и атомарный счётчик квот (`StringIncrementAsync`).
  - OpenSearch (клиент `OpenSearch.Client`) — хранилище логов запросов.
- **UI:** Blazor WebAssembly + [MudBlazor](https://mudblazor.com/).
- **API‑документация:** OpenAPI + [Scalar](https://scalar.com/) (админ‑API), Swagger UI (тестовое API). Клиент к админ‑API генерируется из `swagger.json` через NSwag.
- **Маппинг DTO:** Mapster.
- **Аутентификация админ‑API:** cookie‑аутентификация (`SameSite=Strict`, `HttpOnly`).
- **Тестирование:** xUnit, Moq, `Microsoft.EntityFrameworkCore.InMemory`.
- **Телеметрия:** OpenTelemetry (трассировки/метрики) с экспортом по OTLP.

## Структура решения

| Проект | Назначение |
| --- | --- |
| `RequestMonitoring.AppHost` | Aspire host: поднимает Redis (с Redis Commander), OpenSearch, OpenSearch Dashboards и все сервисы решения. |
| `RequestMonitoring.Library` | Ядро: middleware (`RequestMonitoringMiddleware`, `RequestLoggingMiddleware`), сервисы проверки доменов, квот, кэширования, логирования в OpenSearch, EF Core контекст и миграции. |
| `RequestMonitoring.Library.Shared` | Общие типы (например, `QuotaType`), используемые библиотекой и UI. |
| `RequestMonitoring.AdminApi` | REST API для управления доменами, статусами и квотами (Scalar UI, cookie‑auth, CORS для админ‑панели). |
| `RequestMonitoring.AdminPanel` | Blazor WebAssembly + MudBlazor админ‑панель, использующая сгенерированный по OpenAPI клиент к `AdminApi`. |
| `RequestMonitoring.Test.Api` | Демонстрационное защищаемое API: подключает `RequestLoggingMiddleware` и `RequestMonitoringMiddleware`, использует SQLite и Redis. |
| `RequestMonitoring.Tests` | Юнит‑тесты на сервисы библиотеки (xUnit + Moq + EF InMemory). |

## Как это работает

Поток обработки входящего HTTP‑запроса в защищаемом API (`RequestMonitoring.Test.Api`):

1. **`RequestLoggingMiddleware`** запускает `Stopwatch`, прокидывает запрос дальше по конвейеру и после ответа собирает запись `RequestLog` (метод, путь, query, IP клиента, заголовки, код ответа, длительность). Запись индексируется в OpenSearch асинхронно (`IndexAsync`), а действия оборачиваются в `Activity` для OpenTelemetry.

2. **`RequestMonitoringMiddleware`** определяет хост домена (берётся из заголовка `X-Test-Host` либо `Host`) и вызывает `IDomainCheckService`, который через `IDomainCacheService` (Redis) с фоллбэком на БД возвращает статус домена:
   - **`Whitelisted` (Id = 1)** — вызывается `IQuotaService.CheckAndIncrementAsync(host)`:
     - В Redis атомарно инкрементируется счётчик через `StringIncrementAsync`. Применяется политика квоты (`QuotaPolicy.Create(quota.Type)`): абсолютная или периодическая. Каждые `QuotaSettings:SyncEveryNRequests` запросов значение синхронизируется в SQLite.
     - При **`Exceeded`** (исчерпана абсолютная квота) домен автоматически переводится в `Greylisted` (обновление БД + инвалидация кэша), запрос возвращает `402 Payment Required`.
     - При **`TemporarilyExceeded`** (исчерпана периодическая квота) запрос возвращает `429 Too Many Requests` (счётчик сбросится в конце периода).
     - Иначе — управление передаётся следующему middleware (`await next(context)`).
   - **`Greylisted` (Id = 2)** — `402 Payment Required`.
   - **`Unknown` (Id = 3)** — `401 Unauthorized`.

3. **Управление данными** осуществляется через `RequestMonitoring.AdminApi` и Blazor‑панель: добавление/удаление доменов, смена статусов, конфигурация квот. После изменений соответствующие записи в Redis инвалидируются через `IDomainCacheService` / `IQuotaCacheService`.

4. **Aspire AppHost** связывает компоненты: Redis (с Redis Commander), OpenSearch + Dashboards, `Test.Api` (с `WaitFor(redis)` и переменными `OpenSearch__Uri` / `OpenSearch__Index`), `AdminApi` и `AdminPanel`.

## Запуск

Требования:
- .NET 10 SDK
- Docker (для контейнеров Redis и OpenSearch, поднимаемых Aspire)

Запуск всего решения через Aspire:

```bash
dotnet run --project RequestMonitoring.AppHost
```

В Aspire Dashboard будут доступны:
- `api` — защищаемое тестовое API (Swagger UI),
- `adminapi` — админ‑API (Scalar UI),
- `adminpanel` — Blazor‑панель администратора,
- `cache` — Redis + Redis Commander,
- `opensearch` и `opensearch-dashboard` — хранилище логов и UI для их просмотра.

Запуск тестов:

```bash
dotnet test
```

## Конфигурация

Основные настройки задаются в `appsettings.json` соответствующих проектов и переменных окружения, передаваемых из `AppHost`:

- `ConnectionStrings:Default` — строка подключения к SQLite.
- `OpenSearch:Uri`, `OpenSearch:Index` — адрес и индекс OpenSearch.
- `QuotaSettings:SyncEveryNRequests` — частота синхронизации счётчика квоты из Redis в БД (по умолчанию 10).
- `AllowedOrigins` — разрешённые origins для CORS админ‑API.
