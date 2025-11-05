using GasForecast.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GasForecast.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AccountController : ControllerBase
{
    private readonly IAuthService _svc;

    // Добавляем зависимость через конструктор
    public AccountController(IAuthService svc)
    {
        _svc = svc;
    }

    [HttpPost("login")]
    [AllowAnonymous] // Явно разрешаем доступ без аутентификации
    public async Task<IActionResult> GetToken([FromBody] LoginData ld, CancellationToken ct)
    {
        try
        {
            var token = await _svc.GetTokenAsync(ld, ct);
            if (token is null)
                return Unauthorized(new { message = "Неправильный логин или пароль" });

            return Ok(token);
        }
        catch (Exception ex)
        {
            return Problem(
                title: "Внутренняя ошибка сервера",
                detail: ex.Message,
                statusCode: 500);
        }
    }

    [HttpGet("validate")]
    [Authorize]
    public IActionResult ValidateToken()
    {
        var userName = User.Identity?.Name;
        var role = User.FindFirst(ClaimTypes.Role)?.Value;

        return Ok(new
        {
            message = "Токен валиден",
            username = userName,
            role = role
        });
    }
}