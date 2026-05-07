using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RequestMonitoring.AdminApi.DTO;
using System.Security.Claims;

namespace RequestMonitoring.AdminApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(IConfiguration configuration) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
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

    [HttpGet("me")]
    [Authorize]
    public IActionResult Me() => Ok();

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync("Cookies");
        return Ok();
    }

}
