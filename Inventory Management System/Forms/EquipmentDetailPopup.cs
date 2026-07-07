using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Microsoft.EntityFrameworkCore;
using Inventory_Management_System.Models;
using Inventory_Management_System.Helpers;

namespace Inventory_Management_System.Forms
{
    public class EquipmentDetailPopup : Form
    {
        private Equipment _equipment;

        public EquipmentDetailPopup(Equipment equipment)
        {
            _equipment = equipment;

            using var db = new InventoryDbContext();
            var eq = db.Equipment
                .Include(e => e.Allocations)
                    .ThenInclude(a => a.Location)
                .Include(e => e.Allocations)
                    .ThenInclude(a => a.Employee)
                .Include(e => e.ProcurementType)
                .FirstOrDefault(e => e.Id == equipment.Id);
            if (eq != null) _equipment = eq;

            InitializeUI();
        }

        private void InitializeUI()
        {
            Text = $"Equipment Details — {_equipment.EquipmentCode}";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            StartPosition = FormStartPosition.CenterParent;

            ThemeHelper.ApplyFormBackground(this);

            int yLeft = 20;

            // Section: Equipment Information
            var lblEqInfo = new Label
            {
                Text = "Equipment Information",
                Location = new Point(20, yLeft),
                AutoSize = true,
                Font = ThemeHelper.FontBold,
                ForeColor = ThemeHelper.Success
            };
            Controls.Add(lblEqInfo);
            yLeft += 30;

            bool isAltLeft = false;

            AddField("Equipment Code", _equipment.EquipmentCode, 20, ref yLeft, ref isAltLeft);
            AddField("QR Code", _equipment.QrCode, 20, ref yLeft, ref isAltLeft);
            AddField("Name", _equipment.Name, 20, ref yLeft, ref isAltLeft);
            AddField("Make", _equipment.Make, 20, ref yLeft, ref isAltLeft);
            AddField("Model", _equipment.Model, 20, ref yLeft, ref isAltLeft);
            AddField("Serial Number", _equipment.SerialNumber, 20, ref yLeft, ref isAltLeft);
            AddField("Category", _equipment.Category, 20, ref yLeft, ref isAltLeft);

            // Status Badge
            AddStatusField("Status", _equipment.CurrentStatus, 20, ref yLeft, ref isAltLeft);

            AddField("Condition", _equipment.ConditionGrade, 20, ref yLeft, ref isAltLeft);
            AddField("Purchase Date", _equipment.PurchaseDate?.ToString("yyyy-MM-dd"), 20, ref yLeft, ref isAltLeft);
            AddField("Price", _equipment.Price?.ToString("C"), 20, ref yLeft, ref isAltLeft);
            AddField("Procurement Type", _equipment.ProcurementType?.TypeName, 20, ref yLeft, ref isAltLeft);
            AddField("Vendor Name", _equipment.VendorName, 20, ref yLeft, ref isAltLeft);
            AddField("Warranty Expiry", _equipment.WarrantyExpiry?.ToString("yyyy-MM-dd") ?? "—", 20, ref yLeft, ref isAltLeft);
            AddField("Description", _equipment.Description, 20, ref yLeft, ref isAltLeft);
            AddField("Notes", _equipment.Notes, 20, ref yLeft, ref isAltLeft);

            int yRight = 20;

            // Section: Current Allocation
            var lblAllocInfo = new Label
            {
                Text = "Current Allocation",
                Location = new Point(420, yRight),
                AutoSize = true,
                Font = ThemeHelper.FontBold,
                ForeColor = ThemeHelper.Success
            };
            Controls.Add(lblAllocInfo);
            yRight += 30;

            var activeAlloc = _equipment.Allocations.FirstOrDefault(a => a.Status == "approved");
            bool isAltRight = false;

            AddField("Current Location", activeAlloc?.Location?.LocationName ?? "—", 420, ref yRight, ref isAltRight);
            AddField("Currently Assigned To", activeAlloc?.Employee?.FullName ?? "—", 420, ref yRight, ref isAltRight);
            AddField("Allocation Date", activeAlloc?.AllocationDate.ToString("yyyy-MM-dd") ?? "—", 420, ref yRight, ref isAltRight);
            AddField("Expected Return", activeAlloc?.ExpectedReturnDate?.ToString("yyyy-MM-dd") ?? "—", 420, ref yRight, ref isAltRight);

            int maxY = Math.Max(yLeft, yRight) + 30;

            var btnClose = new Button
            {
                Text = "Close",
                Size = new Size(80, 32),
                Location = new Point((820 - 80) / 2, maxY)
            };
            ThemeHelper.StyleSecondaryButton(btnClose);
            btnClose.Click += (s, e) => Close();
            Controls.Add(btnClose);
            
            maxY += 52; // Button height + bottom padding

            this.ClientSize = new Size(820, maxY);
        }

        private void AddField(string labelName, string? valueText, int x, ref int y, ref bool isAlt)
        {
            var lblName = new Label
            {
                Text = labelName + ":",
                Location = new Point(5, 8),
                AutoSize = true,
                MaximumSize = new Size(150, 0),
                Font = ThemeHelper.FontBold,
                ForeColor = ThemeHelper.TextPrimary
            };

            var lblValue = new Label
            {
                Text = string.IsNullOrWhiteSpace(valueText) ? "—" : valueText,
                Location = new Point(165, 8),
                AutoSize = true,
                MaximumSize = new Size(210, 0),
                Font = ThemeHelper.FontDefault,
                ForeColor = ThemeHelper.TextPrimary
            };

            int nameH = lblName.GetPreferredSize(new Size(150, 0)).Height;
            int valH = lblValue.GetPreferredSize(new Size(210, 0)).Height;
            int pnlHeight = Math.Max(32, Math.Max(nameH, valH) + 16);

            var pnl = new Panel
            {
                Location = new Point(x, y),
                Size = new Size(380, pnlHeight),
                BackColor = isAlt ? Color.FromArgb(248, 250, 248) : Color.White
            };

            pnl.Controls.Add(lblName);
            pnl.Controls.Add(lblValue);
            Controls.Add(pnl);

            y += pnlHeight;
            isAlt = !isAlt;
        }

        private void AddStatusField(string labelName, string status, int x, ref int y, ref bool isAlt)
        {
            var lblName = new Label
            {
                Text = labelName + ":",
                Location = new Point(5, 8),
                AutoSize = true,
                MaximumSize = new Size(150, 0),
                Font = ThemeHelper.FontBold,
                ForeColor = ThemeHelper.TextPrimary
            };

            var lblValue = new Label
            {
                Text = status,
                Location = new Point(165, 6),
                AutoSize = true,
                Font = ThemeHelper.FontBold,
                ForeColor = Color.White,
                Padding = new Padding(2, 2, 2, 2)
            };

            switch (status)
            {
                case "in_store":
                    lblValue.BackColor = ThemeHelper.Success;
                    break;
                case "allocated":
                    lblValue.BackColor = ThemeHelper.Warning;
                    break;
                case "under_repair":
                    lblValue.BackColor = Color.FromArgb(255, 112, 67);
                    break;
                case "decommissioned":
                    lblValue.BackColor = ThemeHelper.Danger;
                    break;
                default:
                    lblValue.BackColor = ThemeHelper.GridAltRow;
                    break;
            }

            int nameH = lblName.GetPreferredSize(new Size(150, 0)).Height;
            int valH = lblValue.GetPreferredSize(new Size(0, 0)).Height;
            int pnlHeight = Math.Max(32, Math.Max(nameH, valH) + 16);

            var pnl = new Panel
            {
                Location = new Point(x, y),
                Size = new Size(380, pnlHeight),
                BackColor = isAlt ? Color.FromArgb(248, 250, 248) : Color.White
            };

            pnl.Controls.Add(lblName);
            pnl.Controls.Add(lblValue);
            Controls.Add(pnl);

            y += pnlHeight;
            isAlt = !isAlt;
        }
    }
}
