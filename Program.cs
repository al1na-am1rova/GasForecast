using GasForecast.Auth;
using GasForecast.Data;
using GasForecast.Models;
using GasForecast.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

using GasForecast.Services.ML;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddSwaggerGen();
builder.Services.AddOpenApi();
// Scoped - один экземпл€р на весь запрос
builder.Services.AddScoped<IElectricityConsumptionCalculator, ElectricityConsumptionCalculator>();
// Singleton - один экземпл€р на все приложение
builder.Services.AddSingleton<ElectricityCoefficientsService, ElectricityCoefficientsService>();
// ƒобавл€ем в Program.cs
builder.Services.AddScoped<IAccountService, AccountService>();

// –егистрируем DbContext дл€ PostgreSQL
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHttpClient<IMLServiceClient, MLServiceClient>(client =>
{
    var mlUrl = builder.Configuration["MLService:Url"] ?? "http://localhost:8000";
    client.BaseAddress = new Uri(mlUrl);
    client.Timeout = TimeSpan.FromMinutes(5);
});

builder.Services.AddScoped<IModelManagementService, ModelManagementService>();

builder.Services.AddHostedService<ModelTrainingBackgroundService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        policy.WithOrigins(
            "http://localhost:4200",    // Angular dev server
            "https://localhost:4200"    // если используете HTTPS
        )
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials();
    });
});

builder.Services.AddAuthorization();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            // укзывает, будет ли валидироватьс€ издатель при валидации токена
            ValidateIssuer = true,
            // строка, представл€юща€ издател€
            ValidIssuer = AuthOptions.ISSUER,

            // будет ли валидироватьс€ потребитель токена
            ValidateAudience = true,
            // установка потребител€ токена
            ValidAudience = AuthOptions.AUDIENCE,
            // будет ли валидироватьс€ врем€ существовани€
            ValidateLifetime = true,

            // установка ключа безопасности
            IssuerSigningKey = AuthOptions.GetSymmetricSecurityKey(),
            // валидаци€ ключа безопасности
            ValidateIssuerSigningKey = true,
        };
    });

var app = builder.Build();

app.UseCors("AllowAngular");

// —оздаем базу данных при запуске
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    //dbContext.Database.EnsureCreated();
    dbContext.Database.Migrate();
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

app.Run();