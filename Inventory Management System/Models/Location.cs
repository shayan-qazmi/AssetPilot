// Models/Location.cs
using System;
using System.Collections.Generic;

namespace Inventory_Management_System.Models;

public partial class Location
{
    public int Id { get; set; }

    public string? LocationName { get; set; }

    public string? Building { get; set; }

    public string? RoomNumber { get; set; }

    // NEW
    public string? RoomName { get; set; }

    public DateTime? CreatedAt { get; set; }


    public virtual ICollection<Allocation> Allocations { get; set; } = new List<Allocation>();

    public virtual ICollection<Employee> Employees { get; set; } = new List<Employee>();

    public virtual ICollection<EquipmentLocationHistory> EquipmentLocationHistories { get; set; } = new List<EquipmentLocationHistory>();
}