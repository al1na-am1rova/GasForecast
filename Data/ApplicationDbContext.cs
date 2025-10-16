using Microsoft.EntityFrameworkCore;
using GasForecast.Models;
using GasForecast.Models.DTO;

namespace GasForecast.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }
        public DbSet<ElectricalPowerStation> ElectricalPowerStations { get; set; }
        public DbSet<ElectricalUnitPassport> ElectricalUnitPassports { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ElectricalPowerStation>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
                entity.Property(e => e.UnitType).IsRequired().HasMaxLength(100);
                entity.Property(e => e.LaunchDate).IsRequired();
                entity.Property(e => e.GasConsumption);
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
        }
    }
}