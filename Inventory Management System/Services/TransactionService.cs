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
                transaction.ReturnProcessedAt = DateTime.UtcNow;
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
                var checkOutTx = db.Transactions.Include(t => t.Equipment).FirstOrDefault(t => t.TransactionId == transactionId);
                if (checkOutTx == null)
                    return ServiceResult<bool>.Fail("Transaction not found.");

                checkOutTx.ActualReturn = DateOnly.FromDateTime(DateTime.Today);
                checkOutTx.Status = "Completed";
                checkOutTx.ConditionAfter = conditionAfter;
                checkOutTx.Remarks = remarks;
                checkOutTx.ReturnProcessedAt = DateTime.UtcNow;

                // NEW: log the check-in as its own row
                var checkInTx = new Transaction
                {
                    EquipmentId = checkOutTx.EquipmentId,
                    TransactionType = "Check-In",
                    IssuedTo = checkOutTx.IssuedTo,
                    Department = checkOutTx.Department,
                    OperatorName = checkOutTx.OperatorName,
                    TransactionDate = DateTime.UtcNow,
                    ActualReturn = checkOutTx.ActualReturn,
                    Status = "Completed",
                    ConditionBefore = checkOutTx.ConditionBefore,
                    ConditionAfter = conditionAfter,
                    Remarks = remarks,
                    CreatedAt = DateTime.UtcNow,
                    ReturnProcessedAt = DateTime.UtcNow
                };
                db.Transactions.Add(checkInTx);

                var equipment = db.Equipment.FirstOrDefault(e => e.Id == checkOutTx.EquipmentId);
                if (equipment != null) equipment.CurrentStatus = "in_store";

                db.SaveChanges();

                _audit.Log(checkOutTx.EquipmentId, AuditService.ActionCheckedIn,
                    $"Status: Allocated | Condition Before: {checkOutTx.ConditionBefore ?? "—"}",
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