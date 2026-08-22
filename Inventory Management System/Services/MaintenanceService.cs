// Services/MaintenanceService.cs
using System;
using System.Collections.Generic;
using System.Linq;
using Inventory_Management_System.Helpers;
using Inventory_Management_System.Models;

namespace Inventory_Management_System.Services
{
    public class MaintenanceService
    {
        // ── Get all records for an equipment, newest first ─────────────────

        private readonly AuditService _audit = new();
        public ServiceResult<List<MaintenanceRecord>> GetByEquipmentId(int equipmentId)
        {
            try
            {
                using var db = new InventoryDbContext();
                var list = db.MaintenanceRecords
                    .Where(r => r.EquipmentId == equipmentId)
                    .OrderByDescending(r => r.ReportedDate)
                    .ThenByDescending(r => r.CreatedAt)
                    .ToList();
                return ServiceResult<List<MaintenanceRecord>>.Ok(list);
            }
            catch (Exception ex)
            {
                return ServiceResult<List<MaintenanceRecord>>.Fail(
                    $"Error retrieving maintenance records: {ex.Message}");
            }
        }

        // ── Open a new maintenance record ──────────────────────────────────
        public ServiceResult<MaintenanceRecord> OpenRecord(
            int equipmentId,
            string issueDescription,
            string? servicedBy,
            decimal? cost)
        {
            if (string.IsNullOrWhiteSpace(issueDescription))
                return ServiceResult<MaintenanceRecord>.Fail(
                    "Issue description is required.");

            try
            {
                using var db = new InventoryDbContext();
                var record = new MaintenanceRecord
                {
                    EquipmentId       = equipmentId,
                    IssueDescription  = issueDescription.Trim(),
                    ServicedBy        = string.IsNullOrWhiteSpace(servicedBy)
                                            ? null : servicedBy.Trim(),
                    Cost              = cost,
                    Status            = "open",
                    ReportedDate      = DateOnly.FromDateTime(DateTime.Today),
                    // CreatedAt intentionally omitted — DB default CURRENT_TIMESTAMP handles it
                };
                db.MaintenanceRecords.Add(record);
                db.SaveChanges();
                _audit.Log(equipmentId, AuditService.ActionMaintenanceOpened,
       "Status: In Store",
       $"Issue: {record.IssueDescription} | Serviced By: {record.ServicedBy ?? "—"} | Est. Cost: {(cost.HasValue ? cost.Value.ToString("N2") : "—")} | Source Screen: Edit Equipment");
                return ServiceResult<MaintenanceRecord>.Ok(record);
            }
            catch (Exception ex)
            {
                var msg = ex.InnerException?.Message ?? ex.Message;
                return ServiceResult<MaintenanceRecord>.Fail(
                    $"Error opening maintenance record: {msg}");
            }
        }

        // ── Resolve an existing record ─────────────────────────────────────
        public ServiceResult<bool> ResolveRecord(
            int recordId,
            DateOnly resolvedDate,
            decimal? finalCost)
        {
            try
            {
                using var db = new InventoryDbContext();
                var record = db.MaintenanceRecords.FirstOrDefault(r => r.Id == recordId);
                if (record == null)
                    return ServiceResult<bool>.Fail("Maintenance record not found.");

                record.Status       = "resolved";
                record.ResolvedDate = resolvedDate;
                if (finalCost.HasValue)
                    record.Cost = finalCost;

                db.SaveChanges();
                _audit.Log(record.EquipmentId, AuditService.ActionMaintenanceResolved,
       $"Status: Under Repair | Issue: {record.IssueDescription}",
       $"Status: In Store | Resolved: {resolvedDate:yyyy-MM-dd} | Final Cost: {(finalCost.HasValue ? finalCost.Value.ToString("N2") : "—")} | Source Screen: Edit Equipment");
                return ServiceResult<bool>.Ok(true);
            }
            catch (Exception ex)
            {
                return ServiceResult<bool>.Fail(
                    $"Error resolving maintenance record: {ex.Message}");
            }
        }

        // ── Get the current open/in_progress record for an equipment ───────
        public ServiceResult<MaintenanceRecord?> GetOpenRecordForEquipment(int equipmentId)
        {
            try
            {
                using var db = new InventoryDbContext();
                var record = db.MaintenanceRecords
                    .Where(r => r.EquipmentId == equipmentId
                             && (r.Status == "open" || r.Status == "in_progress"))
                    .OrderByDescending(r => r.ReportedDate)
                    .ThenByDescending(r => r.CreatedAt)
                    .FirstOrDefault();
                return ServiceResult<MaintenanceRecord?>.Ok(record);
            }
            catch (Exception ex)
            {
                return ServiceResult<MaintenanceRecord?>.Fail(
                    $"Error retrieving open maintenance record: {ex.Message}");
            }
        }
    }
}
