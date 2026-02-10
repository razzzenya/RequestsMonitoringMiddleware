using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using RequestMonitoringLibrary.Context;
using RequestMonitoringLibrary.Enitites.Domain;

namespace RequestMonitoringLibrary.Middleware.Services.DomainCheck;

public class DomainCheckService(DomainListsContext dbcontext) : IDomainCheckService
{
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

    //private static Request ConstructRequestFromContext(HttpContext context)
    //{
    //    var userIp = context.Connection.RemoteIpAddress.ToString();
    //    var requestTime = DateTime.UtcNow;
    //    var parameters = HttpUtility.ParseQueryString(context.Request.QueryString.Value) ?? [];

    //    var request = new Request
    //    {
    //        UserIp = userIp,
    //        RequestTime = requestTime,
    //        Parameters = parameters.ToDictionary()
    //    };

    //    return request;
    //}
}
