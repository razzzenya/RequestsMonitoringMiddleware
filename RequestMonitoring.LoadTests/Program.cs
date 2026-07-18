using NBomber.Contracts;
using NBomber.CSharp;
using NBomber.Http.CSharp;

const int WarmupSeconds = 10;
const int DurationSeconds = 60;

var baseUrl = args.Length > 0 ? args[0] : "http://localhost:5000";

using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };

ScenarioProps BuildScenario(string name, string[] domains, string[] expectedStatusCodes, int rps) =>
    Scenario.Create(name, async context =>
    {
        var domain = domains[context.InvocationNumber % domains.Length];
        var request = Http.CreateRequest("GET", $"{baseUrl}/WeatherForecast")
            .WithHeader("X-Test-Host", domain);
        var response = await Http.Send(httpClient, request);

        return Array.IndexOf(expectedStatusCodes, response.StatusCode) >= 0
            ? Response.Ok(statusCode: response.StatusCode, sizeBytes: response.SizeBytes)
            : Response.Fail(statusCode: response.StatusCode, message: $"Unexpected status {response.StatusCode}");
    })
    .WithWarmUpDuration(TimeSpan.FromSeconds(WarmupSeconds))
    .WithLoadSimulations(
        Simulation.Inject(rate: rps, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(DurationSeconds))
    );

foreach (var rps in new[] { 100, 200, 300, 500 })
{
    Console.WriteLine($"=== Starting load test at {rps} RPS ===");

    var allowedScenario = BuildScenario($"allowed_{rps}rps", ["allowed.com", "lol.com"], ["OK", "PaymentRequired", "TooManyRequests", "Forbidden"], rps);
    var greylistedScenario = BuildScenario($"greylisted_{rps}rps", ["blacklisted.com", "greylisted.com"], ["PaymentRequired"], rps);
    var unknownScenario = BuildScenario($"unknown_{rps}rps", ["unknown.com", "notexist.com"], ["Forbidden"], rps);

    NBomberRunner
        .RegisterScenarios(allowedScenario, greylistedScenario, unknownScenario)
        .WithReportFileName($"load_test_report_{rps}rps")
        .WithReportFolder("reports")
        .Run();

    Console.WriteLine($"=== Finished load test at {rps} RPS ===");
    Console.WriteLine();
}
