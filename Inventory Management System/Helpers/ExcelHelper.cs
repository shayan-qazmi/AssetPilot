// Helpers/ExcelHelper.cs
using ClosedXML.Excel;
using Inventory_Management_System.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Inventory_Management_System.Helpers
{
    public static class ExcelHelper
    {
        /// <summary>
        /// Reads equipment rows from an Excel file.
        /// Expected columns (row 1 = header, row 2+ = data):
        /// A: equipment_code  B: qr_code  C: name  D: make
        /// E: model  F: serial_number  G: current_status  H: purchase_date
        /// </summary>
        public static List<Equipment> ImportEquipment(string filePath)
        {
            var list = new List<Equipment>();

            using var wb = new XLWorkbook(filePath);
            var ws = wb.Worksheet(1);

            foreach (var row in ws.RowsUsed().Skip(1))
            {
                try
                {
                    var eq = new Equipment
                    {
                        EquipmentCode = row.Cell(1).GetString().Trim().ToUpper(),
                        QrCode = row.Cell(2).GetString().Trim(),
                        Name = row.Cell(3).GetString().Trim(),
                        Make = row.Cell(4).GetString().Trim(),
                        Model = row.Cell(5).GetString().Trim(),
                        SerialNumber = row.Cell(6).GetString().Trim().ToUpper(),
                        CurrentStatus = string.IsNullOrWhiteSpace(row.Cell(7).GetString())
                       ? "in_store"
                       : row.Cell(7).GetString().Trim().ToLower(),
                        PurchaseDate = row.Cell(8).TryGetValue<DateTime>(out var dt) ? DateOnly.FromDateTime(dt) : null,
                        CreatedByUserId = 1
                    };


                    // Skip empty rows
                    if (!string.IsNullOrWhiteSpace(eq.EquipmentCode))
                        list.Add(eq);
                }
                catch
                {
                    // Skip malformed rows silently
                }
            }

            return list;
        }

        /// <summary>
        /// Exports a list of equipment to an Excel file at the given path.
        /// </summary>
        public static void ExportEquipment(List<Equipment> items, string filePath)
        {
            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Equipment");

            // Headers
            ws.Cell(1, 1).Value = "Equipment Code";
            ws.Cell(1, 2).Value = "QR Code";
            ws.Cell(1, 3).Value = "Name";
            ws.Cell(1, 4).Value = "Make";
            ws.Cell(1, 5).Value = "Model";
            ws.Cell(1, 6).Value = "Serial Number";
            ws.Cell(1, 7).Value = "Status";
            ws.Cell(1, 8).Value = "Purchase Date";

            // Style header row
            var headerRow = ws.Range(1, 1, 1, 8);
            headerRow.Style.Font.Bold      = true;
            headerRow.Style.Fill.BackgroundColor = XLColor.FromArgb(46, 125, 50);
            headerRow.Style.Font.FontColor = XLColor.White;

            // Data rows
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
    }
}