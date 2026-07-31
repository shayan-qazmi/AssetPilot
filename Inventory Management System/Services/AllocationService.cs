// Services/AllocationService.cs
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Inventory_Management_System.Models;
using Inventory_Management_System.Helpers;

namespace Inventory_Management_System.Services
{
    public class AllocationService
    {
        public ServiceResult<List<Allocation>> GetAll()
        {
            try
            {
                using var db = new InventoryDbContext();
                var list = db.Allocations
                    .Include(a => a.Equipment)
                    .Include(a => a.Employee)
                    .Include(a => a.Location)
                    .Include(a => a.PreviousAllocation).ThenInclude(p => p!.Location)
                    .ToList();
                return ServiceResult<List<Allocation>>.Ok(list);
            }
            catch (Exception ex)
            {
                return ServiceResult<List<Allocation>>.Fail($"Error retrieving allocations: {ex.Message}");
            }
        }

        public ServiceResult<Allocation> Allocate(Allocation allocation)
        {
            try
            {
                using var db = new InventoryDbContext();

                var equipment = db.Equipment.FirstOrDefault(e => e.Id == allocation.EquipmentId);
                if (equipment == null) return ServiceResult<Allocation>.Fail("Equipment not found.");
                if (equipment.CurrentStatus != "in_store" && equipment.CurrentStatus != "allocated")
                    return ServiceResult<Allocation>.Fail("Equipment is not available.");

                // Employee is now optional — only validate it exists if one was actually supplied.
                if (allocation.EmployeeId.HasValue)
                {
                    var employeeExists = db.Employees.Any(e => e.Id == allocation.EmployeeId.Value);
                    if (!employeeExists) return ServiceResult<Allocation>.Fail("Selected employee not found.");
                }

                // Same for Location — optional, only validate if supplied.
                if (allocation.LocationId.HasValue)
                {
                    var locationExists = db.Locations.Any(l => l.Id == allocation.LocationId.Value);
                    if (!locationExists) return ServiceResult<Allocation>.Fail("Selected location not found.");
                }

                if (!allocation.EmployeeId.HasValue && !allocation.LocationId.HasValue)
                    return ServiceResult<Allocation>.Fail("Either an employee or a location is required.");

                var existingAllocation = db.Allocations.Any(a => a.EquipmentId == allocation.EquipmentId && (a.Status == "approved" || a.Status == "pending"));
                if (existingAllocation)
                    return ServiceResult<Allocation>.Fail("Equipment already has an active or pending allocation.");

                if (string.IsNullOrWhiteSpace(allocation.Status))
                    allocation.Status = "approved";

                db.Allocations.Add(allocation);

                if (allocation.Status == "approved")
                {
                    equipment.CurrentStatus = "allocated";
                }

                db.SaveChanges();
                return ServiceResult<Allocation>.Ok(allocation);
            }
            catch (Exception ex)
            {
                return ServiceResult<Allocation>.Fail($"Error creating allocation: {ex.Message}");
            }
        }

        public ServiceResult<bool> Deallocate(int allocationId)
        {
            try
            {
                using var db = new InventoryDbContext();
                var allocation = db.Allocations.FirstOrDefault(a => a.Id == allocationId);
                if (allocation == null) return ServiceResult<bool>.Fail("Allocation not found.");

                allocation.Status = "returned";

                var equipment = db.Equipment.FirstOrDefault(e => e.Id == allocation.EquipmentId);
                if (equipment != null)
                {
                    equipment.CurrentStatus = "in_store";
                }

                db.SaveChanges();
                return ServiceResult<bool>.Ok(true);
            }
            catch (Exception ex)
            {
                return ServiceResult<bool>.Fail($"Error deallocating: {ex.Message}");
            }
        }

        public ServiceResult<List<Allocation>> GetByEmployee(int employeeId)
        {
            try
            {
                using var db = new InventoryDbContext();
                var list = db.Allocations.Where(a => a.EmployeeId == employeeId).ToList();
                return ServiceResult<List<Allocation>>.Ok(list);
            }
            catch (Exception ex)
            {
                return ServiceResult<List<Allocation>>.Fail($"Error retrieving allocations: {ex.Message}");
            }
        }

        public ServiceResult<List<Allocation>> GetByEquipment(int equipmentId)
        {
            try
            {
                using var db = new InventoryDbContext();
                var list = db.Allocations.Where(a => a.EquipmentId == equipmentId).ToList();
                return ServiceResult<List<Allocation>>.Ok(list);
            }
            catch (Exception ex)
            {
                return ServiceResult<List<Allocation>>.Fail($"Error retrieving allocations: {ex.Message}");
            }
        }
    }
}