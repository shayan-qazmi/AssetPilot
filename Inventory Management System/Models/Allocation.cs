//Models/Allocation.cs
using System;
using System.Collections.Generic;

namespace Inventory_Management_System.Models;

public partial class Allocation
{
    public int Id { get; set; }

    public int EquipmentId { get; set; }

    public int? EmployeeId { get; set; }

    public int? LocationId { get; set; }

    public DateTime AllocationDate { get; set; }

    public DateOnly? ExpectedReturnDate { get; set; }

    public DateOnly? ActualReturnDate { get; set; }

    public string Status { get; set; } = null!;

    public int AllocatedByUserId { get; set; }

    public int? ApprovedByUserId { get; set; }

    public DateTime? ApprovalDate { get; set; }

    public string? RejectionReason { get; set; }

    public int? PreviousAllocationId { get; set; }

    public virtual User AllocatedByUser { get; set; } = null!;

    public virtual User? ApprovedByUser { get; set; }

    public virtual Employee? Employee { get; set; }

    public virtual Equipment Equipment { get; set; } = null!;

    public virtual Location? Location { get; set; }

    public virtual Allocation? PreviousAllocation { get; set; }

    public virtual ICollection<Allocation> InversePreviousAllocation { get; set; } = new List<Allocation>();
}