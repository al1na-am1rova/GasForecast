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

                // Для PostgreSQL используем numeric
                entity.Property(e => e.GasConsumption)
                    .HasColumnType("numeric(18,3)");

                entity.Property(e => e.OutsideTemperature)
                    .HasColumnType("numeric(5,2)");

                entity.Property(e => e.OperatingHours)
                    .HasColumnType("numeric(10,2)");

                entity.Property(e => e.TotalOperatingHours)
                    .HasColumnType("numeric(10,2)");

                entity.Property(e => e.UnitPowerPercentage)
                    .HasColumnType("numeric(5,2)");

                entity.Property(e => e.LowerHeatingValue)
                    .HasColumnType("numeric(8,2)");

                entity.Property(e => e.Timestamp)
                    .IsRequired();

                // Индексы
                entity.HasIndex(e => e.Timestamp);
                entity.HasIndex(e => e.UnitType);
                entity.HasIndex(e => e.OutsideTemperature);
            });

            // Начальные данные для тестирования - исправлены типы
            modelBuilder.Entity<ElectricityConsumptionData>().HasData(
                new ElectricityConsumptionData
                {
                    Id = 1,
                    Timestamp = DateTime.UtcNow.AddDays(-10),
                    UnitType = "ГТЭС-12",
                    ActiveUnitsCount = 2,
                    OutsideTemperature = -15.5, // double
                    OperatingHours = 720.0, // double
                    TotalOperatingHours = 15000.0, // double
                    UnitPowerPercentage = 85.0, // double
                    LowerHeatingValue = 8500.0, // double
                    GasConsumption = 2456.324 // double
                },
                new ElectricityConsumptionData
                {
                    Id = 2,
                    Timestamp = DateTime.UtcNow.AddDays(-5),
                    UnitType = "ЭГ-6000",
                    ActiveUnitsCount = 1,
                    OutsideTemperature = 5.2, // double
                    OperatingHours = 480.0, // double
                    TotalOperatingHours = 12000.0, // double
                    UnitPowerPercentage = 75.0, // double
                    LowerHeatingValue = 8300.0, // double
                    GasConsumption = 1567.891 // double
                },
                new ElectricityConsumptionData
                {
                    Id = 3,
                    Timestamp = DateTime.UtcNow.AddDays(-1),
                    UnitType = "АСГД-500",
                    ActiveUnitsCount = 3,
                    OutsideTemperature = -20.1, // double
                    OperatingHours = 600.0, // double
                    TotalOperatingHours = 8000.0, // double
                    UnitPowerPercentage = 90.0, // double
                    LowerHeatingValue = 8600.0, // double
                    GasConsumption = 1234.567 // double
                },
                new ElectricityConsumptionData
                {
                    Id = 4,
                    Timestamp = DateTime.UtcNow.AddHours(-12),
                    UnitType = "ГТЭС-12",
                    ActiveUnitsCount = 1,
                    OutsideTemperature = 10.5, // double
                    OperatingHours = 240.0, // double
                    TotalOperatingHours = 20000.0, // double
                    UnitPowerPercentage = 70.0, // double
                    LowerHeatingValue = 8400.0, // double
                    GasConsumption = 987.654 // double
                },
                new ElectricityConsumptionData
                {
                    Id = 5,
                    Timestamp = DateTime.UtcNow.AddHours(-6),
                    UnitType = "ЭГ-6000",
                    ActiveUnitsCount = 2,
                    OutsideTemperature = 15.3, // double
                    OperatingHours = 360.0, // double
                    TotalOperatingHours = 9000.0, // double
                    UnitPowerPercentage = 95.0, // double
                    LowerHeatingValue = 8550.0, // double
                    GasConsumption = 2100.123 // double
                }
            );
        }
    }
}