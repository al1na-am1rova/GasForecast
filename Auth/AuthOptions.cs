using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace GasForecast.Auth
{
    public static class AuthOptions
    {
        public const string ISSUER = "Auth";
        public const string AUDIENCE = "Client";
        public const int LIFETIME = 30;

        private const string KEY = "super_secret_key_12345_super_secret_key_12345_super_secret_key_12345";

        public static SymmetricSecurityKey GetSymmetricSecurityKey()
        {
            return new SymmetricSecurityKey(Encoding.ASCII.GetBytes(KEY));
        }

        // Создаем модель для ответа
        public record TokenResponse(string AccessToken, string Username, string Role, DateTime Expires);

        // ИЗМЕНЕНО: добавляем userId
        public static TokenResponse GenerateToken(int userId, string login, string role)
        {
            var claims = new List<Claim>
            {
                new Claim("id", userId.ToString()),           // ДОБАВЛЯЕМ ID пользователя
                new Claim(ClaimTypes.Name, login),
                new Claim(ClaimTypes.Role, role)
            };

            var now = DateTime.UtcNow;
            var expires = now.AddMinutes(LIFETIME);

            var jwt = new JwtSecurityToken(
                issuer: ISSUER,
                audience: AUDIENCE,
                notBefore: now,
                expires: expires,
                claims: claims,
                signingCredentials: new SigningCredentials(
                    GetSymmetricSecurityKey(),
                    SecurityAlgorithms.HmacSha256)
            );

            var token = new JwtSecurityTokenHandler().WriteToken(jwt);

            return new TokenResponse(
                AccessToken: token,
                Username: login,
                Role: role,
                Expires: expires
            );
        }
    }
}