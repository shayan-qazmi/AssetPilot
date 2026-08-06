using System;
using System.Collections.Generic;

namespace Inventory_Management_System.Models;

public partial class Employee
{
    public int Id { get; set; }

    public string EmployeeCode { get; set; } = null!;

    public string FullName { get; set; } = null!;

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public string? Department { get; set; }

    public int? DefaultLocationId { get; set; }

    public bool IsActive { get; set; }

    public string? PicturePath { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Location? DefaultLocation { get; set; }

    public virtual ICollection<Allocation> Allocations { get; set; } = new List<Allocation>();
}