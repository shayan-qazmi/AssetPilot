// Services/TransactionService.cs
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Inventory_Management_System.Models;
using Inventory_Management_System.Helpers;

namespace Inventory_Management_System.Services
{
    public class TransactionService
    {
        private readonly AuditService _audit = new AuditService();

        public ServiceResult<List<Transaction>> GetAll()
        {
            try
            {
                using var db = new InventoryDbContext();
                var list = db.Transactions
                    .Include(t => t.Equipment)
                    .OrderByDescending(t => t.TransactionDate)
                    .ToList();
                return ServiceResult<List<Transaction>>.Ok(list);
            }
            catch (Exception ex)
            {
                return ServiceResult<List<Transaction>>.Fail($"Error retrieving transactions: {ex.Message}");
            }
        }

        public ServiceResult<List<Transaction>> GetByEquipment(int equipmentId)
        {
            try
            {
                using var db = new InventoryDbContext();
                var list = db.Transactions
                    .Include(t => t.Equipment)
                    .Where(t => t.EquipmentId == equipmentId)
                    .OrderByDescending(t => t.TransactionDate)
                    .ToList();
                return ServiceResult<List<Transaction>>.Ok(list);
            }
            catch (Exception ex)
            {
                return ServiceResult<List<Transaction>>.Fail($"Error retrieving transactions for equipment: {ex.Message}");
            }
        }

        public ServiceResult<List<Transaction>> GetOverdue()
        {
            try
            {
                using var db = new InventoryDbContext();
                var today = DateOnly.FromDateTime(DateTime.Today);
                var list = db.Transactions
                    .Include(t => t.Equipment)
                    .Where(t => t.Status == "Pending" && t.ExpectedReturn != null && t.ExpectedReturn < today)
                    .OrderByDescending(t => t.TransactionDate)
                    .ToList();
                return ServiceResult<List<Transaction>>.Ok(list);
            }
            catch (Exception ex)
            {
                return ServiceResult<List<Transaction>>.Fail($"Error retrieving overdue transactions: {ex.Message}");
            }
        }

        public ServiceResult<Transaction> CheckOut(Transaction transaction)
        {
            try
            {
                using var db = new InventoryDbContext();
                var equipment = db.Equipment.FirstOrDefault(e => e.Id == transaction.EquipmentId);
                if (equipment == null)
                    return ServiceResult<Transaction>.Fail("Equipment not found.");

                var hasActiveTransaction = db.Transactions.Any(t => t.EquipmentId == transaction.EquipmentId && (t.Status == "Pending" || t.Status == "Overdue"));
                if (hasActiveTransaction)
                    return ServiceResult<Transaction>.Fail("Equipment is already checked out.");

                transaction.TransactionType = "Check-Out";
                transaction.Status = "Pending";
                transaction.TransactionDate = DateTime.UtcNow;
                transaction.CreatedAt = DateTime.UtcNow;

                db.Transactions.Add(transaction);
                equipment.CurrentStatus = "allocated";

                db.SaveChanges();

                _audit.Log(transaction.EquipmentId, AuditService.ActionCheckedOut,
                    "Status: In Store",
                    $"Issued To: {transaction.IssuedTo} | Expected Return: {transaction.ExpectedReturn?.ToString("yyyy-MM-dd") ?? "—"} | Condition: {transaction.ConditionBefore ?? "—"}");

                return ServiceResult<Transaction>.Ok(transaction);
            }
            catch (Exception ex)
            {
                return ServiceResult<Transaction>.Fail($"Error checking out equipment: {ex.Message}");
            }
        }

        public ServiceResult<bool> CheckIn(int transactionId, string conditionAfter, string remarks)
        {
            try
            {
                using var db = new InventoryDbContext();
                var transaction = db.Transactions.FirstOrDefault(t => t.TransactionId == transactionId);
                if (transaction == null)
                    return ServiceResult<bool>.Fail("Transaction not found.");

                transaction.ActualReturn = DateOnly.FromDateTime(DateTime.Today);
                transaction.Status = "Completed";
                transaction.ConditionAfter = conditionAfter;
                transaction.Remarks = remarks;

                var equipment = db.Equipment.FirstOrDefault(e => e.Id == transaction.EquipmentId);
                if (equipment != null)
                {
                    equipment.CurrentStatus = "in_store";
                }

                db.SaveChanges();

                _audit.Log(transaction.EquipmentId, AuditService.ActionCheckedIn,
                    $"Status: Allocated | Condition Before: {transaction.ConditionBefore ?? "—"}",
                    $"Status: In Store | Condition After: {conditionAfter} | Remarks: {(string.IsNullOrWhiteSpace(remarks) ? "—" : remarks)}");

                return ServiceResult<bool>.Ok(true);
            }
            catch (Exception ex)
            {
                return ServiceResult<bool>.Fail($"Error checking in equipment: {ex.Message}");
            }
        }

        public void UpdateStatus()
        {
            try
            {
                using var db = new InventoryDbContext();
                var today = DateOnly.FromDateTime(DateTime.Today);
                var pendingOverdue = db.Transactions
                    .Where(t => t.Status == "Pending" && t.ExpectedReturn != null && t.ExpectedReturn < today)
                    .ToList();

                if (pendingOverdue.Any())
                {
                    foreach (var transaction in pendingOverdue)
                    {
                        transaction.Status = "Overdue";
                    }
                    db.SaveChanges();
                }
            }
            catch
            {
                // Void method, silent error handling on startup as typical
            }
        }
    }
}