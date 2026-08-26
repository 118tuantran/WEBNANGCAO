using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WarehouseManagement.Data;

namespace WarehouseManagement.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(WarehouseDbContext db) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var user = await db.Users.SingleOrDefaultAsync(x => x.Username == request.Username && x.IsActive);
        if (user is null || user.PasswordHash != WarehouseDbContext.Hash(request.Password)) return Unauthorized(new { message = "Sai tài khoản hoặc mật khẩu." });
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()), new Claim(ClaimTypes.Name, user.Username), new Claim(ClaimTypes.Role, user.Role.ToString()) };
        await HttpContext.SignInAsync("WarehouseCookie", new ClaimsPrincipal(new ClaimsIdentity(claims, "WarehouseCookie")));
        return Ok(new { user.Username, role = user.Role.ToString() });
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout() { await HttpContext.SignOutAsync("WarehouseCookie"); return NoContent(); }
}

public sealed record LoginRequest(string Username, string Password);
