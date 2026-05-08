# ER-диаграмма базы данных

Диаграмма отражает реляционную модель данных, описанную в
[`DomainListsContext`](../RequestMonitoring.Library/Context/DomainListsContext.cs)
и сущностях из [`RequestMonitoring.Library/Enitites`](../RequestMonitoring.Library/Enitites).

В реляционной БД хранятся три сущности:

- **`domain`** — домены, мониторинг которых ведётся.
- **`DomainStatusTypes`** — справочник статусов домена (`Allowed`, `Greylisted`, `Unauthorized`).
  Имя таблицы соответствует имени C#-сущности `DomainStatusType` во множественном числе
  (на сущности нет атрибута `[Table]`, поэтому EF Core использует имя `DbSet`).
- **`quota`** — настройки и текущее состояние квоты для домена (связь 1:1 с `domain`,
  обеспечивается уникальным индексом по `quota.domain_id`).

> Сущность `RequestLog` не входит в `DbContext` и не отображается в реляционной БД,
> поэтому на диаграмме отсутствует.

```mermaid
erDiagram
    DomainStatusTypes ||--o{ domain : "status_id"
    domain ||--|| quota : "domain_id (UNIQUE)"

    DomainStatusTypes {
        int    id    PK
        string name  "MaxLength(30), NOT NULL"
    }

    domain {
        int    id         PK
        string host       "NOT NULL"
        int    status_id  FK "-> DomainStatusTypes.id, NOT NULL"
    }

    quota {
        int      id              PK
        int      domain_id       FK "-> domain.id, UNIQUE, NOT NULL"
        int      type            "QuotaType enum, NOT NULL"
        int      max_requests    "nullable (null = безлимит)"
        int      period_seconds  "nullable (null = без сброса)"
        datetime expires_at      "nullable (null = бессрочно)"
        bigint   request_count   "NOT NULL"
        datetime last_reset_at   "nullable"
    }
```

## Связи

| Связь                          | Кардинальность | Описание                                                                                       |
|--------------------------------|----------------|------------------------------------------------------------------------------------------------|
| `DomainStatusTypes` → `domain` | 1 : N          | У каждого домена ровно один статус; один статус может быть присвоен многим доменам.            |
| `domain` → `quota`             | 1 : 1          | У каждого домена не более одной квоты (уникальный индекс по `quota.domain_id`).                |

## Значения справочника `DomainStatusTypes` (seed-данные)

| id | name         |
|----|--------------|
| 1  | Allowed      |
| 2  | Greylisted   |
| 3  | Unauthorized |

## Значения перечисления `QuotaType` (хранится в `quota.type`)

| Значение            | Описание                                                |
|---------------------|---------------------------------------------------------|
| `Unlimited`         | Без ограничений                                         |
| `Periodic`          | Лимит запросов с периодическим сбросом счётчика         |
| `Total`             | Суммарный лимит запросов без сброса                     |
| `ExpiringUnlimited` | Без ограничений до даты `expires_at`                    |
| `ExpiringTotal`     | Суммарный лимит, действует до `expires_at`              |
| `ExpiringPeriodic`  | Периодический лимит, действует до `expires_at`          |
