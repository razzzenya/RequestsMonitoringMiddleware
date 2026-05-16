using NBomber.CSharp;
using NBomber.Http.CSharp;

var baseUrl = args.FirstOrDefault() ?? "http://localhost:5000";

using var httpClient = new HttpClient();

var scenario = Scenario.Create("weather_forecast", async context =>
{
    var request = Http.CreateRequest("GET", $"{baseUrl}/WeatherForecast");
    var response = await Http.Send(httpClient, request);
    return response;
})
.WithWarmUpDuration(TimeSpan.FromSeconds(5))
.WithLoadSimulations(
    Simulation.RampingInject(rate: 100,  interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(30)),
    Simulation.Inject(rate: 100,         interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(30)),
    Simulation.RampingInject(rate: 500,  interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(30)),
    Simulation.Inject(rate: 500,         interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(30)),
    Simulation.RampingInject(rate: 1000, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(30)),
    Simulation.Inject(rate: 1000,        interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(30))
);

NBomberRunner
    .RegisterScenarios(scenario)
    .WithReportFileName("load_test_report")
    .WithReportFolder("reports")
    .Run();
