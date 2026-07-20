// Services/CategoryService.cs
using Inventory_Management_System.Helpers;
using Inventory_Management_System.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Inventory_Management_System.Services
{
    public class CategoryService
    {
        public ServiceResult<List<Category>> GetAll()
        {
            try
            {
                using var db = new InventoryDbContext();
                var list = db.Categories
                    .OrderBy(c => c.Name)
                    .ToList();
                return ServiceResult<List<Category>>.Ok(list);
            }
            catch (Exception ex)
            {
                return ServiceResult<List<Category>>.Fail($"Error retrieving categories: {ex.Message}");
            }
        }

        public ServiceResult<Category> Add(string name)
        {
            try
            {
                name = (name ?? "").Trim();
                if (string.IsNullOrWhiteSpace(name))
                    return ServiceResult<Category>.Fail("Category name cannot be empty.");

                using var db = new InventoryDbContext();

                bool exists = db.Categories.Any(c => c.Name.ToLower() == name.ToLower());
                if (exists)
                    return ServiceResult<Category>.Fail("That category already exists.");

                var category = new Category { Name = name };
                db.Categories.Add(category);
                db.SaveChanges();

                return ServiceResult<Category>.Ok(category);
            }
            catch (Exception ex)
            {
                var innerMsg = ex.InnerException?.Message ?? ex.Message;
                return ServiceResult<Category>.Fail($"Error adding category: {innerMsg}");
            }
        }

        public ServiceResult<bool> Delete(int id)
        {
            try
            {
                using var db = new InventoryDbContext();
                var existing = db.Categories.FirstOrDefault(c => c.Id == id);
                if (existing == null)
                    return ServiceResult<bool>.Fail("Category not found.");

                // Delete-protection: check whether any equipment record still
                // uses this category's name (case-insensitive), same behavior
                // as the flat-file version was intended to have.
                int usageCount = db.Equipment
                    .Count(e => e.Category != null &&
                                e.Category.ToLower() == existing.Name.ToLower());

                if (usageCount > 0)
                    return ServiceResult<bool>.Fail(
                        $"Cannot delete — {usageCount} equipment item(s) are using this category.");

                db.Categories.Remove(existing);
                db.SaveChanges();

                return ServiceResult<bool>.Ok(true);
            }
            catch (Exception ex)
            {
                var innerMsg = ex.InnerException?.Message ?? ex.Message;
                return ServiceResult<bool>.Fail($"Error deleting category: {innerMsg}");
            }
        }
    }
}