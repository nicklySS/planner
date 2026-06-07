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

        // 1. Roles
        public DbSet<Role> Roles { get; set; }

        // 2. People
        public DbSet<Person> People { get; set; }

        // 2a. Person <> Roles (M:N)
        public DbSet<PersonRole> PersonRoles { get; set; }

        // 3. WorkPlaces
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

        // 9a. Detail Operations (отдельные операции для каждой детали)
        public DbSet<DetailOperation> DetailOperations { get; set; }

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

        // 17. Detail Stock (склад готовых деталей)
        public DbSet<DetailStock> DetailStocks { get; set; }

        // 18. Detail Transaction (журнал движения деталей)
        public DbSet<DetailTransaction> DetailTransactions { get; set; }

        // 19. Monthly production plan
        public DbSet<MonthlyProductionPlan> MonthlyProductionPlans { get; set; }
        public DbSet<MonthlyProductionPlanItem> MonthlyProductionPlanItems { get; set; }

        // 20. Generated shift plans
        public DbSet<GeneratedProductionPlan> GeneratedProductionPlans { get; set; }
        public DbSet<GeneratedProductionPlanItem> GeneratedProductionPlanItems { get; set; }

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

            // Конфигурация ShiftWorkLog (новые связи)
            modelBuilder.Entity<ShiftWorkLog>()
                .HasOne(swl => swl.Worker)
                .WithMany()
                .HasForeignKey(swl => swl.WorkerID)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<ShiftWorkLog>()
                .HasOne(swl => swl.Detail)
                .WithMany()
                .HasForeignKey(swl => swl.DetailID)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<ShiftWorkLog>()
                .HasOne(swl => swl.Material)
                .WithMany()
                .HasForeignKey(swl => swl.MaterialID)
                .OnDelete(DeleteBehavior.SetNull);

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

            modelBuilder.Entity<Operation>()
                .HasOne(o => o.MaterialSize)
                .WithMany()
                .HasForeignKey(o => o.MaterialSizeID)
                .OnDelete(DeleteBehavior.SetNull);

            // Настройка для Detail
            modelBuilder.Entity<Detail>()
                .HasOne(d => d.Material)
                .WithMany(m => m.Details)
                .HasForeignKey(d => d.MainMaterial)
                .OnDelete(DeleteBehavior.SetNull);

            // Настройка для MaterialSize (решаем предупреждение о decimal)
            modelBuilder.Entity<MaterialSize>()
                .Property(ms => ms.SizeValue)
                .HasPrecision(18, 3); // 18 цифр всего, 3 после запятой

            // Настройка для Person
            modelBuilder.Entity<Person>()
                .HasMany(p => p.ShiftWorkLogSetupPeople)
                .WithOne(sp => sp.Person)
                .HasForeignKey(sp => sp.PersonID);

            // Настройка для Person и WorkPlace (1:1 relationship)
            modelBuilder.Entity<Person>()
                .HasOne(p => p.WorkPlace)
                .WithOne(wp => wp.ResponsiblePerson)
                .HasForeignKey<Person>(p => p.WorkPlaceID)
                .OnDelete(DeleteBehavior.SetNull);

            // Настройка для PersonRole (M:N между Person и Role)
            modelBuilder.Entity<PersonRole>()
                .HasKey(pr => pr.PersonRoleID);

            modelBuilder.Entity<PersonRole>()
                .HasOne(pr => pr.Person)
                .WithMany(p => p.PersonRoles)
                .HasForeignKey(pr => pr.PersonID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PersonRole>()
                .HasOne(pr => pr.Role)
                .WithMany(r => r.PersonRoles)
                .HasForeignKey(pr => pr.RoleID)
                .OnDelete(DeleteBehavior.Cascade);

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

            modelBuilder.Entity<DetailStock>()
                .HasIndex(ds => ds.DetailID)
                .IsUnique();

            modelBuilder.Entity<DetailStock>()
                .HasOne(ds => ds.Detail)
                .WithMany()
                .HasForeignKey(ds => ds.DetailID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DetailTransaction>()
                .HasOne(dt => dt.Detail)
                .WithMany()
                .HasForeignKey(dt => dt.DetailID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<MonthlyProductionPlan>()
                .HasIndex(p => new { p.Year, p.Month })
                .IsUnique();

            modelBuilder.Entity<MonthlyProductionPlanItem>()
                .HasOne(i => i.Plan)
                .WithMany(p => p.Items)
                .HasForeignKey(i => i.PlanID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<MonthlyProductionPlanItem>()
                .HasOne(i => i.Detail)
                .WithMany()
                .HasForeignKey(i => i.DetailID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<GeneratedProductionPlanItem>()
                .HasOne(i => i.GeneratedPlan)
                .WithMany(p => p.Items)
                .HasForeignKey(i => i.GeneratedPlanID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<GeneratedProductionPlanItem>()
                .HasOne(i => i.Equipment)
                .WithMany()
                .HasForeignKey(i => i.EquipmentID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<GeneratedProductionPlanItem>()
                .HasOne(i => i.Detail)
                .WithMany()
                .HasForeignKey(i => i.DetailID)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
