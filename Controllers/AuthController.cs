using System.Security.Claims;
using EmployeeRegistry.Api.Modules.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EmployeeRegistry.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(AuthService auth) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await auth.LoginAsync(request, cancellationToken);
        return result is null
            ? Unauthorized(new { message = "Invalid login or password" })
            : Ok(result);
    }

    [Authorize]
    [HttpGet("me")]
    public IActionResult Me()
    {
        var login = User.Identity?.Name;
        var role = User.FindFirstValue(ClaimTypes.Role);
        if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(role))
            return Unauthorized(new { message = "Token required" });

        return Ok(new MeResponse(login, role));
    }
}
