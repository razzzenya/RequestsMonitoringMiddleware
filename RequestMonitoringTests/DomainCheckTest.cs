using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RequestMonitoringLibrary.Context;
using RequestMonitoringLibrary.Enitites.Domain;
using System.Net;

namespace RequestMonitoringTests;

public class DomainCheckTest : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public DomainCheckTest(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<DomainListsContext>));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                services.AddDbContext<DomainListsContext>(options =>
                {
                    options.UseInMemoryDatabase("TestDb");
                });

                var sp = services.BuildServiceProvider();

                using (var scope = sp.CreateScope())
                {
                    var scopedServices = scope.ServiceProvider;
                    var db = scopedServices.GetRequiredService<DomainListsContext>();
                    db.Database.EnsureCreated();
                    SeedTestData(db);
                }
            });
        });
    }

    private static void SeedTestData(DomainListsContext context)
    {
        context.Domains.RemoveRange(context.Domains);
        context.SaveChanges();

        var allowedStatus = context.DomainStatusTypes.First(s => s.Id == 1);

        context.Domains.Add(new Domain
        {
            Id = 1,
            Host = "allowed.com",
            DomainStatusTypeId = 1,
            DomainStatusType = allowedStatus
        });

        context.SaveChanges();
    }

    [Fact]
    public async Task AllowedDomainTest()
    {
        var client = _factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "/weatherforecast");
        request.Headers.Host = "allowed.com";

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
