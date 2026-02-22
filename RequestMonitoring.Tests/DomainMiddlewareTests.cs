using Microsoft.Extensions.Logging;

namespace RequestMonitoring.Tests;

/// <summary>
/// Тесты для проверки работы Domain Monitoring Middleware
/// </summary>
public class DomainMiddlewareTests
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(60);

    [Fact]
    public async Task MultipleDomains_ProcessedConcurrently()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var appHost = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.RequestMonitoring_AppHost>(cancellationToken);

        await using var app = await appHost.BuildAsync(cancellationToken).WaitAsync(DefaultTimeout, cancellationToken);
        await app.StartAsync(cancellationToken).WaitAsync(DefaultTimeout, cancellationToken);

        using var httpClient = app.CreateHttpClient("api");
        await app.ResourceNotifications
            .WaitForResourceHealthyAsync("api", cancellationToken)
            .WaitAsync(DefaultTimeout, cancellationToken);

        var domains = new[]
        {
            ("allowed.com", HttpStatusCode.OK),
            ("greylisted.com", HttpStatusCode.PaymentRequired),
            ("unknown.example.com", HttpStatusCode.Unauthorized),
        };

        var tasks = domains.Select(async domainInfo =>
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "/weatherforecast");
            request.Headers.Add("X-Test-Host", domainInfo.Item1);
            var response = await httpClient.SendAsync(request, cancellationToken);
            return (domain: domainInfo.Item1, expected: domainInfo.Item2, actual: response.StatusCode);
        }).ToArray();

        var results = await Task.WhenAll(tasks);

        foreach (var result in results)
        {
            Assert.Equal(result.expected, result.actual);
        }
    }
}
