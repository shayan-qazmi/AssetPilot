using System;
using System.Collections.Generic;

namespace Inventory_Management_System.Models;

public partial class Role
{
    public int Id { get; set; }

    /// <summary>
    /// Admin, Operator
    /// </summary>
    public string RoleName { get; set; } = null!;

    public string? Description { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}
