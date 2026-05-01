using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using RequestMonitoring.AdminPanel;
using RequestMonitoring.AdminPanel.Api;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped<CookieHandler>();

builder.Services.AddHttpClient("AdminApi")
    .AddHttpMessageHandler<CookieHandler>();

builder.Services.AddScoped<IRequestMonitoringAdminPanelApiWrapper>(sp =>
{
    var factory = sp.GetRequiredService<IHttpClientFactory>();
    var httpClient = factory.CreateClient("AdminApi");
    var configuration = sp.GetRequiredService<IConfiguration>();
    return new RequestMonitoringAdminPanelApiWrapper(configuration, httpClient);
});

builder.Services.AddMudServices();

builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<CookieAuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp =>
    sp.GetRequiredService<CookieAuthStateProvider>());

await builder.Build().RunAsync();
