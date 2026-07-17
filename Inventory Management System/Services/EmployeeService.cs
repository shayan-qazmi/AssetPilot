// Services/EmployeeService.cs
using System;
using System.Collections.Generic;
using System.Linq;
using Inventory_Management_System.Models;
using Inventory_Management_System.Helpers;

namespace Inventory_Management_System.Services
{
    public class EmployeeService
    {
        // ── Paged retrieval ──────────────────────────────────────────────────
        public ServiceResult<PagedResult<Employee>> GetPage(int pageNumber, int pageSize)
        {
            try
            {
                using var db = new InventoryDbContext();
                var total = db.Employees.Count();
                var items = db.Employees
                    .OrderBy(e => e.FullName)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                return ServiceResult<PagedResult<Employee>>.Ok(new PagedResult<Employee>
                {
                    Items = items,
                    TotalCount = total,
                    PageNumber = pageNumber,
                    PageSize = pageSize
                });
            }
            catch (Exception ex)
            {
                return ServiceResult<PagedResult<Employee>>.Fail($"Error paging employees: {ex.Message}");
            }
        }

        // ── Fuzzy search (returns full match set; caller pages in memory) ────
        public ServiceResult<List<Employee>> Search(string term)
        {
            try
            {
                using var db = new InventoryDbContext();
                var all = db.Employees.ToList();
                var list = all
                    .Where(e => FuzzySearchHelper.IsFuzzyMatch(e.FullName, term))
                    .OrderByDescending(e => FuzzySearchHelper.FuzzyScore(e.FullName, term))
                    .ToList();

                return ServiceResult<List<Employee>>.Ok(list);
            }
            catch (Exception ex)
            {
                return ServiceResult<List<Employee>>.Fail($"Error searching employees: {ex.Message}");
            }
        }

        public ServiceResult<List<Employee>> GetAll()
        {
            try
            {
                using var db = new InventoryDbContext();
                return ServiceResult<List<Employee>>.Ok(db.Employees.ToList());
            }
            catch (Exception ex)
            {
                return ServiceResult<List<Employee>>.Fail($"Error retrieving employees: {ex.Message}");
            }
        }

        public ServiceResult<Employee> GetById(int id)
        {
            try
            {
                using var db = new InventoryDbContext();
                var emp = db.Employees.FirstOrDefault(e => e.Id == id);
                if (emp == null) return ServiceResult<Employee>.Fail("Employee not found.");
                return ServiceResult<Employee>.Ok(emp);
            }
            catch (Exception ex)
            {
                return ServiceResult<Employee>.Fail($"Error retrieving employee: {ex.Message}");
            }
        }

        // ── Auto-generation helper ───────────────────────────────────────────
        // Mirrors EquipmentService.GenerateEquipmentCode() — sequential,
        // collision-checked, based on the highest existing "EMP###" number.
        private string GenerateEmployeeCode(InventoryDbContext db)
        {
            var codes = db.Employees
                .Select(e => e.EmployeeCode)
                .Where(c => c != null && c.StartsWith("EMP"))
                .ToList();

            int maxNum = 0;
            foreach (var c in codes)
            {
                var digits = new string(c!.Skip(3).TakeWhile(char.IsDigit).ToArray());
                if (int.TryParse(digits, out int n) && n > maxNum) maxNum = n;
            }

            string candidate;
            int next = maxNum + 1;
            do
            {
                candidate = $"EMP{next:000}";
                next++;
            } while (db.Employees.Any(e => e.EmployeeCode == candidate));

            return candidate;
        }

        public ServiceResult<Employee> Add(Employee employee)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(employee.FullName))
                    return ServiceResult<Employee>.Fail("Full Name is required.");

                using var db = new InventoryDbContext();

                // Employee Code is no longer typed manually — auto-generate
                // when blank. If a value was already supplied (e.g. Excel
                // import), keep it as-is.
                if (string.IsNullOrWhiteSpace(employee.EmployeeCode))
                    employee.EmployeeCode = GenerateEmployeeCode(db);
                else
                    employee.EmployeeCode = employee.EmployeeCode.Trim().ToUpper();

                // Safety net — same defensive pattern as EquipmentService.
                if (db.Employees.Any(e => e.EmployeeCode == employee.EmployeeCode))
                    return ServiceResult<Employee>.Fail($"Employee code '{employee.EmployeeCode}' already exists.");

                if (!string.IsNullOrWhiteSpace(employee.Email))
                {
                    var exists = db.Employees.Any(e => e.Email == employee.Email);
                    if (exists) return ServiceResult<Employee>.Fail("Email must be unique.");
                }

                db.Employees.Add(employee);
                db.SaveChanges();
                return ServiceResult<Employee>.Ok(employee);
            }
            catch (Exception ex)
            {
                return ServiceResult<Employee>.Fail($"Error adding employee: {ex.Message}");
            }
        }

        public ServiceResult<Employee> Update(Employee employee)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(employee.FullName))
                    return ServiceResult<Employee>.Fail("Full Name is required.");

                using var db = new InventoryDbContext();

                if (!string.IsNullOrWhiteSpace(employee.Email))
                {
                    var exists = db.Employees.Any(e => e.Email == employee.Email && e.Id != employee.Id);
                    if (exists) return ServiceResult<Employee>.Fail("Email must be unique.");
                }

                var existing = db.Employees.FirstOrDefault(e => e.Id == employee.Id);
                if (existing == null) return ServiceResult<Employee>.Fail("Employee not found.");

                existing.FullName = employee.FullName;
                existing.EmployeeCode = employee.EmployeeCode;
                existing.Email = employee.Email;
                existing.Department = employee.Department;
                existing.Phone = employee.Phone;
                existing.DefaultLocationId = employee.DefaultLocationId;
                existing.IsActive = employee.IsActive;
                existing.UpdatedAt = DateTime.Now;

                db.SaveChanges();
                return ServiceResult<Employee>.Ok(existing);
            }
            catch (Exception ex)
            {
                return ServiceResult<Employee>.Fail($"Error updating employee: {ex.Message}");
            }
        }

        public ServiceResult<bool> Delete(int id)
        {
            try
            {
                using var db = new InventoryDbContext();
                var existing = db.Employees.FirstOrDefault(e => e.Id == id);
                if (existing == null) return ServiceResult<bool>.Fail("Employee not found.");

                var hasActiveAllocations = db.Allocations.Any(a => a.EmployeeId == id && a.Status == "approved");
                if (hasActiveAllocations)
                {
                    return ServiceResult<bool>.Fail("Cannot delete employee with active allocations.");
                }

                db.Employees.Remove(existing);
                db.SaveChanges();
                return ServiceResult<bool>.Ok(true);
            }
            catch (Exception ex)
            {
                return ServiceResult<bool>.Fail($"Error deleting employee: {ex.Message}");
            }
        }
    }
}