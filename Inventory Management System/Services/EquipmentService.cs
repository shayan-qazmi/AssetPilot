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
                    .Include(e => e.ProcurementType)
                    .FirstOrDefault(e => e.Id == id);
                return eq == null
                    ? ServiceResult<Equipment>.Fail("Equipment not found.")
                    : ServiceResult<Equipment>.Ok(eq);
            }
            catch (Exception ex)
            {
                var innerMsg = ex.InnerException?.Message ?? ex.Message;
                return ServiceResult<Equipment>.Fail($"Error retrieving equipment: {innerMsg}");
            }
        }


        public ServiceResult<List<Equipment>> Search(string term)
        {
            try
            {
                using var db = new InventoryDbContext();
                var allItems = db.Equipment
                    .Include(e => e.Allocations).ThenInclude(a => a.Location)
                    .Include(e => e.ProcurementType)
                    .ToList();

                var list = allItems
                    .Where(e => FuzzySearchHelper.IsFuzzyMatch(e.Name, term)
                             || FuzzySearchHelper.IsFuzzyMatch(e.EquipmentCode, term)
                             || FuzzySearchHelper.IsFuzzyMatch(e.SerialNumber, term)
                             || FuzzySearchHelper.IsFuzzyMatch(e.Make, term)
                             || FuzzySearchHelper.IsFuzzyMatch(e.Model, term))
                    .OrderByDescending(e => Math.Max(
                        FuzzySearchHelper.FuzzyScore(e.Name, term),
                        Math.Max(
                            FuzzySearchHelper.FuzzyScore(e.EquipmentCode, term),
                            Math.Max(
                                FuzzySearchHelper.FuzzyScore(e.SerialNumber, term),
                                Math.Max(
                                    FuzzySearchHelper.FuzzyScore(e.Make, term),
                                    FuzzySearchHelper.FuzzyScore(e.Model, term)
                                )
                            )
                        )
                    ))
                    .ToList();

                return ServiceResult<List<Equipment>>.Ok(list);
            }
            catch (Exception ex)
            {
                return ServiceResult<List<Equipment>>.Fail($"Error searching equipment: {ex.Message}");
            }
        }

        // ── Auto-generation helpers ─────────────────────────────────────────
        // Equipment Code and QR Code are no longer typed manually in the UI.
        // These generate sequential, collision-checked identifiers.

        private string GenerateEquipmentCode(InventoryDbContext db)
        {
            var codes = db.Equipment
                .Select(e => e.EquipmentCode)
                .Where(c => c != null && c.StartsWith("EQ"))
                .ToList();

            int maxNum = 0;
            foreach (var c in codes)
            {
                var digits = new string(c!.Skip(2).TakeWhile(char.IsDigit).ToArray());
                if (int.TryParse(digits, out int n) && n > maxNum) maxNum = n;
            }

            string candidate;
            int next = maxNum + 1;
            do
            {
                candidate = $"EQ{next:000}";
                next++;
            } while (db.Equipment.Any(e => e.EquipmentCode == candidate));

            return candidate;
        }

        private string GenerateQrCode(InventoryDbContext db)
        {
            var codes = db.Equipment
                .Select(e => e.QrCode)
                .Where(c => c != null && c.StartsWith("QR"))
                .ToList();

            int maxNum = 0;
            foreach (var c in codes)
            {
                var digits = new string(c!.Skip(2).TakeWhile(char.IsDigit).ToArray());
                if (int.TryParse(digits, out int n) && n > maxNum) maxNum = n;
            }

            string candidate;
            int next = maxNum + 1;
            do
            {
                candidate = $"QR{next:000}";
                next++;
            } while (db.Equipment.Any(e => e.QrCode == candidate));

            return candidate;
        }

        // ── Procurement Type: resolve-or-create by typed name ───────────────
        // Supports converting the Add/Edit form's ComboBox to a plain TextBox
        // while preserving the ProcurementTypeId foreign key relationship.

        public int? ResolveProcurementTypeId(string? typeName)
        {
            if (string.IsNullOrWhiteSpace(typeName)) return null;

            string trimmed = typeName.Trim();

            using var db = new InventoryDbContext();
            var existing = db.ProcurementTypes
                .FirstOrDefault(p => p.TypeName.ToLower() == trimmed.ToLower());
            if (existing != null) return existing.Id;

            var created = new ProcurementType { TypeName = trimmed };
            db.ProcurementTypes.Add(created);
            db.SaveChanges();
            return created.Id;
        }

        public string? GetProcurementTypeName(int? id)
        {
            if (id == null) return null;
            using var db = new InventoryDbContext();
            return db.ProcurementTypes.FirstOrDefault(p => p.Id == id)?.TypeName;
        }

        public ServiceResult<Equipment> Add(Equipment equipment)
        {
            try
            {
                // Validation — Equipment Code and QR Code are no longer
                // required from the caller; they're auto-generated below.
                if (string.IsNullOrWhiteSpace(equipment.Name))
                    return ServiceResult<Equipment>.Fail("Equipment name is required.");
                if (string.IsNullOrWhiteSpace(equipment.SerialNumber))
                    return ServiceResult<Equipment>.Fail("Serial number is required.");

                equipment.SerialNumber  = equipment.SerialNumber.Trim().ToUpper();
                equipment.Name          = equipment.Name.Trim();

                using var db = new InventoryDbContext();

                if (string.IsNullOrWhiteSpace(equipment.EquipmentCode))
                    equipment.EquipmentCode = GenerateEquipmentCode(db);
                else
                    equipment.EquipmentCode = equipment.EquipmentCode.Trim().ToUpper();

                if (string.IsNullOrWhiteSpace(equipment.QrCode))
                    equipment.QrCode = GenerateQrCode(db);

                // Uniqueness checks — kept as a safety net even though codes
                // are now auto-generated (also still applies to bulk imports
                // via AddRange, which may supply their own codes).
                if (db.Equipment.Any(e => e.EquipmentCode == equipment.EquipmentCode))
                    return ServiceResult<Equipment>.Fail($"Equipment code '{equipment.EquipmentCode}' already exists.");
                if (db.Equipment.Any(e => e.SerialNumber == equipment.SerialNumber))
                    return ServiceResult<Equipment>.Fail($"Serial number '{equipment.SerialNumber}' already exists.");

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
                existing.Category      = equipment.Category;
                existing.Description   = equipment.Description;
                existing.VendorName    = equipment.VendorName;
                existing.ConditionGrade = equipment.ConditionGrade;
                existing.WarrantyExpiry = equipment.WarrantyExpiry;
                existing.Notes          = equipment.Notes;
                existing.ProcurementTypeId = equipment.ProcurementTypeId;

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
                    .Include(e => e.Transactions)
                    .FirstOrDefault(e => e.Id == id);
                if (existing == null)
                    return ServiceResult<bool>.Fail("Equipment not found.");

                // Block if any allocation is currently active (checked out).
                bool hasActiveAllocation = existing.Allocations
                    .Any(a => a.Status != null &&
                              (a.Status.Equals("approved", StringComparison.OrdinalIgnoreCase) ||
                               a.Status.Equals("pending",  StringComparison.OrdinalIgnoreCase)));
                if (hasActiveAllocation)
                    return ServiceResult<bool>.Fail("Cannot delete equipment that is currently allocated.");

                // Block if there's ANY allocation or transaction history at all
                // (even closed/returned records). Deleting equipment with history
                // would either fail on the FK constraint or silently destroy the
                // audit trail. Point the user to Decommission instead, which
                // retires the equipment while preserving its records.
                bool hasAnyHistory = existing.Allocations.Any() || existing.Transactions.Any();
                if (hasAnyHistory)
                    return ServiceResult<bool>.Fail(
                        "This equipment has allocation or transaction history and cannot be permanently deleted. " +
                        "Use Decommission instead to retire it while preserving its records.");

                db.Equipment.Remove(existing);
                db.SaveChanges();

                return ServiceResult<bool>.Ok(true);
            }
            catch (Exception ex)
            {
                var innerMsg = ex.InnerException?.Message ?? ex.Message;
                return ServiceResult<bool>.Fail($"Error deleting equipment: {innerMsg}");
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