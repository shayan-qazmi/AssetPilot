// Forms/AddEditEquipmentForm.cs
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
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

        // ── Form controls (unchanged) ──────────────────────────────────────
        private TextBox       txtEquipmentCode  = null!;
        private TextBox       txtQrCode         = null!;
        private TextBox       txtName           = null!;
        private TextBox       txtMake           = null!;
        private TextBox       txtModel          = null!;
        private TextBox       txtSerialNumber   = null!;
        private ComboBox      cmbStatus         = null!;
        private DateTimePicker dtpPurchaseDate  = null!;
        private ComboBox      cmbCategory       = null!;
        private TextBox       txtDescription    = null!;
        private TextBox       txtVendorName     = null!;
        private ComboBox      cmbConditionGrade = null!;
        private CheckBox      chkWarrantyExpiry = null!;
        private DateTimePicker dtpWarrantyExpiry = null!;
        private TextBox       txtNotes          = null!;
        private Label?        lblLocationValue  = null;
        private ComboBox      cmbProcurementType = null!;
        private Button        btnSave           = null!;
        private Button        btnCancel         = null!;

        private List<ProcurementType> _procurementTypes = new();

        // ── Layout constants (matching EquipmentDetailPopup) ───────────────
        private const int DialogW     = 860;
        private const int OuterPad    = 20;
        private const int HeaderH     = 52;
        private const int ColLeft     = 20;
        private const int ColRight    = 440;
        private const int ColW        = 390;
        private const int FieldX      = 155;   // input start inside a row panel
        private const int LabelW      = 148;
        private const int InputW      = 215;
        private const int RowH        = 38;
        private const int MultiRowH   = 88;
        private const int SectionGap  = 14;

        // ── Colors (same as EquipmentDetailPopup) ─────────────────────────
        private static readonly Color BgPage      = Color.FromArgb(245, 247, 250);
        private static readonly Color CardBg      = Color.White;
        private static readonly Color CardBorder  = Color.FromArgb(220, 225, 230);
        private static readonly Color SectionClr  = Color.FromArgb(46, 125, 50);
        private static readonly Color RowEven     = Color.White;
        private static readonly Color RowOdd      = Color.FromArgb(248, 250, 248);
        private static readonly Color SeparatorClr = Color.FromArgb(234, 237, 241);

        public AddEditEquipmentForm(Equipment? equipment = null)
        {
            _equipment        = equipment ?? new Equipment { CurrentStatus = "in_store" };
            _isEditMode       = equipment != null;
            _equipmentService = new EquipmentService();

            InitializeUI();
            LoadDropdownData();
            LoadData();
        }

        // ══════════════════════════════════════════════════════════════════
        //  UI LAYOUT
        // ══════════════════════════════════════════════════════════════════
        private void InitializeUI()
        {
            Text            = _isEditMode ? "Edit Equipment" : "Add Equipment";
            StartPosition   = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox     = false;
            MinimizeBox     = false;
            BackColor       = BgPage;

            // ── Header strip ─────────────────────────────────────────────
            var header = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = HeaderH,
                BackColor = ThemeHelper.Header,
            };
            var lblTitle = new Label
            {
                Text      = $"  {(_isEditMode ? "Edit" : "Add")} Equipment",
                Dock      = DockStyle.Fill,
                Font      = new Font("Segoe UI", 11.5f, FontStyle.Bold, GraphicsUnit.Point),
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding   = new Padding(8, 0, 0, 0),
            };
            header.Controls.Add(lblTitle);
            Controls.Add(header);

            // ── Card container ────────────────────────────────────────────
            var card = new Panel
            {
                BackColor = CardBg,
                Location  = new Point(OuterPad, HeaderH + OuterPad),
                Width     = DialogW - OuterPad * 2,
            };
            card.Paint += DrawCardBorder;
            Controls.Add(card);

            // ════ LEFT COLUMN — Equipment Identification ════
            int yL = SectionGap;
            AddSectionHeader(card, "Equipment Identification", ColLeft, ref yL);
            bool altL = false;

            txtEquipmentCode = AddFieldRow(card, "Equipment Code *", ColLeft, ref yL, ref altL);
            if (_isEditMode) txtEquipmentCode.Enabled = false;

            txtQrCode = AddFieldRow(card, "QR Code *", ColLeft, ref yL, ref altL);
            if (_isEditMode) txtQrCode.Enabled = false;

            txtName         = AddFieldRow(card, "Name *",          ColLeft, ref yL, ref altL);
            txtMake         = AddFieldRow(card, "Make",            ColLeft, ref yL, ref altL);
            txtModel        = AddFieldRow(card, "Model",           ColLeft, ref yL, ref altL);
            txtSerialNumber = AddFieldRow(card, "Serial Number",   ColLeft, ref yL, ref altL);

            cmbStatus = AddComboRow(card, "Current Status", ColLeft, ref yL, ref altL,
                new[] { "in_store", "allocated", "decommissioned", "under_repair" });

            dtpPurchaseDate = AddDateRow(card, "Purchase Date", ColLeft, ref yL, ref altL);

            cmbCategory = AddComboRow(card, "Category", ColLeft, ref yL, ref altL,
                new[] { "", "IT Equipment", "Furniture", "Vehicle", "Accessory", "Other" });

            // Location info (edit mode) or hint note (add mode)
            if (_isEditMode)
            {
                lblLocationValue = AddReadonlyLabelRow(card, "Current Location", ColLeft, ref yL, ref altL);
            }
            else
            {
                AddHintRow(card,
                    "💡 Location is assigned via the Allocation screen after adding equipment.",
                    ColLeft, ref yL, ref altL);
            }

            // ════ RIGHT COLUMN — Additional Details ════
            int yR = SectionGap;
            AddSectionHeader(card, "Additional Details", ColRight, ref yR);
            bool altR = false;

            txtDescription    = AddMultilineRow(card, "Description",      ColRight, ref yR, ref altR);
            txtVendorName     = AddFieldRow(card,    "Vendor Name",        ColRight, ref yR, ref altR);
            cmbConditionGrade = AddComboRow(card,    "Condition Grade",    ColRight, ref yR, ref altR,
                new[] { "", "New", "Good", "Fair", "Poor", "Unserviceable" });

            AddWarrantyRow(card, ColRight, ref yR, ref altR);

            cmbProcurementType = AddProcurementRow(card, ColRight, ref yR, ref altR);

            txtNotes = AddMultilineRow(card, "Notes", ColRight, ref yR, ref altR);

            // ── Vertical divider between columns ─────────────────────────
            int divX = ColRight - 10;
            card.Paint += (_, e) =>
            {
                using var pen = new Pen(SeparatorClr, 1);
                e.Graphics.DrawLine(pen, divX, SectionGap, divX, Math.Max(yL, yR) - 4);
            };

            // ── Size card ─────────────────────────────────────────────────
            int cardH = Math.Max(yL, yR) + SectionGap;
            card.Height = cardH;

            // ── Action buttons ────────────────────────────────────────────
            int btnY = HeaderH + OuterPad + cardH + 14;

            btnSave = new Button
            {
                Text     = "Save",
                Size     = new Size(100, 34),
                Location = new Point(DialogW / 2 - 110, btnY),
                Font     = ThemeHelper.FontBold,
                Cursor   = Cursors.Hand,
            };
            ThemeHelper.StyleButton(btnSave);
            btnSave.Click += BtnSave_Click;
            Controls.Add(btnSave);

            btnCancel = new Button
            {
                Text     = "Cancel",
                Size     = new Size(100, 34),
                Location = new Point(DialogW / 2 + 10, btnY),
                Font     = ThemeHelper.FontDefault,
                Cursor   = Cursors.Hand,
            };
            ThemeHelper.StyleSecondaryButton(btnCancel);
            btnCancel.Click += (s, e) => DialogResult = DialogResult.Cancel;
            Controls.Add(btnCancel);

            ClientSize   = new Size(DialogW, btnY + 34 + OuterPad);
            AcceptButton = btnSave;
            CancelButton = btnCancel;
        }

        // ══════════════════════════════════════════════════════════════════
        //  ROW BUILDERS  (shared helper pattern)
        // ══════════════════════════════════════════════════════════════════

        private void AddSectionHeader(Panel card, string title, int x, ref int y)
        {
            var pnl = new Panel { Location = new Point(x, y), Size = new Size(ColW, 34), BackColor = Color.Transparent };
            var accent = new Panel { Location = new Point(0, 5), Size = new Size(3, 22), BackColor = SectionClr };
            pnl.Controls.Add(accent);
            var lbl = new Label
            {
                Text      = title,
                Location  = new Point(10, 7),
                AutoSize  = true,
                Font      = new Font("Segoe UI", 10f, FontStyle.Bold, GraphicsUnit.Point),
                ForeColor = SectionClr,
            };
            pnl.Controls.Add(lbl);
            pnl.Paint += (_, e) =>
            {
                using var pen = new Pen(Color.FromArgb(180, 215, 180), 1);
                e.Graphics.DrawLine(pen, 0, 33, ColW, 33);
            };
            card.Controls.Add(pnl);
            y += 40;
        }

        /// Plain single-line text field row — returns the TextBox
        private TextBox AddFieldRow(Panel card, string label, int x, ref int y, ref bool isAlt)
        {
            var pnl = MakeRowPanel(card, label, x, RowH, ref y, ref isAlt);

            var txt = new TextBox { Location = new Point(FieldX, 6), Width = InputW, Height = 24 };
            ThemeHelper.StyleTextBox(txt);
            pnl.Controls.Add(txt);
            return txt;
        }

        /// ComboBox row — returns the ComboBox
        private ComboBox AddComboRow(Panel card, string label, int x, ref int y, ref bool isAlt, string[] items)
        {
            var pnl = MakeRowPanel(card, label, x, RowH, ref y, ref isAlt);

            var cmb = new ComboBox
            {
                Location      = new Point(FieldX, 6),
                Width         = InputW,
                DropDownStyle = ComboBoxStyle.DropDownList,
            };
            cmb.Items.AddRange(items);
            ThemeHelper.StyleComboBox(cmb);
            pnl.Controls.Add(cmb);
            return cmb;
        }

        /// DateTimePicker row — returns the DTP
        private DateTimePicker AddDateRow(Panel card, string label, int x, ref int y, ref bool isAlt)
        {
            var pnl = MakeRowPanel(card, label, x, RowH, ref y, ref isAlt);

            var dtp = new DateTimePicker
            {
                Location = new Point(FieldX, 6),
                Width    = InputW,
                Format   = DateTimePickerFormat.Short,
            };
            pnl.Controls.Add(dtp);
            return dtp;
        }

        /// Multiline TextBox row (taller) — returns the TextBox
        private TextBox AddMultilineRow(Panel card, string label, int x, ref int y, ref bool isAlt)
        {
            var pnl = MakeRowPanel(card, label, x, MultiRowH, ref y, ref isAlt);
            // Reposition label to top
            foreach (Control c in pnl.Controls)
                if (c is Label l) { l.Top = 8; l.TextAlign = ContentAlignment.TopLeft; }

            var txt = new TextBox
            {
                Location   = new Point(FieldX, 6),
                Width      = InputW,
                Height     = MultiRowH - 14,
                Multiline  = true,
                ScrollBars = ScrollBars.Vertical,
            };
            ThemeHelper.StyleTextBox(txt);
            pnl.Controls.Add(txt);
            return txt;
        }

        /// Read-only display label row (edit mode location) — returns the Label
        private Label AddReadonlyLabelRow(Panel card, string label, int x, ref int y, ref bool isAlt)
        {
            var pnl = MakeRowPanel(card, label, x, RowH, ref y, ref isAlt);

            var lbl = new Label
            {
                Location  = new Point(FieldX, 0),
                Size      = new Size(InputW, RowH),
                TextAlign = ContentAlignment.MiddleLeft,
                Font      = ThemeHelper.FontDefault,
                ForeColor = ThemeHelper.TextPrimary,
            };
            pnl.Controls.Add(lbl);
            return lbl;
        }

        /// Hint note row (add mode location note)
        private void AddHintRow(Panel card, string hint, int x, ref int y, ref bool isAlt)
        {
            var pnl = MakeRowPanel(card, "", x, 44, ref y, ref isAlt, hideLabel: true);

            var lbl = new Label
            {
                Text      = hint,
                Location  = new Point(8, 6),
                Width     = ColW - 16,
                Height    = 32,
                Font      = ThemeHelper.FontSmall,
                ForeColor = ThemeHelper.TextSecondary,
                BackColor = Color.Transparent,
            };
            pnl.Controls.Add(lbl);
        }

        /// Warranty row: checkbox + DateTimePicker in one panel
        private void AddWarrantyRow(Panel card, int x, ref int y, ref bool isAlt)
        {
            var pnl = MakeRowPanel(card, "", x, RowH, ref y, ref isAlt, hideLabel: true);

            chkWarrantyExpiry = new CheckBox
            {
                Text      = "Warranty Expiry:",
                Location  = new Point(8, 9),
                AutoSize  = true,
                Checked   = false,
                Font      = ThemeHelper.FontBold,
                ForeColor = ThemeHelper.TextSecondary,
                BackColor = Color.Transparent,
            };
            pnl.Controls.Add(chkWarrantyExpiry);

            dtpWarrantyExpiry = new DateTimePicker
            {
                Location = new Point(FieldX, 7),
                Width    = InputW,
                Format   = DateTimePickerFormat.Short,
                Enabled  = false,
            };
            pnl.Controls.Add(dtpWarrantyExpiry);

            chkWarrantyExpiry.CheckedChanged += (_, __) =>
                dtpWarrantyExpiry.Enabled = chkWarrantyExpiry.Checked;
        }

        /// Procurement Type — needs special ComboBox from DB
        private ComboBox AddProcurementRow(Panel card, int x, ref int y, ref bool isAlt)
        {
            var pnl = MakeRowPanel(card, "Procurement Type", x, RowH, ref y, ref isAlt);

            var cmb = new ComboBox
            {
                Location      = new Point(FieldX, 6),
                Width         = InputW,
                DropDownStyle = ComboBoxStyle.DropDownList,
            };
            ThemeHelper.StyleComboBox(cmb);
            pnl.Controls.Add(cmb);
            return cmb;
        }

        /// Core panel factory — creates a styled row with label and bottom separator
        private Panel MakeRowPanel(Panel card, string labelText, int x, int h,
            ref int y, ref bool isAlt, bool hideLabel = false)
        {
            var pnl = new Panel
            {
                Location  = new Point(x, y),
                Size      = new Size(ColW, h),
                BackColor = isAlt ? RowOdd : RowEven,
            };
            pnl.Paint += (_, e) =>
            {
                using var pen = new Pen(SeparatorClr, 1);
                e.Graphics.DrawLine(pen, 10, pnl.Height - 1, pnl.Width - 10, pnl.Height - 1);
            };

            if (!hideLabel && !string.IsNullOrEmpty(labelText))
            {
                var lbl = new Label
                {
                    Text      = labelText + ":",
                    Location  = new Point(8, 0),
                    Size      = new Size(LabelW, h),
                    Font      = ThemeHelper.FontBold,
                    ForeColor = ThemeHelper.TextSecondary,
                    TextAlign = ContentAlignment.MiddleLeft,
                };
                pnl.Controls.Add(lbl);
            }

            card.Controls.Add(pnl);
            y     += h;
            isAlt  = !isAlt;
            return pnl;
        }

        // ══════════════════════════════════════════════════════════════════
        //  DATA  (completely unchanged)
        // ══════════════════════════════════════════════════════════════════
        private void LoadDropdownData()
        {
            try
            {
                using var db = new InventoryDbContext();
                _procurementTypes = db.ProcurementTypes.OrderBy(p => p.TypeName).ToList();
                cmbProcurementType.Items.Add("— None —");
                foreach (var pt in _procurementTypes)
                    cmbProcurementType.Items.Add(pt.TypeName);
                cmbProcurementType.SelectedIndex = 0;
            }
            catch { }
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

            if (_isEditMode && lblLocationValue != null)
            {
                var activeAllocation = _equipment.Allocations
                    .FirstOrDefault(a => a.Status != null &&
                                         a.Status.Equals("approved", StringComparison.OrdinalIgnoreCase));
                if (activeAllocation?.Location != null)
                {
                    lblLocationValue.Text = activeAllocation.Location.LocationName;
                    ThemeHelper.StyleLabel(lblLocationValue, bold: false, color: ThemeHelper.TextPrimary);
                }
                else
                {
                    lblLocationValue.Text = "Not allocated";
                    ThemeHelper.StyleLabel(lblLocationValue, bold: false, color: ThemeHelper.TextSecondary);
                }
            }
        }

        // ══════════════════════════════════════════════════════════════════
        //  SAVE  (completely unchanged)
        // ══════════════════════════════════════════════════════════════════
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

        // ══════════════════════════════════════════════════════════════════
        //  CARD BORDER + SHADOW  (same as EquipmentDetailPopup)
        // ══════════════════════════════════════════════════════════════════
        private void DrawCardBorder(object? sender, PaintEventArgs e)
        {
            if (sender is not Panel pnl) return;
            var g    = e.Graphics;
            var rect = new Rectangle(0, 0, pnl.Width - 1, pnl.Height - 1);

            using var shadowBrush = new SolidBrush(Color.FromArgb(18, 0, 0, 0));
            g.FillRectangle(shadowBrush, new Rectangle(2, 2, pnl.Width - 1, pnl.Height - 1));

            using var bgBrush = new SolidBrush(CardBg);
            g.FillRectangle(bgBrush, rect);

            using var pen = new Pen(CardBorder, 1);
            g.DrawRectangle(pen, rect);
        }
    }
}