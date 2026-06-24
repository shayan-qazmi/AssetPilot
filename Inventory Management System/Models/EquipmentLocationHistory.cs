using System;
using System.Collections.Generic;

namespace Inventory_Management_System.Models;

public partial class EquipmentLocationHistory
{
    public int Id { get; set; }

    public int EquipmentId { get; set; }

    public int LocationId { get; set; }

    public DateTime AssignedDate { get; set; }

    /// <summary>
    /// NULL means currently active location
    /// </summary>
    public DateTime? EndDate { get; set; }

    public int ChangedByUserId { get; set; }

    public string? Notes { get; set; }

    public virtual User ChangedByUser { get; set; } = null!;

    public virtual Equipment Equipment { get; set; } = null!;

    public virtual Location Location { get; set; } = null!;
}
