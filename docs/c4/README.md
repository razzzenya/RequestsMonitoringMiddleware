# C4-диаграммы проекта RequestsMonitoringMiddleware

Полный набор архитектурных диаграмм по [модели C4](https://c4model.com/) для системы
мониторинга и контроля HTTP-запросов по доменам.

Диаграммы выполнены в нотации **Mermaid** (поддерживается GitHub «из коробки»):
для уровней 1–3 используется встроенный синтаксис `C4Context` / `C4Container` /
`C4Component`, для уровня 4 — `classDiagram` и `sequenceDiagram`.

## Состав

| Уровень | Назначение | Файл |
|---|---|---|
| 1. System Context | Система и её внешние пользователи/системы | [01-context.md](./01-context.md) |
| 2. Containers     | Развёртываемые контейнеры (приложения, БД, кэш) | [02-container.md](./02-container.md) |
| 3. Components (Test.Api + Library) | Компоненты защищаемого API и библиотеки middleware | [03-component-test-api.md](./03-component-test-api.md) |
| 3. Components (AdminApi) | Компоненты административного API | [03-component-admin-api.md](./03-component-admin-api.md) |
| 4. Code — Class diagram | Ключевые классы и их связи | [04-code-class.md](./04-code-class.md) |
| 4. Code — Dynamic / Sequence | Поток обработки HTTP-запроса через middleware | [04-code-sequence.md](./04-code-sequence.md) |

## Обзор системы

`RequestsMonitoringMiddleware` — это набор middleware и сопровождающих сервисов
для ASP.NET Core, который:

- проверяет доменное имя входящего запроса по белому/серому списку
  (`RequestMonitoringMiddleware` + `DomainCheckService`);
- учитывает квоты на запросы (общие, периодические, бессрочные/истекающие)
  атомарно через Redis (`QuotaService` + `QuotaPolicy`);
- логирует каждый запрос в OpenSearch (`RequestLoggingMiddleware` +
  `OpenSearchLogService`);
- предоставляет административный REST API (`RequestMonitoring.AdminApi`) и
  Blazor WebAssembly UI (`RequestMonitoring.AdminPanel`) для управления
  доменами, статусами и квотами;
- оркестрируется через .NET Aspire (`RequestMonitoring.AppHost`).

Хранилища: SQLite (домены, статусы, квоты), Redis (кэш статусов и атомарные
счётчики квот), OpenSearch (логи запросов).
