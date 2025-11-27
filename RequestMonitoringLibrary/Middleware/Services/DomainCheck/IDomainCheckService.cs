using Microsoft.AspNetCore.Http;
using RequestMonitoringLibrary.Enitites.Domain;

namespace RequestMonitoringLibrary.Middleware.Services.DomainCheck;

public interface IDomainCheckService
{
    Task<DomainStatusType> IsDomainAllowedAsync(HttpContext context);
}
