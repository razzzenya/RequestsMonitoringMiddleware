using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using RequestMonitoring.Library.Context;
using RequestMonitoring.Library.Dto;
using RequestMonitoring.Library.Enitites;
using RequestMonitoring.Library.Middleware.Services.DomainCache;
using RequestMonitoring.Library.Middleware.Services.QuotaCache;
using RequestMonitoring.Library.Middleware.Services.QuotaCheck;
using RequestMonitoring.Library.Shared;
using StackExchange.Redis;

namespace RequestMonitoring.Tests;

/// <summary>
/// Тесты логики проверки квот всех типов
/// </summary>
public class QuotaServiceTests
{
    private static (DomainListsContext Ctx, SqliteConnection Connection) CreateDbContext()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<DomainListsContext>()
            .UseSqlite(connection)
            .Options;

        var ctx = new DomainListsContext(options);
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

    private static QuotaCheckService CreateService(IConnectionMultiplexer redis, DomainListsContext ctx)
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
            .Setup(s => s.DeleteCounterAsync(It.IsAny<int>()))
            .Returns(Task.CompletedTask);

        return new QuotaCheckService(redis, ctx, config, domainCacheMock.Object, quotaCacheMock.Object,
            NullLogger<QuotaCheckService>.Instance);
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

    private static QuotaMetaDto ToMeta(Quota q) =>
        new(q.Id, q.DomainId, q.Type, q.MaxRequests, q.PeriodSeconds, q.ExpiresAt);

    // ── Total ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Счётчик превысил MaxRequests — домен переводится в Greylisted
    /// </summary>
    [Fact]
    public async Task TotalQuota_Exceeded_ReturnsExceeded()
    {
        var (ctx, connection) = CreateDbContext();
        await using var _ = connection;
        var (domain, _) = AddAllowedDomain(ctx, "example.com");

        var (_, mockMux) = CreateRedisMocks(() => 6);

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
        var result = await service.CheckAndIncrementAsync("example.com", ToMeta(quota));

        Assert.Equal(QuotaCheckResult.Exceeded, result);

        var updatedDomain = await ctx.Domains.AsNoTracking()
            .FirstAsync(d => d.Id == domain.Id, TestContext.Current.CancellationToken);
        Assert.Equal(2, updatedDomain.DomainStatusTypeId);
    }

    /// <summary>
    /// Счётчик не превысил MaxRequests — запрос разрешён
    /// </summary>
    [Fact]
    public async Task TotalQuota_NotExceeded_ReturnsAllowed()
    {
        var (ctx, connection) = CreateDbContext();
        await using var _ = connection;
        var (domain, _) = AddAllowedDomain(ctx, "example.com");

        var (_, mockMux) = CreateRedisMocks(() => 3);

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
        var result = await service.CheckAndIncrementAsync("example.com", ToMeta(quota));

        Assert.Equal(QuotaCheckResult.Allowed, result);
    }

    // ── Periodic ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Лимит периода исчерпан, но период ещё не истёк — возвращает 429, домен остаётся Allowed
    /// </summary>
    [Fact]
    public async Task PeriodicQuota_TemporarilyExceeded_Returns429Result()
    {
        var (ctx, connection) = CreateDbContext();
        await using var _ = connection;
        var (domain, _) = AddAllowedDomain(ctx, "example.com");

        var (_, mockMux) = CreateRedisMocks(() => 6);

        var quota = new Quota
        {
            Id = 1,
            DomainId = domain.Id,
            Domain = domain,
            Type = QuotaType.Periodic,
            MaxRequests = 5,
            PeriodSeconds = 3600,
            RequestCount = 5,
            LastResetAt = DateTime.UtcNow
        };

        ctx.Quotas.Add(quota);
        await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);

        var service = CreateService(mockMux.Object, ctx);
        var result = await service.CheckAndIncrementAsync("example.com", ToMeta(quota));

        Assert.Equal(QuotaCheckResult.TemporarilyExceeded, result);

        var updatedDomain = await ctx.Domains.AsNoTracking()
            .FirstAsync(d => d.Id == domain.Id, TestContext.Current.CancellationToken);
        Assert.Equal(1, updatedDomain.DomainStatusTypeId);
    }

    /// <summary>
    /// Период истёк — счётчик в БД и Redis-ключ сбрасываются, запрос разрешён
    /// </summary>
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
            LastResetAt = DateTime.UtcNow.AddSeconds(-5)
        };

        ctx.Quotas.Add(quota);
        await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);

        var service = CreateService(mockMux.Object, ctx);
        var result = await service.CheckAndIncrementAsync("example.com", ToMeta(quota));

        Assert.Equal(QuotaCheckResult.Allowed, result);

        mockDb.Verify(d => d.KeyDeleteAsync(
            It.Is<RedisKey>(k => k.ToString().Contains(domain.Id.ToString())),
            It.IsAny<CommandFlags>()), Times.Once);

        var updatedQuota = await ctx.Quotas.AsNoTracking()
            .FirstAsync(q => q.Id == quota.Id, TestContext.Current.CancellationToken);
        Assert.Equal(1, updatedQuota.RequestCount);
    }

    /// <summary>
    /// Полный цикл: N запросов разрешены → N+1 заблокирован → после истечения периода снова разрешён
    /// </summary>
    [Fact]
    public async Task PeriodicQuota_FullCycle_AllowedThenBlockedThenAllowedAfterReset()
    {
        var (ctx, connection) = CreateDbContext();
        await using var _ = connection;
        var (domain, _) = AddAllowedDomain(ctx, "example.com");

        var counter = 0L;
        var (mockDb, mockMux) = CreateRedisMocks(() => ++counter);

        var quota = new Quota
        {
            Id = 1,
            DomainId = domain.Id,
            Domain = domain,
            Type = QuotaType.Periodic,
            MaxRequests = 3,
            PeriodSeconds = 1,
            RequestCount = 0,
            LastResetAt = DateTime.UtcNow
        };

        ctx.Quotas.Add(quota);
        await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);

        var service = CreateService(mockMux.Object, ctx);

        // Первые 3 запроса — разрешены
        for (var i = 0; i < 3; i++)
        {
            var r = await service.CheckAndIncrementAsync("example.com", ToMeta(quota));
            Assert.Equal(QuotaCheckResult.Allowed, r);
        }

        // 4-й запрос — превышен лимит периода
        var exceeded = await service.CheckAndIncrementAsync("example.com", ToMeta(quota));
        Assert.Equal(QuotaCheckResult.TemporarilyExceeded, exceeded);

        // Ждём истечения периода
        await Task.Delay(TimeSpan.FromSeconds(1.1), TestContext.Current.CancellationToken);

        // После сброса периода снова разрешён
        counter = 0;
        var afterReset = await service.CheckAndIncrementAsync("example.com", ToMeta(quota));
        Assert.Equal(QuotaCheckResult.Allowed, afterReset);

        mockDb.Verify(d => d.KeyDeleteAsync(
            It.Is<RedisKey>(k => k.ToString().Contains(domain.Id.ToString())),
            It.IsAny<CommandFlags>()), Times.Once);
    }

    // ── Unlimited ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Безлимитная квота — всегда разрешает запрос независимо от счётчика
    /// </summary>
    [Fact]
    public async Task UnlimitedQuota_AlwaysReturnsAllowed()
    {
        var (ctx, connection) = CreateDbContext();
        await using var _ = connection;
        var (domain, _) = AddAllowedDomain(ctx, "example.com");

        var (_, mockMux) = CreateRedisMocks(() => 999999);

        var quota = new Quota
        {
            Id = 1,
            DomainId = domain.Id,
            Domain = domain,
            Type = QuotaType.Unlimited,
            RequestCount = 0
        };

        ctx.Quotas.Add(quota);
        await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);

        var service = CreateService(mockMux.Object, ctx);
        var result = await service.CheckAndIncrementAsync("example.com", ToMeta(quota));

        Assert.Equal(QuotaCheckResult.Allowed, result);
    }

    // ── NoQuota ───────────────────────────────────────────────────────────────

    /// <summary>
    /// У домена нет квоты — доступ запрещён
    /// </summary>
    [Fact]
    public async Task NoQuota_ReturnsNoQuota()
    {
        var (ctx, connection) = CreateDbContext();
        await using var _ = connection;
        AddAllowedDomain(ctx, "noquota.com");

        var (_, mockMux) = CreateRedisMocks(() => 1);

        var service = CreateService(mockMux.Object, ctx);
        var result = await service.CheckAndIncrementAsync("noquota.com", null);

        Assert.Equal(QuotaCheckResult.NoQuota, result);
    }

    // ── ExpiringTotal ─────────────────────────────────────────────────────────

    /// <summary>
    /// Срок действия не истёк, счётчик не превышен — запрос разрешён
    /// </summary>
    [Fact]
    public async Task ExpiringTotal_NotExpired_NotExceeded_ReturnsAllowed()
    {
        var (ctx, connection) = CreateDbContext();
        await using var _ = connection;
        var (domain, _) = AddAllowedDomain(ctx, "example.com");

        var (_, mockMux) = CreateRedisMocks(() => 3);

        var quota = new Quota
        {
            Id = 1,
            DomainId = domain.Id,
            Domain = domain,
            Type = QuotaType.ExpiringTotal,
            MaxRequests = 5,
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            RequestCount = 0
        };

        ctx.Quotas.Add(quota);
        await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);

        var service = CreateService(mockMux.Object, ctx);
        var result = await service.CheckAndIncrementAsync("example.com", ToMeta(quota));

        Assert.Equal(QuotaCheckResult.Allowed, result);
    }

    /// <summary>
    /// Срок действия не истёк, но счётчик превышен — домен переводится в Greylisted
    /// </summary>
    [Fact]
    public async Task ExpiringTotal_NotExpired_Exceeded_ReturnsExceeded()
    {
        var (ctx, connection) = CreateDbContext();
        await using var _ = connection;
        var (domain, _) = AddAllowedDomain(ctx, "example.com");

        var (_, mockMux) = CreateRedisMocks(() => 6);

        var quota = new Quota
        {
            Id = 1,
            DomainId = domain.Id,
            Domain = domain,
            Type = QuotaType.ExpiringTotal,
            MaxRequests = 5,
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            RequestCount = 0
        };

        ctx.Quotas.Add(quota);
        await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);

        var service = CreateService(mockMux.Object, ctx);
        var result = await service.CheckAndIncrementAsync("example.com", ToMeta(quota));

        Assert.Equal(QuotaCheckResult.Exceeded, result);
    }

    /// <summary>
    /// Срок действия истёк — домен переводится в Greylisted независимо от счётчика
    /// </summary>
    [Fact]
    public async Task ExpiringTotal_Expired_ReturnsExceeded()
    {
        var (ctx, connection) = CreateDbContext();
        await using var _ = connection;
        var (domain, _) = AddAllowedDomain(ctx, "example.com");

        var (_, mockMux) = CreateRedisMocks(() => 1);

        var quota = new Quota
        {
            Id = 1,
            DomainId = domain.Id,
            Domain = domain,
            Type = QuotaType.ExpiringTotal,
            MaxRequests = 100,
            ExpiresAt = DateTime.UtcNow.AddDays(-1),
            RequestCount = 0
        };

        ctx.Quotas.Add(quota);
        await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);

        var service = CreateService(mockMux.Object, ctx);
        var result = await service.CheckAndIncrementAsync("example.com", ToMeta(quota));

        Assert.Equal(QuotaCheckResult.Exceeded, result);

        var updatedDomain = await ctx.Domains.AsNoTracking()
            .FirstAsync(d => d.Id == domain.Id, TestContext.Current.CancellationToken);
        Assert.Equal(2, updatedDomain.DomainStatusTypeId);
    }

    /// <summary>
    /// Полный цикл: запросы разрешены → после истечения срока доступ пропадает и домен уходит в Greylisted
    /// </summary>
    [Fact]
    public async Task ExpiringTotal_FullCycle_AllowedThenBlockedAfterExpiry()
    {
        var (ctx, connection) = CreateDbContext();
        await using var _ = connection;
        var (domain, _) = AddAllowedDomain(ctx, "example.com");

        var counter = 0L;
        var (_, mockMux) = CreateRedisMocks(() => ++counter);

        var quota = new Quota
        {
            Id = 1,
            DomainId = domain.Id,
            Domain = domain,
            Type = QuotaType.ExpiringTotal,
            MaxRequests = 100,
            ExpiresAt = DateTime.UtcNow.AddSeconds(1),
            RequestCount = 0
        };

        ctx.Quotas.Add(quota);
        await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);

        var service = CreateService(mockMux.Object, ctx);

        var before = await service.CheckAndIncrementAsync("example.com", ToMeta(quota));
        Assert.Equal(QuotaCheckResult.Allowed, before);

        await Task.Delay(TimeSpan.FromSeconds(1.1), TestContext.Current.CancellationToken);

        var after = await service.CheckAndIncrementAsync("example.com", ToMeta(quota));
        Assert.Equal(QuotaCheckResult.Exceeded, after);

        var updatedDomain = await ctx.Domains.AsNoTracking()
            .FirstAsync(d => d.Id == domain.Id, TestContext.Current.CancellationToken);
        Assert.Equal(2, updatedDomain.DomainStatusTypeId);
    }

    // ── ExpiringUnlimited ─────────────────────────────────────────────────────

    /// <summary>
    /// Срок действия не истёк — запрос разрешён без ограничений по счётчику
    /// </summary>
    [Fact]
    public async Task ExpiringUnlimited_NotExpired_ReturnsAllowed()
    {
        var (ctx, connection) = CreateDbContext();
        await using var _ = connection;
        var (domain, _) = AddAllowedDomain(ctx, "example.com");

        var (_, mockMux) = CreateRedisMocks(() => 1);

        var quota = new Quota
        {
            Id = 1,
            DomainId = domain.Id,
            Domain = domain,
            Type = QuotaType.ExpiringUnlimited,
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            RequestCount = 0
        };

        ctx.Quotas.Add(quota);
        await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);

        var service = CreateService(mockMux.Object, ctx);
        var result = await service.CheckAndIncrementAsync("example.com", ToMeta(quota));

        Assert.Equal(QuotaCheckResult.Allowed, result);
    }

    /// <summary>
    /// Срок действия истёк — домен переводится в Greylisted
    /// </summary>
    [Fact]
    public async Task ExpiringUnlimited_Expired_ReturnsExceeded()
    {
        var (ctx, connection) = CreateDbContext();
        await using var _ = connection;
        var (domain, _) = AddAllowedDomain(ctx, "example.com");

        var (_, mockMux) = CreateRedisMocks(() => 1);

        var quota = new Quota
        {
            Id = 1,
            DomainId = domain.Id,
            Domain = domain,
            Type = QuotaType.ExpiringUnlimited,
            ExpiresAt = DateTime.UtcNow.AddDays(-1),
            RequestCount = 0
        };

        ctx.Quotas.Add(quota);
        await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);

        var service = CreateService(mockMux.Object, ctx);
        var result = await service.CheckAndIncrementAsync("example.com", ToMeta(quota));

        Assert.Equal(QuotaCheckResult.Exceeded, result);

        var updatedDomain = await ctx.Domains.AsNoTracking()
            .FirstAsync(d => d.Id == domain.Id, TestContext.Current.CancellationToken);
        Assert.Equal(2, updatedDomain.DomainStatusTypeId);
    }

    /// <summary>
    /// Полный цикл: запросы разрешены → после истечения срока доступ пропадает и домен уходит в Greylisted
    /// </summary>
    [Fact]
    public async Task ExpiringUnlimited_FullCycle_AllowedThenBlockedAfterExpiry()
    {
        var (ctx, connection) = CreateDbContext();
        await using var _ = connection;
        var (domain, _) = AddAllowedDomain(ctx, "example.com");

        var (_, mockMux) = CreateRedisMocks(() => 1);

        var quota = new Quota
        {
            Id = 1,
            DomainId = domain.Id,
            Domain = domain,
            Type = QuotaType.ExpiringUnlimited,
            ExpiresAt = DateTime.UtcNow.AddSeconds(1),
            RequestCount = 0
        };

        ctx.Quotas.Add(quota);
        await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);

        var service = CreateService(mockMux.Object, ctx);

        var before = await service.CheckAndIncrementAsync("example.com", ToMeta(quota));
        Assert.Equal(QuotaCheckResult.Allowed, before);

        await Task.Delay(TimeSpan.FromSeconds(1.1), TestContext.Current.CancellationToken);

        var after = await service.CheckAndIncrementAsync("example.com", ToMeta(quota));
        Assert.Equal(QuotaCheckResult.Exceeded, after);

        var updatedDomain = await ctx.Domains.AsNoTracking()
            .FirstAsync(d => d.Id == domain.Id, TestContext.Current.CancellationToken);
        Assert.Equal(2, updatedDomain.DomainStatusTypeId);
    }

    // ── ExpiringPeriodic ──────────────────────────────────────────────────────

    /// <summary>
    /// Срок действия не истёк, лимит периода не превышен — запрос разрешён
    /// </summary>
    [Fact]
    public async Task ExpiringPeriodic_NotExpired_NotExceeded_ReturnsAllowed()
    {
        var (ctx, connection) = CreateDbContext();
        await using var _ = connection;
        var (domain, _) = AddAllowedDomain(ctx, "example.com");

        var (_, mockMux) = CreateRedisMocks(() => 3);

        var quota = new Quota
        {
            Id = 1,
            DomainId = domain.Id,
            Domain = domain,
            Type = QuotaType.ExpiringPeriodic,
            MaxRequests = 5,
            PeriodSeconds = 3600,
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            RequestCount = 2,
            LastResetAt = DateTime.UtcNow
        };

        ctx.Quotas.Add(quota);
        await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);

        var service = CreateService(mockMux.Object, ctx);
        var result = await service.CheckAndIncrementAsync("example.com", ToMeta(quota));

        Assert.Equal(QuotaCheckResult.Allowed, result);
    }

    /// <summary>
    /// Срок действия не истёк, лимит периода исчерпан — возвращает 429, домен остаётся Allowed
    /// </summary>
    [Fact]
    public async Task ExpiringPeriodic_NotExpired_PeriodExceeded_ReturnsTemporarilyExceeded()
    {
        var (ctx, connection) = CreateDbContext();
        await using var _ = connection;
        var (domain, _) = AddAllowedDomain(ctx, "example.com");

        var (_, mockMux) = CreateRedisMocks(() => 6);

        var quota = new Quota
        {
            Id = 1,
            DomainId = domain.Id,
            Domain = domain,
            Type = QuotaType.ExpiringPeriodic,
            MaxRequests = 5,
            PeriodSeconds = 3600,
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            RequestCount = 5,
            LastResetAt = DateTime.UtcNow
        };

        ctx.Quotas.Add(quota);
        await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);

        var service = CreateService(mockMux.Object, ctx);
        var result = await service.CheckAndIncrementAsync("example.com", ToMeta(quota));

        Assert.Equal(QuotaCheckResult.TemporarilyExceeded, result);
    }

    /// <summary>
    /// Срок действия истёк — домен переводится в Greylisted независимо от счётчика периода
    /// </summary>
    [Fact]
    public async Task ExpiringPeriodic_Expired_ReturnsExceeded()
    {
        var (ctx, connection) = CreateDbContext();
        await using var _ = connection;
        var (domain, _) = AddAllowedDomain(ctx, "example.com");

        var (_, mockMux) = CreateRedisMocks(() => 1);

        var quota = new Quota
        {
            Id = 1,
            DomainId = domain.Id,
            Domain = domain,
            Type = QuotaType.ExpiringPeriodic,
            MaxRequests = 100,
            PeriodSeconds = 3600,
            ExpiresAt = DateTime.UtcNow.AddDays(-1),
            RequestCount = 0,
            LastResetAt = DateTime.UtcNow
        };

        ctx.Quotas.Add(quota);
        await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);

        var service = CreateService(mockMux.Object, ctx);
        var result = await service.CheckAndIncrementAsync("example.com", ToMeta(quota));

        Assert.Equal(QuotaCheckResult.Exceeded, result);

        var updatedDomain = await ctx.Domains.AsNoTracking()
            .FirstAsync(d => d.Id == domain.Id, TestContext.Current.CancellationToken);
        Assert.Equal(2, updatedDomain.DomainStatusTypeId);
    }

    /// <summary>
    /// Полный цикл: запросы разрешены → после истечения срока доступ пропадает и домен уходит в Greylisted
    /// </summary>
    [Fact]
    public async Task ExpiringPeriodic_FullCycle_AllowedThenBlockedAfterExpiry()
    {
        var (ctx, connection) = CreateDbContext();
        await using var _ = connection;
        var (domain, _) = AddAllowedDomain(ctx, "example.com");

        var counter = 0L;
        var (_, mockMux) = CreateRedisMocks(() => ++counter);

        var quota = new Quota
        {
            Id = 1,
            DomainId = domain.Id,
            Domain = domain,
            Type = QuotaType.ExpiringPeriodic,
            MaxRequests = 100,
            PeriodSeconds = 3600,
            ExpiresAt = DateTime.UtcNow.AddSeconds(1),
            RequestCount = 0,
            LastResetAt = DateTime.UtcNow
        };

        ctx.Quotas.Add(quota);
        await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);

        var service = CreateService(mockMux.Object, ctx);

        var before = await service.CheckAndIncrementAsync("example.com", ToMeta(quota));
        Assert.Equal(QuotaCheckResult.Allowed, before);

        await Task.Delay(TimeSpan.FromSeconds(1.1), TestContext.Current.CancellationToken);

        var after = await service.CheckAndIncrementAsync("example.com", ToMeta(quota));
        Assert.Equal(QuotaCheckResult.Exceeded, after);

        var updatedDomain = await ctx.Domains.AsNoTracking()
            .FirstAsync(d => d.Id == domain.Id, TestContext.Current.CancellationToken);
        Assert.Equal(2, updatedDomain.DomainStatusTypeId);
    }
}
