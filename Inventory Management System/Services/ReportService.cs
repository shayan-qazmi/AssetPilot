using System;
using System.Collections.Generic;
using System.Linq;
using Inventory_Management_System.Models;
using Inventory_Management_System.Helpers;

namespace Inventory_Management_System.Services
{
    public class ReportService
    {
        public ServiceResult<List<Equipment>> GetAllEquipmentReport()
        {
            try
            {
                using var db = new InventoryDbContext();
                return ServiceResult<List<Equipment>>.Ok(db.Equipment.ToList());
            }
            catch (Exception ex)
            {
                return ServiceResult<List<Equipment>>.Fail($"Error generating report: {ex.Message}");
            }
        }

        public ServiceResult<List<Allocation>> GetAllocationSummaryByEmployee(int employeeId)
        {
            try
            {
                using var db = new InventoryDbContext();
                var list = db.Allocations.Where(a => a.EmployeeId == employeeId).ToList();
                return ServiceResult<List<Allocation>>.Ok(list);
            }
            catch (Exception ex)
            {
                return ServiceResult<List<Allocation>>.Fail($"Error generating report: {ex.Message}");
            }
        }

        public ServiceResult<List<Equipment>> GetAvailableEquipment()
        {
            try
            {
                using var db = new InventoryDbContext();
                var list = db.Equipment.Where(e => e.CurrentStatus == "in_store").ToList();
                return ServiceResult<List<Equipment>>.Ok(list);
            }
            catch (Exception ex)
            {
                return ServiceResult<List<Equipment>>.Fail($"Error generating report: {ex.Message}");
            }
        }

        public ServiceResult<List<Equipment>> GetEquipmentByStatus(string status)
        {
            try
            {
                using var db = new InventoryDbContext();
                var list = db.Equipment.Where(e => e.CurrentStatus == status).ToList();
                return ServiceResult<List<Equipment>>.Ok(list);
            }
            catch (Exception ex)
            {
                return ServiceResult<List<Equipment>>.Fail($"Error generating report: {ex.Message}");
            }
        }
    }
}
