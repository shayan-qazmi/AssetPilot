// Models/Equipment.cs
using System;
using System.Collections.Generic;

namespace Inventory_Management_System.Models;

public partial class Equipment
{
    public int Id { get; set; }

    public string EquipmentCode { get; set; } = null!;

    public string QrCode { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? Make { get; set; }

    public string? Model { get; set; }

    public string? SerialNumber { get; set; }

    public DateOnly? PurchaseDate { get; set; }

    public decimal? Price { get; set; }

    public int? ProcurementTypeId { get; set; }

    public int CreatedByUserId { get; set; }

    public string CurrentStatus { get; set; } = null!;

    public string? Category       { get; set; }

    public string? Description    { get; set; }

    public string? VendorName     { get; set; }

    public string? ConditionGrade { get; set; }

    public DateOnly? WarrantyExpiry { get; set; }

    public string? Notes          { get; set; }

    public virtual ICollection<Allocation> Allocations { get; set; } = new List<Allocation>();

    public virtual User CreatedByUser { get; set; } = null!;

    public virtual ICollection<EquipmentLocationHistory> EquipmentLocationHistories { get; set; } = new List<EquipmentLocationHistory>();

    public virtual ICollection<EquipmentLog> EquipmentLogs { get; set; } = new List<EquipmentLog>();

    public virtual ProcurementType? ProcurementType { get; set; }

    public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}
