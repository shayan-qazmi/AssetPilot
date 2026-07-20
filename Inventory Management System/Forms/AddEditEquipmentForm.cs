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
        private MaintenanceService _maintenanceService;
        private bool _isEditMode;

        // Tracks the status the equipment had when this form opened, so we can
        // detect transitions into/out of "under_repair" via cmbStatus.
        private string? _originalStatus;
        private bool _suppressStatusEvent = false;

        // ── Form controls ───────────────────────────────────────────────
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
        private TextBox       txtProcurementType = null!;
        private Button        btnSave           = null!;
        private Button        btnCancel         = null!;

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
            _equipment          = equipment ?? new Equipment { CurrentStatus = "in_store" };
            _isEditMode         = equipment != null;
            _equipmentService   = new EquipmentService();
            _maintenanceService = new MaintenanceService();

            InitializeUI();
            LoadData();

            // Capture the starting status AFTER LoadData has set cmbStatus,
            // and only in edit mode — Add mode never triggers the maintenance
            // modal logic since there's no "transition" happening yet.
            _originalStatus = _isEditMode ? _equipment.CurrentStatus : null;
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

            txtName         = AddFieldRow(card, "Name *",          ColLeft, ref yL, ref altL);
            txtMake         = AddFieldRow(card, "Make",            ColLeft, ref yL, ref altL);
            txtModel        = AddFieldRow(card, "Model",           ColLeft, ref yL, ref altL);
            txtSerialNumber = AddFieldRow(card, "Serial Number",   ColLeft, ref yL, ref altL);

            cmbStatus = AddComboRow(card, "Current Status", ColLeft, ref yL, ref altL,
                new[] { "In Store", "Allocated", "Decommissioned", "Under Repair" });
            cmbStatus.SelectedIndexChanged += CmbStatus_SelectedIndexChanged;

            if (!_isEditMode)
            {
                cmbStatus.SelectedIndex = cmbStatus.Items.IndexOf("In Store");
                cmbStatus.Enabled = false;
            }

            dtpPurchaseDate = AddDateRow(card, "Purchase Date", ColLeft, ref yL, ref altL);

            cmbCategory = AddComboRow(card, "Category", ColLeft, ref yL, ref altL,
                BuildCategoryItems());

         // Location info (edit mode) or hint note (add mode)
            if (_isEditMode)
            {
                lblLocationValue = AddReadonlyLabelRow(card, "Current Location", ColLeft, ref yL, ref altL);
                AddHintRow(card,
                    "💡 Location can only be changed from the Allocation screen.",
                    ColLeft, ref yL, ref altL);
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

            txtProcurementType = AddFieldRow(card, "Procurement Type", ColRight, ref yR, ref altR);

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
        //  MAINTENANCE STATUS-TRANSITION HANDLING
        // ══════════════════════════════════════════════════════════════════
        private void CmbStatus_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (_suppressStatusEvent) return;
            if (!_isEditMode || _originalStatus == null) return; // Add mode: no-op

            string? selectedItem = cmbStatus.SelectedItem?.ToString();
            if (selectedItem == null) return;
            string newStatus = UnformatStatus(selectedItem);

            bool wasUnderRepair = _originalStatus.Equals("under_repair", StringComparison.OrdinalIgnoreCase);
            bool isNowUnderRepair = newStatus.Equals("under_repair", StringComparison.OrdinalIgnoreCase);

            if (!wasUnderRepair && isNowUnderRepair)
            {
                ShowOpenMaintenanceDialog();
            }
            else if (wasUnderRepair && !isNowUnderRepair)
            {
                ShowResolveMaintenanceDialog();
            }
        }

        private void RevertStatusTo(string status)
        {
            _suppressStatusEvent = true;
            cmbStatus.SelectedItem = FormatStatus(status);
            _suppressStatusEvent = false;
        }

        private void ShowOpenMaintenanceDialog()
        {
            using var dlg = new Form
            {
                Text            = "Log Maintenance Issue",
                Size            = new Size(420, 320),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox     = false,
                MinimizeBox     = false,
                StartPosition   = FormStartPosition.CenterParent,
                BackColor       = CardBg,
            };

            var lblIssue = new Label
            {
                Text = "Issue Description *:", Location = new Point(20, 16), AutoSize = true,
                Font = ThemeHelper.FontBold, ForeColor = ThemeHelper.TextSecondary
            };
            var txtIssue = new TextBox
            {
                Location = new Point(20, 38), Size = new Size(360, 70),
                Multiline = true, ScrollBars = ScrollBars.Vertical
            };
            ThemeHelper.StyleTextBox(txtIssue);

            var lblServicedBy = new Label
            {
                Text = "Serviced By / Vendor:", Location = new Point(20, 118), AutoSize = true,
                Font = ThemeHelper.FontBold, ForeColor = ThemeHelper.TextSecondary
            };
            var txtServicedBy = new TextBox { Location = new Point(20, 140), Size = new Size(360, 24) };
            ThemeHelper.StyleTextBox(txtServicedBy);

            var lblCost = new Label
            {
                Text = "Estimated Cost:", Location = new Point(20, 174), AutoSize = true,
                Font = ThemeHelper.FontBold, ForeColor = ThemeHelper.TextSecondary
            };
            var txtCost = new TextBox { Location = new Point(20, 196), Size = new Size(360, 24) };
            ThemeHelper.StyleTextBox(txtCost);

            var lblError = new Label
            {
                Location = new Point(20, 226), Size = new Size(360, 20),
                ForeColor = ThemeHelper.Danger, Font = ThemeHelper.FontSmall
            };

            var btnOk = new Button { Text = "OK", Location = new Point(200, 252), Size = new Size(85, 32) };
            ThemeHelper.StyleButton(btnOk);
            var btnCancelDlg = new Button { Text = "Cancel", Location = new Point(295, 252), Size = new Size(85, 32) };
            ThemeHelper.StyleSecondaryButton(btnCancelDlg);

            bool confirmed = false;

            btnOk.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtIssue.Text))
                {
                    lblError.Text = "Issue description is required.";
                    return;
                }

                decimal? cost = null;
                if (!string.IsNullOrWhiteSpace(txtCost.Text))
                {
                    if (!decimal.TryParse(txtCost.Text, out var parsedCost))
                    {
                        lblError.Text = "Cost must be a valid number.";
                        return;
                    }
                    cost = parsedCost;
                }

                var result = _maintenanceService.OpenRecord(
                    _equipment.Id, txtIssue.Text.Trim(),
                    string.IsNullOrWhiteSpace(txtServicedBy.Text) ? null : txtServicedBy.Text.Trim(),
                    cost);

                if (!result.Success)
                {
                    lblError.Text = result.ErrorMessage;
                    return;
                }

                confirmed = true;
                dlg.DialogResult = DialogResult.OK;
                dlg.Close();
            };

            btnCancelDlg.Click += (s, e) => { dlg.DialogResult = DialogResult.Cancel; dlg.Close(); };

            dlg.Controls.Add(lblIssue);
            dlg.Controls.Add(txtIssue);
            dlg.Controls.Add(lblServicedBy);
            dlg.Controls.Add(txtServicedBy);
            dlg.Controls.Add(lblCost);
            dlg.Controls.Add(txtCost);
            dlg.Controls.Add(lblError);
            dlg.Controls.Add(btnOk);
            dlg.Controls.Add(btnCancelDlg);
            dlg.AcceptButton = btnOk;
            dlg.CancelButton = btnCancelDlg;

            dlg.ShowDialog(this);

            if (confirmed)
            {
                _originalStatus = "under_repair";
            }
            else
            {
                RevertStatusTo(_originalStatus!);
            }
        }

        private void ShowResolveMaintenanceDialog()
        {
            var openResult = _maintenanceService.GetOpenRecordForEquipment(_equipment.Id);
            var openRecord = openResult.Success ? openResult.Data : null;

            if (openRecord == null)
            {
                // No open record found (e.g. it was already resolved elsewhere) —
                // allow the status change through without prompting.
                _originalStatus = cmbStatus.SelectedItem?.ToString();
                return;
            }

            using var dlg = new Form
            {
                Text            = "Resolve Maintenance Issue",
                Size            = new Size(380, 240),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox     = false,
                MinimizeBox     = false,
                StartPosition   = FormStartPosition.CenterParent,
                BackColor       = CardBg,
            };

            var lblDate = new Label
            {
                Text = "Resolution Date:", Location = new Point(20, 16), AutoSize = true,
                Font = ThemeHelper.FontBold, ForeColor = ThemeHelper.TextSecondary
            };
            var dtpResolved = new DateTimePicker
            {
                Location = new Point(20, 38), Size = new Size(320, 24),
                Format = DateTimePickerFormat.Short, Value = DateTime.Today
            };

            var lblCost = new Label
            {
                Text = "Final Cost:", Location = new Point(20, 74), AutoSize = true,
                Font = ThemeHelper.FontBold, ForeColor = ThemeHelper.TextSecondary
            };
            var txtFinalCost = new TextBox
            {
                Location = new Point(20, 96), Size = new Size(320, 24),
                Text = openRecord.Cost.HasValue ? openRecord.Cost.Value.ToString("0.##") : ""
            };
            ThemeHelper.StyleTextBox(txtFinalCost);

            var lblError = new Label
            {
                Location = new Point(20, 126), Size = new Size(320, 20),
                ForeColor = ThemeHelper.Danger, Font = ThemeHelper.FontSmall
            };

            var btnOk = new Button { Text = "OK", Location = new Point(160, 156), Size = new Size(85, 32) };
            ThemeHelper.StyleButton(btnOk);
            var btnCancelDlg = new Button { Text = "Cancel", Location = new Point(255, 156), Size = new Size(85, 32) };
            ThemeHelper.StyleSecondaryButton(btnCancelDlg);

            bool confirmed = false;

            btnOk.Click += (s, e) =>
            {
                decimal? finalCost = null;
                if (!string.IsNullOrWhiteSpace(txtFinalCost.Text))
                {
                    if (!decimal.TryParse(txtFinalCost.Text, out var parsedCost))
                    {
                        lblError.Text = "Cost must be a valid number.";
                        return;
                    }
                    finalCost = parsedCost;
                }

                var result = _maintenanceService.ResolveRecord(
                    openRecord.Id, DateOnly.FromDateTime(dtpResolved.Value), finalCost);

                if (!result.Success)
                {
                    lblError.Text = result.ErrorMessage;
                    return;
                }

                confirmed = true;
                dlg.DialogResult = DialogResult.OK;
                dlg.Close();
            };

            btnCancelDlg.Click += (s, e) => { dlg.DialogResult = DialogResult.Cancel; dlg.Close(); };

            dlg.Controls.Add(lblDate);
            dlg.Controls.Add(dtpResolved);
            dlg.Controls.Add(lblCost);
            dlg.Controls.Add(txtFinalCost);
            dlg.Controls.Add(lblError);
            dlg.Controls.Add(btnOk);
            dlg.Controls.Add(btnCancelDlg);
            dlg.AcceptButton = btnOk;
            dlg.CancelButton = btnCancelDlg;

            dlg.ShowDialog(this);

            if (confirmed)
            {
                _originalStatus = UnformatStatus(cmbStatus.SelectedItem?.ToString() ?? "");
            }
            else
            {
                RevertStatusTo("under_repair");
            }
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
        private static string FormatStatus(string raw) => raw switch
        {
            "in_store"       => "In Store",
            "allocated"      => "Allocated",
            "decommissioned" => "Decommissioned",
            "under_repair"   => "Under Repair",
            _                => raw
        };

        private static string UnformatStatus(string display) => display switch
        {
            "In Store"       => "in_store",
            "Allocated"      => "allocated",
            "Decommissioned" => "decommissioned",
            "Under Repair"   => "under_repair",
            _                => display
        };

        // ══════════════════════════════════════════════════════════════════
        //  DATA
        // ══════════════════════════════════════════════════════════════════
        private static string[] BuildCategoryItems()
        {
            var items = new List<string> { "" };
            var names = new CategoryService().GetAll().Data?
                .Select(c => c.Name) ?? Enumerable.Empty<string>();
            items.AddRange(names);
            return items.ToArray();
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

            txtName.Text            = _equipment.Name;
            txtMake.Text            = _equipment.Make;
            txtModel.Text           = _equipment.Model;
            txtSerialNumber.Text    = _equipment.SerialNumber;

            // Suppress the transition-detection event while we programmatically
            // set the initial status from loaded data — this is not a user-driven
            // status change and must never trigger the maintenance modal.
            _suppressStatusEvent = true;
            cmbStatus.SelectedItem  = FormatStatus(_equipment.CurrentStatus);
            _suppressStatusEvent = false;

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

            txtProcurementType.Text =
                _equipmentService.GetProcurementTypeName(_equipment.ProcurementTypeId) ?? "";

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
        //  SAVE
        // ══════════════════════════════════════════════════════════════════
        private void BtnSave_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                MessageBox.Show("Name is required.",
                    "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Equipment Code and QR Code are no longer entered manually —
            // EquipmentService auto-generates them on Add when left blank,
            // and they are simply left untouched on Update.
            _equipment.Name           = txtName.Text.Trim();
            _equipment.Make           = txtMake.Text.Trim();
            _equipment.Model          = txtModel.Text.Trim();
            _equipment.SerialNumber   = txtSerialNumber.Text.Trim();
            _equipment.CurrentStatus  = UnformatStatus(cmbStatus.SelectedItem?.ToString() ?? "In Store");
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

            // Resolve-or-create ProcurementType by typed name (preserves the FK)
            _equipment.ProcurementTypeId = _equipmentService.ResolveProcurementTypeId(txtProcurementType.Text);

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