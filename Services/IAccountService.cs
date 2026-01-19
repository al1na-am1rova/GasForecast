using static GasForecast.Auth.AuthOptions;

namespace GasForecast.Services
{
    public interface IAccountService
    {
        Task<TokenResponse?> GetTokenAsync(LoginData loginData, CancellationToken ct);
        Task<RegisterResponse> RegisterUserAsync(RegisterData registerData, CancellationToken ct);
    }
    public record LoginData(string Username, string Password);

    public record RegisterData(string Username, string Password, string Role);

    public record RegisterResponse( bool success, string? ExMessage = null);
}