using NBomber.Contracts;
using NBomber.CSharp;
using NBomber.Http.CSharp;

const int WarmupSeconds = 5;
const int StepSeconds = 15;
const int MaxRps = 15;

var baseUrl = args.FirstOrDefault() ?? "http://localhost:5000";
using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };

ScenarioProps BuildScenario(string name, string[] domains, string[] expectedStatusCodes) =>
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
        Simulation.RampingInject(rate: 5, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(StepSeconds)),
        Simulation.Inject(rate: 5, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(StepSeconds)),
        Simulation.RampingInject(rate: 10, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(StepSeconds)),
        Simulation.Inject(rate: 10, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(StepSeconds)),
        Simulation.RampingInject(rate: MaxRps, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(StepSeconds)),
        Simulation.Inject(rate: MaxRps, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(StepSeconds))
    );

var allowedScenario = BuildScenario("allowed_domains", ["allowed.com", "lol.com"], ["OK", "PaymentRequired", "TooManyRequests"]);
var greylistedScenario = BuildScenario("greylisted_domains", ["blacklisted.com", "greylisted.com"], ["PaymentRequired"]);
var unknownScenario = BuildScenario("unknown_domains", ["unknown.com", "notexist.com"], ["Unauthorized"]);

NBomberRunner
    .RegisterScenarios(allowedScenario, greylistedScenario, unknownScenario)
    .WithReportFileName("load_test_report")
    .WithReportFolder("reports")
    .Run();
