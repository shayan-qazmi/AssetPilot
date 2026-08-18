#nullable disable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Inventory_Management_System.Models;
using ClosedXML.Excel;

namespace Inventory_Management_System.Helpers
{
    public static class ExcelHelper
    {
        #region Employee Import & Export
        public static (List<Employee> Items, List<string> Errors) ImportEmployees(string filePath)
        {
            var items = new List<Employee>();
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                errors.Add("Selected file does not exist.");
                return (items, errors);
            }

            string ext = Path.GetExtension(filePath).ToLower();

            if (ext == ".csv")
                return ImportEmployeesFromCsv(filePath);
            else
                return ImportEmployeesFromExcel(filePath);
        }

        private static (List<Employee> Items, List<string> Errors) ImportEmployeesFromExcel(string filePath)
        {
            var items = new List<Employee>();
            var errors = new List<string>();

            try
            {
                using var workbook = new XLWorkbook(filePath);

                IXLWorksheet worksheet = workbook.Worksheets.FirstOrDefault(w =>
                    w.Name.Equals("Employees", StringComparison.OrdinalIgnoreCase));

                if (worksheet == null)
                {
                    foreach (var ws in workbook.Worksheets)
                    {
                        if (HasHeader(ws, "Full Name", "Name"))
                        {
                            worksheet = ws;
                            break;
                        }
                    }
                }

                worksheet ??= workbook.Worksheets.FirstOrDefault();

                if (worksheet == null)
                {
                    errors.Add("No valid worksheets found in the file.");
                    return (items, errors);
                }

                IXLRow headerRow = null;
                int nameCol = -1, deptCol = -1, emailCol = -1, phoneCol = -1, codeCol = -1, statusCol = -1;

                int maxScan = Math.Min(15, worksheet.LastRowUsed()?.RowNumber() ?? 1);
                for (int r = 1; r <= maxScan; r++)
                {
                    var row = worksheet.Row(r);
                    foreach (var cell in row.CellsUsed())
                    {
                        string val = GetCellText(cell);
                        if (val.Equals("Full Name", StringComparison.OrdinalIgnoreCase) ||
                            val.Equals("Name", StringComparison.OrdinalIgnoreCase))
                        {
                            headerRow = row;
                            break;
                        }
                    }
                    if (headerRow != null) break;
                }

                if (headerRow == null)
                {
                    errors.Add("Could not find header row containing 'Full Name' or 'Name'.");
                    return (items, errors);
                }

                foreach (var cell in headerRow.CellsUsed())
                {
                    string h = GetCellText(cell);
                    if (h.Equals("Full Name", StringComparison.OrdinalIgnoreCase) || h.Equals("Name", StringComparison.OrdinalIgnoreCase)) nameCol = cell.Address.ColumnNumber;
                    else if (h.Equals("Designation", StringComparison.OrdinalIgnoreCase) || h.Equals("Department", StringComparison.OrdinalIgnoreCase)) deptCol = cell.Address.ColumnNumber;
                    else if (h.Equals("Email", StringComparison.OrdinalIgnoreCase)) emailCol = cell.Address.ColumnNumber;
                    else if (h.Equals("Phone", StringComparison.OrdinalIgnoreCase)) phoneCol = cell.Address.ColumnNumber;
                    else if (h.Equals("Employee Code", StringComparison.OrdinalIgnoreCase) || h.Equals("Code", StringComparison.OrdinalIgnoreCase)) codeCol = cell.Address.ColumnNumber;
                    else if (h.Equals("Status", StringComparison.OrdinalIgnoreCase)) statusCol = cell.Address.ColumnNumber;
                }

                int autoCodeIndex = 1;
                int lastRow = worksheet.LastRowUsed()?.RowNumber() ?? headerRow.RowNumber();

                for (int r = headerRow.RowNumber() + 1; r <= lastRow; r++)
                {
                    var row = worksheet.Row(r);
                    string fullName = nameCol > 0 ? GetCellText(row.Cell(nameCol)) : "";
                    if (string.IsNullOrWhiteSpace(fullName)) continue;

                    string code = codeCol > 0 ? GetCellText(row.Cell(codeCol)) : "";
                    if (string.IsNullOrWhiteSpace(code)) code = $"EMP{autoCodeIndex:D3}";
                    autoCodeIndex++;

                    string dept = deptCol > 0 ? GetCellText(row.Cell(deptCol)) : "";
                    string email = emailCol > 0 ? GetCellText(row.Cell(emailCol)).Replace("\n", "").Replace("\r", "") : "";
                    string phone = phoneCol > 0 ? GetCellText(row.Cell(phoneCol)) : "";
                    string status = statusCol > 0 ? GetCellText(row.Cell(statusCol)) : "Active";

                    bool isActive = string.IsNullOrWhiteSpace(status) ||
                                    status.Equals("Active", StringComparison.OrdinalIgnoreCase) ||
                                    status.Equals("1") ||
                                    status.Equals("True", StringComparison.OrdinalIgnoreCase);

                    items.Add(new Employee
                    {
                        EmployeeCode = code,
                        FullName = fullName,
                        Department = dept,
                        Email = email,
                        Phone = phone,
                        IsActive = isActive
                    });
                }
            }
            catch (Exception ex)
            {
                errors.Add($"Excel import error: {ex.Message}");
            }

            return (items, errors);
        }

        private static (List<Employee> Items, List<string> Errors) ImportEmployeesFromCsv(string filePath)
        {
            var items = new List<Employee>();
            var errors = new List<string>();

            try
            {
                string[] lines = File.ReadAllLines(filePath);
                int headerLineIdx = -1;
                int nameIdx = -1, deptIdx = -1, emailIdx = -1, phoneIdx = -1, codeIdx = -1, statusIdx = -1;

                for (int i = 0; i < Math.Min(15, lines.Length); i++)
                {
                    string[] cols = ParseCsvLine(lines[i]);
                    for (int c = 0; c < cols.Length; c++)
                    {
                        string val = cols[c].Trim();
                        if (val.Equals("Full Name", StringComparison.OrdinalIgnoreCase) ||
                            val.Equals("Name", StringComparison.OrdinalIgnoreCase))
                        {
                            headerLineIdx = i;
                            break;
                        }
                    }
                    if (headerLineIdx != -1) break;
                }

                if (headerLineIdx == -1)
                {
                    errors.Add("Could not find CSV header row with 'Full Name' or 'Name'.");
                    return (items, errors);
                }

                string[] headerCols = ParseCsvLine(lines[headerLineIdx]);
                for (int c = 0; c < headerCols.Length; c++)
                {
                    string h = headerCols[c].Trim();
                    if (h.Equals("Full Name", StringComparison.OrdinalIgnoreCase) || h.Equals("Name", StringComparison.OrdinalIgnoreCase)) nameIdx = c;
                    else if (h.Equals("Designation", StringComparison.OrdinalIgnoreCase) || h.Equals("Department", StringComparison.OrdinalIgnoreCase)) deptIdx = c;
                    else if (h.Equals("Email", StringComparison.OrdinalIgnoreCase)) emailIdx = c;
                    else if (h.Equals("Phone", StringComparison.OrdinalIgnoreCase)) phoneIdx = c;
                    else if (h.Equals("Employee Code", StringComparison.OrdinalIgnoreCase) || h.Equals("Code", StringComparison.OrdinalIgnoreCase)) codeIdx = c;
                    else if (h.Equals("Status", StringComparison.OrdinalIgnoreCase)) statusIdx = c;
                }

                int autoCodeIndex = 1;
                for (int i = headerLineIdx + 1; i < lines.Length; i++)
                {
                    if (string.IsNullOrWhiteSpace(lines[i])) continue;
                    string[] cols = ParseCsvLine(lines[i]);

                    string fullName = (nameIdx >= 0 && nameIdx < cols.Length) ? cols[nameIdx].Trim() : "";
                    if (string.IsNullOrWhiteSpace(fullName)) continue;

                    string code = (codeIdx >= 0 && codeIdx < cols.Length) ? cols[codeIdx].Trim() : "";
                    if (string.IsNullOrWhiteSpace(code)) code = $"EMP{autoCodeIndex:D3}";
                    autoCodeIndex++;

                    string dept = (deptIdx >= 0 && deptIdx < cols.Length) ? cols[deptIdx].Trim() : "";
                    string email = (emailIdx >= 0 && emailIdx < cols.Length) ? cols[emailIdx].Trim().Replace("\n", "").Replace("\r", "") : "";
                    string phone = (phoneIdx >= 0 && phoneIdx < cols.Length) ? cols[phoneIdx].Trim() : "";
                    string status = (statusIdx >= 0 && statusIdx < cols.Length) ? cols[statusIdx].Trim() : "Active";

                    bool isActive = string.IsNullOrWhiteSpace(status) ||
                                    status.Equals("Active", StringComparison.OrdinalIgnoreCase) ||
                                    status.Equals("1") ||
                                    status.Equals("True", StringComparison.OrdinalIgnoreCase);

                    items.Add(new Employee
                    {
                        EmployeeCode = code,
                        FullName = fullName,
                        Department = dept,
                        Email = email,
                        Phone = phone,
                        IsActive = isActive
                    });
                }
            }
            catch (Exception ex)
            {
                errors.Add($"CSV import error: {ex.Message}");
            }

            return (items, errors);
        }

        public static bool ExportEmployees(string filePath, IEnumerable<Employee> employees)
        {
            try
            {
                using var workbook = new XLWorkbook();
                var ws = workbook.Worksheets.Add("Employees");
                ws.Cell(1, 1).Value = "Employee Code";
                ws.Cell(1, 2).Value = "Full Name";
                ws.Cell(1, 3).Value = "Department";
                ws.Cell(1, 4).Value = "Phone";
                ws.Cell(1, 5).Value = "Email";
                ws.Cell(1, 6).Value = "Status";

                int row = 2;
                foreach (var emp in employees ?? Enumerable.Empty<Employee>())
                {
                    ws.Cell(row, 1).Value = emp.EmployeeCode;
                    ws.Cell(row, 2).Value = emp.FullName;
                    ws.Cell(row, 3).Value = emp.Department;
                    ws.Cell(row, 4).Value = emp.Phone;
                    ws.Cell(row, 5).Value = emp.Email;
                    ws.Cell(row, 6).Value = emp.IsActive ? "Active" : "Inactive";
                    row++;
                }
                workbook.SaveAs(filePath);
                return true;
            }
            catch { return false; }
        }

        public static bool ExportEmployees(IEnumerable<Employee> employees, string filePath) => ExportEmployees(filePath, employees);
        #endregion

        #region Location Import & Export
        public static (List<Location> Items, List<string> Errors) ImportLocations(string filePath)
        {
            var items = new List<Location>();
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                errors.Add("Selected file does not exist.");
                return (items, errors);
            }

            string ext = Path.GetExtension(filePath).ToLower();
            if (ext == ".csv")
            {
                return ImportLocationsFromCsv(filePath);
            }

            try
            {
                using var workbook = new XLWorkbook(filePath);
                IXLWorksheet worksheet = workbook.Worksheets.FirstOrDefault(w => w.Name.Equals("Locations", StringComparison.OrdinalIgnoreCase))
                                         ?? workbook.Worksheets.FirstOrDefault();

                if (worksheet == null)
                {
                    errors.Add("No valid worksheets found for Locations.");
                    return (items, errors);
                }

                IXLRow headerRow = null;
                int nameCol = -1, bldgCol = -1, roomNoCol = -1, roomNameCol = -1;

                int maxScan = Math.Min(15, worksheet.LastRowUsed()?.RowNumber() ?? 1);
                for (int r = 1; r <= maxScan; r++)
                {
                    var row = worksheet.Row(r);
                    foreach (var cell in row.CellsUsed())
                    {
                        string val = GetCellText(cell);
                        if (val.Equals("Location Name", StringComparison.OrdinalIgnoreCase) || val.Equals("Name", StringComparison.OrdinalIgnoreCase))
                        {
                            headerRow = row;
                            break;
                        }
                    }
                    if (headerRow != null) break;
                }

                if (headerRow != null)
                {
                    foreach (var cell in headerRow.CellsUsed())
                    {
                        string h = GetCellText(cell);
                        if (h.Equals("Location Name", StringComparison.OrdinalIgnoreCase) || h.Equals("Name", StringComparison.OrdinalIgnoreCase) || h.Equals("Location ID", StringComparison.OrdinalIgnoreCase)) nameCol = cell.Address.ColumnNumber;
                        else if (h.Equals("Building", StringComparison.OrdinalIgnoreCase) || h.Equals("Address", StringComparison.OrdinalIgnoreCase)) bldgCol = cell.Address.ColumnNumber;
                        else if (h.Equals("Room Number", StringComparison.OrdinalIgnoreCase) || h.Equals("RoomNo", StringComparison.OrdinalIgnoreCase)) roomNoCol = cell.Address.ColumnNumber;
                        else if (h.Equals("Room Name", StringComparison.OrdinalIgnoreCase) || h.Equals("Allocated Person", StringComparison.OrdinalIgnoreCase)) roomNameCol = cell.Address.ColumnNumber;
                    }

                    int lastRow = worksheet.LastRowUsed()?.RowNumber() ?? headerRow.RowNumber();
                    for (int r = headerRow.RowNumber() + 1; r <= lastRow; r++)
                    {
                        var row = worksheet.Row(r);
                        string locName = nameCol > 0 ? GetCellText(row.Cell(nameCol)) : "";
                        if (string.IsNullOrWhiteSpace(locName)) continue;

                        var loc = new Location();
                        SetProp(loc, "LocationName", locName);
                        SetProp(loc, "Name", locName);

                        string bldg = bldgCol > 0 ? GetCellText(row.Cell(bldgCol)) : "";
                        SetProp(loc, "Building", bldg);
                        SetProp(loc, "Address", bldg);

                        string roomNo = roomNoCol > 0 ? GetCellText(row.Cell(roomNoCol)) : "";
                        SetProp(loc, "RoomNumber", roomNo);

                        string roomName = roomNameCol > 0 ? GetCellText(row.Cell(roomNameCol)) : "";
                        SetProp(loc, "RoomName", roomName);

                        items.Add(loc);
                    }
                }
                else
                {
                    errors.Add("Could not find header row with 'Location Name' or 'Name'.");
                }
            }
            catch (Exception ex)
            {
                errors.Add($"Location import error: {ex.Message}");
            }

            return (items, errors);
        }

        private static (List<Location> Items, List<string> Errors) ImportLocationsFromCsv(string filePath)
        {
            var items = new List<Location>();
            var errors = new List<string>();

            try
            {
                string[] lines = File.ReadAllLines(filePath);
                int headerIdx = -1;
                int nameIdx = -1, bldgIdx = -1, roomNoIdx = -1, roomNameIdx = -1;

                for (int i = 0; i < Math.Min(15, lines.Length); i++)
                {
                    string[] cols = ParseCsvLine(lines[i]);
                    for (int c = 0; c < cols.Length; c++)
                    {
                        string val = cols[c].Trim();
                        if (val.Equals("Location Name", StringComparison.OrdinalIgnoreCase) || val.Equals("Name", StringComparison.OrdinalIgnoreCase))
                        {
                            headerIdx = i;
                            break;
                        }
                    }
                    if (headerIdx != -1) break;
                }

                if (headerIdx == -1)
                {
                    errors.Add("Could not find CSV header containing 'Location Name' or 'Name'.");
                    return (items, errors);
                }

                string[] headers = ParseCsvLine(lines[headerIdx]);
                for (int c = 0; c < headers.Length; c++)
                {
                    string h = headers[c].Trim();
                    if (h.Equals("Location Name", StringComparison.OrdinalIgnoreCase) || h.Equals("Name", StringComparison.OrdinalIgnoreCase)) nameIdx = c;
                    else if (h.Equals("Building", StringComparison.OrdinalIgnoreCase) || h.Equals("Address", StringComparison.OrdinalIgnoreCase)) bldgIdx = c;
                    else if (h.Equals("Room Number", StringComparison.OrdinalIgnoreCase)) roomNoIdx = c;
                    else if (h.Equals("Room Name", StringComparison.OrdinalIgnoreCase) || h.Equals("Allocated Person", StringComparison.OrdinalIgnoreCase)) roomNameIdx = c;
                }

                for (int i = headerIdx + 1; i < lines.Length; i++)
                {
                    if (string.IsNullOrWhiteSpace(lines[i])) continue;
                    string[] cols = ParseCsvLine(lines[i]);

                    string locName = (nameIdx >= 0 && nameIdx < cols.Length) ? cols[nameIdx].Trim() : "";
                    if (string.IsNullOrWhiteSpace(locName)) continue;

                    var loc = new Location();
                    SetProp(loc, "LocationName", locName);
                    SetProp(loc, "Name", locName);

                    string bldg = (bldgIdx >= 0 && bldgIdx < cols.Length) ? cols[bldgIdx].Trim() : "";
                    SetProp(loc, "Building", bldg);
                    SetProp(loc, "Address", bldg);

                    string roomNo = (roomNoIdx >= 0 && roomNoIdx < cols.Length) ? cols[roomNoIdx].Trim() : "";
                    SetProp(loc, "RoomNumber", roomNo);

                    string roomName = (roomNameIdx >= 0 && roomNameIdx < cols.Length) ? cols[roomNameIdx].Trim() : "";
                    SetProp(loc, "RoomName", roomName);

                    items.Add(loc);
                }
            }
            catch (Exception ex)
            {
                errors.Add($"CSV import error: {ex.Message}");
            }

            return (items, errors);
        }

        public static bool ExportLocations(string filePath, IEnumerable<Location> locations)
        {
            try
            {
                using var workbook = new XLWorkbook();
                var ws = workbook.Worksheets.Add("Locations");
                ws.Cell(1, 1).Value = "Location Name";
                ws.Cell(1, 2).Value = "Building";
                ws.Cell(1, 3).Value = "Room Number";
                ws.Cell(1, 4).Value = "Room Name";

                int row = 2;
                foreach (var loc in locations ?? Enumerable.Empty<Location>())
                {
                    ws.Cell(row, 1).Value = GetPropStr(loc, "LocationName", "Name");
                    ws.Cell(row, 2).Value = GetPropStr(loc, "Building", "Address", "AddressLine");
                    ws.Cell(row, 3).Value = GetPropStr(loc, "RoomNumber");
                    ws.Cell(row, 4).Value = GetPropStr(loc, "RoomName");
                    row++;
                }
                workbook.SaveAs(filePath);
                return true;
            }
            catch { return false; }
        }

        public static bool ExportLocations(IEnumerable<Location> locations, string filePath) => ExportLocations(filePath, locations);
        #endregion

        #region Equipment Import & Export
        public static (List<Equipment> Items, List<string> Errors) ImportEquipment(string filePath, object service = null)
        {
            var items = new List<Equipment>();
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                errors.Add("Selected file does not exist.");
                return (items, errors);
            }

            try
            {
                using var workbook = new XLWorkbook(filePath);
                IXLWorksheet worksheet = workbook.Worksheets.FirstOrDefault(w => w.Name.Equals("Equipment", StringComparison.OrdinalIgnoreCase))
                                         ?? workbook.Worksheets.FirstOrDefault();

                if (worksheet == null)
                {
                    errors.Add("No valid worksheets found for Equipment.");
                    return (items, errors);
                }

                IXLRow headerRow = null;
                int codeCol = -1, qrCol = -1, nameCol = -1, catCol = -1, locCol = -1,
                    statusCol = -1, serialCol = -1, makeCol = -1, modelCol = -1,
                    procTypeCol = -1, condCol = -1, purchDateCol = -1, priceCol = -1,
                    descCol = -1, vendorCol = -1, notesCol = -1, warrantyCol = -1;

                for (int r = 1; r <= 15; r++)
                {
                    var row = worksheet.Row(r);
                    foreach (var cell in row.CellsUsed())
                    {
                        string val = GetCellText(cell);
                        if (val.Equals("Equipment Code", StringComparison.OrdinalIgnoreCase) ||
                            val.Equals("Name", StringComparison.OrdinalIgnoreCase) ||
                            val.Equals("Equipment Name", StringComparison.OrdinalIgnoreCase))
                        {
                            headerRow = row;
                            break;
                        }
                    }
                    if (headerRow != null) break;
                }

                if (headerRow != null)
                {
                    foreach (var cell in headerRow.CellsUsed())
                    {
                        string raw = GetCellText(cell);
                        string h = raw.Replace("_", " ").Replace("-", " ").Trim();
                        if (h.Equals("Equipment Code", StringComparison.OrdinalIgnoreCase) || h.Equals("Code", StringComparison.OrdinalIgnoreCase)) codeCol = cell.Address.ColumnNumber;
                        else if (h.Equals("QR Code", StringComparison.OrdinalIgnoreCase) || h.Equals("QRCode", StringComparison.OrdinalIgnoreCase)) qrCol = cell.Address.ColumnNumber;
                        else if (h.Equals("Equipment Name", StringComparison.OrdinalIgnoreCase) || h.Equals("Name", StringComparison.OrdinalIgnoreCase)) nameCol = cell.Address.ColumnNumber;
                        else if (h.Equals("Category", StringComparison.OrdinalIgnoreCase)) catCol = cell.Address.ColumnNumber;
                        else if (h.Equals("Location", StringComparison.OrdinalIgnoreCase)) locCol = cell.Address.ColumnNumber;
                        else if (h.Equals("Status", StringComparison.OrdinalIgnoreCase)) statusCol = cell.Address.ColumnNumber;
                        else if (h.Equals("Serial Number", StringComparison.OrdinalIgnoreCase) || h.Equals("Serial No", StringComparison.OrdinalIgnoreCase) || h.Equals("SerialNumber", StringComparison.OrdinalIgnoreCase)) serialCol = cell.Address.ColumnNumber;
                        else if (h.Equals("Make", StringComparison.OrdinalIgnoreCase) || h.Equals("Manufacturer", StringComparison.OrdinalIgnoreCase) || h.Equals("Brand", StringComparison.OrdinalIgnoreCase)) makeCol = cell.Address.ColumnNumber;
                        else if (h.Equals("Model", StringComparison.OrdinalIgnoreCase)) modelCol = cell.Address.ColumnNumber;
                        else if (h.Equals("Procurement Type", StringComparison.OrdinalIgnoreCase) || h.Equals("ProcurementType", StringComparison.OrdinalIgnoreCase)) procTypeCol = cell.Address.ColumnNumber;
                        else if (h.Equals("Condition", StringComparison.OrdinalIgnoreCase) || h.Equals("Condition Grade", StringComparison.OrdinalIgnoreCase)) condCol = cell.Address.ColumnNumber;
                        else if (h.Equals("Purchase Date", StringComparison.OrdinalIgnoreCase) || h.Equals("PurchaseDate", StringComparison.OrdinalIgnoreCase)) purchDateCol = cell.Address.ColumnNumber;
                        else if (h.Equals("Price", StringComparison.OrdinalIgnoreCase) || h.Equals("Cost", StringComparison.OrdinalIgnoreCase)) priceCol = cell.Address.ColumnNumber;
                        else if (h.Equals("Description", StringComparison.OrdinalIgnoreCase)) descCol = cell.Address.ColumnNumber;
                        else if (h.Equals("Vendor", StringComparison.OrdinalIgnoreCase) || h.Equals("Vendor Name", StringComparison.OrdinalIgnoreCase)) vendorCol = cell.Address.ColumnNumber;
                        else if (h.Equals("Notes", StringComparison.OrdinalIgnoreCase) || h.Equals("Remarks", StringComparison.OrdinalIgnoreCase)) notesCol = cell.Address.ColumnNumber;
                        else if (h.Equals("Warranty Expiry", StringComparison.OrdinalIgnoreCase) || h.Equals("WarrantyExpiry", StringComparison.OrdinalIgnoreCase) || h.Equals("Warranty", StringComparison.OrdinalIgnoreCase)) warrantyCol = cell.Address.ColumnNumber;
                    }

                    int lastRow = worksheet.LastRowUsed()?.RowNumber() ?? headerRow.RowNumber();

                    // Resolve the EquipmentService for procurement-type resolution (if provided)
                    var eqService = service as Services.EquipmentService;

                    for (int r = headerRow.RowNumber() + 1; r <= lastRow; r++)
                    {
                        var row = worksheet.Row(r);
                        string eqName = nameCol > 0 ? GetCellText(row.Cell(nameCol)) : "";
                        if (string.IsNullOrWhiteSpace(eqName)) continue;

                        string eqCode = codeCol > 0 ? GetCellText(row.Cell(codeCol)) : "";
                        // Leave blank if the sheet has no code — EquipmentService.Add()
                        // will generate one that continues the real DB sequence
                        // (EQ001, EQ002...) instead of a separate counter that
                        // ignores what's already in the database.

                        var eq = new Equipment();
                        if (!string.IsNullOrWhiteSpace(eqCode))
                        {
                            SetProp(eq, "EquipmentCode", eqCode);
                            SetProp(eq, "Code", eqCode);
                        }
                        SetProp(eq, "Name", eqName);

                        // QR Code
                        string qr = qrCol > 0 ? CleanNa(GetCellText(row.Cell(qrCol))) : "";
                        if (!string.IsNullOrWhiteSpace(qr))
                        {
                            SetProp(eq, "QrCode", qr);
                        }

                        // Serial Number (blank / N/A treated as null by EquipmentService.Add)
                        string serial = serialCol > 0 ? CleanNa(GetCellText(row.Cell(serialCol))) : "";
                        SetProp(eq, "SerialNumber", serial);
                        SetProp(eq, "SerialNo", serial);

                        // Make & Model
                        string make = makeCol > 0 ? CleanNa(GetCellText(row.Cell(makeCol))) : "";
                        SetProp(eq, "Make", make);

                        string model = modelCol > 0 ? CleanNa(GetCellText(row.Cell(modelCol))) : "";
                        SetProp(eq, "Model", model);

                        // Category
                        string cat = catCol > 0 ? CleanNa(GetCellText(row.Cell(catCol))) : "";
                        SetProp(eq, "Category", cat);
                        SetProp(eq, "CategoryName", cat);

                        // Location
                        string loc = locCol > 0 ? CleanNa(GetCellText(row.Cell(locCol))) : "";
                        SetProp(eq, "Location", loc);
                        SetProp(eq, "LocationName", loc);

                        // Status — default to "in_store" if missing
                        string status = statusCol > 0 ? CleanNa(GetCellText(row.Cell(statusCol))) : "";
                        if (string.IsNullOrWhiteSpace(status)) status = "in_store";
                        // Normalise common display values to DB snake_case
                        if (status.Equals("In Store", StringComparison.OrdinalIgnoreCase) ||
                            status.Equals("In-Store", StringComparison.OrdinalIgnoreCase) ||
                            status.Equals("Available", StringComparison.OrdinalIgnoreCase))
                            status = "in_store";
                        else if (status.Equals("Allocated", StringComparison.OrdinalIgnoreCase))
                            status = "allocated";
                        else if (status.Equals("Under Repair", StringComparison.OrdinalIgnoreCase))
                            status = "under_repair";
                        else if (status.Equals("Decommissioned", StringComparison.OrdinalIgnoreCase))
                            status = "decommissioned";
                        SetProp(eq, "CurrentStatus", status);

                        string cond = condCol > 0 ? CleanNa(GetCellText(row.Cell(condCol))) : "";
                        eq.ConditionGrade = NormalizeConditionGrade(cond);   // set directly — keep true null, don't let SetProp coerce it to ""

                        // Purchase Date
                        if (purchDateCol > 0)
                        {
                            string dateStr = GetCellText(row.Cell(purchDateCol));
                            if (!string.IsNullOrWhiteSpace(dateStr) && DateTime.TryParse(dateStr, out var dt))
                                eq.PurchaseDate = DateOnly.FromDateTime(dt);
                        }

                        // Price
                        if (priceCol > 0)
                        {
                            string priceStr = CleanNa(GetCellText(row.Cell(priceCol)));
                            if (!string.IsNullOrWhiteSpace(priceStr) && decimal.TryParse(priceStr, out var pv))
                                eq.Price = pv;
                        }

                        // Procurement Type — resolve to ID if service available
                        if (procTypeCol > 0 && eqService != null)
                        {
                            string procTypeName = CleanNa(GetCellText(row.Cell(procTypeCol)));
                            if (!string.IsNullOrWhiteSpace(procTypeName))
                                eq.ProcurementTypeId = eqService.ResolveProcurementTypeId(procTypeName);
                        }

                        // Description, Vendor, Notes, Warranty
                        string desc = descCol > 0 ? CleanNa(GetCellText(row.Cell(descCol))) : "";
                        SetProp(eq, "Description", desc);

                        string vendor = vendorCol > 0 ? CleanNa(GetCellText(row.Cell(vendorCol))) : "";
                        SetProp(eq, "VendorName", vendor);

                        string notes = notesCol > 0 ? CleanNa(GetCellText(row.Cell(notesCol))) : "";
                        SetProp(eq, "Notes", notes);

                        if (warrantyCol > 0)
                        {
                            string warStr = GetCellText(row.Cell(warrantyCol));
                            if (!string.IsNullOrWhiteSpace(warStr) && DateTime.TryParse(warStr, out var wdt))
                                eq.WarrantyExpiry = DateOnly.FromDateTime(wdt);
                        }

                        items.Add(eq);
                    }
                }
                else
                {
                    errors.Add("Could not find header row containing 'Equipment Code', 'Name', or 'Equipment Name'.");
                }
            }
            catch (Exception ex)
            {
                errors.Add($"Equipment import error: {ex.Message}");
            }

            return (items, errors);
        }

        /// <summary>Converts "N/A", "NA", "-" to empty string so downstream code treats them as missing.</summary>
        private static string CleanNa(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "";
            var trimmed = value.Trim();
            if (trimmed.Equals("N/A", StringComparison.OrdinalIgnoreCase) ||
                trimmed.Equals("NA", StringComparison.OrdinalIgnoreCase) ||
                trimmed == "-")
                return "";
            return trimmed;
        }

        private static readonly string[] ValidConditionGrades = { "New", "Good", "Fair", "Poor", "Unserviceable" };

        private static string NormalizeConditionGrade(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;

            string trimmed = value.Trim();

            // Exact match (case-insensitive) against the DB enum
            var match = ValidConditionGrades.FirstOrDefault(v =>
                v.Equals(trimmed, StringComparison.OrdinalIgnoreCase));
            if (match != null) return match;

            // Common alternate spellings — extend this list as needed
            if (trimmed.Equals("Excellent", StringComparison.OrdinalIgnoreCase)) return "New";
            if (trimmed.Equals("Used", StringComparison.OrdinalIgnoreCase)) return "Fair";
            if (trimmed.Equals("Damaged", StringComparison.OrdinalIgnoreCase)) return "Poor";
            if (trimmed.Equals("Not Working", StringComparison.OrdinalIgnoreCase) ||
                trimmed.Equals("Broken", StringComparison.OrdinalIgnoreCase)) return "Unserviceable";

            // Unknown value — don't let it crash the whole import; leave blank instead
            return null;
        }

        public static bool ExportEquipment(string filePath, IEnumerable<Equipment> equipments)
        {
            try
            {
                using var workbook = new XLWorkbook();
                var ws = workbook.Worksheets.Add("Equipment");
                ws.Cell(1, 1).Value = "Equipment Code";
                ws.Cell(1, 2).Value = "Name";
                ws.Cell(1, 3).Value = "Category";
                ws.Cell(1, 4).Value = "Location";
                ws.Cell(1, 5).Value = "Status";
                ws.Cell(1, 6).Value = "Serial Number";

                int row = 2;
                foreach (var eq in equipments ?? Enumerable.Empty<Equipment>())
                {
                    ws.Cell(row, 1).Value = GetPropStr(eq, "EquipmentCode", "Code");
                    ws.Cell(row, 2).Value = GetPropStr(eq, "Name");
                    ws.Cell(row, 3).Value = GetPropStr(eq, "Category", "CategoryName");
                    ws.Cell(row, 4).Value = GetPropStr(eq, "Location", "LocationName");
                    ws.Cell(row, 5).Value = GetPropStr(eq, "Status", "EquipmentStatus");
                    ws.Cell(row, 6).Value = GetPropStr(eq, "SerialNumber", "SerialNo");
                    row++;
                }
                workbook.SaveAs(filePath);
                return true;
            }
            catch { return false; }
        }

        public static bool ExportEquipment(IEnumerable<Equipment> equipments, string filePath) => ExportEquipment(filePath, equipments);
        #endregion

        #region Property Helper Utilities
        private static bool HasHeader(IXLWorksheet ws, params string[] headerNames)
        {
            int maxR = Math.Min(10, ws.LastRowUsed()?.RowNumber() ?? 1);
            for (int r = 1; r <= maxR; r++)
            {
                foreach (var cell in ws.Row(r).CellsUsed())
                {
                    string val = GetCellText(cell);
                    if (headerNames.Any(hn => val.Equals(hn, StringComparison.OrdinalIgnoreCase)))
                        return true;
                }
            }
            return false;
        }

        private static string GetCellText(IXLCell cell)
        {
            if (cell == null || cell.IsEmpty()) return string.Empty;
            return cell.GetFormattedString().Trim();
        }

        private static void SetProp(object obj, string propName, object value)
        {
            var p = obj.GetType().GetProperty(propName);
            if (p != null && p.CanWrite)
            {
                try
                {
                    if (p.PropertyType == typeof(string))
                        p.SetValue(obj, value?.ToString() ?? "");
                    else if (p.PropertyType == typeof(int) || p.PropertyType == typeof(int?))
                    {
                        if (int.TryParse(value?.ToString(), out int val))
                            p.SetValue(obj, val);
                    }
                }
                catch { }
            }
        }

        private static string GetPropStr(object obj, params string[] propNames)
        {
            foreach (var name in propNames)
            {
                var p = obj.GetType().GetProperty(name);
                if (p != null)
                {
                    var val = p.GetValue(obj);
                    if (val != null) return val.ToString();
                }
            }
            return string.Empty;
        }

        private static string[] ParseCsvLine(string line)
        {
            var result = new List<string>();
            bool inQuotes = false;
            var current = new System.Text.StringBuilder();

            foreach (char c in line)
            {
                if (c == '"')
                {
                    inQuotes = !inQuotes;
                }
                else if (c == ',' && !inQuotes)
                {
                    result.Add(current.ToString());
                    current.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }
            result.Add(current.ToString());
            return result.ToArray();
        }
        #endregion
    }
}