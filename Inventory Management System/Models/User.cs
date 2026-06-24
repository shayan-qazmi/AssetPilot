using System;
using System.Collections.Generic;

namespace Inventory_Management_System.Models;

public partial class User
{
    public int Id { get; set; }

    public string Username { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string? Email { get; set; }

    public string FullName { get; set; } = null!;

    public bool? IsActive { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<Allocation> AllocationAllocatedByUsers { get; set; } = new List<Allocation>();

    public virtual ICollection<Allocation> AllocationApprovedByUsers { get; set; } = new List<Allocation>();

    public virtual ICollection<Equipment> Equipment { get; set; } = new List<Equipment>();

    public virtual ICollection<EquipmentLocationHistory> EquipmentLocationHistories { get; set; } = new List<EquipmentLocationHistory>();

    public virtual ICollection<EquipmentLog> EquipmentLogs { get; set; } = new List<EquipmentLog>();

    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}
