using static GasForecast.Auth.AuthOptions;

namespace GasForecast.Services
{
    public interface IAuthService
    {
        Task<TokenResponse?> GetTokenAsync(LoginData loginData, CancellationToken ct);
    }
    public record LoginData(string Username, string Password);
}