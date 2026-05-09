# Аппаратные и программные требования

Документ описывает минимальные и рекомендуемые требования для разработки, сборки и эксплуатации решения **RequestsMonitoringMiddleware**.

Решение представляет собой набор .NET-проектов на базе .NET Aspire:

- `RequestMonitoring.Library` — библиотека middleware для мониторинга и контроля HTTP-запросов (квоты, чёрные/белые списки доменов, телеметрия через OpenTelemetry, логирование в OpenSearch).
- `RequestMonitoring.Library.Shared` — общие контракты и модели.
- `RequestMonitoring.AdminApi` — REST API администрирования (ASP.NET Core, EF Core + SQLite, Redis).
- `RequestMonitoring.AdminPanel` — веб-панель администратора на Blazor WebAssembly (MudBlazor).
- `RequestMonitoring.Test.Api` — тестовый API для интеграционной проверки middleware.
- `RequestMonitoring.AppHost` — оркестратор .NET Aspire для локального запуска всех сервисов.
- `RequestMonitoring.Tests` — модульные тесты.

## 1. Программные требования

### 1.1. Требования для разработки и сборки

| Компонент | Минимальная версия | Назначение |
|-----------|--------------------|------------|
| .NET SDK | 10.0 | Сборка всех проектов решения (`TargetFramework=net10.0`) |
| .NET Aspire workload | 13.1+ | Запуск `RequestMonitoring.AppHost` (`Aspire.AppHost.Sdk 13.1.0`) |
| ASP.NET Core Runtime | 10.0 | Запуск Web API и Blazor WebAssembly хоста |
| Docker / Docker Desktop | 24+ | Поднятие контейнеров (Redis, OpenSearch и др.), используемых Aspire |
| Git | 2.30+ | Работа с исходным кодом |
| IDE (любая на выбор) | — | Visual Studio 2022 17.10+ / JetBrains Rider 2024.2+ / VS Code с расширением C# Dev Kit |

Дополнительные пакеты подтягиваются автоматически через NuGet (см. `*.csproj`), среди ключевых:

- `Microsoft.EntityFrameworkCore.Sqlite` 10.0.7
- `Aspire.StackExchange.Redis` 13.2.4 / `Aspire.StackExchange.Redis.DistributedCaching` 13.2.4
- `OpenTelemetry.*` 1.15.x (трассировка, метрики, инструментирование ASP.NET Core / HTTP / Runtime / Process)
- `OpenSearch.Client` 1.8.0
- `MudBlazor` 8.8.0, `Microsoft.AspNetCore.Components.WebAssembly` 10.0.3
- `Mapster` 10.0.7, `Scalar.AspNetCore` 2.14.11
- `Microsoft.Extensions.Http.Resilience` 10.5.0, `Microsoft.Extensions.ServiceDiscovery` 10.5.0

Тестовые пакеты: `xUnit`, `Moq` 4.20.72, `Microsoft.EntityFrameworkCore.InMemory` 10.0.3, `Microsoft.Extensions.Logging.Abstractions` 10.0.3.

### 1.2. Требования для запуска (runtime)

| Компонент | Минимальная версия | Назначение |
|-----------|--------------------|------------|
| ASP.NET Core Runtime | 10.0 | Исполнение AdminApi и Test.Api |
| Redis | 7.0+ | Хранилище для распределённого кэша и атомарных счётчиков квот (`IConnectionMultiplexer`, `StringIncrementAsync`) |
| SQLite | 3.35+ | Локальная БД для AdminApi (через EF Core) |
| OpenSearch | 2.x | Хранилище журналов запросов (через `OpenSearch.Client`) |
| OpenTelemetry Collector (опционально) | актуальная | Приём метрик и трассировок по протоколу OTLP |
| Современный браузер | Chrome/Edge 110+, Firefox 110+, Safari 16+ | Работа с Blazor WebAssembly административной панелью |

При локальной разработке Redis, OpenSearch и прочая инфраструктура поднимаются автоматически через `RequestMonitoring.AppHost` (.NET Aspire) в Docker.

### 1.3. Поддерживаемые операционные системы

- Windows 10 версии 1809 / Windows 11 / Windows Server 2019, 2022 (x64, ARM64)
- Linux: Ubuntu 22.04+, Debian 12+, RHEL 9+, Alpine 3.19+ (x64, ARM64)
- macOS 13 Ventura и новее (x64, Apple Silicon)

Поддерживаются те же ОС, что и для .NET 10 / Docker Desktop.

## 2. Аппаратные требования

### 2.1. Машина разработчика

| Ресурс | Минимум | Рекомендуется |
|--------|---------|---------------|
| CPU | 4 ядра x64/ARM64 с поддержкой SSE4.2 | 8 ядер и более |
| ОЗУ | 8 ГБ | 16 ГБ и более (с учётом запуска Docker, Redis, OpenSearch) |
| Свободное место на диске | 15 ГБ (SDK, NuGet-кэш, Docker-образы) | 40 ГБ SSD |
| Сеть | Доступ в интернет к `nuget.org`, `mcr.microsoft.com`, `docker.io` | Стабильное широкополосное подключение |

### 2.2. Сервер для эксплуатации (на сервис AdminApi / Test.Api)

| Ресурс | Минимум | Рекомендуется |
|--------|---------|---------------|
| CPU | 2 vCPU | 4 vCPU и более |
| ОЗУ | 2 ГБ | 4 ГБ и более |
| Свободное место на диске | 5 ГБ (включая SQLite-файл и логи) | 20 ГБ SSD |
| Сеть | TCP-доступ к Redis и OpenSearch, открытые порты HTTP/HTTPS | Низкая задержка до Redis (< 5 мс) |

### 2.3. Инфраструктурные сервисы

| Сервис | Минимум | Рекомендуется |
|--------|---------|---------------|
| Redis | 1 vCPU, 512 МБ ОЗУ | 2 vCPU, 2 ГБ ОЗУ, persistence по необходимости |
| OpenSearch | 2 vCPU, 4 ГБ ОЗУ, 20 ГБ диска | 4 vCPU, 8 ГБ ОЗУ, SSD от 100 ГБ, кластер из 3 узлов |

## 3. Сетевые требования

- Исходящий доступ AdminApi/Test.Api к Redis (по умолчанию TCP 6379) и OpenSearch (TCP 9200).
- Входящий HTTP/HTTPS-трафик к AdminApi и хосту Blazor WebAssembly административной панели.
- Доступ к OTLP-эндпоинту коллектора OpenTelemetry (если настроен экспорт телеметрии).

## 4. Дополнительные замечания

- Все проекты ориентированы на .NET 10; использование более ранних версий рантайма не поддерживается.
- Для воспроизводимой локальной среды рекомендуется использовать `dotnet run --project RequestMonitoring.AppHost`, что автоматически поднимает зависимые контейнеры.
- Для запуска модульных тестов достаточно установленного .NET SDK 10 — внешняя инфраструктура не требуется (используются `Microsoft.EntityFrameworkCore.InMemory` и `Moq`).
