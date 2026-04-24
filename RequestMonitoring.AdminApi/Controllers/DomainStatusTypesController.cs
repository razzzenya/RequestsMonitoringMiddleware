using Mapster;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RequestMonitoring.AdminApi.DTO;
using RequestMonitoring.Library.Context;

namespace RequestMonitoring.AdminApi.Controllers;

/// <summary>
/// Справочник типов статусов доменов
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class DomainStatusTypesController(DomainListsContext context, ILogger<DomainStatusTypesController> logger) : ControllerBase
{
    /// <summary>
    /// Получить список всех типов статусов доменов
    /// </summary>
    [HttpGet]
    [ProducesResponseType<IEnumerable<DomainStatusTypeDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<DomainStatusTypeDto>>> GetAllAsync()
    {
        try
        {
            var statusTypes = await context.DomainStatusTypes.ToListAsync();
            return Ok(statusTypes.Adapt<IEnumerable<DomainStatusTypeDto>>());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting domain status types");
            return StatusCode(500, new { message = "Error retrieving domain status types" });
        }
    }
}
