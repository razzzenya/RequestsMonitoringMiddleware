using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace RequestMonitoring.AdminPanel.Api;

public class CookieAuthStateProvider(IRequestMonitoringAdminPanelApiWrapper api) : AuthenticationStateProvider
{
    private bool _isAuthenticated;

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        if (_isAuthenticated)
            return AuthenticatedState();

        try
        {
            await api.GetDomainList();
            _isAuthenticated = true;
            return AuthenticatedState();
        }
        catch
        {
            return AnonymousState();
        }
    }

    public void NotifyLogin()
    {
        _isAuthenticated = true;
        NotifyAuthenticationStateChanged(Task.FromResult(AuthenticatedState()));
    }

    public void NotifyLogout()
    {
        _isAuthenticated = false;
        NotifyAuthenticationStateChanged(Task.FromResult(AnonymousState()));
    }

    private static AuthenticationState AuthenticatedState()
    {
        var identity = new ClaimsIdentity([new Claim(ClaimTypes.Name, "admin")], "Cookies");
        return new AuthenticationState(new ClaimsPrincipal(identity));
    }

    private static AuthenticationState AnonymousState() =>
        new(new ClaimsPrincipal(new ClaimsIdentity()));
}
