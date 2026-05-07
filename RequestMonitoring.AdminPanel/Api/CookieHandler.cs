using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Http;

namespace RequestMonitoring.AdminPanel.Api;

public class CookieHandler(CookieAuthStateProvider authStateProvider) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        request.SetBrowserRequestCredentials(BrowserRequestCredentials.Include);

        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized
            || response.StatusCode == System.Net.HttpStatusCode.Forbidden)
        {
            authStateProvider.NotifyLogout();
        }

        return response;
    }
}
