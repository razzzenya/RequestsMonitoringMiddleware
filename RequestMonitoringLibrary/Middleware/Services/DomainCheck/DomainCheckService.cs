using Microsoft.AspNetCore.Http;
using RequestMonitoringLibrary.Enitites.Domain;
using RequestMonitoringLibrary.Middleware.Services.FileReader;

namespace RequestMonitoringLibrary.Middleware.Services.DomainCheck;

public class DomainCheckService(IFileReaderService fileReaderService) : IDomainCheckService
{
    private readonly string pathToWhitelistFile = "Lists/whitelist.json"; // временно
    private readonly string pathToGreylistFile = "Lists/greylist.json";
    public async Task<DomainStatusType> IsDomainAllowedAsync(HttpContext context)
    {
        var domain = ExtractDomainFromContext(context);
        var allowedDomains = await fileReaderService.ReadFileLinesAsync(pathToWhitelistFile);
        var greylistedDomains = await fileReaderService.ReadFileLinesAsync(pathToGreylistFile);
        if (!allowedDomains.Contains(domain))
        {
            return DomainStatusType.Forbidden;
        }
        if (greylistedDomains.Contains(domain))
        {
            return DomainStatusType.Greylisted;
        }
        return DomainStatusType.Allowed;
    }

    private static string ExtractDomainFromContext(HttpContext context)
    {
        var host = context.Request.Host.Host;
        return host;
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
