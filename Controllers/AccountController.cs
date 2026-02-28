using Azure.Core;
using GasForecast.Data;
using GasForecast.Models;
using GasForecast.Models.DTO;
using GasForecast.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GasForecast.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AccountController : ControllerBase
{
    private readonly IAccountService _svc;
    private readonly ApplicationDbContext _context;

    // Добавляем зависимость через конструктор
    public AccountController(IAccountService svc, ApplicationDbContext context)
    {
        _svc = svc;
        _context = context;
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

            var user = _context.Users.FirstOrDefault(u => u.Username == ld.Username);
            user.LastSessionTime = DateTime.Now;

            _context.Users.Update(user);
            _context.SaveChanges();
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

    [HttpPost("register")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Register([FromBody] RegisterData rd, CancellationToken ct)
    {
        try
        {
            // Валидация входных данных
            if (string.IsNullOrWhiteSpace(rd.Username) || rd.Username.Length < 3)
                return BadRequest(new { message = "Имя пользователя должно содержать минимум 3 символа" });

            if (string.IsNullOrWhiteSpace(rd.Password) || rd.Password.Length < 6)
                return BadRequest(new { message = "Пароль должен содержать минимум 6 символов" });

            // Проверяем, существует ли пользователь
            var userExists = await _context.Users
                .AnyAsync(u => u.Username == rd.Username, ct);

            if (userExists)
                return Conflict(new { message = "Пользователь с таким именем уже существует" });

            var result = await _svc.RegisterUserAsync(rd, ct);

            if (result.success == true)
            {
                return Ok(new
                {
                    message = "Регистрация успешна",
                    username = rd.Username,
                    role = rd.Role
                });
            }

            else
            {
                return StatusCode(500, $"Ошибка: {result.ExMessage}");
            }
        }

        catch (Exception ex)
        {
            return Problem(
                title: "Внутренняя ошибка сервера",
                detail: ex.Message,
                statusCode: 500);
        }
    }

    [HttpGet("getLastSessionTime")]
    [Authorize(Roles = "admin")]
    public IActionResult GetLastSessionTime(string userName)
    {
        try
        {
            // 1. Находим пользователя по имени
            var user = _context.Users
                .FirstOrDefault(u => u.Username == userName);

            // 2. Если пользователь не найден
            if (user == null)
            {
                return NotFound($"Пользователь с именем '{userName}' не найден");
            }

            // 3. Если время входа null (никогда не заходил)
            if (user.LastSessionTime == null)
            {
                return Ok(new
                {
                    lastLogin = (string?)null,
                });
            }

            // 4. Возвращаем время последнего входа
            return Ok(new
            {
                formattedTime = user.LastSessionTime.Value.ToString("dd.MM.yyyy HH:mm:ss"),
            });
        }
        catch (Exception ex)
        {
            // 5. Обработка ошибок
            return StatusCode(500, $"Ошибка сервера: {ex.Message}");
        }
    }

    [HttpGet("getUsers")]
    [Authorize(Roles = "admin")]
    public IActionResult GetUsers()
    {
        try
        {
            var users = _context.Users
                .Select(u => new
                {
                    id = u.Id,
                    UserName = u.Username,
                    Role = u.Role,
                    lastSessionTime = u.LastSessionTime.HasValue
                        ? u.LastSessionTime.Value.ToString("dd.MM.yyyy HH:mm")
                        : "Никогда"
                })
                .ToList();

            return Ok(users);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Ошибка сервера: {ex.Message}");
        }
    }

    [HttpPut("updatePassword")]
    public IActionResult updatePassword(string userName, string newPassword)
    {
        try
        {
            // 1. Находим пользователя
            var user = _context.Users
                .FirstOrDefault(u => u.Username == userName);

            if (user == null)
            {
                return NotFound($"Пользователь '{userName}' не найден");
            }

            // 2. Проверяем сложность пароля (опционально)
            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
            {
                return BadRequest("Пароль должен содержать минимум 6 символов");
            }

            // 3. Хешируем новый пароль
            string passwordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);

            // 4. Обновляем пароль в базе
            user.PasswordHash = passwordHash;

            if (user.temporaryPassword) user.temporaryPassword = false;

            _context.Users.Update(user);
            _context.SaveChanges();

            // 5. Возвращаем успешный результат
            return Ok(new
            {
                message = "Пароль успешно обновлен",
                userName = user.Username
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Ошибка сервера: {ex.Message}");
        }
    }

    [HttpGet("isTemporaryPassword")]
    public IActionResult isTemporaryPassword(string userName)
    {
        try
        {
            // 1. Находим пользователя по имени
            var user = _context.Users
                .FirstOrDefault(u => u.Username == userName);

            // 2. Если пользователь не найден
            if (user == null)
            {
                return NotFound($"Пользователь с именем '{userName}' не найден");
            }

            return Ok(user.temporaryPassword);
        }
        catch (Exception ex)
        {
            // 5. Обработка ошибок
            return StatusCode(500, $"Ошибка сервера: {ex.Message}");
        }
    }

    [HttpDelete("delete/{id}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> DeleteAccount(int id)
    {
        try
        {
            var account = await _context.Users.FindAsync(id);
            if (account == null)
            {
                return NotFound($"Аккаунт с id = {id} не найден");
            }

            _context.Users.Remove(account);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = $"Аккаунт пользователя '{account.Username}' успешно удален.",
                AccountID = id
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Внутрення ошибка сервера: {ex.Message}");
        }
    }

    [HttpPut("changeRole/{id}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> ChangeRole (int id, string newRole)
    {
        try
        {
            
            var existingAccount = await _context.Users.FindAsync(id);
            if (existingAccount == null)
            {
                return NotFound($"Аккаунт пользователя с id = {id} не найден");
            }
            existingAccount.Role = newRole;

            await _context.SaveChangesAsync();

            return Ok(existingAccount);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Внутренняя ошибка сервера: {ex.Message}");
        }
    }

}