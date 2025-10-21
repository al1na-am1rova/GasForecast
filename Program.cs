using GasForecast.Auth;
using GasForecast.Data;
using GasForecast.Models;
using GasForecast.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

//// условная бд с пользователями
//var people = new List<Person>
// {
//    new Person("user", "user", "user"),
//    new Person("admin", "admin", "admin")
//};

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
//builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddOpenApi();
builder.Services.AddScoped<IElectricityConsumptionCalculator, ElectricityConsumptionCalculator>();
builder.Services.AddSingleton<ElectricityCoefficientsService, ElectricityCoefficientsService>();


// Регистрируем DbContext для PostgreSQL
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

//builder.Services.AddAuthorization();
//builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
//    .AddJwtBearer(options =>
//    {
//        options.TokenValidationParameters = new TokenValidationParameters
//        {
//            // указывает, будет ли валидироваться издатель при валидации токена
//            ValidateIssuer = true,
//            // строка, представляющая издателя
//            ValidIssuer = AuthOptions.ISSUER,
//            // будет ли валидироваться потребитель токена
//            ValidateAudience = true,
//            // установка потребителя токена
//            ValidAudience = AuthOptions.AUDIENCE,
//            // будет ли валидироваться время существования
//            ValidateLifetime = true,
//            // установка ключа безопасности
//            IssuerSigningKey = AuthOptions.GetSymmetricSecurityKey(),
//            // валидация ключа безопасности
//            ValidateIssuerSigningKey = true,
//        };
//    });

builder.Services.ConfigureAuthentication();

var app = builder.Build();

// Создаем базу данных при запуске
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    dbContext.Database.EnsureCreated(); // Или используйте миграции
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapControllers();

app.UseAuthentication();

app.UseAuthorization();

//app.MapPost("/login", (string entered_login, string entered_password) =>
//{
//    // находим пользователя 
//    Person? person = people.FirstOrDefault(p => p.Login == entered_login && p.Password == entered_password);
//    // если пользователь не найден, отправляем статусный код 401
//    if (person is null) return Results.Unauthorized();

//    var claims = new List<Claim>
//    {
//    new Claim(ClaimTypes.Name, person.Login),
//    new Claim(ClaimTypes.Role, person.Role)
//    };
//    // создаем JWT-токен
//    var jwt = new JwtSecurityToken(
//            issuer: AuthOptions.ISSUER,
//            audience: AuthOptions.AUDIENCE,
//            claims: claims,
//            expires: DateTime.UtcNow.Add(TimeSpan.FromMinutes(20)),
//            signingCredentials: new SigningCredentials(AuthOptions.GetSymmetricSecurityKey(), SecurityAlgorithms.HmacSha256));
//    var encodedJwt = new JwtSecurityTokenHandler().WriteToken(jwt);

//    // формируем ответ
//    var response = new
//    {
//        access_token = encodedJwt,
//        username = person.Login,
//        role = person.Role
//    };

//    return Results.Json(response);
//});

//app.Map("/data", [Authorize] () => new { message = "Hello World!" });

app.MapAuthEndpoints();
app.Run();

//public class AuthOptions
//{
//    public const string ISSUER = "MyAuthServer"; // издатель токена
//    public const string AUDIENCE = "MyAuthClient"; // потребитель токена
//    const string KEY = "mysupersecret_secretsecretsecretkey!123";   // ключ для шифрации
//    public static SymmetricSecurityKey GetSymmetricSecurityKey() =>
//        new SymmetricSecurityKey(Encoding.UTF8.GetBytes(KEY));
//}

//record class Person(string Login, string Password, string Role);
