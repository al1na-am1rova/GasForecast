using GasForecast.Auth;
using GasForecast.Data;
using GasForecast.Models;
using GasForecast.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddSwaggerGen();
builder.Services.AddOpenApi();
// Scoped - один экземпляр на весь запрос
builder.Services.AddScoped<IElectricityConsumptionCalculator, ElectricityConsumptionCalculator>();
// Singleton - один экземпляр на все приложение
builder.Services.AddSingleton<ElectricityCoefficientsService, ElectricityCoefficientsService>();


// Регистрируем DbContext для PostgreSQL
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.ConfigureAuthentication();

var app = builder.Build();

// Создаем базу данных при запуске
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

app.MapAuthEndpoints();
app.Run();