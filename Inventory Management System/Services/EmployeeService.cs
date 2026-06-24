using System;
using System.Collections.Generic;
using System.Linq;
using Inventory_Management_System.Models;
using Inventory_Management_System.Helpers;

namespace Inventory_Management_System.Services
{
    public class EmployeeService
    {
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

        public ServiceResult<Employee> Add(Employee employee)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(employee.FullName))
                    return ServiceResult<Employee>.Fail("Full Name is required.");
                if (string.IsNullOrWhiteSpace(employee.EmployeeCode))
                    return ServiceResult<Employee>.Fail("Employee Code is required.");

                using var db = new InventoryDbContext();

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
