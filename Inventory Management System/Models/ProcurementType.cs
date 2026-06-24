using System;
using System.Collections.Generic;

namespace Inventory_Management_System.Models;

public partial class ProcurementType
{
    public int Id { get; set; }

    /// <summary>
    /// Tender, Direct_Purchase, Loan, Donation
    /// </summary>
    public string TypeName { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<Equipment> Equipment { get; set; } = new List<Equipment>();
}
