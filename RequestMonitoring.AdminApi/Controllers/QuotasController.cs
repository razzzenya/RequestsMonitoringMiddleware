using Mapster;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RequestMonitoring.AdminApi.DTO;
using RequestMonitoring.Library.Context;
using RequestMonitoring.Library.Enitites;
using RequestMonitoring.Library.Middleware.Services.QuotaCache;

namespace RequestMonitoring.AdminApi.Controllers;

/// <summary>
/// Управление квотами
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class QuotasController(DomainListsContext context, IQuotaCacheService cacheService, ILogger<QuotasController> logger) : ControllerBase
{
    /// <summary>
    /// Получить список всех квот
    /// </summary>
    [HttpGet]
    [ProducesResponseType<IEnumerable<QuotaDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<QuotaDto>>> GetAllAsync()
    {
        try
        {
            var quotas = await context.Quotas.ToListAsync();
            return Ok(quotas.Adapt<IEnumerable<QuotaDto>>());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting all quotas");
            return StatusCode(500, new { message = "Error retrieving quotas" });
        }
    }

    /// <summary>
    /// Получить квоту по идентификатору
    /// </summary>
    /// <param name="id">Идентификатор квоты</param>
    [HttpGet("{id}")]
    [ProducesResponseType<QuotaDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<QuotaDto>> GetByIdAsync(int id)
    {
        try
        {
            var quota = await context.Quotas.FindAsync(id);
            if (quota == null)
                return NotFound(new { message = $"Quota with ID {id} not found" });

            return Ok(quota.Adapt<QuotaDto>());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting quota by ID {QuotaId}", id);
            return StatusCode(500, new { message = "Error retrieving quota" });
        }
    }

    /// <summary>
    /// Получить квоту по идентификатору домена
    /// </summary>
    /// <param name="domainId">Идентификатор домена</param>
    [HttpGet("by-domain/{domainId}")]
    [ProducesResponseType<QuotaDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<QuotaDto>> GetByDomainIdAsync(int domainId)
    {
        try
        {
            var quota = await context.Quotas.FirstOrDefaultAsync(q => q.DomainId == domainId);
            if (quota == null)
                return NotFound(new { message = $"Quota for domain ID {domainId} not found" });

            return Ok(quota.Adapt<QuotaDto>());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting quota for domain ID {DomainId}", domainId);
            return StatusCode(500, new { message = "Error retrieving quota" });
        }
    }

    /// <summary>
    /// Создать квоту для домена
    /// </summary>
    /// <param name="dto">Данные квоты</param>
    [HttpPost]
    [ProducesResponseType<QuotaDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<QuotaDto>> CreateAsync([FromBody] CreateUpdateQuotaDto dto)
    {
        try
        {
            var domainExists = await context.Domains.AnyAsync(d => d.Id == dto.DomainId);
            if (!domainExists)
                return BadRequest(new { message = "Domain not found" });

            var quotaExists = await context.Quotas.AnyAsync(q => q.DomainId == dto.DomainId);
            if (quotaExists)
                return Conflict(new { message = "Quota for this domain already exists" });

            var domain = await context.Domains.FindAsync(dto.DomainId);

            var quota = new Quota
            {
                Id = 0,
                DomainId = dto.DomainId,
                Domain = domain!,
                Type = dto.Type,
                MaxRequests = dto.MaxRequests,
                PeriodSeconds = dto.PeriodSeconds,
                ExpiresAt = dto.ExpiresAt,
                RequestCount = 0,
                LastResetAt = null
            };

            context.Quotas.Add(quota);
            await context.SaveChangesAsync();

            await cacheService.InvalidateQuotaAsync(quota.DomainId);

            return Ok(quota.Adapt<QuotaDto>());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating quota");
            return StatusCode(500, new { message = "Error creating quota" });
        }
    }

    /// <summary>
    /// Обновить квоту
    /// </summary>
    /// <param name="id">Идентификатор квоты</param>
    /// <param name="dto">Новые данные квоты</param>
    [HttpPut("{id}")]
    [ProducesResponseType<QuotaDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<QuotaDto>> UpdateAsync(int id, [FromBody] CreateUpdateQuotaDto dto)
    {
        try
        {
            var quota = await context.Quotas.FindAsync(id);
            if (quota == null)
                return NotFound(new { message = $"Quota with ID {id} not found" });

            var domainExists = await context.Domains.AnyAsync(d => d.Id == dto.DomainId);
            if (!domainExists)
                return BadRequest(new { message = "Domain not found" });

            if (quota.DomainId != dto.DomainId)
            {
                var quotaExists = await context.Quotas.AnyAsync(q => q.DomainId == dto.DomainId);
                if (quotaExists)
                    return Conflict(new { message = "Quota for this domain already exists" });
            }

            var oldDomainId = quota.DomainId;

            quota.DomainId = dto.DomainId;
            quota.Type = dto.Type;
            quota.MaxRequests = dto.MaxRequests;
            quota.PeriodSeconds = dto.PeriodSeconds;
            quota.ExpiresAt = dto.ExpiresAt;

            await context.SaveChangesAsync();

            if (oldDomainId != dto.DomainId)
                await cacheService.InvalidateQuotaAsync(oldDomainId);
            await cacheService.InvalidateQuotaAsync(quota.DomainId);

            return Ok(quota.Adapt<QuotaDto>());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating quota {QuotaId}", id);
            return StatusCode(500, new { message = "Error updating quota" });
        }
    }

    /// <summary>
    /// Удалить квоту
    /// </summary>
    /// <param name="id">Идентификатор квоты</param>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> DeleteAsync(int id)
    {
        try
        {
            var quota = await context.Quotas.FindAsync(id);
            if (quota == null)
                return NotFound(new { message = $"Quota with ID {id} not found" });

            var domainId = quota.DomainId;

            context.Quotas.Remove(quota);
            await context.SaveChangesAsync();

            await cacheService.InvalidateQuotaAsync(domainId);

            return NoContent();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting quota {QuotaId}", id);
            return StatusCode(500, new { message = "Error deleting quota" });
        }
    }

    /// <summary>
    /// Сбросить счётчик запросов квоты
    /// </summary>
    /// <param name="id">Идентификатор квоты</param>
    [HttpPost("{id}/reset-counter")]
    [ProducesResponseType<QuotaDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<QuotaDto>> ResetCounterAsync(int id)
    {
        try
        {
            var quota = await context.Quotas.FindAsync(id);
            if (quota == null)
                return NotFound(new { message = $"Quota with ID {id} not found" });

            quota.RequestCount = 0;
            quota.LastResetAt = DateTime.UtcNow;

            await context.SaveChangesAsync();

            await cacheService.InvalidateQuotaAsync(quota.DomainId);

            return Ok(quota.Adapt<QuotaDto>());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error resetting counter for quota {QuotaId}", id);
            return StatusCode(500, new { message = "Error resetting quota counter" });
        }
    }
}
