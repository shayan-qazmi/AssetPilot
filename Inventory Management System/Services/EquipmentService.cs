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
        private readonly int _currentUserId;

        public EquipmentService(int? currentUserId = null)
        {
            _currentUserId = currentUserId ?? AuthService.CurrentUserId;
            _audit = new AuditService(_currentUserId);
        }

        public ServiceResult<List<Equipment>> GetAll()
        {
            try
            {
                using var db = new InventoryDbContext();
                var list = db.Equipment
                    .Where(e => !e.IsDeleted)
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


        private static int ExtractCodeNumber(string code)
        {
            if (string.IsNullOrWhiteSpace(code)) return int.MaxValue;
            var digits = new string(code.Where(char.IsDigit).ToArray());
            return int.TryParse(digits, out int n) ? n : int.MaxValue;
        }

        public ServiceResult<PagedResult<Equipment>> GetPage(int pageNumber, int pageSize, string category = null)
        {
            try
            {
                using var db = new InventoryDbContext();
                var query = db.Equipment.Where(e => !e.IsDeleted);

                if (!string.IsNullOrWhiteSpace(category))
                    query = query.Where(e => e.Category == category);

                query = query
                    .Include(e => e.Allocations).ThenInclude(a => a.Location)
                    .Include(e => e.ProcurementType);

                var all = query.ToList()
                    .OrderBy(e => ExtractCodeNumber(e.EquipmentCode))
                    .ToList();

                var totalCount = all.Count;
                var items = all.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();

                return ServiceResult<PagedResult<Equipment>>.Ok(new PagedResult<Equipment>
                {
                    Items = items,
                    TotalCount = totalCount,
                    PageNumber = pageNumber,
                    PageSize = pageSize
                });
            }
            catch (Exception ex)
            {
                return ServiceResult<PagedResult<Equipment>>.Fail($"Error paging equipment: {ex.Message}");
            }
        }

        public ServiceResult<Equipment> GetById(int id)
        {
            try
            {
                using var db = new InventoryDbContext();
                var eq = db.Equipment
                    .Where(e => !e.IsDeleted)
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

        public ServiceResult<int> DeleteRange(IEnumerable<int> ids)
        {
            try
            {
                using var db = new InventoryDbContext();
                var idList = ids.ToList();

                var items = db.Equipment.Where(e => idList.Contains(e.Id)).ToList();
                if (items.Count == 0)
                    return ServiceResult<int>.Fail("No matching equipment found.");

                foreach (var eq in items)
                    eq.IsDeleted = true;

                db.SaveChanges();

                foreach (var eq in items)
                    _audit.Log(eq.Id, AuditService.ActionEquipmentDeleted,
                        $"{eq.EquipmentCode} - {eq.Name}",
                        $"Category: {eq.Category ?? "—"} | Status: {eq.CurrentStatus}");

                return ServiceResult<int>.Ok(items.Count);
            }
            catch (Exception ex)
            {
                return ServiceResult<int>.Fail($"Bulk delete failed: {ex.Message}");
            }
        }

        public ServiceResult<List<Equipment>> Search(string term)
        {
            try
            {
                using var db = new InventoryDbContext();
                var allItems = db.Equipment
                    .Where(e => !e.IsDeleted)
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

        private int GetNextEquipmentNumber(InventoryDbContext db)
        {
            var usedNumbers = new HashSet<int>();

            foreach (var c in db.Equipment.Select(e => e.EquipmentCode).Where(c => c != null && c.StartsWith("EQ")).ToList())
            {
                var digits = new string(c!.Skip(2).TakeWhile(char.IsDigit).ToArray());
                if (int.TryParse(digits, out int n)) usedNumbers.Add(n);
            }
            foreach (var c in db.Equipment.Select(e => e.QrCode).Where(c => c != null && c.StartsWith("QR")).ToList())
            {
                var digits = new string(c!.Skip(2).TakeWhile(char.IsDigit).ToArray());
                if (int.TryParse(digits, out int n)) usedNumbers.Add(n);
            }

            int candidate = 1;
            while (usedNumbers.Contains(candidate)) candidate++;
            return candidate;
        }

        private (string EquipmentCode, string QrCode) GenerateCodes(InventoryDbContext db)
        {
            int next = GetNextEquipmentNumber(db);
            string eqCandidate, qrCandidate;
            do
            {
                eqCandidate = $"EQ{next:000}";
                qrCandidate = $"QR{next:000}";
                next++;
            } while (db.Equipment.Any(e => e.EquipmentCode == eqCandidate || e.QrCode == qrCandidate));

            return (eqCandidate, qrCandidate);
        }

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
                if (string.IsNullOrWhiteSpace(equipment.Name))
                    return ServiceResult<Equipment>.Fail("Equipment name is required.");

                // Normalise serial number: treat "N/A", "NA", "-" and whitespace-only as null
                if (!string.IsNullOrWhiteSpace(equipment.SerialNumber))
                {
                    var sn = equipment.SerialNumber.Trim();
                    if (sn.Equals("N/A", StringComparison.OrdinalIgnoreCase) ||
                        sn.Equals("NA", StringComparison.OrdinalIgnoreCase) ||
                        sn == "-")
                        equipment.SerialNumber = null;
                    else
                        equipment.SerialNumber = sn.ToUpper();
                }
                else
                {
                    equipment.SerialNumber = null;
                }
                equipment.Name = equipment.Name.Trim();

                using var db = new InventoryDbContext();

                if (string.IsNullOrWhiteSpace(equipment.EquipmentCode) && string.IsNullOrWhiteSpace(equipment.QrCode))
                {
                    // Neither provided — generate both using the SAME number,
                    // so EQ and QR codes can never drift apart again.
                    var (genCode, genQr) = GenerateCodes(db);
                    equipment.EquipmentCode = genCode;
                    equipment.QrCode = genQr;
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(equipment.EquipmentCode))
                        equipment.EquipmentCode = GenerateCodes(db).EquipmentCode;
                    else
                        equipment.EquipmentCode = equipment.EquipmentCode.Trim().ToUpper();

                    if (string.IsNullOrWhiteSpace(equipment.QrCode))
                        equipment.QrCode = GenerateCodes(db).QrCode;
                }   

                if (db.Equipment.Any(e => e.EquipmentCode == equipment.EquipmentCode))
                    return ServiceResult<Equipment>.Fail($"Equipment code '{equipment.EquipmentCode}' already exists.");
                if (!string.IsNullOrWhiteSpace(equipment.SerialNumber) &&
                    db.Equipment.Any(e => e.SerialNumber == equipment.SerialNumber))
                    return ServiceResult<Equipment>.Fail($"Serial number '{equipment.SerialNumber}' already exists.");

                if (string.IsNullOrWhiteSpace(equipment.CurrentStatus))
                    equipment.CurrentStatus = "in_store";
                if (equipment.CreatedByUserId <= 0)
                    equipment.CreatedByUserId = _currentUserId;

                db.Equipment.Add(equipment);
                db.SaveChanges();

                _audit.Log(equipment.Id, AuditService.ActionEquipmentCreated,
                    null, $"Created: {equipment.EquipmentCode} - {equipment.Name}");

                return ServiceResult<Equipment>.Ok(equipment);
            }
            catch (Exception ex)
            {
                var innerMsg = ex.InnerException?.Message ?? ex.Message;
                return ServiceResult<Equipment>.Fail($"Error adding equipment: {innerMsg}");
            }
        } 

        public ServiceResult<Equipment> Update(Equipment equipment)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(equipment.Name))
                    return ServiceResult<Equipment>.Fail("Equipment name is required.");

                equipment.EquipmentCode = equipment.EquipmentCode.Trim().ToUpper();

                // Normalise serial number: treat "N/A", "NA", "-" and whitespace-only as null
                if (!string.IsNullOrWhiteSpace(equipment.SerialNumber))
                {
                    var sn = equipment.SerialNumber.Trim();
                    if (sn.Equals("N/A", StringComparison.OrdinalIgnoreCase) ||
                        sn.Equals("NA", StringComparison.OrdinalIgnoreCase) ||
                        sn == "-")
                        equipment.SerialNumber = null;
                    else
                        equipment.SerialNumber = sn.ToUpper();
                }
                else
                {
                    equipment.SerialNumber = null;
                }

                using var db = new InventoryDbContext();
                var existing = db.Equipment.FirstOrDefault(e => e.Id == equipment.Id);
                if (existing == null)
                    return ServiceResult<Equipment>.Fail("Equipment not found.");

                if (db.Equipment.Any(e => e.EquipmentCode == equipment.EquipmentCode && e.Id != equipment.Id))
                    return ServiceResult<Equipment>.Fail($"Equipment code '{equipment.EquipmentCode}' already exists.");
                if (!string.IsNullOrWhiteSpace(equipment.SerialNumber) &&
                    db.Equipment.Any(e => e.SerialNumber == equipment.SerialNumber && e.Id != equipment.Id))
                    return ServiceResult<Equipment>.Fail($"Serial number '{equipment.SerialNumber}' already exists.");

                var (oldSummary, newSummary) = AuditService.BuildEquipmentDiff(existing, equipment);

                existing.EquipmentCode = equipment.EquipmentCode;
                existing.QrCode = equipment.QrCode;
                existing.Name = equipment.Name;
                existing.Make = equipment.Make;
                existing.Model = equipment.Model;
                existing.SerialNumber = equipment.SerialNumber;
                existing.CurrentStatus = equipment.CurrentStatus;
                existing.PurchaseDate = equipment.PurchaseDate;
                existing.Price = equipment.Price;
                existing.Category = equipment.Category;
                existing.Description = equipment.Description;
                existing.VendorName = equipment.VendorName;
                existing.ConditionGrade = equipment.ConditionGrade;
                existing.WarrantyExpiry = equipment.WarrantyExpiry;
                existing.Notes = equipment.Notes;
                existing.ProcurementTypeId = equipment.ProcurementTypeId;
                existing.PicturePath = equipment.PicturePath;

                db.SaveChanges();

                _audit.Log(existing.Id, AuditService.ActionEquipmentUpdated, oldSummary, newSummary);

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
                var eq = db.Equipment.FirstOrDefault(e => e.Id == id);
                if (eq == null)
                    return ServiceResult<bool>.Fail("Equipment not found.");

                string deletedInfo = $"{eq.EquipmentCode} - {eq.Name} - {eq.CurrentStatus}";

                eq.IsDeleted = true;
                db.SaveChanges();

                _audit.Log(eq.Id, AuditService.ActionEquipmentDeleted,
          $"{eq.EquipmentCode} - {eq.Name}",
          $"Category: {eq.Category ?? "—"} | Status: {eq.CurrentStatus} | Serial: {eq.SerialNumber ?? "—"}");

                return ServiceResult<bool>.Ok(true);
            }
            catch (Exception ex)
            {
                return ServiceResult<bool>.Fail("Delete failed: " + ex.Message);
            }
        }

        public ServiceResult<bool> Restore(int id)
        {
            try
            {
                using var db = new InventoryDbContext();
                var eq = db.Equipment.FirstOrDefault(e => e.Id == id);
                if (eq == null)
                    return ServiceResult<bool>.Fail("Equipment not found.");

                eq.IsDeleted = false;
                db.SaveChanges();

                _audit.Log(eq.Id, AuditService.ActionEquipmentRestored, null, $"{eq.EquipmentCode} - {eq.Name} restored");

                return ServiceResult<bool>.Ok(true);
            }
            catch (Exception ex)
            {
                return ServiceResult<bool>.Fail("Restore failed: " + ex.Message);
            }
        }

        public ServiceResult<bool> HardDelete(int id)
        {
            try
            {
                using var db = new InventoryDbContext();
                var eq = db.Equipment.FirstOrDefault(e => e.Id == id);
                if (eq == null)
                    return ServiceResult<bool>.Fail("Equipment not found.");

                db.Equipment.Remove(eq);
                db.SaveChanges();
                return ServiceResult<bool>.Ok(true);
            }
            catch (Exception ex)
            {
                return ServiceResult<bool>.Fail("Permanent delete failed: " + ex.Message);
            }
        }

        public ServiceResult<List<Equipment>> GetDeleted()
        {
            try
            {
                using var db = new InventoryDbContext();
                var items = db.Equipment.Where(e => e.IsDeleted).Include(e => e.ProcurementType).ToList();
                return ServiceResult<List<Equipment>>.Ok(items);
            }
            catch (Exception ex)
            {
                return ServiceResult<List<Equipment>>.Fail("Load failed: " + ex.Message);
            }
        }

        public ServiceResult<int> AddRange(IEnumerable<Equipment> items)
        {
            int successCount = 0;
            var errors = new List<string>();

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