using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RequestMonitoring.AdminApi.DTO;
using System.Security.Claims;

namespace RequestMonitoring.AdminApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public class AuthController(IConfiguration configuration) : ControllerBase
{
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var login = configuration["AdminApi:Login"];
        var password = configuration["AdminApi:Password"];

        if (dto.Login != login || dto.Password != password)
            return Unauthorized();

        var claims = new List<Claim> { new(ClaimTypes.Name, dto.Login) };
        var identity = new ClaimsIdentity(claims, "Cookies");
        await HttpContext.SignInAsync("Cookies", new ClaimsPrincipal(identity),
            new AuthenticationProperties { ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8) });

        return Ok();
    }

}
