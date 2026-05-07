using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RequestMonitoring.AdminApi.DTO;
using RequestMonitoring.Library.Context;
using RequestMonitoring.Library.Enitites;
using RequestMonitoring.Library.Middleware.Services.DomainCache;

namespace RequestMonitoring.AdminApi.Controllers;

/// <summary>
/// Управление доменами
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DomainsController(DomainListsContext context, IDomainCacheService cacheService, ILogger<DomainsController> logger) : ControllerBase
{
    /// <summary>
    /// Получить список всех доменов
    /// </summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<DomainDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IReadOnlyList<DomainDto>>> GetAllAsync()
    {
        try
        {
            var domains = await context.Domains
                .Include(d => d.DomainStatusType)
                .ToListAsync();

            return Ok(domains.Adapt<IReadOnlyList<DomainDto>>());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting all domains");
            return StatusCode(500, new { message = "Error retrieving domains" });
        }
    }

    /// <summary>
    /// Получить страницу доменов с фильтрацией
    /// </summary>
    /// <param name="page">Номер страницы (начиная с 1)</param>
    /// <param name="pageSize">Размер страницы</param>
    /// <param name="search">Фильтр по хосту</param>
    [HttpGet("paged")]
    [EndpointName("GetDomainsPaged")]
    [ProducesResponseType<PagedResult<DomainDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PagedResult<DomainDto>>> GetPagedAsync(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null)
    {
        if (page < 1 || pageSize < 1 || pageSize > 100)
            return BadRequest(new { message = "page >= 1, pageSize от 1 до 100" });

        try
        {
            var query = context.Domains
                .Include(d => d.DomainStatusType)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(d => d.Host.Contains(search));

            var totalCount = await query.CountAsync();

            var domains = await query
                .OrderBy(d => d.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Ok(new PagedResult<DomainDto>(domains.Adapt<IReadOnlyList<DomainDto>>(), totalCount, page, pageSize));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting paged domains");
            return StatusCode(500, new { message = "Error retrieving domains" });
        }
    }

    /// <summary>
    /// Получить домен по идентификатору
    /// </summary>
    /// <param name="id">Идентификатор домена</param>
    [HttpGet("{id}")]
    [ProducesResponseType<DomainDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<DomainDto>> GetByIdAsync(int id)
    {
        try
        {
            var domain = await context.Domains
                .Include(d => d.DomainStatusType)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (domain == null)
                return NotFound(new { message = $"Domain with ID {id} not found" });

            return Ok(domain.Adapt<DomainDto>());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting domain by ID {DomainId}", id);
            return StatusCode(500, new { message = "Error retrieving domain" });
        }
    }

    /// <summary>
    /// Создать новый домен
    /// </summary>
    /// <param name="dto">Данные нового домена</param>
    [HttpPost]
    [ProducesResponseType<DomainDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<DomainDto>> CreateAsync([FromBody] DomainCreateUpdateDto dto)
    {
        try
        {
            var statusType = await context.DomainStatusTypes.FindAsync(dto.DomainStatusTypeId);
            if (statusType == null)
                return BadRequest(new { message = "Invalid domain status type ID" });

            var hostExists = await context.Domains.AnyAsync(d => d.Host == dto.Host);
            if (hostExists)
                return Conflict(new { message = "Domain with this host already exists" });

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

            return Ok(domain.Adapt<DomainDto>());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating domain");
            return StatusCode(500, new { message = "Error creating domain" });
        }
    }

    /// <summary>
    /// Обновить домен
    /// </summary>
    /// <param name="id">Идентификатор домена</param>
    /// <param name="dto">Новые данные домена</param>
    [HttpPut("{id}")]
    [ProducesResponseType<DomainDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<DomainDto>> UpdateAsync(int id, [FromBody] DomainCreateUpdateDto dto)
    {
        try
        {
            var domain = await context.Domains.FindAsync(id);
            if (domain == null)
                return NotFound(new { message = $"Domain with ID {id} not found" });

            var statusType = await context.DomainStatusTypes.FindAsync(dto.DomainStatusTypeId);
            if (statusType == null)
                return BadRequest(new { message = "Invalid domain status type ID" });

            var hostExists = await context.Domains.AnyAsync(d => d.Host == dto.Host && d.Id != id);
            if (hostExists)
                return Conflict(new { message = "Domain with this host already exists" });

            var oldHost = domain.Host;

            domain.Host = dto.Host;
            domain.DomainStatusTypeId = dto.DomainStatusTypeId;
            domain.DomainStatusType = statusType;

            await context.SaveChangesAsync();

            if (oldHost != dto.Host)
                await cacheService.InvalidateDomainAsync(oldHost);
            await cacheService.InvalidateDomainAsync(domain.Host);

            return Ok(domain.Adapt<DomainDto>());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating domain {DomainId}", id);
            return StatusCode(500, new { message = "Error updating domain" });
        }
    }

    /// <summary>
    /// Удалить домен
    /// </summary>
    /// <param name="id">Идентификатор домена</param>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> DeleteAsync(int id)
    {
        try
        {
            var domain = await context.Domains.FindAsync(id);
            if (domain == null)
                return NotFound(new { message = $"Domain with ID {id} not found" });

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

    /// <summary>
    /// Сбросить кэш всех доменов
    /// </summary>
    [HttpPost("cache/invalidate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
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
