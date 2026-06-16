using Microsoft.EntityFrameworkCore;
using Diplom.Models.Fields;
using Diplom.Models.Tools;
using Diplom.Models.Warehouses;
using Diplom.Models;
using Diplom.Models.Navigation;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Diplom.Models.Identity;

namespace Diplom.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        
        public DbSet<FieldSituationLogModel> FieldSituationLogs { get; set; }
        public DbSet<FieldEntity> Fields { get; set; }
        public DbSet<CropRotationLog> CropRotationLogs { get; set; }
        public DbSet<FieldWorkLogPlanModel> FieldWorkLogPlans { get; set; }
        public DbSet<FieldWorkLogPlanMaterialModel> FieldWorkLogPlanMaterials { get; set; }
        public DbSet<FieldWorkLogPlanAggregateModel> FieldWorkLogPlanAggregates { get; set; }
        public DbSet<FieldWorkLogModel> FieldWorkLogs { get; set; }
        public DbSet<FieldWorkLogMaterialModel> FieldWorkLogMaterials { get; set; }
        public DbSet<FieldWorkLogAggregateModel> FieldWorkLogAggregates { get; set; }


        
        public DbSet<EncyclopediaItem> EncyclopediaItems { get; set; }
        public DbSet<Vocabulary> Vocabulary { get; set; }


        
        public DbSet<EquipmentModel> Equipments { get; set; }

        public DbSet<EquipmentJournalModel> EquipmentJournals { get; set; }

        public DbSet<EquipmentJournalModel.EquipmentJournalMaterialModel> EquipmentJournalMaterials { get; set; }

        public DbSet<OperatorModel> Operators { get; set; }


        
        public DbSet<MaterialLogModel> MaterialLogs{ get; set; }

        public DbSet<InventoryHistoryModel> InventoryHistoryModels { get; set; }


        public DbSet<UserQuickAction> UserQuickActions { get; set; }
        public DbSet<FriendRequest> FriendRequests { get; set; }
        public DbSet<Friendship> Friendships { get; set; }
        public DbSet<DataAccessGrant> DataAccessGrants { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<CropRotationLog>()
                .HasIndex(c => new { c.FieldId, c.Year, c.CropId })
                .IsUnique(false);

            modelBuilder.Entity<CropRotationLog>(entity =>
            {
                entity.Property(e => e.LastModified)
                      .HasColumnType("timestamptz")
                      .IsRequired();
            });

            modelBuilder.Entity<FriendRequest>()
                .HasIndex(x => new { x.SenderUserId, x.ReceiverUserId })
                .IsUnique();

            modelBuilder.Entity<Friendship>()
                .HasIndex(x => new { x.UserId, x.FriendUserId })
                .IsUnique();

            modelBuilder.Entity<DataAccessGrant>()
                .HasIndex(x => new { x.OwnerUserId, x.GranteeUserId, x.Section })
                .IsUnique();

            modelBuilder.Entity<Vocabulary>().HasData(
                new Vocabulary { Id = 1, Key = "Sowing", Language = "en", Value = "Sowing" }
            );

        }
    }
}