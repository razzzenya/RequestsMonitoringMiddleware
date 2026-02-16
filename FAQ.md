# FAQ - Часто задаваемые вопросы

## 🧪 Тестирование

### Q: Почему тесты используют X-Test-Host вместо Host заголовка?

**A:** При установке `Host` заголовка напрямую, HttpClient пытается физически подключиться к указанному домену:

```csharp
// ❌ Не работает в тестах
request.Headers.Host = "allowed.example.com";
// HttpClient пытается подключиться к allowed.example.com → SocketException
```

**Решение:** Используем специальный заголовок `X-Test-Host`:

```csharp
// ✅ Работает в тестах
request.Headers.Add("X-Test-Host", "allowed.example.com");
// Подключается к localhost, но middleware видит домен allowed.example.com
```

`DomainCheckService` проверяет этот заголовок:

```csharp
var domain = context.Request.Headers["X-Test-Host"].FirstOrDefault() 
             ?? context.Request.Host.Host;
```

В production `X-Test-Host` не используется, поэтому берется реальный `Host`.

### Q: Как добавить тестовые домены в базу данных?

**A:** Выполните SQL скрипт:

```powershell
cd Test.Api
sqlite3 domains.db < seed-test-domains.sql
```

Или используйте DB Browser for SQLite для GUI интерфейса.

Подробнее: [TEST_SETUP.md](TEST_SETUP.md)

### Q: Тесты падают с ошибкой "Docker is not running"

**A:** Aspire требует Docker для запуска контейнеров (OpenSearch). 

**Решение:**
1. Установите [Docker Desktop](https://www.docker.com/products/docker-desktop/)
2. Запустите Docker Desktop
3. Проверьте: `docker ps`
4. Перезапустите тесты

### Q: Как запустить только Domain middleware тесты?

**A:** 

```powershell
dotnet test --filter "FullyQualifiedName~DomainMiddlewareTests"
```

Или конкретный тест:

```powershell
dotnet test --filter "RequestFromDomain_ReturnsExpectedStatusCode"
```

### Q: Тесты не находят базу данных

**A:** База данных создается при первом запуске миграций:

```powershell
cd RequestMonitoringLibrary
dotnet ef database update --startup-project ../Test.Api
```

Затем добавьте тестовые данные через `seed-test-domains.sql`.

## 🚀 Aspire

### Q: Что такое Aspire Dashboard?

**A:** Это веб-интерфейс для мониторинга Aspire приложений:
- Логи всех сервисов
- Метрики производительности
- Распределенная трассировка
- Статус контейнеров

Открывается автоматически при `dotnet run` в AppHost.

### Q: Как изменить порт OpenSearch?

**A:** В `AppHost.cs`:

```csharp
.WithHttpEndpoint(port: 9200, targetPort: 9200, name: "http")
//             ^--- внешний порт  ^--- внутренний порт контейнера
```

Также обновите `appsettings.json`:

```json
"OpenSearch": {
  "Uri": "http://localhost:9200"
}
```

### Q: Можно ли запустить без Docker?

**A:** Да, закомментируйте OpenSearch контейнер в `AppHost.cs`:

```csharp
// var openSearch = builder.AddContainer(...);

var api = builder.AddProject<Projects.RequestMonitoring_Test_Api>("api");
```

API будет использовать OpenSearch из `appsettings.json`.

## 📝 Логирование

### Q: Куда сохраняются логи?

**A:** В два места:

1. **OpenSearch** (если запущен):
   - `request-logs` - HTTP метрики от middleware
   - `app-logs-{дата}` - логи приложения от Serilog

2. **Консоль** - все логи дублируются в консоль

### Q: Как посмотреть логи в OpenSearch?

**A:** 

```bash
# Все индексы
curl http://localhost:9200/_cat/indices?v

# Логи запросов
curl http://localhost:9200/request-logs/_search?pretty

# Логи приложения (укажите дату)
curl http://localhost:9200/app-logs-2026.02.14/_search?pretty
```

Или используйте Aspire Dashboard → Logs.

### Q: Отключить логирование в OpenSearch?

**A:** В `Program.cs` закомментируйте:

```csharp
// .WriteTo.OpenSearch(...)
```

Логи будут только в консоль.

## 🔐 Domain Middleware

### Q: Как добавить новый домен?

**A:** Добавьте в базу данных:

```sql
-- Allowed домен
INSERT INTO domain (host, status_id, DomainStatusTypeId) 
VALUES ('newdomain.com', 1, 1);

-- Greylisted домен  
INSERT INTO domain (host, status_id, DomainStatusTypeId) 
VALUES ('limited.com', 2, 2);

-- Blocked домен
INSERT INTO domain (host, status_id, DomainStatusTypeId) 
VALUES ('blocked.com', 3, 3);
```

### Q: Какие статусы доменов существуют?

**A:**

| ID | Название | HTTP код | Описание |
|----|----------|----------|----------|
| 1 | Allowed | 200 OK | Полный доступ |
| 2 | Greylisted | 402 Payment Required | Ограниченный доступ |
| 3 | Unauthorized | 401 Unauthorized | Доступ запрещен |

### Q: Как отключить Domain middleware?

**A:** В `Program.cs` закомментируйте:

```csharp
// app.UseMiddleware<RequestMonitoringMiddleware>();
```

Все запросы будут проходить без проверки домена.

## 🛠️ Troubleshooting

### Q: Ошибка "Port 9200 is already in use"

**A:** Остановите другие экземпляры OpenSearch:

```powershell
docker stop $(docker ps -q)
```

### Q: EF миграции не работают

**A:** Проверьте версии пакетов - должны быть одинаковые во всех проектах:

```powershell
# Посмотреть версии EF
dotnet list package | findstr EntityFramework

# Обновить EF tools
dotnet tool update --global dotnet-ef
```

### Q: Aspire не может найти проект

**A:** Пересоберите решение:

```powershell
dotnet clean
dotnet build
```

Aspire генерирует код для `Projects.RequestMonitoring_Test_Api` при сборке.

## 📚 Полезные команды

```powershell
# Запуск Aspire
cd RequestMonitoring.AppHost
dotnet run

# Все тесты
dotnet test

# Только интеграционные тесты
dotnet test RequestMonitoring.Tests

# С подробным выводом
dotnet test --logger "console;verbosity=detailed"

# Создать миграцию
cd RequestMonitoringLibrary
dotnet ef migrations add MigrationName --startup-project ../Test.Api

# Применить миграцию
dotnet ef database update --startup-project ../Test.Api

# Посмотреть логи Docker контейнера
docker logs <container_id>

# Остановить все контейнеры
docker stop $(docker ps -q)

# Очистить Docker
docker system prune -a
```

## 🔗 Ссылки на документацию

- [QUICKSTART.md](QUICKSTART.md) - Быстрый старт
- [ASPIRE_GUIDE.md](ASPIRE_GUIDE.md) - Полное руководство по Aspire
- [LOGGING.md](LOGGING.md) - Про логирование
- [TEST_SETUP.md](TEST_SETUP.md) - Настройка тестов

---

Не нашли ответ? Создайте issue или обратитесь к документации выше.
