using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace GasForecast.Auth
{
    // Класс с настройками JWT
    public class AuthOptions
    {
        public const string ISSUER = "MyAuthServer";
        public const string AUDIENCE = "MyAuthClient";
        const string KEY = "mysupersecret_secretsecretsecretkey!123";

        public static SymmetricSecurityKey GetSymmetricSecurityKey() =>
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(KEY));
    }

    // Модель пользователя
    public record class Person(string Login, string Password, string Role);

    // Статический класс для настройки сервисов аутентификации
    public static class AuthServiceConfiguration
    {
        public static void ConfigureAuthentication(this IServiceCollection services)
        {
            services.AddAuthorization();
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = AuthOptions.ISSUER,
                        ValidateAudience = true,
                        ValidAudience = AuthOptions.AUDIENCE,
                        ValidateLifetime = true,
                        IssuerSigningKey = AuthOptions.GetSymmetricSecurityKey(),
                        ValidateIssuerSigningKey = true,
                    };
                });
        }
    }

    // Статический класс для эндпоинтов аутентификации
    public static class AuthEndpoints
    {
        // условная бд с пользователями
        private static readonly List<Person> people = new()
        {
            new Person("user", "user", "user"),
            new Person("admin", "admin", "admin")
        };

        public static void MapAuthEndpoints(this WebApplication app)
        {
            app.MapPost("/login", (string entered_login, string entered_password) =>
            {
                // находим пользователя 
                Person? person = people.FirstOrDefault(p => p.Login == entered_login && p.Password == entered_password);
                // если пользователь не найден, отправляем статусный код 401
                if (person is null) return Results.Unauthorized();

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, person.Login),
                    new Claim(ClaimTypes.Role, person.Role)
                };

                // создаем JWT-токен
                var jwt = new JwtSecurityToken(
                    issuer: AuthOptions.ISSUER,
                    audience: AuthOptions.AUDIENCE,
                    claims: claims,
                    expires: DateTime.UtcNow.Add(TimeSpan.FromMinutes(20)),
                    signingCredentials: new SigningCredentials(AuthOptions.GetSymmetricSecurityKey(), SecurityAlgorithms.HmacSha256));

                var encodedJwt = new JwtSecurityTokenHandler().WriteToken(jwt);

                // формируем ответ
                var response = new
                {
                    access_token = encodedJwt,
                    username = person.Login,
                    role = person.Role
                };

                return Results.Json(response);
            });

            app.Map("/data", [Authorize] () => new { message = "Hello World!" });
        }
    }
}