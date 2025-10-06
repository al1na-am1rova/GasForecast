// Data/ApplicationDbContext.cs
using GasForecast.Models;
using Microsoft.EntityFrameworkCore;

namespace GasForecast.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<ElectricityConsumptionData> ElectricityConsumptionData { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ElectricityConsumptionData>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.UnitType)
                    .IsRequired()
                    .HasMaxLength(100);

                // Для SQLite используем REAL вместо numeric
                entity.Property(e => e.GasConsumption)
                    .HasColumnType("REAL"); // REAL для чисел с плавающей точкой

                entity.Property(e => e.OutsideTemperature)
                    .HasColumnType("REAL");

                entity.Property(e => e.OperatingHours)
                    .HasColumnType("REAL");

                entity.Property(e => e.TotalOperatingHours)
                    .HasColumnType("REAL");

                entity.Property(e => e.UnitPowerPercentage)
                    .HasColumnType("REAL");

                entity.Property(e => e.LowerHeatingValue)
                    .HasColumnType("REAL");

                entity.Property(e => e.Timestamp)
                    .IsRequired();

                // Индексы
                entity.HasIndex(e => e.Timestamp);
                entity.HasIndex(e => e.UnitType);
                entity.HasIndex(e => e.OutsideTemperature);
            });

            // Начальные данные для тестирования
            modelBuilder.Entity<ElectricityConsumptionData>().HasData(
                new ElectricityConsumptionData
                {
                    Id = 1,
                    Timestamp = DateTime.UtcNow.AddDays(-10),
                    UnitType = "ГТЭС-12",
                    ActiveUnitsCount = 2,
                    OutsideTemperature = -15.5,
                    OperatingHours = 720.0,
                    TotalOperatingHours = 15000.0,
                    UnitPowerPercentage = 85.0,
                    LowerHeatingValue = 8500.0,
                    GasConsumption = 2456.324
                },
                new ElectricityConsumptionData
                {
                    Id = 2,
                    Timestamp = DateTime.UtcNow.AddDays(-5),
                    UnitType = "ЭГ-6000",
                    ActiveUnitsCount = 1,
                    OutsideTemperature = 5.2,
                    OperatingHours = 480.0,
                    TotalOperatingHours = 12000.0,
                    UnitPowerPercentage = 75.0,
                    LowerHeatingValue = 8300.0,
                    GasConsumption = 1567.891
                },
                new ElectricityConsumptionData
                {
                    Id = 3,
                    Timestamp = DateTime.UtcNow.AddDays(-1),
                    UnitType = "АСГД-500",
                    ActiveUnitsCount = 3,
                    OutsideTemperature = -20.1,
                    OperatingHours = 600.0,
                    TotalOperatingHours = 8000.0,
                    UnitPowerPercentage = 90.0,
                    LowerHeatingValue = 8600.0,
                    GasConsumption = 1234.567
                },
                new ElectricityConsumptionData
                {
                    Id = 4,
                    Timestamp = DateTime.UtcNow.AddHours(-12),
                    UnitType = "ГТЭС-12",
                    ActiveUnitsCount = 1,
                    OutsideTemperature = 10.5,
                    OperatingHours = 240.0,
                    TotalOperatingHours = 20000.0,
                    UnitPowerPercentage = 70.0,
                    LowerHeatingValue = 8400.0,
                    GasConsumption = 987.654
                },
                new ElectricityConsumptionData
                {
                    Id = 5,
                    Timestamp = DateTime.UtcNow.AddHours(-6),
                    UnitType = "ЭГ-6000",
                    ActiveUnitsCount = 2,
                    OutsideTemperature = 15.3,
                    OperatingHours = 360.0,
                    TotalOperatingHours = 9000.0,
                    UnitPowerPercentage = 95.0,
                    LowerHeatingValue = 8550.0,
                    GasConsumption = 2100.123
                }
            );
        }
    }
}