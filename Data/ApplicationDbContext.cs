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
        public DbSet<ElectricalUnit> ElectricalUnits { get; set; }
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
                entity.Property(e => e.GasConsumption).HasColumnType("decimal(18,2)");
            });

            modelBuilder.Entity<ElectricalUnit>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.UnitType).IsRequired().HasMaxLength(100);
                entity.Property(e => e.CurrentPowerPercentage).IsRequired();

                // Одна станция может иметь много агрегатов одного типа
                entity.HasOne<ElectricalPowerStation>()
                      .WithMany()
                      .HasForeignKey(e => e.UnitType)
                      .HasPrincipalKey(p => p.UnitType)
                      .OnDelete(DeleteBehavior.Restrict);

                // Связь один-к-одному с паспортом через UnitType
                entity.HasOne<ElectricalUnitPassport>()
                      .WithOne()
                      .HasForeignKey<ElectricalUnit>(e => e.UnitType)
                      .HasPrincipalKey<ElectricalUnitPassport>(p => p.UnitType)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<ElectricalUnitPassport>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.UnitType).IsRequired().HasMaxLength(100);
                entity.Property(e => e.EngineType).IsRequired().HasMaxLength(50);
                entity.Property(e => e.RatedPower).IsRequired();
                entity.Property(e => e.StandartPower).IsRequired();
                entity.Property(e => e.ConsumptionNorm).HasColumnType("decimal(18,6)");
            });
        }
    }
}