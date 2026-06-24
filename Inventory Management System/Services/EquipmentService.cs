// Services/EquipmentService.cs
using Inventory_Management_System.Helpers;
using Inventory_Management_System.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Inventory_Management_System.Services
{
    public class EquipmentService
    {
        private readonly AuditService _audit;

        public EquipmentService(int currentUserId = 1)
        {
            _audit = new AuditService(currentUserId);
        }

        public ServiceResult<List<Equipment>> GetAll()
        {
            try
            {
                using var db = new InventoryDbContext();
                var list = db.Equipment
                    .Include(e => e.Allocations)
                        .ThenInclude(a => a.Location)
                    .Include(e => e.ProcurementType)
                    .OrderBy(e => e.EquipmentCode)
                    .ToList();
                return ServiceResult<List<Equipment>>.Ok(list);
            }
            catch (Exception ex)
            {
                return ServiceResult<List<Equipment>>.Fail($"Error retrieving equipment: {ex.Message}");
            }
        }

        public ServiceResult<Equipment> GetById(int id)
        {
            try
            {
                using var db = new InventoryDbContext();
                var eq = db.Equipment
                    .Include(e => e.Allocations).ThenInclude(a => a.Location)
                    .FirstOrDefault(e => e.Id == id);
                return eq == null
                    ? ServiceResult<Equipment>.Fail("Equipment not found.")
                    : ServiceResult<Equipment>.Ok(eq);
            }
            catch (Exception ex)
            {
                var innerMsg = ex.InnerException?.Message ?? ex.Message;
                return ServiceResult<Equipment>.Fail($"Error adding equipment: {innerMsg}");
            }
        }
        

        public ServiceResult<List<Equipment>> Search(string term)
        {
            try
            {
                using var db = new InventoryDbContext();
                term = term.ToLower();
                var list = db.Equipment
                    .Include(e => e.Allocations).ThenInclude(a => a.Location)
                    .Where(e => e.Name.ToLower().Contains(term)
                             || e.EquipmentCode.ToLower().Contains(term)
                             || (e.SerialNumber != null && e.SerialNumber.ToLower().Contains(term))
                             || (e.Make != null && e.Make.ToLower().Contains(term))
                             || (e.Model != null && e.Model.ToLower().Contains(term)))
                    .OrderBy(e => e.EquipmentCode)
                    .ToList();
                return ServiceResult<List<Equipment>>.Ok(list);
            }
            catch (Exception ex)
            {
                return ServiceResult<List<Equipment>>.Fail($"Error searching equipment: {ex.Message}");
            }
        }

        public ServiceResult<Equipment> Add(Equipment equipment)
        {
            try
            {
                // Validation
                if (string.IsNullOrWhiteSpace(equipment.Name))
                    return ServiceResult<Equipment>.Fail("Equipment name is required.");
                if (string.IsNullOrWhiteSpace(equipment.EquipmentCode))
                    return ServiceResult<Equipment>.Fail("Equipment code is required.");
                if (string.IsNullOrWhiteSpace(equipment.SerialNumber))
                    return ServiceResult<Equipment>.Fail("Serial number is required.");

                // Normalize
                equipment.EquipmentCode = equipment.EquipmentCode.Trim().ToUpper();
                equipment.SerialNumber  = equipment.SerialNumber.Trim().ToUpper();
                equipment.Name          = equipment.Name.Trim();

                using var db = new InventoryDbContext();

                // Uniqueness checks
                if (db.Equipment.Any(e => e.EquipmentCode == equipment.EquipmentCode))
                    return ServiceResult<Equipment>.Fail($"Equipment code '{equipment.EquipmentCode}' already exists.");
                if (db.Equipment.Any(e => e.SerialNumber == equipment.SerialNumber))
                    return ServiceResult<Equipment>.Fail($"Serial number '{equipment.SerialNumber}' already exists.");

                // Default status
                if (string.IsNullOrWhiteSpace(equipment.CurrentStatus))
                    equipment.CurrentStatus = "in_store";

                db.Equipment.Add(equipment);
                db.SaveChanges();

                _audit.Log(equipment.Id, AuditService.ActionEquipmentCreated,
                    null, $"Created: {equipment.EquipmentCode} - {equipment.Name}");

                return ServiceResult<Equipment>.Ok(equipment);
            }
            catch (Exception ex)
            {
                return ServiceResult<Equipment>.Fail($"Error adding equipment: {ex.Message}");
            }
        }

        public ServiceResult<Equipment> Update(Equipment equipment)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(equipment.Name))
                    return ServiceResult<Equipment>.Fail("Equipment name is required.");

                equipment.EquipmentCode = equipment.EquipmentCode.Trim().ToUpper();
                equipment.SerialNumber  = equipment.SerialNumber?.Trim().ToUpper() ?? string.Empty;

                using var db = new InventoryDbContext();
                var existing = db.Equipment.FirstOrDefault(e => e.Id == equipment.Id);
                if (existing == null)
                    return ServiceResult<Equipment>.Fail("Equipment not found.");

                // Uniqueness checks excluding self
                if (db.Equipment.Any(e => e.EquipmentCode == equipment.EquipmentCode && e.Id != equipment.Id))
                    return ServiceResult<Equipment>.Fail($"Equipment code '{equipment.EquipmentCode}' already exists.");
                if (db.Equipment.Any(e => e.SerialNumber == equipment.SerialNumber && e.Id != equipment.Id))
                    return ServiceResult<Equipment>.Fail($"Serial number '{equipment.SerialNumber}' already exists.");

                string oldValue = $"{existing.EquipmentCode} - {existing.Name} - {existing.CurrentStatus}";

                existing.EquipmentCode = equipment.EquipmentCode;
                existing.QrCode        = equipment.QrCode;
                existing.Name          = equipment.Name;
                existing.Make          = equipment.Make;
                existing.Model         = equipment.Model;
                existing.SerialNumber  = equipment.SerialNumber;
                existing.CurrentStatus = equipment.CurrentStatus;
                existing.PurchaseDate  = equipment.PurchaseDate;
                existing.Price         = equipment.Price;

                db.SaveChanges();

                _audit.Log(existing.Id, AuditService.ActionLocationChanged,
                    oldValue, $"{existing.EquipmentCode} - {existing.Name} - {existing.CurrentStatus}");

                return ServiceResult<Equipment>.Ok(existing);
            }
            catch (Exception ex)
            {
                var innerMsg = ex.InnerException?.Message ?? ex.Message;
                return ServiceResult<Equipment>.Fail($"Error adding equipment: {innerMsg}");
            }
        }

        public ServiceResult<bool> Delete(int id)
        {
            try
            {
                using var db = new InventoryDbContext();
                var existing = db.Equipment
                    .Include(e => e.Allocations)
                    .FirstOrDefault(e => e.Id == id);
                if (existing == null)
                    return ServiceResult<bool>.Fail("Equipment not found.");

                bool hasActiveAllocation = existing.Allocations
                    .Any(a => a.Status == "approved" || a.Status == "pending");
                if (hasActiveAllocation)
                    return ServiceResult<bool>.Fail("Cannot delete equipment that is currently allocated.");

                db.Equipment.Remove(existing);
                db.SaveChanges();

                return ServiceResult<bool>.Ok(true);
            }
            catch (Exception ex)
            {
                return ServiceResult<bool>.Fail($"Error deleting equipment: {ex.Message}");
            }
        }

        public ServiceResult<int> AddRange(IEnumerable<Equipment> items)
        {
            int successCount = 0;
            var errors       = new List<string>();

            foreach (var item in items)
            {
                var result = Add(item);
                if (result.Success)
                    successCount++;
                else
                    errors.Add($"{item.EquipmentCode}: {result.ErrorMessage}");
            }

            if (successCount == 0 && errors.Count > 0)
                return ServiceResult<int>.Fail(string.Join("\n", errors));

            return ServiceResult<int>.Ok(successCount);
        }
    }
}