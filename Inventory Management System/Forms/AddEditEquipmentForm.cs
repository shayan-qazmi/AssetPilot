// Forms/AddEditEquipmentForm.cs
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Inventory_Management_System.Helpers;
using Inventory_Management_System.Models;
using Inventory_Management_System.Services;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management_System.Forms
{
    public class AddEditEquipmentForm : Form
    {
        private Equipment _equipment;
        private EquipmentService _equipmentService;
        private bool _isEditMode;

        // ── Existing fields ───────────────────────────────────────────────
        private TextBox      txtEquipmentCode = null!;
        private TextBox      txtQrCode        = null!;
        private TextBox      txtName          = null!;
        private TextBox      txtMake          = null!;
        private TextBox      txtModel         = null!;
        private TextBox      txtSerialNumber  = null!;
        private ComboBox     cmbStatus        = null!;
        private DateTimePicker dtpPurchaseDate = null!;

        // ── New fields ────────────────────────────────────────────────────
        private ComboBox     cmbCategory       = null!;
        private TextBox      txtDescription    = null!;
        private TextBox      txtVendorName     = null!;
        private ComboBox     cmbConditionGrade = null!;
        private CheckBox     chkWarrantyExpiry = null!;
        private DateTimePicker dtpWarrantyExpiry = null!;
        private TextBox      txtNotes          = null!;

        // ── Added fields ──────────────────────────────────────────────────
        private ComboBox cmbLocation        = null!;
        private ComboBox cmbProcurementType = null!;

        private Button btnSave   = null!;
        private Button btnCancel = null!;

        // Data loaded from DB
        private List<Location>        _locations        = new();
        private List<ProcurementType> _procurementTypes = new();

        public AddEditEquipmentForm(Equipment? equipment = null)
        {
            _equipment        = equipment ?? new Equipment { CurrentStatus = "in_store" };
            _isEditMode       = equipment != null;
            _equipmentService = new EquipmentService();

            InitializeUI();
            LoadDropdownData();
            LoadData();
        }

        private void InitializeUI()
        {
            Text            = _isEditMode ? "Edit Equipment" : "Add Equipment";
            StartPosition   = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox     = false;
            MinimizeBox     = false;
            AutoScroll      = true;

            ThemeHelper.ApplyFormBackground(this);

            int x = 20;
            int w = 340;
            int y = 20;

            // ── Equipment Code ────────────────────────────────────────────
            AddLabel("Equipment Code:", x, ref y);
            txtEquipmentCode = AddTextBox(x, w, ref y);
            if (_isEditMode) txtEquipmentCode.Enabled = false;

            // ── QR Code ───────────────────────────────────────────────────
            AddLabel("QR Code:", x, ref y);
            txtQrCode = AddTextBox(x, w, ref y);
            if (_isEditMode) txtQrCode.Enabled = false;

            // ── Name ──────────────────────────────────────────────────────
            AddLabel("Name:", x, ref y);
            txtName = AddTextBox(x, w, ref y);

            // ── Make ──────────────────────────────────────────────────────
            AddLabel("Make:", x, ref y);
            txtMake = AddTextBox(x, w, ref y);

            // ── Model ─────────────────────────────────────────────────────
            AddLabel("Model:", x, ref y);
            txtModel = AddTextBox(x, w, ref y);

            // ── Serial Number ─────────────────────────────────────────────
            AddLabel("Serial Number:", x, ref y);
            txtSerialNumber = AddTextBox(x, w, ref y);

            // ── Status ────────────────────────────────────────────────────
            AddLabel("Current Status:", x, ref y);
            cmbStatus = AddComboBox(x, w, ref y,
                new[] { "in_store", "allocated", "decommissioned", "under_repair" });

            // ── Purchase Date ─────────────────────────────────────────────
            AddLabel("Purchase Date:", x, ref y);
            dtpPurchaseDate = new DateTimePicker
            {
                Location = new Point(x, y), Width = w,
                Format   = DateTimePickerFormat.Short
            };
            Controls.Add(dtpPurchaseDate);
            y += 40;

            // ── Category ──────────────────────────────────────────────────
            AddLabel("Category:", x, ref y);
            cmbCategory = AddComboBox(x, w, ref y,
                new[] { "", "IT Equipment", "Furniture", "Vehicle", "Accessory", "Other" });

            // ── Description ───────────────────────────────────────────────
            AddLabel("Description:", x, ref y);
            txtDescription = new TextBox
            {
                Location   = new Point(x, y), Width = w, Height = 64,
                Multiline  = true, ScrollBars = ScrollBars.Vertical
            };
            ThemeHelper.StyleTextBox(txtDescription);
            Controls.Add(txtDescription);
            y += 72;

            // ── Vendor Name ───────────────────────────────────────────────
            AddLabel("Vendor Name:", x, ref y);
            txtVendorName = AddTextBox(x, w, ref y);

            // ── Condition Grade ───────────────────────────────────────────
            AddLabel("Condition Grade:", x, ref y);
            cmbConditionGrade = AddComboBox(x, w, ref y,
                new[] { "", "New", "Good", "Fair", "Poor", "Unserviceable" });

            // ── Warranty Expiry ───────────────────────────────────────────
            chkWarrantyExpiry = new CheckBox
            {
                Text      = "Warranty Expiry:",
                Location  = new Point(x, y),
                AutoSize  = true,
                Checked   = false,
                Font      = ThemeHelper.FontDefault,
                ForeColor = ThemeHelper.TextPrimary,
                BackColor = Color.Transparent
            };
            Controls.Add(chkWarrantyExpiry);
            y += 24;

            dtpWarrantyExpiry = new DateTimePicker
            {
                Location = new Point(x, y), Width = w,
                Format   = DateTimePickerFormat.Short,
                Enabled  = false
            };
            Controls.Add(dtpWarrantyExpiry);
            chkWarrantyExpiry.CheckedChanged += (s, ev) =>
                dtpWarrantyExpiry.Enabled = chkWarrantyExpiry.Checked;
            y += 40;

            // ── Location (display only — no FK on Equipment) ──────────────
            AddLabel("Location:", x, ref y);
            cmbLocation = new ComboBox
            {
                Location      = new Point(x, y),
                Width         = w,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            ThemeHelper.StyleComboBox(cmbLocation);
            Controls.Add(cmbLocation);
            y += 34;

            // ── Procurement Type ──────────────────────────────────────────
            AddLabel("Procurement Type:", x, ref y);
            cmbProcurementType = new ComboBox
            {
                Location      = new Point(x, y),
                Width         = w,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            ThemeHelper.StyleComboBox(cmbProcurementType);
            Controls.Add(cmbProcurementType);
            y += 34;

            // ── Notes ─────────────────────────────────────────────────────
            AddLabel("Notes:", x, ref y);
            txtNotes = new TextBox
            {
                Location   = new Point(x, y), Width = w, Height = 64,
                Multiline  = true, ScrollBars = ScrollBars.Vertical
            };
            ThemeHelper.StyleTextBox(txtNotes);
            Controls.Add(txtNotes);
            y += 72;

            // ── Buttons ───────────────────────────────────────────────────
            y += 16;
            btnSave = new Button
            {
                Text     = "Save",
                Location = new Point(x + w - 160, y),
                Width    = 75, Height = 30
            };
            ThemeHelper.StyleButton(btnSave);
            btnSave.Click += BtnSave_Click;
            Controls.Add(btnSave);

            btnCancel = new Button
            {
                Text     = "Cancel",
                Location = new Point(x + w - 76, y),
                Width    = 76, Height = 30
            };
            ThemeHelper.StyleSecondaryButton(btnCancel);
            btnCancel.Click += (s, e) => DialogResult = DialogResult.Cancel;
            Controls.Add(btnCancel);

            ClientSize   = new Size(w + 2 * x, y + 30 + 24);
            AcceptButton = btnSave;
            CancelButton = btnCancel;
        }

        // ── Dropdown data from DB ─────────────────────────────────────────
        private void LoadDropdownData()
        {
            try
            {
                using var db = new InventoryDbContext();

                // Locations — display only
                _locations = db.Locations.OrderBy(l => l.LocationName).ToList();
                cmbLocation.Items.Add("— None —");
                foreach (var loc in _locations)
                    cmbLocation.Items.Add(loc.LocationName);
                cmbLocation.SelectedIndex = 0;

                // Procurement Types
                _procurementTypes = db.ProcurementTypes.OrderBy(p => p.TypeName).ToList();
                cmbProcurementType.Items.Add("— None —");
                foreach (var pt in _procurementTypes)
                    cmbProcurementType.Items.Add(pt.TypeName);
                cmbProcurementType.SelectedIndex = 0;
            }
            catch
            {
                // DB unavailable — dropdowns stay with "— None —"
            }
        }

        private void LoadData()
        {
            if (!_isEditMode)
            {
                cmbStatus.SelectedIndex         = 0;
                cmbCategory.SelectedIndex       = 0;
                cmbConditionGrade.SelectedIndex = 0;
                return;
            }

            // Reload fresh from DB with all includes
            try
            {
                using var db = new InventoryDbContext();
                var eq = db.Equipment
                    .Include(e => e.Allocations)
                    .ThenInclude(a => a.Location)
                    .FirstOrDefault(e => e.Id == _equipment.Id);
                if (eq != null) _equipment = eq;
            }
            catch { }

            txtEquipmentCode.Text   = _equipment.EquipmentCode;
            txtQrCode.Text          = _equipment.QrCode;
            txtName.Text            = _equipment.Name;
            txtMake.Text            = _equipment.Make;
            txtModel.Text           = _equipment.Model;
            txtSerialNumber.Text    = _equipment.SerialNumber;
            cmbStatus.SelectedItem  = _equipment.CurrentStatus;

            if (_equipment.PurchaseDate.HasValue)
                dtpPurchaseDate.Value =
                    _equipment.PurchaseDate.Value.ToDateTime(TimeOnly.MinValue);

            cmbCategory.SelectedItem       = _equipment.Category ?? "";
            txtDescription.Text            = _equipment.Description ?? "";
            txtVendorName.Text             = _equipment.VendorName ?? "";
            cmbConditionGrade.SelectedItem = _equipment.ConditionGrade ?? "";
            txtNotes.Text                  = _equipment.Notes ?? "";

            if (_equipment.WarrantyExpiry.HasValue)
            {
                chkWarrantyExpiry.Checked = true;
                dtpWarrantyExpiry.Enabled = true;
                dtpWarrantyExpiry.Value   =
                    _equipment.WarrantyExpiry.Value.ToDateTime(TimeOnly.MinValue);
            }

            // Procurement Type
            if (_equipment.ProcurementTypeId.HasValue)
            {
                var pt = _procurementTypes
                    .FirstOrDefault(p => p.Id == _equipment.ProcurementTypeId.Value);
                if (pt != null)
                {
                    int idx = cmbProcurementType.Items.IndexOf(pt.TypeName);
                    if (idx >= 0) cmbProcurementType.SelectedIndex = idx;
                }
            }

            // Location — from active allocation
            var activeAllocation = _equipment.Allocations
                .FirstOrDefault(a => a.Status != null &&
                                     a.Status.Equals("approved", StringComparison.OrdinalIgnoreCase));
            if (activeAllocation?.Location != null)
            {
                int idx = cmbLocation.Items.IndexOf(activeAllocation.Location.LocationName);
                if (idx >= 0) cmbLocation.SelectedIndex = idx;
            }
        }
        private void BtnSave_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtName.Text) ||
                string.IsNullOrWhiteSpace(txtEquipmentCode.Text) ||
                string.IsNullOrWhiteSpace(txtQrCode.Text))
            {
                MessageBox.Show("Equipment Code, QR Code, and Name are required.",
                    "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _equipment.EquipmentCode  = txtEquipmentCode.Text.Trim();
            _equipment.QrCode         = txtQrCode.Text.Trim();
            _equipment.Name           = txtName.Text.Trim();
            _equipment.Make           = txtMake.Text.Trim();
            _equipment.Model          = txtModel.Text.Trim();
            _equipment.SerialNumber   = txtSerialNumber.Text.Trim();
            _equipment.CurrentStatus  = cmbStatus.SelectedItem?.ToString() ?? "in_store";
            _equipment.PurchaseDate   = DateOnly.FromDateTime(dtpPurchaseDate.Value);

            _equipment.Category =
                cmbCategory.SelectedItem?.ToString() is { Length: > 0 } cat ? cat : null;
            _equipment.Description =
                string.IsNullOrWhiteSpace(txtDescription.Text) ? null : txtDescription.Text.Trim();
            _equipment.VendorName =
                string.IsNullOrWhiteSpace(txtVendorName.Text) ? null : txtVendorName.Text.Trim();
            _equipment.ConditionGrade =
                cmbConditionGrade.SelectedItem?.ToString() is { Length: > 0 } cg ? cg : null;
            _equipment.WarrantyExpiry = chkWarrantyExpiry.Checked
                ? DateOnly.FromDateTime(dtpWarrantyExpiry.Value) : null;
            _equipment.Notes =
                string.IsNullOrWhiteSpace(txtNotes.Text) ? null : txtNotes.Text.Trim();

            // Procurement Type
            var selectedPt = cmbProcurementType.SelectedItem?.ToString();
            if (selectedPt != null && selectedPt != "— None —")
            {
                var pt = _procurementTypes.FirstOrDefault(p => p.TypeName == selectedPt);
                _equipment.ProcurementTypeId = pt?.Id;
            }
            else
            {
                _equipment.ProcurementTypeId = null;
            }

            if (!_isEditMode)
                _equipment.CreatedByUserId = 1;

            var result = _isEditMode
                ? _equipmentService.Update(_equipment)
                : _equipmentService.Add(_equipment);

            if (result.Success)
                DialogResult = DialogResult.OK;
            else
                MessageBox.Show(result.ErrorMessage, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        // ── UI helpers ────────────────────────────────────────────────────
        private void AddLabel(string text, int x, ref int y)
        {
            var lbl = new Label { Text = text, Location = new Point(x, y), AutoSize = true };
            ThemeHelper.StyleLabel(lbl);
            Controls.Add(lbl);
            y += 22;
        }

        private TextBox AddTextBox(int x, int w, ref int y)
        {
            var txt = new TextBox { Location = new Point(x, y), Width = w, Height = 26 };
            ThemeHelper.StyleTextBox(txt);
            Controls.Add(txt);
            y += 34;
            return txt;
        }

        private ComboBox AddComboBox(int x, int w, ref int y, string[] items)
        {
            var cmb = new ComboBox
            {
                Location      = new Point(x, y),
                Width         = w,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmb.Items.AddRange(items);
            ThemeHelper.StyleComboBox(cmb);
            Controls.Add(cmb);
            y += 34;
            return cmb;
        }
    }
}