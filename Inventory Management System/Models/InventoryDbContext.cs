// Models/InventoryDbContext.cs
using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql.Scaffolding.Internal;

namespace Inventory_Management_System.Models;

public partial class InventoryDbContext : DbContext
{
    public InventoryDbContext()
    {
    }

    public InventoryDbContext(DbContextOptions<InventoryDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Allocation> Allocations { get; set; }

    public virtual DbSet<Employee> Employees { get; set; }

    public virtual DbSet<Equipment> Equipment { get; set; }

    public virtual DbSet<EquipmentLocationHistory> EquipmentLocationHistories { get; set; }

    public virtual DbSet<EquipmentLog> EquipmentLogs { get; set; }

    public virtual DbSet<Location> Locations { get; set; }

    public virtual DbSet<ProcurementType> ProcurementTypes { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserRole> UserRoles { get; set; }

    public virtual DbSet<AppSetting> AppSettings { get; set; }

    public virtual DbSet<Transaction> Transactions { get; set; }

    public virtual DbSet<MaintenanceRecord> MaintenanceRecords { get; set; }

    public virtual DbSet<Category> Categories { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseMySql(
            "server=localhost;database=inventory_management;user=root;password=",
            Microsoft.EntityFrameworkCore.ServerVersion.Parse("8.0.46-mysql"));

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .UseCollation("utf8mb4_0900_ai_ci")
            .HasCharSet("utf8mb4");

        modelBuilder.Entity<AppSetting>(entity =>
        {
            entity.ToTable("app_settings");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CompanyName).HasColumnName("company_name").HasMaxLength(150);
            entity.Property(e => e.Theme).HasColumnName("theme").HasMaxLength(20);
            entity.Property(e => e.DbServer).HasColumnName("db_server").HasMaxLength(150);
            entity.Property(e => e.DbName).HasColumnName("db_name").HasMaxLength(150);
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.Property(e => e.PrinterName).HasColumnName("printer_name").HasMaxLength(200);
            entity.Property(e => e.LabelWidthMm).HasColumnName("label_width_mm");
            entity.Property(e => e.LabelHeightMm).HasColumnName("label_height_mm");
        });

        modelBuilder.Entity<Allocation>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("allocations");

            entity.HasIndex(e => e.AllocatedByUserId, "allocated_by_user_id");

            entity.HasIndex(e => e.ApprovedByUserId, "approved_by_user_id");

            entity.HasIndex(e => new { e.EquipmentId, e.Status }, "idx_allocations_active");

            entity.HasIndex(e => e.EmployeeId, "idx_allocations_employee");

            entity.HasIndex(e => e.EquipmentId, "idx_allocations_equipment");

            entity.HasIndex(e => e.Status, "idx_allocations_status");

            entity.HasIndex(e => e.LocationId, "location_id");

            entity.HasIndex(e => e.PreviousAllocationId, "previous_allocation_id");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ActualReturnDate).HasColumnName("actual_return_date");
            entity.Property(e => e.AllocatedByUserId)
                .HasComment("Operator who requested")
                .HasColumnName("allocated_by_user_id");
            entity.Property(e => e.AllocationDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp")
                .HasColumnName("allocation_date");
            entity.Property(e => e.ApprovalDate)
                .HasColumnType("timestamp")
                .HasColumnName("approval_date");
            entity.Property(e => e.ApprovedByUserId)
                .HasComment("Admin who approved")
                .HasColumnName("approved_by_user_id");
            entity.Property(e => e.EmployeeId).HasColumnName("employee_id");
            entity.Property(e => e.EquipmentId).HasColumnName("equipment_id");
            entity.Property(e => e.ExpectedReturnDate).HasColumnName("expected_return_date");
            entity.Property(e => e.LocationId)
                .HasComment("Location where equipment will be used")
                .HasColumnName("location_id");
            entity.Property(e => e.PreviousAllocationId)
                .HasComment("Link to previous allocation (for transfers)")
                .HasColumnName("previous_allocation_id");
            entity.Property(e => e.RejectionReason)
                .HasColumnType("text")
                .HasColumnName("rejection_reason");
            entity.Property(e => e.Status)
                .HasDefaultValueSql("'pending'")
                .HasColumnType("enum('pending','approved','rejected','returned')")
                .HasColumnName("status");

            entity.HasOne(d => d.AllocatedByUser).WithMany(p => p.AllocationAllocatedByUsers)
                .HasForeignKey(d => d.AllocatedByUserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("allocations_ibfk_4");

            entity.HasOne(d => d.ApprovedByUser).WithMany(p => p.AllocationApprovedByUsers)
                .HasForeignKey(d => d.ApprovedByUserId)
                .HasConstraintName("allocations_ibfk_5");

            entity.HasOne(d => d.Employee).WithMany(p => p.Allocations)
                .HasForeignKey(d => d.EmployeeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("allocations_ibfk_2");

            entity.HasOne(d => d.Equipment).WithMany(p => p.Allocations)
                .HasForeignKey(d => d.EquipmentId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("allocations_ibfk_1");

            entity.HasOne(d => d.Location).WithMany(p => p.Allocations)
                .HasForeignKey(d => d.LocationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("allocations_ibfk_3");

            entity.HasOne(d => d.PreviousAllocation).WithMany(p => p.InversePreviousAllocation)
                .HasForeignKey(d => d.PreviousAllocationId)
                .HasConstraintName("allocations_ibfk_6");
        });

        modelBuilder.Entity<Employee>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("employees");

            entity.HasIndex(e => e.DefaultLocationId, "default_location_id");

            entity.HasIndex(e => e.EmployeeCode, "employee_code").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp")
                .HasColumnName("created_at");
            entity.Property(e => e.DefaultLocationId)
                .HasComment("Employees primary work location")
                .HasColumnName("default_location_id");
            entity.Property(e => e.Department)
                .HasMaxLength(100)
                .HasColumnName("department");
            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .HasColumnName("email");
            entity.Property(e => e.EmployeeCode)
                .HasMaxLength(50)
                .HasColumnName("employee_code");
            entity.Property(e => e.FullName)
                .HasMaxLength(255)
                .HasColumnName("full_name");
            entity.Property(e => e.IsActive)
                .HasDefaultValueSql("'1'")
                .HasColumnName("is_active");
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .HasColumnName("phone");
            entity.Property(e => e.UpdatedAt)
                .ValueGeneratedOnAddOrUpdate()
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.DefaultLocation).WithMany(p => p.Employees)
                .HasForeignKey(d => d.DefaultLocationId)
                .HasConstraintName("employees_ibfk_1");
        });

        modelBuilder.Entity<Equipment>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("equipment");

            entity.HasIndex(e => e.CreatedByUserId, "created_by_user_id");

            entity.HasIndex(e => e.EquipmentCode, "equipment_code").IsUnique();

            entity.HasIndex(e => e.QrCode, "idx_equipment_qr").IsUnique();

            entity.HasIndex(e => e.CurrentStatus, "idx_equipment_status");

            entity.HasIndex(e => e.ProcurementTypeId, "procurement_type_id");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedByUserId).HasColumnName("created_by_user_id");
            entity.Property(e => e.CurrentStatus)
                .HasDefaultValueSql("'in_store'")
                .HasColumnType("enum('in_store','allocated','decommissioned','under_repair')")
                .HasColumnName("current_status");
            entity.Property(e => e.EquipmentCode)
                .HasMaxLength(50)
                .HasColumnName("equipment_code");
            entity.Property(e => e.Make)
                .HasMaxLength(100)
                .HasColumnName("make");
            entity.Property(e => e.Model)
                .HasMaxLength(100)
                .HasColumnName("model");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasColumnName("name");
            entity.Property(e => e.Price)
                .HasPrecision(12, 2)
                .HasColumnName("price");
            entity.Property(e => e.ProcurementTypeId).HasColumnName("procurement_type_id");
            entity.Property(e => e.PurchaseDate).HasColumnName("purchase_date");
            entity.Property(e => e.QrCode).HasColumnName("qr_code");
            entity.Property(e => e.SerialNumber)
                .HasMaxLength(100)
                .HasColumnName("serial_number");
            entity.Property(e => e.Category)
                .HasMaxLength(100)
                .HasColumnName("category");
            entity.Property(e => e.Description)
                .HasColumnType("text")
                .HasColumnName("description");
            entity.Property(e => e.VendorName)
                .HasMaxLength(255)
                .HasColumnName("vendor_name");
            entity.Property(e => e.ConditionGrade)
                .HasColumnType("enum('New','Good','Fair','Poor','Unserviceable')")
                .HasColumnName("condition_grade");
            entity.Property(e => e.WarrantyExpiry).HasColumnName("warranty_expiry");
            entity.Property(e => e.Notes)
                .HasColumnType("text")
                .HasColumnName("notes");

            entity.HasOne(d => d.CreatedByUser).WithMany(p => p.Equipment)
                .HasForeignKey(d => d.CreatedByUserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("equipment_ibfk_2");

            entity.HasOne(d => d.ProcurementType).WithMany(p => p.Equipment)
                .HasForeignKey(d => d.ProcurementTypeId)
                .HasConstraintName("equipment_ibfk_1");
        });

        modelBuilder.Entity<EquipmentLocationHistory>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("equipment_location_history");

            entity.HasIndex(e => e.ChangedByUserId, "changed_by_user_id");

            entity.HasIndex(e => new { e.EquipmentId, e.EndDate }, "idx_location_history_active");

            entity.HasIndex(e => e.LocationId, "location_id");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AssignedDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp")
                .HasColumnName("assigned_date");
            entity.Property(e => e.ChangedByUserId).HasColumnName("changed_by_user_id");
            entity.Property(e => e.EndDate)
                .HasComment("NULL means currently active location")
                .HasColumnType("timestamp")
                .HasColumnName("end_date");
            entity.Property(e => e.EquipmentId).HasColumnName("equipment_id");
            entity.Property(e => e.LocationId).HasColumnName("location_id");
            entity.Property(e => e.Notes)
                .HasColumnType("text")
                .HasColumnName("notes");

            entity.HasOne(d => d.ChangedByUser).WithMany(p => p.EquipmentLocationHistories)
                .HasForeignKey(d => d.ChangedByUserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("equipment_location_history_ibfk_3");

            entity.HasOne(d => d.Equipment).WithMany(p => p.EquipmentLocationHistories)
                .HasForeignKey(d => d.EquipmentId)
                .HasConstraintName("equipment_location_history_ibfk_1");

            entity.HasOne(d => d.Location).WithMany(p => p.EquipmentLocationHistories)
                .HasForeignKey(d => d.LocationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("equipment_location_history_ibfk_2");
        });

        modelBuilder.Entity<EquipmentLog>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("equipment_logs");

            entity.HasIndex(e => e.ChangedByUserId, "changed_by_user_id");

            entity.HasIndex(e => e.CreatedAt, "idx_logs_created");

            entity.HasIndex(e => e.EquipmentId, "idx_logs_equipment");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ActionType)
                .HasColumnType("enum('equipment_created','location_changed','allocation_requested','allocation_approved','allocation_rejected','allocation_returned','decommission_requested','decommission_approved','decommission_rejected','transferred')")
                .HasColumnName("action_type");
            entity.Property(e => e.ChangedByUserId).HasColumnName("changed_by_user_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp")
                .HasColumnName("created_at");
            entity.Property(e => e.EquipmentId).HasColumnName("equipment_id");
            entity.Property(e => e.IpAddress)
                .HasMaxLength(45)
                .HasColumnName("ip_address");
            entity.Property(e => e.NewValue)
                .HasColumnType("text")
                .HasColumnName("new_value");
            entity.Property(e => e.OldValue)
                .HasColumnType("text")
                .HasColumnName("old_value");

            entity.HasOne(d => d.ChangedByUser).WithMany(p => p.EquipmentLogs)
                .HasForeignKey(d => d.ChangedByUserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("equipment_logs_ibfk_2");

            entity.HasOne(d => d.Equipment).WithMany(p => p.EquipmentLogs)
                .HasForeignKey(d => d.EquipmentId)
                .HasConstraintName("equipment_logs_ibfk_1");
        });

        modelBuilder.Entity<Location>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("locations");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Building)
                .HasMaxLength(100)
                .HasColumnName("building");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp")
                .HasColumnName("created_at");
            entity.Property(e => e.LocationName)
                .HasMaxLength(100)
                .HasComment("Lab, Kitchen, Server Room, Store")
                .HasColumnName("location_name");
            entity.Property(e => e.RoomNumber)
                .HasMaxLength(50)
                .HasColumnName("room_number");
            entity.Property(e => e.RoomName)
                .HasMaxLength(255)
                .HasColumnName("room_name");
        });

        modelBuilder.Entity<ProcurementType>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("procurement_types");

            entity.HasIndex(e => e.TypeName, "type_name").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp")
                .HasColumnName("created_at");
            entity.Property(e => e.TypeName)
                .HasMaxLength(50)
                .HasComment("Tender, Direct_Purchase, Loan, Donation")
                .HasColumnName("type_name");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("roles");

            entity.HasIndex(e => e.RoleName, "role_name").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp")
                .HasColumnName("created_at");
            entity.Property(e => e.Description)
                .HasColumnType("text")
                .HasColumnName("description");
            entity.Property(e => e.RoleName)
                .HasMaxLength(50)
                .HasComment("Admin, Operator")
                .HasColumnName("role_name");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("users");

            entity.HasIndex(e => e.Email, "email").IsUnique();

            entity.HasIndex(e => e.Username, "username").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp")
                .HasColumnName("created_at");
            entity.Property(e => e.Email).HasColumnName("email");
            entity.Property(e => e.FullName)
                .HasMaxLength(255)
                .HasColumnName("full_name");
            entity.Property(e => e.IsActive)
                .HasDefaultValueSql("'1'")
                .HasColumnName("is_active");
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(255)
                .HasColumnName("password_hash");
            entity.Property(e => e.UpdatedAt)
                .ValueGeneratedOnAddOrUpdate()
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp")
                .HasColumnName("updated_at");
            entity.Property(e => e.Username)
                .HasMaxLength(100)
                .HasColumnName("username");
        });

        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.RoleId })
                .HasName("PRIMARY")
                .HasAnnotation("MySql:IndexPrefixLength", new[] { 0, 0 });

            entity.ToTable("user_roles");

            entity.HasIndex(e => e.RoleId, "role_id");

            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.RoleId).HasColumnName("role_id");
            entity.Property(e => e.AssignedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp")
                .HasColumnName("assigned_at");

            entity.HasOne(d => d.Role).WithMany(p => p.UserRoles)
                .HasForeignKey(d => d.RoleId)
                .HasConstraintName("user_roles_ibfk_2");

            entity.HasOne(d => d.User).WithMany(p => p.UserRoles)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("user_roles_ibfk_1");
        });

        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.HasKey(e => e.TransactionId).HasName("PRIMARY");

            entity.ToTable("transactions");

            entity.HasIndex(e => e.EquipmentId, "equipment_id");
            entity.HasIndex(e => e.Status, "idx_transactions_status");
            entity.HasIndex(e => e.TransactionDate, "idx_transactions_date");

            entity.Property(e => e.TransactionId).HasColumnName("transaction_id");
            entity.Property(e => e.EquipmentId).HasColumnName("equipment_id");
            entity.Property(e => e.TransactionType)
                .HasColumnType("enum('Check-In','Check-Out','Transfer','Maintenance','Disposal')")
                .HasColumnName("transaction_type");
            entity.Property(e => e.IssuedTo)
                .HasMaxLength(255)
                .HasColumnName("issued_to");
            entity.Property(e => e.Department)
                .HasMaxLength(150)
                .HasColumnName("department");
            entity.Property(e => e.OperatorName)
                .HasMaxLength(255)
                .HasColumnName("operator_name");
            entity.Property(e => e.TransactionDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp")
                .HasColumnName("transaction_date");
            entity.Property(e => e.ExpectedReturn).HasColumnName("expected_return");
            entity.Property(e => e.ActualReturn).HasColumnName("actual_return");
            entity.Property(e => e.Status)
                .HasDefaultValueSql("'Pending'")
                .HasColumnType("enum('Pending','Completed','Overdue','Cancelled')")
                .HasColumnName("status");
            entity.Property(e => e.ConditionBefore)
                .HasColumnType("enum('New','Good','Fair','Poor','Unserviceable')")
                .HasColumnName("condition_before");
            entity.Property(e => e.ConditionAfter)
                .HasColumnType("enum('New','Good','Fair','Poor','Unserviceable')")
                .HasColumnName("condition_after");
            entity.Property(e => e.Remarks)
                .HasColumnType("text")
                .HasColumnName("remarks");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp")
                .HasColumnName("created_at");

            entity.HasOne(d => d.Equipment).WithMany(p => p.Transactions)
                .HasForeignKey(d => d.EquipmentId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("transactions_ibfk_1");
        });

        modelBuilder.Entity<MaintenanceRecord>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("maintenance_records");

            entity.HasIndex(e => e.EquipmentId, "idx_maintenance_equipment");
            entity.HasIndex(e => e.Status, "idx_maintenance_status");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.EquipmentId).HasColumnName("equipment_id");
            entity.Property(e => e.IssueDescription)
                .HasColumnType("text")
                .HasColumnName("issue_description");
            entity.Property(e => e.ReportedDate).HasColumnName("reported_date");
            entity.Property(e => e.ResolvedDate).HasColumnName("resolved_date");
            entity.Property(e => e.Cost)
                .HasPrecision(12, 2)
                .HasColumnName("cost");
            entity.Property(e => e.ServicedBy)
                .HasMaxLength(255)
                .HasColumnName("serviced_by");
            entity.Property(e => e.Status)
                .HasDefaultValueSql("'open'")
                .HasColumnType("enum('open','in_progress','resolved')")
                .HasColumnName("status");
            entity.Property(e => e.Notes)
                .HasColumnType("text")
                .HasColumnName("notes");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp")
                .HasColumnName("created_at");

            entity.HasOne(d => d.Equipment).WithMany(p => p.MaintenanceRecords)
                .HasForeignKey(d => d.EquipmentId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("maintenance_records_ibfk_1");
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");
            entity.ToTable("categories");
            entity.HasIndex(e => e.Name, "category_name").IsUnique();
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name).HasMaxLength(100).HasColumnName("name");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp")
                .HasColumnName("created_at");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}