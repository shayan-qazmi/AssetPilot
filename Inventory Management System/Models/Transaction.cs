// Models/Transaction.cs
using System;
using System.Collections.Generic;

namespace Inventory_Management_System.Models;

public partial class Transaction
{
    /// <summary>transaction_id — primary key</summary>
    public int TransactionId { get; set; }

    /// <summary>equipment_id — FK to equipment</summary>
    public int EquipmentId { get; set; }

    /// <summary>transaction_type ENUM('Check-In','Check-Out','Transfer','Maintenance','Disposal')</summary>
    public string? TransactionType { get; set; }

    /// <summary>issued_to — person the equipment is issued to</summary>
    public string? IssuedTo { get; set; }

    /// <summary>department</summary>
    public string? Department { get; set; }

    /// <summary>operator_name — staff member recording the transaction</summary>
    public string? OperatorName { get; set; }

    /// <summary>transaction_date — when the transaction occurred</summary>
    public DateTime TransactionDate { get; set; }

    /// <summary>expected_return — nullable date the item should come back</summary>
    public DateOnly? ExpectedReturn { get; set; }

    /// <summary>actual_return — nullable date the item was actually returned</summary>
    public DateOnly? ActualReturn { get; set; }

    /// <summary>status ENUM('Pending','Completed','Overdue','Cancelled')</summary>
    public string? Status { get; set; }

    /// <summary>condition_before ENUM — equipment condition at check-out</summary>
    public string? ConditionBefore { get; set; }

    /// <summary>condition_after ENUM — equipment condition at check-in</summary>
    public string? ConditionAfter { get; set; }

    /// <summary>remarks — free-text notes</summary>
    public string? Remarks { get; set; }

    /// <summary>created_at — row creation timestamp</summary>
    public DateTime CreatedAt { get; set; }

    public DateTime? ReturnProcessedAt { get; set; }

    // ── Navigation ────────────────────────────────────────────────────────
    public virtual Equipment Equipment { get; set; } = null!;
}
