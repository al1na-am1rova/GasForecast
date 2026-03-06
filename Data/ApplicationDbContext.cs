using GasForecast.Models;
using GasForecast.Models.DTO;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql.EntityFrameworkCore.PostgreSQL;

namespace GasForecast.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        static ApplicationDbContext()
        {
            // Устанавливаем совместимость с PostgreSQL
            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
        }

        public DbSet<ElectricalPowerStation> ElectricalPowerStations { get; set; }
        public DbSet<ElectricalUnitPassport> ElectricalUnitPassports { get; set; }
        public DbSet<User> Users{ get; set; }
        public DbSet<UserElectricalStation> UserElectricalStations { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.HasDefaultSchema("public");

            modelBuilder.Entity<ElectricalPowerStation>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
                entity.Property(e => e.UnitType).IsRequired().HasMaxLength(100);
                entity.Property(e => e.LaunchDate).IsRequired().HasColumnType("timestamp without time zone");
            });

            modelBuilder.Entity<ElectricalUnitPassport>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.UnitType).IsRequired().HasMaxLength(100);
                entity.Property(e => e.EngineType).IsRequired().HasMaxLength(50);
                entity.Property(e => e.RatedPower).IsRequired();
                entity.Property(e => e.StandartPower).IsRequired();
                entity.Property(e => e.ConsumptionNorm);
            });

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Username).IsRequired().HasMaxLength(100);
                entity.Property(e => e.PasswordHash).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Role).HasDefaultValue(false);
                entity.Property(e => e.LastSessionTime);
                entity.Property(e => e.temporaryPassword).HasDefaultValue(true);

            });

            modelBuilder.Entity<UserElectricalStation>(entity =>
            {
                entity.HasKey(e => e.Id);

                // Настраиваем связь с User
                entity.HasOne(e => e.User)
                    .WithMany(u => u.UserElectricalStations)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Настраиваем связь с ElectricalPowerStation
                entity.HasOne(e => e.ElectricalPowerStation)
                    .WithMany(eps => eps.UserElectricalStations)
                    .HasForeignKey(e => e.ElectricalStationId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Добавляем уникальный индекс, чтобы у пользователя не было дублей одной станции
                entity.HasIndex(e => new { e.UserId, e.ElectricalStationId })
                    .IsUnique();
            });
        }
    }
}