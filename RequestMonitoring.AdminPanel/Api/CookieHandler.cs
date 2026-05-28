using Microsoft.AspNetCore.Components.WebAssembly.Http;

namespace RequestMonitoring.AdminPanel.Api;

public class CookieHandler(IServiceProvider serviceProvider) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        request.SetBrowserRequestCredentials(BrowserRequestCredentials.Include);

        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized
            || response.StatusCode == System.Net.HttpStatusCode.Forbidden)
        {
            var authStateProvider = serviceProvider.GetRequiredService<CookieAuthStateProvider>();
            authStateProvider.NotifyLogout();
        }

        return response;
    }
}
