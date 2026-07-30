// Services/AuditService.cs
using Inventory_Management_System.Models;
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

        private readonly int _systemUserId;

        public AuditService(int systemUserId = 1)
        {
            _systemUserId = systemUserId;
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
            catch
            {
                // Never let audit failure crash the main operation
            }
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