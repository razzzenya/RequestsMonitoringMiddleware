using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using RequestMonitoring.Library.Context;
using RequestMonitoring.Library.Enitites.Domain;

namespace RequestMonitoring.Library.Middleware.Services.DomainCheck;

/// <summary>
/// Сервис проверки статуса домена
/// </summary>
public class DomainCheckService(DomainListsContext dbcontext) : IDomainCheckService
{
    /// <summary>
    /// Проверяет статус домена из контекста запроса
    /// </summary>
    public async Task<DomainStatusType> IsDomainAllowedAsync(HttpContext context)
    {
        var domain = context.Request.Host.Host; 
        var allowedDomains = await dbcontext.Domains.Where(s => s.DomainStatusTypeId == 1).Select(h => h.Host).ToListAsync();
        var greylistedDomains = await dbcontext.Domains.Where(s => s.DomainStatusTypeId == 2).Select(h => h.Host).ToListAsync();
        if (greylistedDomains.Contains(domain))
        {
            return await dbcontext.DomainStatusTypes.FirstAsync(s => s.Id == 2);
        }
        if (allowedDomains.Contains(domain))
        {
            return await dbcontext.DomainStatusTypes.FirstAsync(s => s.Id == 1);
        }
        return await dbcontext.DomainStatusTypes.FirstAsync(s => s.Id == 3);
    }
}
