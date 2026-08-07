//Models/EquipmentLog.cs
using System;
using System.Collections.Generic;

namespace Inventory_Management_System.Models;

public partial class EquipmentLog
{
    public int Id { get; set; }

    public int EquipmentId { get; set; }

    public string ActionType { get; set; } = null!;

    public int ChangedByUserId { get; set; }

    public string? OldValue { get; set; }

    public string? NewValue { get; set; }

    public string? IpAddress { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual User ChangedByUser { get; set; } = null!;

    public virtual Equipment Equipment { get; set; } = null!;
}
