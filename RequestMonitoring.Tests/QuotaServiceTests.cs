using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using RequestMonitoring.Library.Context;
using RequestMonitoring.Library.Enitites;
using RequestMonitoring.Library.Middleware.Services.DomainCache;
using RequestMonitoring.Library.Middleware.Services.QuotaCache;
using RequestMonitoring.Library.Middleware.Services.QuotaCheck;
using RequestMonitoring.Library.Shared;
using StackExchange.Redis;

namespace RequestMonitoring.Tests;

/// <summary>
/// Юнит-тесты для проверки логики квот
/// </summary>
public class QuotaServiceTests
{
    // Соединение живёт на всё время теста, БД удаляется при его закрытии
    private static (DomainListsContext Ctx, SqliteConnection Connection) CreateDbContext()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<DomainListsContext>()
            .UseSqlite(connection)
            .Options;

        var ctx = new DomainListsContext(options);
        // EnsureCreated применяет HasData seed — статусы добавляются автоматически
        ctx.Database.EnsureCreated();

        return (ctx, connection);
    }

    private static (Mock<IDatabase> MockDb, Mock<IConnectionMultiplexer> MockRedis) CreateRedisMocks(
        Func<long> incrementCounter)
    {
        var mockDb = new Mock<IDatabase>();

        mockDb
            .Setup(d => d.StringSetAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<RedisValue>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<bool>(),
                When.NotExists,
                It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        mockDb
            .Setup(d => d.StringIncrementAsync(It.IsAny<RedisKey>(), It.IsAny<long>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(() => incrementCounter());

        mockDb
            .Setup(d => d.KeyDeleteAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        var mockMux = new Mock<IConnectionMultiplexer>();
        mockMux.Setup(m => m.GetDatabase(It.IsAny<int>(), It.IsAny<object?>())).Returns(mockDb.Object);

        return (mockDb, mockMux);
    }

    private static QuotaService CreateService(IConnectionMultiplexer redis, DomainListsContext ctx)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["QuotaSettings:SyncEveryNRequests"] = "1"
            })
            .Build();

        var domainCacheMock = new Mock<IDomainCacheService>();
        domainCacheMock
            .Setup(s => s.InvalidateDomainAsync(It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var quotaCacheMock = new Mock<IQuotaCacheService>();
        quotaCacheMock
            .Setup(s => s.InvalidateQuotaAsync(It.IsAny<int>()))
            .Returns(Task.CompletedTask);

        return new QuotaService(redis, ctx, config, domainCacheMock.Object, quotaCacheMock.Object,
            NullLogger<QuotaService>.Instance);
    }

    private static (Domain Domain, DomainStatusType AllowedStatus) AddAllowedDomain(DomainListsContext ctx, string host)
    {
        var status = ctx.DomainStatusTypes.Single(s => s.Id == 1);

        var domain = new Domain
        {
            Id = 1,
            Host = host,
            DomainStatusTypeId = status.Id,
            DomainStatusType = status
        };

        ctx.Domains.Add(domain);
        ctx.SaveChanges();

        return (domain, status);
    }

    [Fact]
    public async Task TotalQuota_Exceeded_ReturnsExceeded()
    {
        var (ctx, connection) = CreateDbContext();
        await using var _ = connection;
        var (domain, _) = AddAllowedDomain(ctx, "example.com");

        var (_, mockMux) = CreateRedisMocks(() => 6); // Counter returns 6 (over MaxRequests of 5)

        var quota = new Quota
        {
            Id = 1,
            DomainId = domain.Id,
            Domain = domain,
            Type = QuotaType.Total,
            MaxRequests = 5,
            RequestCount = 0
        };

        ctx.Quotas.Add(quota);
        await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);

        var service = CreateService(mockMux.Object, ctx);
        var result = await service.CheckAndIncrementAsync("example.com");

        Assert.Equal(QuotaCheckResult.Exceeded, result);

        // ExecuteUpdateAsync работает напрямую через SQL, поэтому читаем свежие данные через AsNoTracking
        var updatedDomain = await ctx.Domains.AsNoTracking()
            .FirstAsync(d => d.Id == domain.Id, TestContext.Current.CancellationToken);
        Assert.Equal(2, updatedDomain.DomainStatusTypeId);
    }

    [Fact]
    public async Task TotalQuota_NotExceeded_ReturnsAllowed()
    {
        var (ctx, connection) = CreateDbContext();
        await using var _ = connection;
        var (domain, _) = AddAllowedDomain(ctx, "example.com");

        var (_, mockMux) = CreateRedisMocks(() => 3); // Counter returns 3 (under MaxRequests of 5)

        var quota = new Quota
        {
            Id = 1,
            DomainId = domain.Id,
            Domain = domain,
            Type = QuotaType.Total,
            MaxRequests = 5,
            RequestCount = 2
        };

        ctx.Quotas.Add(quota);
        await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);

        var service = CreateService(mockMux.Object, ctx);
        var result = await service.CheckAndIncrementAsync("example.com");

        Assert.Equal(QuotaCheckResult.Allowed, result);
    }

    [Fact]
    public async Task PeriodicQuota_TemporarilyExceeded_Returns429Result()
    {
        var (ctx, connection) = CreateDbContext();
        await using var _ = connection;
        var (domain, _) = AddAllowedDomain(ctx, "example.com");

        var (_, mockMux) = CreateRedisMocks(() => 6); // Counter returns 6 (over MaxRequests of 5)

        var quota = new Quota
        {
            Id = 1,
            DomainId = domain.Id,
            Domain = domain,
            Type = QuotaType.Periodic,
            MaxRequests = 5,
            PeriodSeconds = 3600,
            RequestCount = 5,
            LastResetAt = DateTime.UtcNow  // fresh period — не сбрасываем
        };

        ctx.Quotas.Add(quota);
        await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);

        var service = CreateService(mockMux.Object, ctx);
        var result = await service.CheckAndIncrementAsync("example.com");

        Assert.Equal(QuotaCheckResult.TemporarilyExceeded, result);

        // Домен НЕ должен уйти в Greylisted при временном превышении
        var updatedDomain = await ctx.Domains.AsNoTracking()
            .FirstAsync(d => d.Id == domain.Id, TestContext.Current.CancellationToken);
        Assert.Equal(1, updatedDomain.DomainStatusTypeId);
    }

    [Fact]
    public async Task PeriodicQuota_PeriodExpired_ResetsCounterAndClearsRedisKey()
    {
        var (ctx, connection) = CreateDbContext();
        await using var _ = connection;
        var (domain, _) = AddAllowedDomain(ctx, "example.com");

        var callCount = 0;
        var (mockDb, mockMux) = CreateRedisMocks(() => ++callCount);

        var quota = new Quota
        {
            Id = 1,
            DomainId = domain.Id,
            Domain = domain,
            Type = QuotaType.Periodic,
            MaxRequests = 100,
            PeriodSeconds = 1,
            RequestCount = 50,
            LastResetAt = DateTime.UtcNow.AddSeconds(-5)  // period has expired
        };

        ctx.Quotas.Add(quota);
        await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);

        var service = CreateService(mockMux.Object, ctx);
        var result = await service.CheckAndIncrementAsync("example.com");

        Assert.Equal(QuotaCheckResult.Allowed, result);

        // Redis ключ должен быть удалён при сбросе периода
        mockDb.Verify(d => d.KeyDeleteAsync(
            It.Is<RedisKey>(k => k.ToString().Contains(domain.Id.ToString())),
            It.IsAny<CommandFlags>()), Times.Once);

        // Счётчик сброшен в 0, затем инкрементирован до 1 — читаем через AsNoTracking
        var updatedQuota = await ctx.Quotas.AsNoTracking()
            .FirstAsync(q => q.Id == quota.Id, TestContext.Current.CancellationToken);
        Assert.Equal(1, updatedQuota.RequestCount);
    }

    [Fact]
    public async Task NoQuota_ReturnsNoQuota()
    {
        var (ctx, connection) = CreateDbContext();
        await using var _ = connection;
        AddAllowedDomain(ctx, "noquota.com");

        var (_, mockMux) = CreateRedisMocks(() => 1);

        var service = CreateService(mockMux.Object, ctx);
        var result = await service.CheckAndIncrementAsync("noquota.com");

        Assert.Equal(QuotaCheckResult.NoQuota, result);
    }
}
