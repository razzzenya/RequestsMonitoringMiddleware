using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Testcontainers.Redis;

namespace RequestMonitoring.Benchmarks;

/// <summary>
/// Сравнение реализаций кеша для операции Get/Set строки (имитирует DomainCheckService).
/// Контейнеры Redis, Valkey, Garnet поднимаются автоматически через Testcontainers.
/// </summary>
[SimpleJob(RuntimeMoniker.Net10_0)]
[MemoryDiagnoser]
[Orderer(BenchmarkDotNet.Order.SummaryOrderPolicy.FastestToSlowest)]
public class CacheBenchmark
{
    private const string Key = "Domain_benchmark.example.com";
    private const string Value = "{\"Id\":1,\"Name\":\"Allowed\"}";
    private static readonly DistributedCacheEntryOptions CacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
    };

    private IMemoryCache _memoryCache = null!;
    private IDistributedCache _redisCache = null!;
    private IDistributedCache _valkeyCache = null!;
    private IDistributedCache _garnetCache = null!;
    private HybridCache _hybridCache = null!;
    private ServiceProvider _serviceProvider = null!;

    private RedisContainer _redisContainer = null!;
    private RedisContainer _valkeyContainer = null!;
    private IContainer _garnetContainer = null!;
    private const int GarnetPort = 6379;

    [GlobalSetup]
    public async Task Setup()
    {
        _redisContainer = new RedisBuilder("redis:latest").Build();
        _valkeyContainer = new RedisBuilder("valkey/valkey:latest").Build();
        _garnetContainer = new ContainerBuilder("ghcr.io/microsoft/garnet:latest")
            .WithPortBinding(GarnetPort, true)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilMessageIsLogged("Ready to accept connections"))
            .Build();

        await Task.WhenAll(
            _redisContainer.StartAsync(),
            _valkeyContainer.StartAsync(),
            _garnetContainer.StartAsync()
        );

        _memoryCache = new MemoryCache(new MemoryCacheOptions());
        _redisCache = BuildRedisCache(_redisContainer.GetConnectionString());
        _valkeyCache = BuildRedisCache(_valkeyContainer.GetConnectionString());
        var garnetHost = _garnetContainer.Hostname;
        var garnetPort = _garnetContainer.GetMappedPublicPort(GarnetPort);
        _garnetCache = BuildRedisCache($"{garnetHost}:{garnetPort}");

        var services = new ServiceCollection();
        services.AddHybridCache();
        services.AddStackExchangeRedisCache(o => o.Configuration = _redisContainer.GetConnectionString());
        _serviceProvider = services.BuildServiceProvider();
        _hybridCache = _serviceProvider.GetRequiredService<HybridCache>();

        _memoryCache.Set(Key, Value, TimeSpan.FromMinutes(10));
        await _redisCache.SetStringAsync(Key, Value, CacheOptions);
        await _valkeyCache.SetStringAsync(Key, Value, CacheOptions);
        await _garnetCache.SetStringAsync(Key, Value, CacheOptions);
    }

    [GlobalCleanup]
    public async Task Cleanup()
    {
        _memoryCache.Dispose();
        await _serviceProvider.DisposeAsync();
        await Task.WhenAll(
            _redisContainer.DisposeAsync().AsTask(),
            _valkeyContainer.DisposeAsync().AsTask(),
            _garnetContainer.DisposeAsync().AsTask()
        );
    }

    // ── GET ──────────────────────────────────────────────────────────────────

    [Benchmark(Description = "IMemoryCache Get")]
    public string? MemoryCache_Get()
    {
        _memoryCache.TryGetValue(Key, out string? value);
        return value;
    }

    [Benchmark(Description = "IDistributedCache Redis Get")]
    public async Task<string?> Redis_Get() => await _redisCache.GetStringAsync(Key);

    [Benchmark(Description = "IDistributedCache Valkey Get")]
    public async Task<string?> Valkey_Get() => await _valkeyCache.GetStringAsync(Key);

    [Benchmark(Description = "IDistributedCache Garnet Get")]
    public async Task<string?> Garnet_Get() => await _garnetCache.GetStringAsync(Key);

    [IterationSetup(Target = nameof(HybridCache_Get))]
    public void HybridCache_Get_Setup()
        => _hybridCache.SetAsync(Key, Value, new HybridCacheEntryOptions
        {
            Expiration = TimeSpan.FromMinutes(10)
        }).AsTask().GetAwaiter().GetResult();

    [Benchmark(Description = "HybridCache Get")]
    public async Task<string> HybridCache_Get()
        => await _hybridCache.GetOrCreateAsync(
            Key,
            static _ => ValueTask.FromException<string>(
                new InvalidOperationException("HybridCache_Get benchmark expects the key to be pre-populated.")));

    // ── SET ──────────────────────────────────────────────────────────────────

    [Benchmark(Description = "IMemoryCache Set")]
    public void MemoryCache_Set()
        => _memoryCache.Set(Key, Value, TimeSpan.FromMinutes(10));

    [Benchmark(Description = "IDistributedCache Redis Set")]
    public async Task Redis_Set() => await _redisCache.SetStringAsync(Key, Value, CacheOptions);

    [Benchmark(Description = "IDistributedCache Valkey Set")]
    public async Task Valkey_Set() => await _valkeyCache.SetStringAsync(Key, Value, CacheOptions);

    [Benchmark(Description = "IDistributedCache Garnet Set")]
    public async Task Garnet_Set() => await _garnetCache.SetStringAsync(Key, Value, CacheOptions);

    [Benchmark(Description = "HybridCache Set")]
    public async Task HybridCache_Set()
        => await _hybridCache.SetAsync(Key, Value, new HybridCacheEntryOptions
        {
            Expiration = TimeSpan.FromMinutes(10)
        });

    private static IDistributedCache BuildRedisCache(string connectionString)
    {
        var options = Options.Create(new RedisCacheOptions { Configuration = connectionString });
        return new RedisCache(options);
    }
}
