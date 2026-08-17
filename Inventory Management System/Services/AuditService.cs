// Services/AuditService.cs
using Inventory_Management_System.Models;
using Inventory_Management_System.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management_System.Services
{
    public class AuditService
    {
        public const string ActionEquipmentCreated = "equipment_created";
        public const string ActionEquipmentUpdated = "equipment_updated";
        public const string ActionLocationChanged = "location_changed";
        public const string ActionAllocationRequested = "allocation_requested";
        public const string ActionAllocationApproved = "allocation_approved";
        public const string ActionAllocationRejected = "allocation_rejected";
        public const string ActionAllocationReturned = "allocation_returned";
        public const string ActionDecommissionRequested = "decommission_requested";
        public const string ActionDecommissionApproved = "decommission_approved";
        public const string ActionDecommissionUndone = "decommission_undone";
        public const string ActionDecommissionRejected = "decommission_rejected";
        public const string ActionTransferred = "transferred";
        public const string ActionEmployeeCreated = "employee_created";
        public const string ActionEmployeeUpdated = "employee_updated";
        public const string ActionEmployeeDeleted = "employee_deleted";
        public const string ActionLocationCreated = "location_created";
        public const string ActionLocationUpdated = "location_updated";
        public const string ActionLocationDeleted = "location_deleted";
        public const string ActionCheckedOut = "checked_out";
        public const string ActionCheckedIn = "checked_in";
        public const string ActionEquipmentDeleted = "equipment_deleted";
        public const string ActionEquipmentRestored = "equipment_restored";
        public const string ActionMaintenanceOpened = "maintenance_opened";
        public const string ActionMaintenanceResolved = "maintenance_resolved";

        private readonly int _systemUserId;

        public AuditService(int? systemUserId = null)
        {
            _systemUserId = systemUserId ?? AuthService.CurrentUserId;
        }

        public void Log(int equipmentId, string actionType, string? oldValue = null, string? newValue = null)
        {
            try
            {
                using var db = new InventoryDbContext();
                var log = new EquipmentLog
                {
                    EquipmentId = equipmentId,
                    ActionType = actionType,
                    ChangedByUserId = _systemUserId,
                    OldValue = oldValue,
                    NewValue = newValue,
                    CreatedAt = DateTime.UtcNow
                };
                db.EquipmentLogs.Add(log);
                db.SaveChanges();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "AuditService.Log");
            }
        }

        /// Compares two Equipment snapshots and returns short "Field: old" / "Field: new"
        /// summaries containing ONLY the fields that actually changed.
        public static (string? OldSummary, string? NewSummary) BuildEquipmentDiff(Equipment before, Equipment after)
        {
            var oldParts = new List<string>();
            var newParts = new List<string>();

            void Cmp(string label, string? oldVal, string? newVal)
            {
                string o = string.IsNullOrWhiteSpace(oldVal) ? "—" : oldVal;
                string n = string.IsNullOrWhiteSpace(newVal) ? "—" : newVal;
                if (o == n) return;
                oldParts.Add($"{label}: {o}");
                newParts.Add($"{label}: {n}");
            }

            static string FmtStatus(string? s) => s switch
            {
                "in_store" => "In Store",
                "allocated" => "Allocated",
                "decommissioned" => "Decommissioned",
                "under_repair" => "Under Repair",
                _ => s ?? "—"
            };

            Cmp("Name", before.Name, after.Name);
            Cmp("Make", before.Make, after.Make);
            Cmp("Model", before.Model, after.Model);
            Cmp("Serial Number", before.SerialNumber, after.SerialNumber);
            Cmp("Status", FmtStatus(before.CurrentStatus), FmtStatus(after.CurrentStatus));
            Cmp("Category", before.Category, after.Category);
            Cmp("Purchase Date", before.PurchaseDate?.ToString("yyyy-MM-dd"), after.PurchaseDate?.ToString("yyyy-MM-dd"));
            Cmp("Price", before.Price?.ToString("N2"), after.Price?.ToString("N2"));
            Cmp("Description", before.Description, after.Description);
            Cmp("Vendor", before.VendorName, after.VendorName);
            Cmp("Condition", before.ConditionGrade, after.ConditionGrade);
            Cmp("Warranty Expiry", before.WarrantyExpiry?.ToString("yyyy-MM-dd"), after.WarrantyExpiry?.ToString("yyyy-MM-dd"));
            Cmp("Notes", before.Notes, after.Notes);

            if (before.PicturePath != after.PicturePath)
            {
                string state = string.IsNullOrEmpty(before.PicturePath) ? "added"
                              : string.IsNullOrEmpty(after.PicturePath) ? "removed"
                              : "replaced";
                oldParts.Add($"Picture: {(string.IsNullOrEmpty(before.PicturePath) ? "none" : "present")}");
                newParts.Add($"Picture: {state}");
            }

            if (oldParts.Count == 0) return (null, "No changes made");
            return (string.Join(" | ", oldParts), string.Join(" | ", newParts));
        }

        public List<EquipmentLog> GetAll()
        {
            try
            {
                using var db = new InventoryDbContext();
                return db.EquipmentLogs
                    .Include(l => l.Equipment)
                    .Include(l => l.ChangedByUser)
                    .OrderByDescending(l => l.CreatedAt)
                    .ToList();
            }
            catch
            {
                return new List<EquipmentLog>();
            }
        }

        public List<EquipmentLog> GetByEquipment(int equipmentId)
        {
            try
            {
                using var db = new InventoryDbContext();
                return db.EquipmentLogs
                    .Include(l => l.ChangedByUser)
                    .Where(l => l.EquipmentId == equipmentId)
                    .OrderByDescending(l => l.CreatedAt)
                    .ToList();
            }
            catch
            {
                return new List<EquipmentLog>();
            }
        }
    }
}