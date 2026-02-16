# Настройка тестовых данных для Domain Middleware Tests

Этот гайд поможет настроить тестовые домены в базе данных для корректной работы интеграционных тестов.

## 📋 Что нужно сделать

### 1. Создать базу данных (если еще не создана)

```powershell
cd RequestMonitoringLibrary
dotnet ef database update --startup-project ../Test.Api
```

Это создаст файл `Test.Api/domains.db` с необходимыми таблицами.

### 2. Добавить тестовые домены

Есть несколько способов:

#### Способ А: Через SQL скрипт (рекомендуется)

```powershell
# Установите SQLite CLI если еще нет:
# winget install SQLite.SQLite

# Выполните SQL скрипт
cd Test.Api
sqlite3 domains.db < seed-test-domains.sql
```

#### Способ Б: Через DB Browser for SQLite (GUI)

1. Скачайте [DB Browser for SQLite](https://sqlitebrowser.org/dl/)
2. Откройте файл `Test.Api/domains.db`
3. Перейдите на вкладку "Execute SQL"
4. Вставьте содержимое файла `seed-test-domains.sql`
5. Нажмите "Execute" (▶️)

#### Способ В: Вручную через командную строку

```powershell
cd Test.Api
sqlite3 domains.db

# В интерактивном режиме SQLite выполните:
INSERT INTO domain (host, status_id, DomainStatusTypeId) VALUES ('allowed.example.com', 1, 1);
INSERT INTO domain (host, status_id, DomainStatusTypeId) VALUES ('greylisted.example.com', 2, 2);
INSERT INTO domain (host, status_id, DomainStatusTypeId) VALUES ('blocked.example.com', 3, 3);
INSERT INTO domain (host, status_id, DomainStatusTypeId) VALUES ('localhost', 1, 1);
INSERT INTO domain (host, status_id, DomainStatusTypeId) VALUES ('127.0.0.1', 1, 1);
.exit
```

### 3. Проверить данные

```sql
SELECT 
    d.id,
    d.host,
    dst.name as status_name,
    d.status_id
FROM domain d
JOIN DomainStatusType dst ON d.status_id = dst.id
ORDER BY d.status_id, d.host;
```

Должно показать:

```
id  host                      status_name    status_id
--  ------------------------  -------------  ---------
1   127.0.0.1                Allowed        1
2   allowed.example.com      Allowed        1
3   localhost                Allowed        1
4   greylisted.example.com   Greylisted     2
5   blocked.example.com      Unauthorized   3
```

## 🧪 Тесты Domain Middleware

После настройки тестовых данных, следующие тесты будут работать корректно:

### Базовые тесты по доменам:

1. **`RequestFromDomain_ReturnsExpectedStatusCode`** (Theory тест)
   - Тестирует 4 домена: allowed, greylisted, blocked, unknown
   - Проверяет соответствующие статус коды: 200, 402, 401, 401

2. **`AllowedDomain_CanAccessMultipleEndpoints`**
   - Проверяет что allowed домен может получить доступ ко всем эндпоинтам

3. **`GreylistedDomain_ReceivesPaymentRequiredWithMessage`**
   - Проверяет что greylisted домен получает 402 и сообщение "This domain is greylisted."

4. **`UnauthorizedDomain_ReceivesForbiddenWithMessage`**
   - Проверяет что неизвестный домен получает 401 и сообщение "This domain is forbidden."

5. **`MultipleDomains_ProcessedConcurrently`**
   - Тестирует параллельную обработку запросов с разных доменов

6. **`SpecialDomains_HandleCorrectly`** (Theory тест)
   - Проверяет специальные случаи: пустой host, localhost, IP адреса

## 🚀 Запуск тестов

### Запустить все тесты:

```powershell
dotnet test
```

### Запустить только Domain middleware тесты:

```powershell
dotnet test --filter "FullyQualifiedName~DomainMiddlewareTests"
```

### Запустить конкретный тест:

```powershell
dotnet test --filter "FullyQualifiedName~RequestFromDomain_ReturnsExpectedStatusCode"
```

### С подробным выводом:

```powershell
dotnet test --logger "console;verbosity=detailed"
```

## 🎯 Что тестируется

### Сценарии доступа:

| Домен                    | Статус в БД  | Ожидаемый код | Сообщение                      |
|--------------------------|--------------|---------------|--------------------------------|
| allowed.example.com      | Allowed (1)  | 200 OK        | -                              |
| greylisted.example.com   | Greylisted(2)| 402           | "This domain is greylisted."   |
| blocked.example.com      | Unauthorized(3)| 401         | "This domain is forbidden."    |
| unknown.example.com      | Не в БД      | 401           | "This domain is forbidden."    |
| localhost                | Allowed (1)  | 200 OK        | -                              |
| 127.0.0.1                | Allowed (1)  | 200 OK        | -                              |

## 🔍 Отладка тестов

### Если тесты падают:

1. **Проверьте что база данных существует**:
   ```powershell
   ls Test.Api/domains.db
   ```

2. **Проверьте данные в базе**:
   ```powershell
   cd Test.Api
   sqlite3 domains.db "SELECT * FROM domain;"
   ```

3. **Пересоздайте базу**:
   ```powershell
   rm Test.Api/domains.db
   cd RequestMonitoringLibrary
   dotnet ef database update --startup-project ../Test.Api
   # Затем снова выполните seed-test-domains.sql
   ```

4. **Проверьте что Aspire AppHost не запущен**:
   - Тесты сами запускают AppHost
   - Закройте все запущенные экземпляры

5. **Очистите и пересоберите**:
   ```powershell
   dotnet clean
   dotnet build
   dotnet test
   ```

## 📝 Добавление своих тестовых доменов

Чтобы добавить свой домен для тестирования:

```sql
-- Allowed домен
INSERT INTO domain (host, status_id, DomainStatusTypeId) 
VALUES ('mytest.example.com', 1, 1);

-- Greylisted домен
INSERT INTO domain (host, status_id, DomainStatusTypeId) 
VALUES ('limited.example.com', 2, 2);

-- Blocked домен
INSERT INTO domain (host, status_id, DomainStatusTypeId) 
VALUES ('banned.example.com', 3, 3);
```

Затем добавьте соответствующие тесты в `DomainMiddlewareTests.cs`.

## 🎨 Структура тестов

### Использование X-Test-Host заголовка

Тесты используют специальный заголовок `X-Test-Host` вместо стандартного `Host` заголовка.

**Почему?** При установке `Host` заголовка, HttpClient пытается физически подключиться к указанному домену, что приводит к SocketException. `X-Test-Host` позволяет имитировать разные домены без изменения реального подключения.

```csharp
[Theory]
[InlineData("domain.com", HttpStatusCode.OK)]
public async Task RequestFromDomain_ReturnsExpectedStatusCode(
    string domain, 
    HttpStatusCode expectedStatusCode)
{
    // Arrange - создать AppHost
    // Act - отправить запрос с заголовком X-Test-Host
    var request = new HttpRequestMessage(HttpMethod.Get, "/weatherforecast");
    request.Headers.Add("X-Test-Host", domain);  // ✅ Используем X-Test-Host
    var response = await httpClient.SendAsync(request);
    
    // Assert - проверить статус код
    Assert.Equal(expectedStatusCode, response.StatusCode);
}
```

### Как это работает

`DomainCheckService` сначала проверяет заголовок `X-Test-Host`, и если его нет - использует реальный `Host`:

```csharp
var domain = context.Request.Headers["X-Test-Host"].FirstOrDefault() 
             ?? context.Request.Host.Host;
```

Это позволяет:
- ✅ Тестировать с разными доменами через заголовок
- ✅ В production использовать реальный Host
- ✅ Избежать ошибок подключения в тестах

## ✅ Чеклист перед запуском тестов

- [ ] База данных создана (`domains.db` существует)
- [ ] Тестовые домены добавлены (выполнен `seed-test-domains.sql`)
- [ ] Docker Desktop запущен (для Aspire контейнеров)
- [ ] Проект скомпилирован (`dotnet build`)
- [ ] Нет запущенных экземпляров AppHost

После выполнения чеклиста запустите:

```powershell
dotnet test
```

Все тесты должны пройти ✅ зеленым!

---

**Совет**: В CI/CD пайплайне добавьте автоматическое выполнение `seed-test-domains.sql` перед запуском тестов.
