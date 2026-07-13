// Models/MaintenanceRecord.cs
using System;

namespace Inventory_Management_System.Models;

public partial class MaintenanceRecord
{
    public int Id { get; set; }

    public int EquipmentId { get; set; }

    public string IssueDescription { get; set; } = null!;

    public DateOnly ReportedDate { get; set; }

    public DateOnly? ResolvedDate { get; set; }

    public decimal? Cost { get; set; }

    public string? ServicedBy { get; set; }

    /// <summary>Values: "open" | "in_progress" | "resolved"</summary>
    public string Status { get; set; } = "open";

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Equipment Equipment { get; set; } = null!;
}
