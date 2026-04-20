using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace DACN_CNPM_QuanLyXayDung.Models;

public partial class HeThongQlvongDoiDuAnTaiNguyenContext : DbContext
{
    public HeThongQlvongDoiDuAnTaiNguyenContext()
    {
    }

    public HeThongQlvongDoiDuAnTaiNguyenContext(DbContextOptions<HeThongQlvongDoiDuAnTaiNguyenContext> options)
        : base(options)
    {
    }

    public virtual DbSet<InventoryTransaction> InventoryTransactions { get; set; }

    public virtual DbSet<Material> Materials { get; set; }

    public virtual DbSet<MaterialUsage> MaterialUsages { get; set; }

    public virtual DbSet<Project> Projects { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<Stage> Stages { get; set; }

    public virtual DbSet<Task> Tasks { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<Notification> Notifications { get; set; }

    public virtual DbSet<Supplier> Suppliers { get; set; }
    public virtual DbSet<MaterialRequest> MaterialRequests { get; set; }
    public virtual DbSet<MaterialRequestDetail> MaterialRequestDetails { get; set; }
    public virtual DbSet<PurchaseOrder> PurchaseOrders { get; set; }
    public virtual DbSet<PurchaseOrderDetail> PurchaseOrderDetails { get; set; }
    public virtual DbSet<MaterialReceipt> MaterialReceipts { get; set; }
    public virtual DbSet<MaterialReceiptDetail> MaterialReceiptDetails { get; set; }
    public virtual DbSet<DocumentAttachment> DocumentAttachments { get; set; }
    public virtual DbSet<AuditLog> AuditLogs { get; set; }

    public virtual DbSet<ProjectMaterialRequest> ProjectMaterialRequests { get; set; }
    public virtual DbSet<ProjectMaterialRequestDetail> ProjectMaterialRequestDetails { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Data Source=localhost;Initial Catalog=HeThongQLVongDoiDuAnTaiNguyen;Integrated Security=True;Trust Server Certificate=True");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MaterialRequest>(entity =>
        {
            entity.HasOne(d => d.Creator)
                .WithMany()
                .HasForeignKey(d => d.CreatorId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_MaterialRequest_Creator");

            entity.HasOne(d => d.Approver)
                .WithMany()
                .HasForeignKey(d => d.ApproverId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_MaterialRequest_Approver");
        });

        modelBuilder.Entity<MaterialReceipt>(entity =>
        {
            entity.HasOne(d => d.Receiver)
                .WithMany()
                .HasForeignKey(d => d.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_MaterialReceipt_Receiver");
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasOne(d => d.User)
                .WithMany()
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_AuditLog_User");
        });

        modelBuilder.Entity<InventoryTransaction>(entity =>
        {
            entity.HasKey(e => e.TransactionId).HasName("PK__Inventor__55433A4B41C394E1");

            entity.Property(e => e.TransactionId).HasColumnName("TransactionID");
            entity.Property(e => e.Date)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.MaterialId).HasColumnName("MaterialID");
            entity.Property(e => e.ProjectId).HasColumnName("ProjectID");
            entity.Property(e => e.StageId).HasColumnName("StageID");
            entity.Property(e => e.Type).HasMaxLength(20);
            entity.Property(e => e.WarehouseKeeperId).HasColumnName("WarehouseKeeperID");

            entity.HasOne(d => d.Material).WithMany(p => p.InventoryTransactions)
                .HasForeignKey(d => d.MaterialId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Inventory_Material");

            entity.HasOne(d => d.Project).WithMany(p => p.InventoryTransactions)
                .HasForeignKey(d => d.ProjectId)
                .HasConstraintName("FK_Inventory_Project");

            entity.HasOne(d => d.WarehouseKeeper).WithMany(p => p.InventoryTransactions)
                .HasForeignKey(d => d.WarehouseKeeperId)
                .HasConstraintName("FK_Inventory_User");
                
            entity.HasOne(d => d.Stage).WithMany(p => p.InventoryTransactions)
                .HasForeignKey(d => d.StageId)
                .HasConstraintName("FK_Inventory_Stage");
        });

        modelBuilder.Entity<Material>(entity =>
        {
            entity.HasKey(e => e.MaterialId).HasName("PK__Material__C506131779539DC7");

            entity.Property(e => e.MaterialId).HasColumnName("MaterialID");
            entity.Property(e => e.MaterialName).HasMaxLength(150);
            entity.Property(e => e.MinStockLevel).HasDefaultValue(0);
            entity.Property(e => e.Unit).HasMaxLength(50);
        });

        modelBuilder.Entity<MaterialUsage>(entity =>
        {
            entity.HasKey(e => e.UsageId).HasName("PK__Material__29B197C0B1BF7FD0");

            entity.ToTable("MaterialUsage");

            entity.Property(e => e.UsageId).HasColumnName("UsageID");
            entity.Property(e => e.Date)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.MaterialId).HasColumnName("MaterialID");
            entity.Property(e => e.ProjectId).HasColumnName("ProjectID");

            entity.HasOne(d => d.Material).WithMany(p => p.MaterialUsages)
                .HasForeignKey(d => d.MaterialId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Usage_Material");

            entity.HasOne(d => d.Project).WithMany(p => p.MaterialUsages)
                .HasForeignKey(d => d.ProjectId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Usage_Project");
        });

        modelBuilder.Entity<Project>(entity =>
        {
            entity.HasKey(e => e.ProjectId).HasName("PK__Projects__761ABED0C1B18240");

            entity.Property(e => e.ProjectId).HasColumnName("ProjectID");
            entity.Property(e => e.Budget).HasColumnType("decimal(15, 2)");
            entity.Property(e => e.BudgetLocked).HasDefaultValue(false);
            entity.Property(e => e.ContractFileContent).HasColumnType("varbinary(max)");
            entity.Property(e => e.ContractFileName).HasMaxLength(255);
            entity.Property(e => e.ContractFileContentType).HasMaxLength(100);
            entity.Property(e => e.ContractUploadedAt).HasColumnType("datetime2");
            entity.Property(e => e.ManagerId).HasColumnName("ManagerID");
            entity.Property(e => e.ProjectName).HasMaxLength(150);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("Planned");

            entity.HasOne(d => d.Manager).WithMany(p => p.Projects)
                .HasForeignKey(d => d.ManagerId)
                .HasConstraintName("FK_Projects_Manager");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RoleId).HasName("PK__Roles__8AFACE3AB04EA8A4");

            entity.Property(e => e.RoleId).HasColumnName("RoleID");
            entity.Property(e => e.RoleName).HasMaxLength(50);

            entity.HasData(
                new Role { RoleId = 1, RoleName = "Admin" },
                new Role { RoleId = 2, RoleName = "Warehouse keeper" },
                new Role { RoleId = 3, RoleName = "Engineer" },
                new Role { RoleId = 4, RoleName = "Project Manager" }
            );
        });

        modelBuilder.Entity<Stage>(entity =>
        {
            entity.HasKey(e => e.StageId).HasName("PK__Stages__03EB7AF8309468FE");

            entity.Property(e => e.StageId).HasColumnName("StageID");
            entity.Property(e => e.ProjectId).HasColumnName("ProjectID");
            entity.Property(e => e.AssignedUserId).HasColumnName("AssignedUserID");
            entity.Property(e => e.StageName).HasMaxLength(150);
            entity.Property(e => e.Budget).HasColumnType("decimal(15, 2)");
            entity.Property(e => e.BudgetLocked).HasDefaultValue(false);
            entity.Property(e => e.MaterialDeclarationFileContent).HasColumnType("varbinary(max)");
            entity.Property(e => e.MaterialDeclarationFileName).HasMaxLength(255);
            entity.Property(e => e.MaterialDeclarationContentType).HasMaxLength(100);
            entity.Property(e => e.MaterialDeclarationUploadedAt).HasColumnType("datetime2");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("Planned");

            entity.HasOne(d => d.Project).WithMany(p => p.Stages)
                .HasForeignKey(d => d.ProjectId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Stages_Project");

            entity.HasOne(d => d.AssignedUser).WithMany(p => p.Stages)
                .HasForeignKey(d => d.AssignedUserId)
                .HasConstraintName("FK_Stages_User");
        });

        modelBuilder.Entity<Task>(entity =>
        {
            entity.HasKey(e => e.TaskId).HasName("PK__Tasks__7C6949D13E927275");

            entity.Property(e => e.TaskId).HasColumnName("TaskID");
            entity.Property(e => e.ProjectId).HasColumnName("ProjectID");
            entity.Property(e => e.StageId).HasColumnName("StageID");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("Pending");
            entity.Property(e => e.TaskName).HasMaxLength(150);

            entity.HasOne(d => d.Project).WithMany(p => p.Tasks)
                .HasForeignKey(d => d.ProjectId)
                .HasConstraintName("FK_Tasks_Project");

            entity.HasOne(d => d.Stage).WithMany(p => p.Tasks)
                .HasForeignKey(d => d.StageId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Tasks_Stage");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__Users__1788CCAC6598EAF8");

            entity.HasIndex(e => e.Username, "UQ__Users__A9D10534D8332783").IsUnique();

            entity.Property(e => e.UserId)
                .HasColumnName("UserID")
                .HasMaxLength(50)
                .ValueGeneratedNever();
            entity.Property(e => e.Username).HasMaxLength(100);
            entity.Property(e => e.FullName).HasMaxLength(100);
            entity.Property(e => e.Password).HasMaxLength(255);
            entity.Property(e => e.RoleId).HasColumnName("RoleID");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("Active");

            entity.HasOne(d => d.Role).WithMany(p => p.Users)
                .HasForeignKey(d => d.RoleId)
                .HasConstraintName("FK__Users__RoleID__571DF1D5");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.NotificationId);

            entity.HasOne(d => d.User)
                .WithMany()
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_Notifications_Users");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
