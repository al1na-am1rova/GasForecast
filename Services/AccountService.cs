// AuthService.cs
using GasForecast.Auth;
using GasForecast.Data;
using GasForecast.Models;
using Microsoft.EntityFrameworkCore;
using static GasForecast.Auth.AuthOptions;

namespace GasForecast.Services
{
    public class AccountService : IAccountService
    {
        private readonly ApplicationDbContext _context;


        public AccountService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<TokenResponse?> GetTokenAsync(LoginData loginData, CancellationToken ct)
        {
            try
            {

                // Ищем пользователя в базе данных
                var user = await _context.Users
                    .FirstOrDefaultAsync(u =>
                        u.Username == loginData.Username);

                if (user == null)
                {
                    return null;
                }

                // Проверяем пароль с использованием BCrypt
                if (!BCrypt.Net.BCrypt.Verify(loginData.Password, user.PasswordHash))
                {
                    return null;
                }

                // Генерируем токен с данными из БД
                return AuthOptions.GenerateToken(
                    user.Username,
                    user.Role);
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public async Task<RegisterResponse> RegisterUserAsync(RegisterData registerData, CancellationToken ct)
        {
            try
            {
                var user = new User
                {
                    Username = registerData.Username,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(registerData.Password),
                    Role = registerData.Role

                };
                await _context.Users.AddAsync(user, ct);
                await _context.SaveChangesAsync(ct);

                var result = new RegisterResponse(true);
                return result;
            }

            catch (Exception ex)
            {
                var result = new RegisterResponse(false, ex.Message);
                return result;
            }
        }

    }
}