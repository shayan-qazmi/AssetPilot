using ClosedXML.Excel;
using Inventory_Management_System.Models;
using Inventory_Management_System.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Inventory_Management_System.Helpers
{
    public static class ExcelHelper
    {
        // Valid status values — must match the application's canonical set.
        private static readonly HashSet<string> ValidStatuses = new(StringComparer.OrdinalIgnoreCase)
        {
            "in_store", "allocated", "under_repair", "decommissioned"
        };

        /// <summary>
        /// Reads equipment rows from an Excel file using header-name lookup.
        /// </summary>
        public static (List<Equipment> Items, List<string> Errors) ImportEquipment(
            string filePath,
            EquipmentService equipmentService)
        {
            var items = new List<Equipment>();
            var errors = new List<string>();

            using var wb = new XLWorkbook(filePath);
            var ws = wb.Worksheet(1);

            var headerRow = ws.Row(1);
            var colMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var cell in headerRow.CellsUsed())
            {
                var header = cell.GetString().Trim().ToLowerInvariant();
                if (!string.IsNullOrWhiteSpace(header) && !colMap.ContainsKey(header))
                    colMap[header] = cell.Address.ColumnNumber;
            }

            string CellStr(IXLRow row, string colName)
            {
                if (!colMap.TryGetValue(colName, out int col)) return "";
                return row.Cell(col).GetString().Trim();
            }

            DateOnly? CellDate(IXLRow row, string colName)
            {
                if (!colMap.TryGetValue(colName, out int col)) return null;
                var cell = row.Cell(col);
                if (cell.TryGetValue<DateTime>(out var dt)) return DateOnly.FromDateTime(dt);
                var raw = cell.GetString().Trim();
                if (DateTime.TryParse(raw, out var parsed)) return DateOnly.FromDateTime(parsed);
                return null;
            }

            decimal? CellDecimal(IXLRow row, string colName)
            {
                if (!colMap.TryGetValue(colName, out int col)) return null;
                var cell = row.Cell(col);
                if (cell.TryGetValue<decimal>(out var d)) return d;
                var raw = cell.GetString().Trim();
                if (decimal.TryParse(raw, out var parsed)) return parsed;
                return null;
            }

            int rowNum = 1;
            foreach (var row in ws.RowsUsed().Skip(1))
            {
                rowNum++;
                try
                {
                    var name = CellStr(row, "name");
                    var serialNumber = CellStr(row, "serial_number");

                    if (string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(serialNumber))
                        continue;

                    if (string.IsNullOrWhiteSpace(name))
                    {
                        errors.Add($"Row {rowNum}: 'name' is required but was blank — row skipped.");
                        continue;
                    }
                    if (string.IsNullOrWhiteSpace(serialNumber))
                    {
                        errors.Add($"Row {rowNum}: 'serial_number' is required but was blank — row skipped.");
                        continue;
                    }

                    var rawStatus = CellStr(row, "current_status");
                    string status;
                    if (string.IsNullOrWhiteSpace(rawStatus))
                    {
                        status = "in_store";
                    }
                    else if (ValidStatuses.Contains(rawStatus))
                    {
                        status = rawStatus.ToLowerInvariant();
                    }
                    else
                    {
                        errors.Add($"Row {rowNum}: Invalid current_status '{rawStatus}'. Defaulting to 'in_store'.");
                        status = "in_store";
                    }

                    var procTypeName = CellStr(row, "procurement_type");
                    int? procTypeId = string.IsNullOrWhiteSpace(procTypeName)
                        ? null
                        : equipmentService.ResolveProcurementTypeId(procTypeName);

                    var eq = new Equipment
                    {
                        Name = name,
                        SerialNumber = serialNumber,
                        Make = CellStr(row, "make").NullIfEmpty(),
                        Model = CellStr(row, "model").NullIfEmpty(),
                        CurrentStatus = status,
                        PurchaseDate = CellDate(row, "purchase_date"),
                        Price = CellDecimal(row, "price"),
                        Category = CellStr(row, "category").NullIfEmpty(),
                        Description = CellStr(row, "description").NullIfEmpty(),
                        VendorName = CellStr(row, "vendor_name").NullIfEmpty(),
                        ConditionGrade = CellStr(row, "condition_grade").NullIfEmpty(),
                        WarrantyExpiry = CellDate(row, "warranty_expiry"),
                        Notes = CellStr(row, "notes").NullIfEmpty(),
                        ProcurementTypeId = procTypeId,
                        CreatedByUserId = 1
                    };

                    items.Add(eq);
                }
                catch (Exception ex)
                {
                    errors.Add($"Row {rowNum}: Unexpected error — {ex.Message} — row skipped.");
                }
            }

            return (items, errors);
        }

        /// <summary>
        /// Exports a list of equipment to an Excel file.
        /// </summary>
        public static void ExportEquipment(List<Equipment> items, string filePath)
        {
            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Equipment");

            ws.Cell(1, 1).Value = "Equipment Code";
            ws.Cell(1, 2).Value = "QR Code";
            ws.Cell(1, 3).Value = "Name";
            ws.Cell(1, 4).Value = "Make";
            ws.Cell(1, 5).Value = "Model";
            ws.Cell(1, 6).Value = "Serial Number";
            ws.Cell(1, 7).Value = "Status";
            ws.Cell(1, 8).Value = "Purchase Date";

            var headerRow = ws.Range(1, 1, 1, 8);
            headerRow.Style.Font.Bold = true;
            headerRow.Style.Fill.BackgroundColor = XLColor.FromArgb(46, 125, 50);
            headerRow.Style.Font.FontColor = XLColor.White;

            int row = 2;
            foreach (var item in items)
            {
                ws.Cell(row, 1).Value = item.EquipmentCode;
                ws.Cell(row, 2).Value = item.QrCode;
                ws.Cell(row, 3).Value = item.Name;
                ws.Cell(row, 4).Value = item.Make;
                ws.Cell(row, 5).Value = item.Model;
                ws.Cell(row, 6).Value = item.SerialNumber;
                ws.Cell(row, 7).Value = item.CurrentStatus;
                ws.Cell(row, 8).Value = item.PurchaseDate?.ToString("yyyy-MM-dd") ?? "";
                row++;
            }

            ws.Columns().AdjustToContents();
            wb.SaveAs(filePath);
        }

        /// <summary>
        /// Reads Employee rows from an Excel file using header lookup.
        /// </summary>
        public static (List<Employee> Items, List<string> Errors) ImportEmployees(string filePath)
        {
            var items = new List<Employee>();
            var errors = new List<string>();

            using var wb = new XLWorkbook(filePath);
            var ws = wb.Worksheet(1);

            var headerRow = ws.Row(1);
            var colMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var cell in headerRow.CellsUsed())
            {
                var header = cell.GetString().Trim().ToLowerInvariant();
                if (!string.IsNullOrWhiteSpace(header) && !colMap.ContainsKey(header))
                    colMap[header] = cell.Address.ColumnNumber;
            }

            string CellStr(IXLRow row, params string[] colNames)
            {
                foreach (var colName in colNames)
                {
                    if (colMap.TryGetValue(colName, out int col))
                        return row.Cell(col).GetString().Trim();
                }
                return "";
            }

            var lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;

            for (int r = 2; r <= lastRow; r++)
            {
                var row = ws.Row(r);
                string name = CellStr(row, "name", "full_name", "fullname");
                string email = CellStr(row, "email");
                string phone = CellStr(row, "phone");
                string department = CellStr(row, "department", "designation");
                string statusStr = CellStr(row, "status");

                if (string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(email))
                    continue;

                if (string.IsNullOrWhiteSpace(name))
                {
                    errors.Add($"Row {r}: Name is required.");
                    continue;
                }

                bool isActive = string.IsNullOrWhiteSpace(statusStr) ||
                                statusStr.Equals("Active", StringComparison.OrdinalIgnoreCase) ||
                                statusStr.Equals("1", StringComparison.OrdinalIgnoreCase) ||
                                statusStr.Equals("True", StringComparison.OrdinalIgnoreCase);

                items.Add(new Employee
                {
                    FullName = name,
                    Email = email,
                    Phone = phone,
                    Department = department,
                    IsActive = isActive
                });
            }

            return (items, errors);
        }

        /// <summary>
        /// Reads Location rows from an Excel file using header lookup.
        /// </summary>
        public static (List<Location> Items, List<string> Errors) ImportLocations(string filePath)
        {
            var items = new List<Location>();
            var errors = new List<string>();

            using var wb = new XLWorkbook(filePath);
            var ws = wb.Worksheet(1);

            var headerRow = ws.Row(1);
            var colMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var cell in headerRow.CellsUsed())
            {
                var header = cell.GetString().Trim().ToLowerInvariant();
                if (!string.IsNullOrWhiteSpace(header) && !colMap.ContainsKey(header))
                    colMap[header] = cell.Address.ColumnNumber;
            }

            string CellStr(IXLRow row, params string[] colNames)
            {
                foreach (var colName in colNames)
                {
                    if (colMap.TryGetValue(colName, out int col))
                        return row.Cell(col).GetString().Trim();
                }
                return "";
            }

            var lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;

            for (int r = 2; r <= lastRow; r++)
            {
                var row = ws.Row(r);
                string locationName = CellStr(row, "location_name", "name", "locationname");
                string building = CellStr(row, "building");
                string roomNumber = CellStr(row, "room_number", "roomnumber");
                string roomName = CellStr(row, "room_name", "roomname");

                if (string.IsNullOrWhiteSpace(locationName))
                    continue;

                items.Add(new Location
                {
                    LocationName = locationName,
                    Building = building,
                    RoomNumber = roomNumber,
                    RoomName = string.IsNullOrWhiteSpace(roomName) ? null : roomName
                });
            }

            return (items, errors);
        }
    }

    internal static class StringExcelExtensions
    {
        public static string? NullIfEmpty(this string? s)
            => string.IsNullOrWhiteSpace(s) ? null : s;
    }
}