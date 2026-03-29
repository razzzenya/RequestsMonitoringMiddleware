using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RequestMonitoring.AdminApi.DTO;
using RequestMonitoring.Library.Context;
using RequestMonitoring.Library.Enitites;
using RequestMonitoring.Library.Middleware.Services.DomainCache;

namespace RequestMonitoring.AdminApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DomainsController(DomainListsContext context, IDomainCacheService cacheService, ILogger<DomainsController> logger) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Domain>>> GetAllAsync()
    {
        try
        {
            var domains = await context.Domains
                .Include(d => d.DomainStatusType)
                .ToListAsync();

            return Ok(domains);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting all domains");
            return StatusCode(500, new { message = "Error retrieving domains" });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Domain>> GetByIdAsync(int id)
    {
        try
        {
            var domain = await context.Domains
                .Include(d => d.DomainStatusType)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (domain == null)
            {
                return NotFound(new { message = $"Domain with ID {id} not found" });
            }

            return Ok(domain);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting domain by ID {DomainId}", id);
            return StatusCode(500, new { message = "Error retrieving domain" });
        }
    }

    [HttpPost]
    public async Task<ActionResult<Domain>> CreateAsync([FromBody] CreateDomainDTO dto)
    {
        try
        {
            var statusExists = await context.DomainStatusTypes
                .AnyAsync(s => s.Id == dto.DomainStatusTypeId);

            if (!statusExists)
            {
                return BadRequest(new { message = "Invalid domain status type ID" });
            }

            var hostExists = await context.Domains
                .AnyAsync(d => d.Host == dto.Host);

            if (hostExists)
            {
                return Conflict(new { message = "Domain with this host already exists" });
            }

            var statusType = await context.DomainStatusTypes.FindAsync(dto.DomainStatusTypeId)
                ?? throw new InvalidOperationException("Status type not found");

            var domain = new Domain
            {
                Id = 0,
                Host = dto.Host,
                DomainStatusTypeId = dto.DomainStatusTypeId,
                DomainStatusType = statusType
            };

            context.Domains.Add(domain);
            await context.SaveChangesAsync();

            await cacheService.InvalidateDomainAsync(domain.Host);

            return Ok(domain);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating domain");
            return StatusCode(500, new { message = "Error creating domain" });
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<Domain>> UpdateAsync(int id, [FromBody] UpdateDomainDTO dto)
    {
        try
        {
            var domain = await context.Domains.FindAsync(id);
            if (domain == null)
            {
                return NotFound(new { message = $"Domain with ID {id} not found" });
            }

            var statusExists = await context.DomainStatusTypes
                .AnyAsync(s => s.Id == dto.DomainStatusTypeId);

            if (!statusExists)
            {
                return BadRequest(new { message = "Invalid domain status type ID" });
            }

            var hostExists = await context.Domains
                .AnyAsync(d => d.Host == dto.Host && d.Id != id);

            if (hostExists)
            {
                return Conflict(new { message = "Domain with this host already exists" });
            }

            var oldHost = domain.Host;

            domain.Host = dto.Host;
            domain.DomainStatusTypeId = dto.DomainStatusTypeId;
            domain.DomainStatusType = await context.DomainStatusTypes.FindAsync(dto.DomainStatusTypeId)
                ?? throw new InvalidOperationException("Status type not found");

            await context.SaveChangesAsync();

            if (oldHost != dto.Host)
            {
                await cacheService.InvalidateDomainAsync(oldHost);
            }
            await cacheService.InvalidateDomainAsync(domain.Host);

            return Ok(domain);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating domain {DomainId}", id);
            return StatusCode(500, new { message = "Error updating domain" });
        }
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteAsync(int id)
    {
        try
        {
            var domain = await context.Domains.FindAsync(id);
            if (domain == null)
            {
                return NotFound(new { message = $"Domain with ID {id} not found" });
            }

            var hostToInvalidate = domain.Host;

            context.Domains.Remove(domain);
            await context.SaveChangesAsync();

            await cacheService.InvalidateDomainAsync(hostToInvalidate);

            return NoContent();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting domain {DomainId}", id);
            return StatusCode(500, new { message = "Error deleting domain" });
        }
    }

    [HttpPost("cache/invalidate")]
    public async Task<ActionResult> InvalidateAllCacheAsync()
    {
        try
        {
            await cacheService.InvalidateAllDomainsAsync();
            return Ok(new { message = "Cache invalidated successfully" });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error invalidating cache");
            return StatusCode(500, new { message = "Error invalidating cache" });
        }
    }
}