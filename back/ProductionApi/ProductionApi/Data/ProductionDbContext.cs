using Azure;
using Microsoft.EntityFrameworkCore;
using ProductionApi.Models;
using System;
using Operation = ProductionApi.Models.Operation;

namespace ProductionApi.Data
{
    public class ProductionDbContext : DbContext
    {
        public ProductionDbContext(DbContextOptions<ProductionDbContext> options)
            : base(options)
        {
        }

        // 1. People
        public DbSet<Person> People { get; set; }

        // 2. WorkPlaces
        public DbSet<WorkPlace> WorkPlaces { get; set; }

        // 3. Equipment
        public DbSet<Equipment> Equipment { get; set; }

        // 4. Details
        public DbSet<Detail> Details { get; set; }

        // 5. Detail to Detail Reconfiguration
        public DbSet<DetailToDetailReconfigurationTime> DetailToDetailReconfigurationTimes { get; set; }

        // 6. Materials
        public DbSet<Material> Materials { get; set; }

        // 7. Material Sizes
        public DbSet<MaterialSize> MaterialSizes { get; set; }

        // 8. Material <> MaterialSize (M:N)
        public DbSet<MaterialMaterialSize> MaterialMaterialSizes { get; set; }

        // 9. Operations
        public DbSet<Operation> Operations { get; set; }

        // 10. Shift Work Log
        public DbSet<ShiftWorkLog> ShiftWorkLogs { get; set; }

        // 11. ShiftWorkLog <> Setup People (M:N)
        public DbSet<ShiftWorkLogSetupPerson> ShiftWorkLogSetupPeople { get; set; }

        // 12. ShiftWorkLog <> Equipment (M:N)
        public DbSet<ShiftWorkLogEquipment> ShiftWorkLogEquipments { get; set; }

        // 13. TimeSheet
        public DbSet<TimeSheet> TimeSheet { get; set; }

        // 14. Equipment TimeSheet
        public DbSet<EquipmentTimeSheet> EquipmentTimeSheet { get; set; }

        // 15. Material Stock (остаток материалов)
        public DbSet<MaterialStock> MaterialStocks { get; set; }

        // 16. Material Transaction (журнал операций)
        public DbSet<MaterialTransaction> MaterialTransactions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Настройка для DetailToDetailReconfigurationTime
            modelBuilder.Entity<DetailToDetailReconfigurationTime>(entity =>
            {
                entity.HasKey(e => e.ReconfigurationID);

                // FromDetail > Detail
                entity.HasOne(e => e.FromDetail)
                    .WithMany(d => d.FromReconfigurations)
                    .HasForeignKey(e => e.FromDetailID)
                    .OnDelete(DeleteBehavior.Restrict);

                // ToDetail > Detail
                entity.HasOne(e => e.ToDetail)
                    .WithMany(d => d.ToReconfigurations)
                    .HasForeignKey(e => e.ToDetailID)
                    .OnDelete(DeleteBehavior.Restrict);

                // Equipment
                entity.HasOne(e => e.Equipment)
                    .WithMany(eq => eq.ReconfigurationTimes)
                    .HasForeignKey(e => e.EquipmentID)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // M:N: Material <> MaterialSize
            modelBuilder.Entity<MaterialMaterialSize>()
                .HasKey(mms => new { mms.MaterialID, mms.MaterialSizeID });

            modelBuilder.Entity<MaterialMaterialSize>()
                .HasOne(mms => mms.Material)
                .WithMany(m => m.MaterialMaterialSizes)
                .HasForeignKey(mms => mms.MaterialID);

            modelBuilder.Entity<MaterialMaterialSize>()
                .HasOne(mms => mms.MaterialSize)
                .WithMany(ms => ms.MaterialMaterialSizes)
                .HasForeignKey(mms => mms.MaterialSizeID);

            // M:N: ShiftWorkLog <> Setup People
            modelBuilder.Entity<ShiftWorkLogSetupPerson>()
                .HasKey(sp => new { sp.ShiftWorkLogID, sp.PersonID });

            modelBuilder.Entity<ShiftWorkLogSetupPerson>()
                .HasOne(sp => sp.ShiftWorkLog)
                .WithMany(swl => swl.SetupPeople)
                .HasForeignKey(sp => sp.ShiftWorkLogID);

            modelBuilder.Entity<ShiftWorkLogSetupPerson>()
                .HasOne(sp => sp.Person)
                .WithMany(p => p.ShiftWorkLogSetupPeople)
                .HasForeignKey(sp => sp.PersonID);

            // M:N: ShiftWorkLog <> Equipment
            modelBuilder.Entity<ShiftWorkLogEquipment>()
                .HasKey(se => new { se.ShiftWorkLogID, se.EquipmentID });

            modelBuilder.Entity<ShiftWorkLogEquipment>()
                .HasOne(se => se.ShiftWorkLog)
                .WithMany(sw => sw.Equipments)
                .HasForeignKey(se => se.ShiftWorkLogID);

            modelBuilder.Entity<ShiftWorkLogEquipment>()
                .HasOne(se => se.Equipment)
                .WithMany(eq => eq.ShiftWorkLogs)
                .HasForeignKey(se => se.EquipmentID);

            // Настройка для WorkPlace
            modelBuilder.Entity<WorkPlace>()
                .HasMany(wp => wp.Equipments)
                .WithOne(eq => eq.WorkPlace)
                .HasForeignKey(eq => eq.WorkPlaceID)
                .OnDelete(DeleteBehavior.SetNull);

            // Настройка для Operation
            modelBuilder.Entity<Operation>()
                .HasOne(o => o.Equipment)
                .WithMany(eq => eq.Operations)
                .HasForeignKey(o => o.EquipmentID);

            modelBuilder.Entity<Operation>()
                .HasOne(o => o.Detail)
                .WithMany(d => d.Operations)
                .HasForeignKey(o => o.DetailID);

            // Настройка для MaterialSize (решаем предупреждение о decimal)
            modelBuilder.Entity<MaterialSize>()
                .Property(ms => ms.SizeValue)
                .HasPrecision(18, 3); // 18 цифр всего, 3 после запятой

            // Настройка для Person
            modelBuilder.Entity<Person>()
                .HasMany(p => p.ShiftWorkLogSetupPeople)
                .WithOne(sp => sp.Person)
                .HasForeignKey(sp => sp.PersonID);

            // Настройка для ShiftWorkLog
            modelBuilder.Entity<ShiftWorkLog>()
                .HasOne(swl => swl.Master)
                .WithMany()
                .HasForeignKey(swl => swl.MasterID)
                .OnDelete(DeleteBehavior.Restrict);

            // Настройка для MaterialStock
            modelBuilder.Entity<MaterialStock>()
                .HasIndex(ms => new { ms.MaterialID, ms.MaterialSizeID })
                .IsUnique();

            modelBuilder.Entity<MaterialStock>()
                .HasOne(ms => ms.Material)
                .WithMany()
                .HasForeignKey(ms => ms.MaterialID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<MaterialStock>()
                .HasOne(ms => ms.MaterialSize)
                .WithMany()
                .HasForeignKey(ms => ms.MaterialSizeID)
                .OnDelete(DeleteBehavior.Restrict);

            // Настройка для MaterialTransaction
            modelBuilder.Entity<MaterialTransaction>()
                .HasOne(mt => mt.Material)
                .WithMany()
                .HasForeignKey(mt => mt.MaterialID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<MaterialTransaction>()
                .HasOne(mt => mt.MaterialSize)
                .WithMany()
                .HasForeignKey(mt => mt.MaterialSizeID)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
