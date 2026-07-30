// Services/LocationService.cs
using System;
using System.Collections.Generic;
using System.Linq;
using Inventory_Management_System.Models;
using Inventory_Management_System.Helpers;

namespace Inventory_Management_System.Services
{
    public class LocationService
    {
        private readonly AuditService _audit = new AuditService();

        public ServiceResult<List<Location>> GetAll()
        {
            try
            {
                using var db = new InventoryDbContext();
                return ServiceResult<List<Location>>.Ok(db.Locations.ToList());
            }
            catch (Exception ex)
            {
                return ServiceResult<List<Location>>.Fail($"Error retrieving locations: {ex.Message}");
            }
        }

        public ServiceResult<Location> GetById(int id)
        {
            try
            {
                using var db = new InventoryDbContext();
                var loc = db.Locations.FirstOrDefault(l => l.Id == id);
                if (loc == null) return ServiceResult<Location>.Fail("Location not found.");
                return ServiceResult<Location>.Ok(loc);
            }
            catch (Exception ex)
            {
                return ServiceResult<Location>.Fail($"Error retrieving location: {ex.Message}");
            }
        }

        public ServiceResult<Location> Add(Location location)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(location.LocationName))
                    return ServiceResult<Location>.Fail("Location Name is required.");

                using var db = new InventoryDbContext();
                db.Locations.Add(location);
                db.SaveChanges();

                _audit.Log(0, AuditService.ActionLocationCreated, null, location.LocationName);

                return ServiceResult<Location>.Ok(location);
            }
            catch (Exception ex)
            {
                return ServiceResult<Location>.Fail($"Error adding location: {ex.Message}");
            }
        }

        public ServiceResult<Location> Update(Location location)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(location.LocationName))
                    return ServiceResult<Location>.Fail("Location Name is required.");

                using var db = new InventoryDbContext();
                var existing = db.Locations.FirstOrDefault(l => l.Id == location.Id);
                if (existing == null) return ServiceResult<Location>.Fail("Location not found.");

                string oldValue = existing.LocationName;

                existing.LocationName = location.LocationName;
                existing.Building = location.Building;
                existing.RoomNumber = location.RoomNumber;
                existing.RoomName = location.RoomName;

                db.SaveChanges();

                _audit.Log(0, AuditService.ActionLocationUpdated, oldValue, existing.LocationName);

                return ServiceResult<Location>.Ok(existing);
            }
            catch (Exception ex)
            {
                return ServiceResult<Location>.Fail($"Error updating location: {ex.Message}");
            }
        }

        public ServiceResult<bool> Delete(int id)
        {
            try
            {
                using var db = new InventoryDbContext();
                var existing = db.Locations.FirstOrDefault(l => l.Id == id);
                if (existing == null) return ServiceResult<bool>.Fail("Location not found.");

                var hasEquipment = db.Allocations.Any(a => a.LocationId == id && a.Status == "approved");
                if (hasEquipment)
                {
                    return ServiceResult<bool>.Fail("Cannot delete location with assigned equipment.");
                }

                string deletedName = existing.LocationName;

                db.Locations.Remove(existing);
                db.SaveChanges();

                _audit.Log(0, AuditService.ActionLocationDeleted, deletedName, null);

                return ServiceResult<bool>.Ok(true);
            }
            catch (Exception ex)
            {
                return ServiceResult<bool>.Fail($"Error deleting location: {ex.Message}");
            }
        }
    }
}