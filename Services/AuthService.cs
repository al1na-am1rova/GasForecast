// AuthService.cs
using GasForecast.Auth;
using static GasForecast.Auth.AuthOptions;

namespace GasForecast.Services
{
    public class AuthService : IAuthService
    {
        private readonly Dictionary<string, string> _users = new()
        {
            { "user", "user" },
            { "admin", "admin" }
        };

        public async Task<TokenResponse?> GetTokenAsync(LoginData loginData, CancellationToken ct)
        {
            // Имитация асинхронной операции
            await Task.Delay(100, ct);

            // Проверяем пользователя
            if (_users.TryGetValue(loginData.Username, out var password) &&
                password == loginData.Password)
            {
                // Определяем, админ ли пользователь
                bool isAdmin = loginData.Username == "admin";

                // Генерируем токен
                return AuthOptions.GenerateToken(loginData.Username, isAdmin);
            }

            return null;
        }
    }
}