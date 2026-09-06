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
        private readonly AuditService _audit = new();


        // Tracks the status the equipment had when this form opened, so we can
        // detect transitions into/out of "under_repair" via cmbStatus.
        private string? _originalStatus;
        private bool _suppressStatusEvent = false;
        private string? _decommissionReason;


        // ── Picture state ──────────────────────────────────────────────
        // The relative path (e.g. "Images/Equipment/xxxx.png") of a picture
        // the user just picked in this session but hasn't saved yet.
        private string? _stagedPicturePath;
        // The relative path already on the equipment record when the form
        // opened (edit mode only) — used to know what to clean up on disk.
        private string? _originalPicturePathOnLoad;
        private bool _pictureRemoved = false;

        // ── Form controls ───────────────────────────────────────────────
        private TextBox txtName = null!;
        private TextBox txtMake = null!;
        private TextBox txtModel = null!;
        private TextBox txtSerialNumber = null!;
        private ComboBox cmbStatus = null!;
        private CheckBox chkPurchaseDate = null!;
        private DateTimePicker dtpPurchaseDate = null!;
        private ComboBox cmbCategory = null!;
        private TextBox txtDescription = null!;
        private TextBox txtVendorName = null!;
        private ComboBox cmbConditionGrade = null!;
        private CheckBox chkWarrantyExpiry = null!;
        private DateTimePicker dtpWarrantyExpiry = null!;
        private TextBox txtNotes = null!;
        private Label? lblLocationValue = null;
        private TextBox txtProcurementType = null!;
        private Label lblPictureStatus = null!;
        private Button btnPickPicture = null!;
        private Button btnRemovePicture = null!;
        private Button btnSave = null!;
        private Button btnCancel = null!;
        private TextBox txtPrice = null!;

        // ── Layout constants (matching EquipmentDetailPopup) ───────────────
        private const int DialogW = 860;
        private const int OuterPad = 20;
        private const int HeaderH = 52;
        private const int ColLeft = 20;
        private const int ColRight = 440;
        private const int ColW = 390;
        private const int FieldX = 162;   // input start inside a row panel
        private const int LabelW = 154;   // label width to fit "Serial Number * :"
        private const int InputW = 215;   // input control width
        private const int RowH = 38;
        private const int MultiRowH = 88;
        private const int SectionGap = 14;

        // ── Colors — theme-aware (light / dark) ─────────────────────────
        private static Color BgPage     => ThemeHelper.IsDarkMode ? Color.FromArgb(18, 20, 24)   : Color.FromArgb(245, 247, 250);
        private static Color CardBg     => ThemeHelper.IsDarkMode ? Color.FromArgb(36, 39, 46)   : Color.White;
        private static Color CardBorder => ThemeHelper.IsDarkMode ? Color.FromArgb(55, 58, 65)   : Color.FromArgb(224, 224, 224);
        private static Color SectionClr => ThemeHelper.IsDarkMode ? Color.FromArgb(100, 190, 110) : Color.FromArgb(46, 125, 50);
        private static Color RowEven    => ThemeHelper.IsDarkMode ? Color.FromArgb(36, 39, 46)   : Color.White;
        private static Color RowOdd     => ThemeHelper.IsDarkMode ? Color.FromArgb(30, 33, 39)   : Color.FromArgb(248, 250, 248);
        private static Color SeparatorClr => ThemeHelper.IsDarkMode ? Color.FromArgb(55, 58, 65) : Color.FromArgb(234, 237, 241);
        private static Color MutedText  => ThemeHelper.IsDarkMode ? Color.FromArgb(140, 148, 160) : Color.FromArgb(110, 110, 120);
        private static Color InputBorderClr => ThemeHelper.IsDarkMode ? Color.FromArgb(70, 74, 82) : Color.FromArgb(190, 195, 202);

        public AddEditEquipmentForm(Equipment? equipment = null)
        {
            _equipment = equipment ?? new Equipment { CurrentStatus = "in_store" };
            _isEditMode = equipment != null;
            _equipmentService = new EquipmentService();
            _maintenanceService = new MaintenanceService();

            if (_isEditMode)
            {
                try
                {
                    using var db = new InventoryDbContext();
                    var eq = db.Equipment
                        .Include(e => e.Allocations)
                        .ThenInclude(a => a.Location)
                        .FirstOrDefault(e => e.Id == _equipment.Id);
                    if (eq != null) _equipment = eq;
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "AddEditEquipmentForm Constructor (DB reload)");
                }
            }

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
            Text = _isEditMode ? "Edit Equipment" : "Add Equipment";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = BgPage;
            ForeColor = ThemeHelper.TextPrimary;

            const int formWidth = DialogW;
            const int margin = OuterPad + 8;
            int contentWidth = formWidth - margin * 2;

            // ── Header Banner ─────────────────────────────────────────────
            Color modeAccent = _isEditMode ? Color.FromArgb(245, 158, 11) : ThemeHelper.Header;
            var pnlHeader = new Panel { Location = new Point(0, 0), Size = new Size(formWidth, 10), BackColor = modeAccent };
            Controls.Add(pnlHeader);

            var lblTitle = new Label
            {
                Text = _isEditMode ? "Edit Equipment" : "Add Equipment",
                Font = new Font("Segoe UI", 15.5f, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(margin, 18)
            };
            pnlHeader.Controls.Add(lblTitle);

            var subtitleFont = new Font("Segoe UI", 9.5f, FontStyle.Regular);
            string subtitleText = _isEditMode ? "Update equipment details and specifications below" : "Enter equipment details and specifications below";
            var subtitleSize = TextRenderer.MeasureText(
                subtitleText, subtitleFont, new Size(contentWidth, int.MaxValue), TextFormatFlags.WordBreak);
            var lblSubtitle = new Label
            {
                Text = subtitleText,
                Font = subtitleFont,
                ForeColor = Color.FromArgb(235, 245, 233),
                AutoSize = false,
                Size = new Size(contentWidth, subtitleSize.Height + 4),
                Location = new Point(margin, lblTitle.Bottom + 6)
            };
            pnlHeader.Controls.Add(lblSubtitle);
            pnlHeader.Height = lblSubtitle.Bottom + 18;

            // ── Card container ────────────────────────────────────────────
            var card = new Panel
            {
                BackColor = CardBg,
                Location = new Point(OuterPad, pnlHeader.Height + OuterPad),
                Width = DialogW - OuterPad * 2,
            };
            card.Paint += DrawCardBorder;
            Controls.Add(card);

            // ════ LEFT COLUMN — Equipment Identification ════
            int yL = SectionGap;
            AddSectionHeader(card, "Equipment Identification", ColLeft, ref yL);
            bool altL = false;

            txtName = AddFieldRow(card, "Name * ", ColLeft, ref yL, ref altL);
            txtMake = AddFieldRow(card, "Make", ColLeft, ref yL, ref altL);
            txtModel = AddFieldRow(card, "Model", ColLeft, ref yL, ref altL);
            txtSerialNumber = AddFieldRow(card, "Serial Number * ", ColLeft, ref yL, ref altL);

            bool isAllocated = _equipment.Allocations != null && _equipment.Allocations.Any(a => string.Equals(a.Status, "approved", StringComparison.OrdinalIgnoreCase));
            if (isAllocated)
            {
                _equipment.CurrentStatus = "allocated";
            }

            if (_isEditMode && string.Equals(_equipment.CurrentStatus, "allocated", StringComparison.OrdinalIgnoreCase))
            {
                cmbStatus = AddComboRow(card, "Current Status", ColLeft, ref yL, ref altL,
                     new[] { "Allocated" });
                cmbStatus.Enabled = false;
                
                AddHintRow(card,
                    "Status is locked for allocated equipment. Return to store to edit.",
                    ColLeft, ref yL, ref altL, rowHeight: 64);
            }
            else
            {
                cmbStatus = AddComboRow(card, "Current Status", ColLeft, ref yL, ref altL,
                     new[] { "In Store", "Decommissioned", "Under Repair" });
                cmbStatus.SelectedIndexChanged += CmbStatus_SelectedIndexChanged;

                AddHintRow(card,
                    "Allocation is handled from the Allocation screen",
                    ColLeft, ref yL, ref altL);
            }

            if (!_isEditMode)
            {
                cmbStatus.SelectedIndex = cmbStatus.Items.IndexOf("In Store");
                cmbStatus.Enabled = false;
            }

            AddOptionalPurchaseDateRow(card, ColLeft, ref yL, ref altL);
            txtPrice = AddFieldRow(card, "Purchase Price", ColLeft, ref yL, ref altL);

            cmbCategory = AddComboRow(card, "Category", ColLeft, ref yL, ref altL,
                BuildCategoryItems());

            // Location info (edit mode) or hint note (add mode)
            if (_isEditMode)
            {
                lblLocationValue = AddReadonlyLabelRow(card, "Current Location", ColLeft, ref yL, ref altL);
                AddHintRow(card,
                    "Location can be updated via the Allocation or Transfer screens.",
                    ColLeft, ref yL, ref altL);
            }
            else
            {
                AddHintRow(card,
                    "Location is assigned via the Allocation screen after adding equipment.",
                    ColLeft, ref yL, ref altL);
            }

            // Optional equipment picture (jpg/png/pdf)
            AddPictureRow(card, ColLeft, ref yL, ref altL);

            // ════ RIGHT COLUMN — Additional Details ════
            int yR = SectionGap;
            AddSectionHeader(card, "Additional Details", ColRight, ref yR);
            bool altR = false;

            txtDescription = AddMultilineRow(card, "Description", ColRight, ref yR, ref altR);
            txtVendorName = AddFieldRow(card, "Vendor Name", ColRight, ref yR, ref altR);
            cmbConditionGrade = AddComboRow(card, "Condition Grade", ColRight, ref yR, ref altR,
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

            // ── Footer Panel & Action buttons ─────────────────────────────
            const int footerHeight = 68;
            var pnlFooter = new Panel { Size = new Size(formWidth, footerHeight), BackColor = ThemeHelper.IsDarkMode ? Color.FromArgb(22, 24, 29) : Color.FromArgb(249, 250, 251) };
            pnlFooter.Controls.Add(new Panel { Location = new Point(0, 0), Size = new Size(formWidth, 1), BackColor = CardBorder });

            btnCancel = new Button { Text = "Cancel", Size = new Size(130, 40) };
            ThemeHelper.StyleSecondaryButton(btnCancel);
            btnCancel.Click += (s, e) => DialogResult = DialogResult.Cancel;

            btnSave = new Button { Text = "Save", Size = new Size(140, 40) };
            if (_isEditMode)
                ThemeHelper.StyleButton(btnSave, modeAccent);
            else
                ThemeHelper.StyleButton(btnSave);
            btnSave.Click += BtnSave_Click;

            int totalButtonsWidth = btnCancel.Width + 12 + btnSave.Width;
            int buttonsStartX = (formWidth - totalButtonsWidth) / 2;
            btnSave.Location = new Point(buttonsStartX, 14);
            btnCancel.Location = new Point(buttonsStartX + btnSave.Width + 12, 14);

            pnlFooter.Controls.Add(btnCancel);
            pnlFooter.Controls.Add(btnSave);
            Controls.Add(pnlFooter);

            var lblRequiredNote = new Label
            {
                Text = "* Required field",
                Font = new Font("Segoe UI", 7.5f, FontStyle.Italic),
                ForeColor = MutedText,
                AutoSize = true
            };
            lblRequiredNote.Location = new Point(margin, (footerHeight - lblRequiredNote.PreferredHeight) / 2);
            pnlFooter.Controls.Add(lblRequiredNote);

            int totalHeight = pnlHeader.Height + OuterPad + cardH + OuterPad + footerHeight;
            ClientSize = new Size(formWidth, totalHeight);
            pnlFooter.Location = new Point(0, ClientSize.Height - footerHeight);

            AcceptButton = btnSave;
            CancelButton = btnCancel;

            // Clean up any newly-staged (not-yet-saved) picture file if the
            // dialog is closed without saving — via Cancel button or the
            // window's own close (X) button.
            this.FormClosing += (s, e) =>
            {
                if (DialogResult != DialogResult.OK)
                    CleanupStagedFileIfAny();
            };
        }

        // ══════════════════════════════════════════════════════════════════
        //  PICTURE ROW (optional — jpg/png/pdf)
        // ══════════════════════════════════════════════════════════════════
        private void AddPictureRow(Panel card, int x, ref int y, ref bool isAlt)
        {
            var pnl = MakeRowPanel(card, "Picture", x, RowH, ref y, ref isAlt);

            lblPictureStatus = new Label
            {
                Location = new Point(FieldX, 0),
                Size = new Size(100, RowH),
                TextAlign = ContentAlignment.MiddleLeft,
                Font = ThemeHelper.FontSmall,
                ForeColor = ThemeHelper.TextSecondary,
                Text = "No picture",
                AutoEllipsis = true,
            };
            pnl.Controls.Add(lblPictureStatus);

            btnPickPicture = new Button
            {
                Text = "📎 Choose",
                Location = new Point(FieldX + 104, 5),
                Size = new Size(75, 28),
                Font = ThemeHelper.FontSmall,
                Cursor = Cursors.Hand,
            };
            ThemeHelper.StyleSecondaryButton(btnPickPicture);
            btnPickPicture.Click += BtnPickPicture_Click;
            pnl.Controls.Add(btnPickPicture);

            btnRemovePicture = new Button
            {
                Text = "✕",
                Location = new Point(FieldX + 183, 5),
                Size = new Size(28, 28),
                Font = ThemeHelper.FontSmall,
                Cursor = Cursors.Hand,
                Visible = false,
            };
            ThemeHelper.StyleDangerButton(btnRemovePicture);
            btnRemovePicture.Click += BtnRemovePicture_Click;
            pnl.Controls.Add(btnRemovePicture);
        }

        private void BtnPickPicture_Click(object? sender, EventArgs e)
        {
            using var ofd = new OpenFileDialog
            {
                Filter = "Images and PDF|*.jpg;*.jpeg;*.png;*.pdf|JPEG Image|*.jpg;*.jpeg|PNG Image|*.png|PDF Document|*.pdf",
                Title = "Select Equipment Picture",
            };
            if (ofd.ShowDialog() != DialogResult.OK) return;

            try
            {
                NetworkShareHelper.EnsureConnected();
                System.IO.Directory.CreateDirectory(NetworkPaths.EquipmentPicturesDir);

                string ext = System.IO.Path.GetExtension(ofd.FileName);
                string fileName = $"{Guid.NewGuid():N}{ext}";
                string destPath = System.IO.Path.Combine(NetworkPaths.EquipmentPicturesDir, fileName);
                System.IO.File.Copy(ofd.FileName, destPath, overwrite: true);

                CleanupStagedFileIfAny();

                _stagedPicturePath = fileName; // store bare filename now, not a local relative path
                _pictureRemoved = false;
                UpdatePictureStatusLabel();
            }
            catch (Exception ex)
            {
                ToastHelper.Show(this, "Could not attach picture: " + ex.Message, ToastType.Error);
            }
        }

        private void BtnRemovePicture_Click(object? sender, EventArgs e)
        {
            CleanupStagedFileIfAny();
            _stagedPicturePath = null;
            _pictureRemoved = true;
            UpdatePictureStatusLabel();
        }

        private void CleanupStagedFileIfAny()
        {
            DeleteFileIfExists(_stagedPicturePath);
            _stagedPicturePath = null;
        }

        private static void DeleteFileIfExists(string? storedValue)
        {
            if (string.IsNullOrEmpty(storedValue)) return;
            try
            {
                NetworkShareHelper.EnsureConnected();
                string full = NetworkPaths.GetEquipmentPictureFullPath(storedValue);
                if (System.IO.File.Exists(full)) System.IO.File.Delete(full);
            }
            catch { /* best-effort cleanup — never block the user for this */ }
        }

        private void UpdatePictureStatusLabel()
        {
            if (_pictureRemoved)
            {
                lblPictureStatus.Text = "No picture";
                btnRemovePicture.Visible = false;
            }
            else if (!string.IsNullOrEmpty(_stagedPicturePath))
            {
                lblPictureStatus.Text = System.IO.Path.GetFileName(_stagedPicturePath);
                btnRemovePicture.Visible = true;
            }
            else if (!string.IsNullOrEmpty(_originalPicturePathOnLoad))
            {
                lblPictureStatus.Text = System.IO.Path.GetFileName(_originalPicturePathOnLoad);
                btnRemovePicture.Visible = true;
            }
            else
            {
                lblPictureStatus.Text = "No picture";
                btnRemovePicture.Visible = false;
            }
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
            bool wasDecommissioned = _originalStatus.Equals("decommissioned", StringComparison.OrdinalIgnoreCase);
            bool isNowDecommissioned = newStatus.Equals("decommissioned", StringComparison.OrdinalIgnoreCase);

            if (!wasUnderRepair && isNowUnderRepair)
            {
                ShowOpenMaintenanceDialog();
            }
            else if (wasUnderRepair && !isNowUnderRepair)
            {
                ShowResolveMaintenanceDialog();
            }
            else if (!wasDecommissioned && isNowDecommissioned)
            {
                ShowDecommissionReasonDialog();
            }
            else if (wasDecommissioned && !isNowDecommissioned)
            {
                LogDecommissionUndone(newStatus);
            }
        }

        private void RevertStatusTo(string status)
        {
            _suppressStatusEvent = true;
            string formattedStatus = FormatStatus(_equipment.CurrentStatus);
            if (!cmbStatus.Items.Contains(formattedStatus))
            {
                // Equipment is currently Allocated or Decommissioned — those
                // states are no longer chosen from this form, but we still
                // need to display the real status. Add it as a locked,
                // read-only entry rather than silently dropping it.
                cmbStatus.Items.Add(formattedStatus);
                cmbStatus.Enabled = false;
            }
            cmbStatus.SelectedItem = formattedStatus;
            _suppressStatusEvent = false;
        }

        private void ShowOpenMaintenanceDialog()
        {
            using var dlg = new Form
            {
                Text = "Log Maintenance Issue",
                Size = new Size(440, 390),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                StartPosition = FormStartPosition.CenterParent,
                BackColor = CardBg,
            };

            var lblIssue = new Label
            {
                Text = "Issue Description *:",
                Location = new Point(20, 16),
                AutoSize = true,
                Font = ThemeHelper.FontBold,
                ForeColor = ThemeHelper.TextSecondary
            };
            var txtIssue = new TextBox
            {
                Location = new Point(20, 38),
                Size = new Size(360, 70),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical
            };
            ThemeHelper.StyleTextBox(txtIssue);

            var lblServicedBy = new Label
            {
                Text = "Serviced By / Vendor:",
                Location = new Point(20, 118),
                AutoSize = true,
                Font = ThemeHelper.FontBold,
                ForeColor = ThemeHelper.TextSecondary
            };
            var txtServicedBy = new TextBox { Location = new Point(20, 140), Size = new Size(360, 24) };
            ThemeHelper.StyleTextBox(txtServicedBy);

            var lblCost = new Label
            {
                Text = "Estimated Cost:",
                Location = new Point(20, 174),
                AutoSize = true,
                Font = ThemeHelper.FontBold,
                ForeColor = ThemeHelper.TextSecondary
            };
            var txtCost = new TextBox { Location = new Point(20, 196), Size = new Size(360, 24) };
            ThemeHelper.StyleTextBox(txtCost);

            var lblError = new Label
            {
                Location = new Point(20, 226),
                Size = new Size(380, 40),
                ForeColor = ThemeHelper.Danger,
                Font = ThemeHelper.FontSmall
            };

            var btnOk = new Button { Text = "OK", Location = new Point(220, 310), Size = new Size(85, 32) };
            ThemeHelper.StyleButton(btnOk, Color.FromArgb(33, 150, 243)); // blue — matches Under Repair status badge
            var btnCancelDlg = new Button { Text = "Cancel", Location = new Point(315, 310), Size = new Size(85, 32) };
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
                ToastHelper.Show(this, "Equipment marked as Under Repair.", ToastType.Info, 3000, Color.FromArgb(33, 150, 243));
                NotificationHelper.RefreshNow();
            }
            else
            {
                RevertStatusTo(_originalStatus!);
            }
        }

        private void ShowDecommissionReasonDialog()
        {
            using var dlg = new Form
            {
                Text = "Decommission Reason",
                Size = new Size(420, 220),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                StartPosition = FormStartPosition.CenterParent,
                BackColor = CardBg,
            };

            var lblReason = new Label
            {
                Text = "Reason *:",
                Location = new Point(20, 16),
                AutoSize = true,
                Font = ThemeHelper.FontBold,
                ForeColor = ThemeHelper.TextSecondary
            };
            var cmbReason = new ComboBox { Location = new Point(20, 38), Size = new Size(360, 24), DropDownStyle = ComboBoxStyle.DropDownList };
            cmbReason.Items.AddRange(new[] { "Damaged", "Obsolete", "Beyond Repair", "End of Life", "Lost", "Other" });
            cmbReason.SelectedIndex = 0;
            ThemeHelper.StyleComboBox(cmbReason);

            var lblNotes = new Label
            {
                Text = "Notes:",
                Location = new Point(20, 72),
                AutoSize = true,
                Font = ThemeHelper.FontBold,
                ForeColor = ThemeHelper.TextSecondary
            };
            var txtNotesField = new TextBox { Location = new Point(20, 94), Size = new Size(360, 24) };
            ThemeHelper.StyleTextBox(txtNotesField);

            var btnOk = new Button { Text = "OK", Location = new Point(200, 140), Size = new Size(85, 32) };
            ThemeHelper.StyleDangerButton(btnOk);
            var btnCancelDlg = new Button { Text = "Cancel", Location = new Point(295, 140), Size = new Size(85, 32) };
            ThemeHelper.StyleSecondaryButton(btnCancelDlg);

            bool confirmed = false;
            btnOk.Click += (s, e) =>
            {
                _decommissionReason = $"{cmbReason.SelectedItem}" + (string.IsNullOrWhiteSpace(txtNotesField.Text) ? "" : $" — {txtNotesField.Text.Trim()}");
                confirmed = true;
                dlg.DialogResult = DialogResult.OK;
                dlg.Close();
            };
            btnCancelDlg.Click += (s, e) => { dlg.DialogResult = DialogResult.Cancel; dlg.Close(); };

            dlg.Controls.Add(lblReason);
            dlg.Controls.Add(cmbReason);
            dlg.Controls.Add(lblNotes);
            dlg.Controls.Add(txtNotesField);
            dlg.Controls.Add(btnOk);
            dlg.Controls.Add(btnCancelDlg);
            dlg.AcceptButton = btnOk;
            dlg.CancelButton = btnCancelDlg;

            dlg.ShowDialog(this);

            if (confirmed)
            {
                _audit.Log(_equipment.Id, AuditService.ActionDecommissionApproved,
                    $"Status: {FormatStatus(_originalStatus!)}", $"Status: Decommissioned | Reason: {_decommissionReason} | Source Screen: Edit Equipment");
                _originalStatus = "decommissioned";
                ToastHelper.Show(this, "Equipment marked as Decommissioned.", ToastType.Warning, 3000, ThemeHelper.Danger);
                NotificationHelper.RefreshNow();
            }
            else
            {
                RevertStatusTo(_originalStatus!);
            }
        }

        private void LogDecommissionUndone(string newStatus)
        {
            _audit.Log(_equipment.Id, AuditService.ActionDecommissionUndone,
                "Status: Decommissioned", $"Status: {FormatStatus(newStatus)} | Source Screen: Edit Equipment");
            _originalStatus = newStatus;

            if (string.Equals(newStatus, "in_store", StringComparison.OrdinalIgnoreCase))
                ToastHelper.Show(this, "Equipment returned to In Store.", ToastType.Success, 3000, ThemeHelper.Success);
            else if (string.Equals(newStatus, "under_repair", StringComparison.OrdinalIgnoreCase))
                ToastHelper.Show(this, "Equipment marked as Under Repair.", ToastType.Info, 3000, Color.FromArgb(33, 150, 243));
            NotificationHelper.RefreshNow();
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
                Text = "Resolve Maintenance Issue",
                Size = new Size(380, 240),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                StartPosition = FormStartPosition.CenterParent,
                BackColor = CardBg,
            };

            var lblDate = new Label
            {
                Text = "Resolution Date:",
                Location = new Point(20, 16),
                AutoSize = true,
                Font = ThemeHelper.FontBold,
                ForeColor = ThemeHelper.TextSecondary
            };
            var dtpResolved = new DateTimePicker
            {
                Location = new Point(20, 38),
                Size = new Size(320, 24),
                Format = DateTimePickerFormat.Short,
                Value = DateTime.Today
            };

            var lblCost = new Label
            {
                Text = "Final Cost:",
                Location = new Point(20, 74),
                AutoSize = true,
                Font = ThemeHelper.FontBold,
                ForeColor = ThemeHelper.TextSecondary
            };
            var txtFinalCost = new TextBox
            {
                Location = new Point(20, 96),
                Size = new Size(320, 24),
                Text = openRecord.Cost.HasValue ? openRecord.Cost.Value.ToString("0.##") : ""
            };
            ThemeHelper.StyleTextBox(txtFinalCost);

            var lblError = new Label
            {
                Location = new Point(20, 126),
                Size = new Size(320, 20),
                ForeColor = ThemeHelper.Danger,
                Font = ThemeHelper.FontSmall
            };
            var btnOk = new Button { Text = "OK", Location = new Point(160, 156), Size = new Size(85, 32) };
            ThemeHelper.StyleButton(btnOk, Color.FromArgb(0, 131, 143)); // cyan — matches Maintenance Resolved log color
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
                if (string.Equals(_originalStatus, "in_store", StringComparison.OrdinalIgnoreCase))
                    ToastHelper.Show(this, "Equipment returned to In Store.", ToastType.Success, 3000, ThemeHelper.Success);
                else if (string.Equals(_originalStatus, "decommissioned", StringComparison.OrdinalIgnoreCase))
                    ToastHelper.Show(this, "Equipment marked as Decommissioned.", ToastType.Warning, 3000, ThemeHelper.Danger);
                NotificationHelper.RefreshNow();
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
                Text = title,
                Location = new Point(10, 7),
                AutoSize = true,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold, GraphicsUnit.Point),
                ForeColor = SectionClr,
            };
            pnl.Controls.Add(lbl);
            pnl.Paint += (_, e) =>
            {
                // Keep accent bar and label in sync with the current theme
                accent.BackColor = SectionClr;
                lbl.ForeColor    = SectionClr;
                Color divColor = ThemeHelper.IsDarkMode
                    ? Color.FromArgb(55, 58, 65)
                    : Color.FromArgb(180, 215, 180);
                using var pen = new Pen(divColor, 1);
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
                Location = new Point(FieldX, 6),
                Width = InputW,
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
                Width = InputW,
                Format = DateTimePickerFormat.Short,
            };
            StyleDatePicker(dtp);
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
                Location = new Point(FieldX, 6),
                Width = InputW,
                Height = MultiRowH - 14,
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
            };
            ThemeHelper.StyleTextBox(txt);
            pnl.Controls.Add(txt);
            return txt;
        }

        /// Read-only display label row (edit mode location) — returns the Label
        private Label AddReadonlyLabelRow(Panel card, string label, int x, ref int y, ref bool isAlt, int rowHeight = 80)
        {
            var pnl = MakeRowPanel(card, label, x, rowHeight, ref y, ref isAlt);

            var lbl = new Label
            {
                Location = new Point(FieldX, 8),
                Size = new Size(InputW, rowHeight - 16),
                TextAlign = ContentAlignment.TopLeft,
                AutoSize = false,
                Font = ThemeHelper.FontDefault,
                ForeColor = ThemeHelper.TextPrimary,
            };
            pnl.Controls.Add(lbl);
            return lbl;
        }

        /// Hint note row (add mode location note)
        private void AddHintRow(Panel card, string hint, int x, ref int y, ref bool isAlt, int rowHeight = 64)
        {
            var pnl = MakeRowPanel(card, "", x, rowHeight, ref y, ref isAlt, hideLabel: true);

            var lbl = new Label
            {
                Text = hint,
                Location = new Point(8, 6),
                Width = ColW - 16,
                Height = rowHeight - 12,
                Font = ThemeHelper.FontSmall,
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
                Text = "Warranty Expiry:",
                Location = new Point(8, 9),
                AutoSize = true,
                Checked = false,
                Font = ThemeHelper.FontBold,
                ForeColor = ThemeHelper.TextSecondary,
                BackColor = Color.Transparent,
            };
            pnl.Controls.Add(chkWarrantyExpiry);

            dtpWarrantyExpiry = new DateTimePicker
            {
                Location = new Point(FieldX, 7),
                Width = InputW,
                Format = DateTimePickerFormat.Short,
                Enabled = false,
            };
            StyleDatePicker(dtpWarrantyExpiry);
            pnl.Controls.Add(dtpWarrantyExpiry);

            chkWarrantyExpiry.CheckedChanged += (_, __) =>
                dtpWarrantyExpiry.Enabled = chkWarrantyExpiry.Checked;
        }

        /// Purchase Date row: checkbox + DateTimePicker, mirroring the Warranty
        /// Expiry row — Purchase Date must be optional since equipment can
        /// legitimately have none, and a plain DateTimePicker always carries
        /// SOME value (defaulting to today), which was silently getting saved
        /// even when the user never touched the field.
        private void AddOptionalPurchaseDateRow(Panel card, int x, ref int y, ref bool isAlt)
        {
            var pnl = MakeRowPanel(card, "", x, RowH, ref y, ref isAlt, hideLabel: true);

            chkPurchaseDate = new CheckBox
            {
                Text = "Purchase Date:",
                Location = new Point(8, 9),
                AutoSize = true,
                Checked = false,
                Font = ThemeHelper.FontBold,
                ForeColor = ThemeHelper.TextSecondary,
                BackColor = Color.Transparent,
            };
            pnl.Controls.Add(chkPurchaseDate);

            dtpPurchaseDate = new DateTimePicker
            {
                Location = new Point(FieldX, 7),
                Width = InputW,
                Format = DateTimePickerFormat.Short,
                Enabled = false,
            };
            StyleDatePicker(dtpPurchaseDate);
            pnl.Controls.Add(dtpPurchaseDate);

            chkPurchaseDate.CheckedChanged += (_, __) =>
                dtpPurchaseDate.Enabled = chkPurchaseDate.Checked;
        }

        /// Core panel factory — creates a styled row with label and bottom separator
        private Panel MakeRowPanel(Panel card, string labelText, int x, int h,
            ref int y, ref bool isAlt, bool hideLabel = false)
        {
            var pnl = new Panel
            {
                Location = new Point(x, y),
                Size = new Size(ColW, h),
                BackColor = isAlt ? RowOdd : RowEven,
            };
            // Snapshot the alternating flag before the lambda so we can safely
            // capture it (ref parameters cannot be captured in lambdas/closures).
            bool capturedAlt = isAlt;
            pnl.Paint += (_, e) =>
            {
                // Re-sync background colour whenever the theme changes
                pnl.BackColor = capturedAlt ? RowOdd : RowEven;

                using var pen = new Pen(SeparatorClr, 1);
                e.Graphics.DrawLine(pen, 10, pnl.Height - 1, pnl.Width - 10, pnl.Height - 1);

                // Draw crisp, visible box outline around input controls
                using var borderPen = new Pen(InputBorderClr, 1);
                foreach (Control c in pnl.Controls)
                {
                    if (c is TextBox || c is ComboBox || c is DateTimePicker)
                    {
                        var rect = new Rectangle(c.Left - 1, c.Top - 1, c.Width + 1, c.Height + 1);
                        e.Graphics.DrawRectangle(borderPen, rect);
                    }
                }
            };

            if (!hideLabel && !string.IsNullOrEmpty(labelText))
            {
                string cleanLabel = labelText.Trim();
                string displayLabel = cleanLabel.EndsWith("*")
                    ? cleanLabel.Substring(0, cleanLabel.Length - 1).Trim() + " * :"
                    : cleanLabel + ":";

                var lbl = new Label
                {
                    Text = displayLabel,
                    Location = new Point(6, 0),
                    Size = new Size(LabelW, h),
                    Font = ThemeHelper.FontBold,
                    ForeColor = ThemeHelper.TextSecondary,
                    TextAlign = ContentAlignment.MiddleLeft,
                    AutoEllipsis = false,
                    UseMnemonic = false
                };
                pnl.Controls.Add(lbl);
            }

            card.Controls.Add(pnl);
            y += h;

            isAlt = !isAlt;
            return pnl;
        }

        /// Applies dark-mode-aware colours to a DateTimePicker.
        private static void StyleDatePicker(DateTimePicker dtp)
        {
            // DateTimePicker does not respect BackColor in dark mode by default;
            // switching CalendarForeColor/CalendarMonthBackground and ForeColor
            // is the closest WinForms allows without full owner-draw.
            if (ThemeHelper.IsDarkMode)
            {
                dtp.CalendarForeColor        = ThemeHelper.TextPrimary;
                dtp.CalendarTitleBackColor   = Color.FromArgb(45, 48, 55);
                dtp.CalendarTitleForeColor   = ThemeHelper.TextPrimary;
                dtp.CalendarMonthBackground  = Color.FromArgb(36, 39, 46);
                dtp.CalendarTrailingForeColor = ThemeHelper.TextSecondary;
                dtp.ForeColor = ThemeHelper.TextPrimary;
            }
            else
            {
                dtp.ForeColor = ThemeHelper.TextPrimary;
            }
        }

        // ══════════════════════════════════════════════════════════════════
        private static string FormatStatus(string raw) => raw switch
        {
            "in_store" => "In Store",
            "allocated" => "Allocated",
            "decommissioned" => "Decommissioned",
            "under_repair" => "Under Repair",
            _ => raw
        };

        private static string UnformatStatus(string display) => display switch
        {
            "In Store" => "in_store",
            "Allocated" => "allocated",
            "Decommissioned" => "decommissioned",
            "Under Repair" => "under_repair",
            _ => display
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
                cmbStatus.SelectedIndex = 0;
                cmbCategory.SelectedIndex = 0;
                cmbConditionGrade.SelectedIndex = 0;
                UpdatePictureStatusLabel();
                return;
            }

            txtName.Text = _equipment.Name;
            txtMake.Text = _equipment.Make;
            txtModel.Text = _equipment.Model;
            txtSerialNumber.Text = _equipment.SerialNumber;

            // Suppress the transition-detection event while we programmatically
            // set the initial status from loaded data — this is not a user-driven
            // status change and must never trigger the maintenance modal.
            _suppressStatusEvent = true;
            string formattedInitialStatus = FormatStatus(_equipment.CurrentStatus);
            if (!cmbStatus.Items.Contains(formattedInitialStatus))
            {
                // Equipment is currently Allocated (or any other status not offered
                // in this dropdown) — those states are only changed from the
                // Allocation screen. Add it as a locked, read-only entry so it
                // displays correctly AND so SelectedItem is never left null, which
                // previously caused Save to silently fall back to "In Store" and
                // wipe out the real status.
                cmbStatus.Items.Add(formattedInitialStatus);
                cmbStatus.Enabled = false;
            }
            cmbStatus.SelectedItem = formattedInitialStatus;
            _suppressStatusEvent = false;

                     if (_equipment.PurchaseDate.HasValue)
            {
                chkPurchaseDate.Checked = true;
                dtpPurchaseDate.Enabled = true;
                dtpPurchaseDate.Value =
                    _equipment.PurchaseDate.Value.ToDateTime(TimeOnly.MinValue);
            }
            else
            {
                chkPurchaseDate.Checked = false;
                dtpPurchaseDate.Enabled = false;
            }
           txtPrice.Text = _equipment.Price.HasValue ? _equipment.Price.Value.ToString("0.##") : ""; 

            cmbCategory.SelectedItem = _equipment.Category ?? "";
            txtDescription.Text = _equipment.Description ?? "";
            txtVendorName.Text = _equipment.VendorName ?? "";
            cmbConditionGrade.SelectedItem = _equipment.ConditionGrade ?? "";
            txtNotes.Text = _equipment.Notes ?? "";

            if (_equipment.WarrantyExpiry.HasValue)
            {
                chkWarrantyExpiry.Checked = true;
                dtpWarrantyExpiry.Enabled = true;
                dtpWarrantyExpiry.Value =
                    _equipment.WarrantyExpiry.Value.ToDateTime(TimeOnly.MinValue);
            }

            txtProcurementType.Text =
                _equipmentService.GetProcurementTypeName(_equipment.ProcurementTypeId) ?? "";

            _originalPicturePathOnLoad = _equipment.PicturePath;
            UpdatePictureStatusLabel();

            if (_isEditMode && lblLocationValue != null)
            {
                var activeAllocation = _equipment.Allocations
                    .FirstOrDefault(a => a.Status != null &&
                                         a.Status.Equals("approved", StringComparison.OrdinalIgnoreCase));
                if (activeAllocation?.Location != null)
                {
                    bool hasNum = !string.IsNullOrWhiteSpace(activeAllocation.Location.RoomNumber);
                    bool hasName = !string.IsNullOrWhiteSpace(activeAllocation.Location.RoomName);
                    string roomStr = hasNum ? activeAllocation.Location.RoomNumber : (hasName ? activeAllocation.Location.RoomName : "");
                    string finalLoc = (!hasNum && !hasName) 
                        ? (activeAllocation.Location.LocationName ?? "") 
                        : $"{activeAllocation.Location.LocationName} - {roomStr}";

                    lblLocationValue.Text = finalLoc;
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
                ToastHelper.Show(this, "Name is required.", ToastType.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(txtSerialNumber.Text))
            {
                ToastHelper.Show(this, "Serial Number is required.", ToastType.Warning);
                return;
            }


            // Equipment Code and QR Code are no longer entered manually —
            // EquipmentService auto-generates them on Add when left blank,
            // and they are simply left untouched on Update.
            _equipment.Name = txtName.Text.Trim();
            _equipment.Make = txtMake.Text.Trim();
            _equipment.Model = txtModel.Text.Trim();
            _equipment.SerialNumber = txtSerialNumber.Text.Trim();
            _equipment.CurrentStatus = UnformatStatus(cmbStatus.SelectedItem?.ToString() ?? "In Store");
            _equipment.PurchaseDate = chkPurchaseDate.Checked
                ? DateOnly.FromDateTime(dtpPurchaseDate.Value)
                : null;
            if (!string.IsNullOrWhiteSpace(txtPrice.Text))
            {
                if (!decimal.TryParse(txtPrice.Text, out var price))
                {
                    ToastHelper.Show(this, "Purchase Price must be a valid number.", ToastType.Warning);
                    return;
                }
                _equipment.Price = price;
            }
            else
            {
                _equipment.Price = null;
            }
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

            // ── Picture: commit whatever the user staged/removed this session ──
            if (_pictureRemoved)
            {
                DeleteFileIfExists(_originalPicturePathOnLoad);
                _equipment.PicturePath = null;
            }
            else if (!string.IsNullOrEmpty(_stagedPicturePath))
            {
                if (!string.IsNullOrEmpty(_originalPicturePathOnLoad) &&
                    _originalPicturePathOnLoad != _stagedPicturePath)
                {
                    DeleteFileIfExists(_originalPicturePathOnLoad);
                }
                _equipment.PicturePath = _stagedPicturePath;
            }
            // else: no change made this session — leave _equipment.PicturePath as-is.

            // Resolve-or-create ProcurementType by typed name (preserves the FK)
            _equipment.ProcurementTypeId = _equipmentService.ResolveProcurementTypeId(txtProcurementType.Text);

            if (!_isEditMode)
                _equipment.CreatedByUserId = 1;

            var result = _isEditMode
                ? _equipmentService.Update(_equipment)
                : _equipmentService.Add(_equipment);

            if (result.Success)
            {
        

                // Prevent FormClosing's cleanup handler from deleting the file
                // we just committed to the database.
                _stagedPicturePath = null;
                DialogResult = DialogResult.OK;
            }
            else
                ToastHelper.Show(this, result.ErrorMessage, ToastType.Error);
        }

        // ══════════════════════════════════════════════════════════════════
        //  CARD BORDER + SHADOW  (same as EquipmentDetailPopup)
        // ══════════════════════════════════════════════════════════════════
        private void DrawCardBorder(object? sender, PaintEventArgs e)
        {
            if (sender is not Panel pnl) return;
            var g = e.Graphics;
            var rect = new Rectangle(0, 0, pnl.Width - 1, pnl.Height - 1);

            // Keep card background synced with the current theme
            pnl.BackColor = CardBg;

            using var shadowBrush = new SolidBrush(Color.FromArgb(18, 0, 0, 0));
            g.FillRectangle(shadowBrush, new Rectangle(2, 2, pnl.Width - 1, pnl.Height - 1));

            using var bgBrush = new SolidBrush(CardBg);
            g.FillRectangle(bgBrush, rect);

            using var pen = new Pen(CardBorder, 1);
            g.DrawRectangle(pen, rect);
        }
    }
}