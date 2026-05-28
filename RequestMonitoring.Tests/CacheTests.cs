using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using RequestMonitoring.Library.Context;
using RequestMonitoring.Library.Enitites;
using RequestMonitoring.Library.Middleware.Services.DomainCache;
using RequestMonitoring.Library.Middleware.Services.DomainCheck;
using RequestMonitoring.Library.Middleware.Services.QuotaCache;
using RequestMonitoring.Library.Middleware.Services.QuotaCheck;
using RequestMonitoring.Library.Shared;
using StackExchange.Redis;
using System.Data.Common;

namespace RequestMonitoring.Tests;

/// <summary>
/// Перехватчик команд EF Core для подсчёта запросов к БД
/// </summary>
class QueryCountInterceptor : DbCommandInterceptor
{
    private int _count;
    public int Count => _count;

    public override ValueTask<DbDataReader> ReaderExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        DbDataReader result,
        CancellationToken cancellationToken = default)
    {
        Interlocked.Increment(ref _count);
        return base.ReaderExecutedAsync(command, eventData, result, cancellationToken);
    }
}

/// <summary>
/// Тесты корректности кэширования домена и метаданных квоты
/// </summary>
public class CacheTests
{
    // ── helpers ──────────────────────────────────────────────────────────────

    private static (DomainListsContext Ctx, QueryCountInterceptor Interceptor, SqliteConnection Connection) CreateDbContext()
    {
        var interceptor = new QueryCountInterceptor();

        // SQLite in-memory с именованным соединением — БД живёт пока открыто соединение
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<DomainListsContext>()
            .UseSqlite(connection)
            .AddInterceptors(interceptor)
            .Options;

        var ctx = new DomainListsContext(options);
        // EnsureCreated применяет HasData seed из OnModelCreating — статусы добавляются автоматически
        ctx.Database.EnsureCreated();

        return (ctx, interceptor, connection);
    }

    private static HybridCache CreateHybridCache()
    {
        var services = new ServiceCollection();
        services.AddHybridCache();
        return services.BuildServiceProvider().GetRequiredService<HybridCache>();
    }

    private static IConfiguration CreateConfig(int expirationMinutes = 10) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["CacheSettings:ExpirationMinutes"] = expirationMinutes.ToString(),
                ["QuotaSettings:SyncEveryNRequests"] = "1"
            })
            .Build();

    private static HttpContext CreateHttpContext(string host)
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.Headers["X-Test-Host"] = host;
        return ctx;
    }

    private static Domain AddDomain(DomainListsContext ctx, string host, int statusId = 1)
    {
        var status = ctx.DomainStatusTypes.Single(s => s.Id == statusId);
        var domain = new Domain
        {
            Id = ctx.Domains.Count() + 1,
            Host = host,
            DomainStatusTypeId = statusId,
            DomainStatusType = status
        };
        ctx.Domains.Add(domain);
        ctx.ChangeTracker.Entries<DomainStatusType>()
            .Where(e => e.State == Microsoft.EntityFrameworkCore.EntityState.Added)
            .ToList()
            .ForEach(e => e.State = Microsoft.EntityFrameworkCore.EntityState.Unchanged);
        ctx.SaveChanges();
        return domain;
    }

    private static Quota AddQuota(DomainListsContext ctx, Domain domain, int maxRequests = 100)
    {
        var quota = new Quota
        {
            Id = ctx.Quotas.Count() + 1,
            DomainId = domain.Id,
            Domain = domain,
            Type = QuotaType.Total,
            MaxRequests = maxRequests,
            RequestCount = 0
        };
        ctx.Quotas.Add(quota);
        ctx.SaveChanges();
        return quota;
    }

    private static (Mock<IDatabase> MockDb, Mock<IConnectionMultiplexer> MockRedis) CreateRedisMocks(long counterValue = 1)
    {
        var mockDb = new Mock<IDatabase>();
        mockDb
            .Setup(d => d.StringSetAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(),
                It.IsAny<TimeSpan?>(), It.IsAny<bool>(), When.NotExists, It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);
        mockDb
            .Setup(d => d.StringIncrementAsync(It.IsAny<RedisKey>(), It.IsAny<long>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(counterValue);
        mockDb
            .Setup(d => d.KeyDeleteAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        var mockMux = new Mock<IConnectionMultiplexer>();
        mockMux.Setup(m => m.GetDatabase(It.IsAny<int>(), It.IsAny<object?>())).Returns(mockDb.Object);

        return (mockDb, mockMux);
    }

    // ── DomainCheckService cache tests ────────────────────────────────────────

    [Fact]
    public async Task DomainCheck_SecondCall_DoesNotHitDatabase()
    {
        var (ctx, interceptor, connection) = CreateDbContext();
        await using var _ = connection;

        AddDomain(ctx, "cached.com");
        var service = new DomainCheckService(CreateConfig(), CreateHybridCache(), ctx, NullLogger<DomainCheckService>.Instance);

        await service.IsDomainAllowedAsync(CreateHttpContext("cached.com"));
        var afterFirst = interceptor.Count;

        await service.IsDomainAllowedAsync(CreateHttpContext("cached.com"));

        // Второй вызов не должен добавить новых запросов в БД
        Assert.Equal(afterFirst, interceptor.Count);
    }

    [Fact]
    public async Task DomainCheck_DifferentDomains_EachHitsDatabase()
    {
        var (ctx, interceptor, connection) = CreateDbContext();
        await using var _ = connection;

        AddDomain(ctx, "first.com");
        AddDomain(ctx, "second.com");
        var service = new DomainCheckService(CreateConfig(), CreateHybridCache(), ctx, NullLogger<DomainCheckService>.Instance);

        await service.IsDomainAllowedAsync(CreateHttpContext("first.com"));
        var afterFirst = interceptor.Count;

        await service.IsDomainAllowedAsync(CreateHttpContext("second.com"));

        // Каждый новый домен — новый запрос в БД
        Assert.True(interceptor.Count > afterFirst);
    }

    [Fact]
    public async Task DomainCheck_AfterInvalidation_HitsDatabaseAgain()
    {
        var (ctx, interceptor, connection) = CreateDbContext();
        await using var _ = connection;

        AddDomain(ctx, "invalidate.com");
        var hybridCache = CreateHybridCache();
        var service = new DomainCheckService(CreateConfig(), hybridCache, ctx, NullLogger<DomainCheckService>.Instance);

        // Первый вызов — заполняет кэш
        await service.IsDomainAllowedAsync(CreateHttpContext("invalidate.com"));
        var afterFirst = interceptor.Count;

        // Инвалидируем напрямую через hybridCache (как делает DomainCacheService)
        await hybridCache.RemoveAsync("Domain_invalidate.com");

        // Второй вызов после инвалидации — должен снова обратиться в БД
        await service.IsDomainAllowedAsync(CreateHttpContext("invalidate.com"));

        Assert.True(interceptor.Count > afterFirst);
    }

    [Fact]
    public async Task DomainCheck_UnknownDomain_ReturnsUnauthorized()
    {
        var (ctx, _, connection) = CreateDbContext();
        await using var _ = connection;

        // Домен не добавляем в БД — он неизвестен
        var service = new DomainCheckService(CreateConfig(), CreateHybridCache(), ctx, NullLogger<DomainCheckService>.Instance);

        var result1 = await service.IsDomainAllowedAsync(CreateHttpContext("unknown.com"));
        var result2 = await service.IsDomainAllowedAsync(CreateHttpContext("unknown.com"));

        Assert.Equal("Unauthorized", result1.Name);
        Assert.Equal("Unauthorized", result2.Name);
    }

    // ── QuotaService cache tests ──────────────────────────────────────────────

    [Fact]
    public async Task QuotaService_WhenExceeded_InvalidatesBothDomainAndQuotaCache()
    {
        var (ctx, _, connection) = CreateDbContext();
        await using var _ = connection;

        var domain = AddDomain(ctx, "quota-exceed.com");
        AddQuota(ctx, domain, maxRequests: 5);

        var (_, mockMux) = CreateRedisMocks(counterValue: 6); // сразу превышает лимит

        var domainCacheMock = new Mock<IDomainCacheService>();
        domainCacheMock.Setup(s => s.InvalidateDomainAsync(It.IsAny<string>())).Returns(Task.CompletedTask);
        var quotaCacheMock = new Mock<IQuotaCacheService>();
        quotaCacheMock.Setup(s => s.InvalidateQuotaAsync(It.IsAny<int>())).Returns(Task.CompletedTask);

        var service = new QuotaService(mockMux.Object, ctx, CreateConfig(), domainCacheMock.Object,
            quotaCacheMock.Object, NullLogger<QuotaService>.Instance);

        var result = await service.CheckAndIncrementAsync("quota-exceed.com");

        Assert.Equal(QuotaCheckResult.Exceeded, result);

        // При превышении квоты оба кэша должны быть инвалидированы
        domainCacheMock.Verify(s => s.InvalidateDomainAsync("quota-exceed.com"), Times.Once);
        quotaCacheMock.Verify(s => s.InvalidateQuotaAsync(domain.Id), Times.Once);
    }

    [Fact]
    public async Task QuotaService_WhenTemporarilyExceeded_DoesNotInvalidateCache()
    {
        var (ctx, _, connection) = CreateDbContext();
        await using var _ = connection;

        var domain = AddDomain(ctx, "periodic.com");
        ctx.Quotas.Add(new Quota
        {
            Id = 1,
            DomainId = domain.Id,
            Domain = domain,
            Type = QuotaType.Periodic,
            MaxRequests = 5,
            PeriodSeconds = 3600,
            RequestCount = 5,
            LastResetAt = DateTime.UtcNow  // период ещё не истёк
        });
        await ctx.SaveChangesAsync();

        var (_, mockMux) = CreateRedisMocks(counterValue: 6);
        var domainCacheMock = new Mock<IDomainCacheService>();
        var quotaCacheMock = new Mock<IQuotaCacheService>();

        var service = new QuotaService(mockMux.Object, ctx, CreateConfig(), domainCacheMock.Object,
            quotaCacheMock.Object, NullLogger<QuotaService>.Instance);

        var result = await service.CheckAndIncrementAsync("periodic.com");

        Assert.Equal(QuotaCheckResult.TemporarilyExceeded, result);

        // При временном превышении кэш НЕ инвалидируется — домен остаётся Allowed
        domainCacheMock.Verify(s => s.InvalidateDomainAsync(It.IsAny<string>()), Times.Never);
        quotaCacheMock.Verify(s => s.InvalidateQuotaAsync(It.IsAny<int>()), Times.Never);
    }
}
