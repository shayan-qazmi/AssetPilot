// Forms/CheckInOutForm.cs
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Microsoft.EntityFrameworkCore;
using Inventory_Management_System.Models;
using Inventory_Management_System.Services;
using Inventory_Management_System.Helpers;

namespace Inventory_Management_System.Forms
{
    public partial class CheckInOutForm : Form
    {
        private readonly TransactionService _transactionService = new();
        private readonly EquipmentService   _equipmentService   = new();
        private System.Windows.Forms.Timer   _scanTimer = null!;
        private string                       _lastText = "";
        private Equipment?                   _scannedEquipment = null;
        private Transaction?                 _activeTransaction = null;
        private bool                         _isCheckOutTab = true;
        private EquipmentSuggestHelper?      _suggest;

        private static readonly Font CardLabelFont = new Font("Segoe UI", 8f, FontStyle.Bold);
        private static readonly Font CardValueFont = new Font("Segoe UI", 8f, FontStyle.Regular);

        public CheckInOutForm()
        {
            InitializeComponent();
            ApplyTheme();
            this.Load += (s, e) =>
            {
                txtQRCode.Focus();
                SetupScanTimer();
                SetupSuggestions();
            };
        }

        private void ApplyTheme()
        {
            ThemeHelper.ApplyFormBackground(this);
            ThemeHelper.StyleToolbarPanel(panelTop);
            ThemeHelper.StyleButton(btnSearch);
            ThemeHelper.StyleSecondaryButton(btnClear);
            ThemeHelper.StyleDataGridView(dgvRecentTransactions);

            // Left panel (details card) styling
            panelLeft.BackColor = Color.White;
            panelLeft.Paint += (s, e) =>
            {
                using var pen = new Pen(Color.FromArgb(229, 231, 235), 1f);
                e.Graphics.DrawRectangle(pen, 0, 0, panelLeft.Width - 1, panelLeft.Height - 1);
            };

            // Style left card labels (colors from theme, then compact font applied after)
            ThemeHelper.StyleLabel(lblCardTitle, bold: true);

            var captionLabels = new[]
            {
                lblCodeLabel, lblQrCodeLabel, lblNameLabel, lblCategoryLabel, lblMakeLabel,
                lblModelLabel, lblSerialLabel, lblProcurementTypeLabel, lblStatusLabel,
                lblConditionLabel, lblLocationLabel, lblVendorLabel, lblPurchaseDateLabel,
                lblWarrantyLabel, lblDescriptionTitle, lblNotesTitle
            };
            foreach (var lbl in captionLabels)
            {
                ThemeHelper.StyleLabel(lbl, bold: true);
                lbl.Font = CardLabelFont;
            }

            var valueLabels = new[]
            {
                lblCodeValue, lblQrCodeValue, lblNameValue, lblCategoryValue, lblMakeValue,
                lblModelValue, lblSerialValue, lblProcurementTypeValue, lblStatusValue,
                lblConditionValue, lblLocationValue, lblVendorValue, lblPurchaseDateValue,
                lblWarrantyValue, lblDescriptionValue, lblNotesValue
            };
            foreach (var lbl in valueLabels)
            {
                ThemeHelper.StyleLabel(lbl);
                lbl.Font = CardValueFont;
            }

            // Tab header button defaults
            btnTabCheckOut.FlatStyle = FlatStyle.Flat;
            btnTabCheckOut.FlatAppearance.BorderSize = 0;
            btnTabCheckOut.Font = ThemeHelper.FontBold;
            btnTabCheckOut.Cursor = Cursors.Hand;

            btnTabCheckIn.FlatStyle = FlatStyle.Flat;
            btnTabCheckIn.FlatAppearance.BorderSize = 0;
            btnTabCheckIn.Font = ThemeHelper.FontBold;
            btnTabCheckIn.Cursor = Cursors.Hand;

            // Form action controls
            foreach (var lbl in new[] { lblIssuedTo, lblDepartment, lblOperatorOut, lblExpectedReturn, lblConditionBefore, lblRemarksOut })
            {
                ThemeHelper.StyleLabel(lbl, bold: true);
            }
            foreach (var lbl in new[] { lblCurrentlyIssuedTo, lblOperatorIn, lblConditionAfter, lblRemarksIn })
            {
                ThemeHelper.StyleLabel(lbl, bold: true);
            }

            ThemeHelper.StyleTextBox(txtIssuedTo);
            ThemeHelper.StyleTextBox(txtDepartment);
            ThemeHelper.StyleTextBox(txtOperatorOut);
            ThemeHelper.StyleTextBox(txtCurrentlyIssuedTo);
            ThemeHelper.StyleTextBox(txtOperatorIn);
            ThemeHelper.StyleComboBox(cmbConditionBefore);
            ThemeHelper.StyleComboBox(cmbConditionAfter);
            ThemeHelper.StyleTextBox(txtRemarksOut);
            ThemeHelper.StyleTextBox(txtRemarksIn);

            ThemeHelper.StyleButton(btnConfirmCheckOut);
            ThemeHelper.StyleButton(btnConfirmCheckIn);

            ThemeHelper.StyleLabel(lblGridTitle, bold: true);
            lblNoResult.ForeColor = ThemeHelper.TextSecondary;
            lblNoResult.Font = ThemeHelper.FontDefault;

            SwitchTab(true);
        }

        private void SetupSuggestions()
        {
            try
            {
                var allEquipment = _equipmentService.GetAll().Data ?? new List<Equipment>();
                _suggest = new EquipmentSuggestHelper(
                    textBox:    txtQRCode,
                    hostForm:   this,
                    onSelected: eq =>
                    {
                        _scanTimer?.Stop();
                        _lastText = eq.EquipmentCode;
                        Search();
                    });
                _suggest.LoadEquipment(allEquipment);
            }
            catch { /* fail silently */ }
        }

        private void SetupScanTimer()
        {
            _scanTimer = new System.Windows.Forms.Timer();
            _scanTimer.Interval = 300;
            _scanTimer.Tick += (s, e) =>
            {
                _scanTimer.Stop();
                if (!string.IsNullOrWhiteSpace(txtQRCode.Text) && txtQRCode.Text != _lastText)
                {
                    _lastText = txtQRCode.Text;
                    Search();
                }
            };
        }

        private void txtQRCode_TextChanged(object sender, EventArgs e)
        {
            _scanTimer?.Stop();
            _scanTimer?.Start();
        }

        private void txtQRCode_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                _scanTimer?.Stop();
                Search();
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        private void btnSearch_Click(object sender, EventArgs e) => Search();

        private void Search()
        {
            string code = txtQRCode.Text.Trim();
            if (string.IsNullOrWhiteSpace(code)) return;

            try
            {
                using var db = new InventoryDbContext();
                var equipment = db.Equipment
                    .Include(e => e.Allocations).ThenInclude(a => a.Employee)
                    .Include(e => e.Allocations).ThenInclude(a => a.Location)
                    .Include(e => e.ProcurementType)
                    .FirstOrDefault(e => e.QrCode == code || e.EquipmentCode == code || e.SerialNumber == code);

                if (equipment == null)
                {
                    panelMainContent.Visible = false;
                    lblNoResult.Visible = true;
                    lblNoResult.Text = $"❌ No equipment found for: \"{code}\"";
                    _scannedEquipment = null;
                    _activeTransaction = null;
                    return;
                }

                _scannedEquipment = equipment;
                lblNoResult.Visible = false;
                panelMainContent.Visible = true;

                // Populate details card
                lblCodeValue.Text            = equipment.EquipmentCode;
                lblQrCodeValue.Text          = equipment.QrCode ?? "—";
                lblNameValue.Text            = equipment.Name;
                lblCategoryValue.Text        = equipment.Category ?? "—";
                lblMakeValue.Text            = equipment.Make ?? "—";
                lblModelValue.Text           = equipment.Model ?? "—";
                lblSerialValue.Text          = equipment.SerialNumber ?? "—";
                lblProcurementTypeValue.Text = equipment.ProcurementType?.TypeName ?? "—";
                lblConditionValue.Text       = equipment.ConditionGrade ?? "—";
                lblVendorValue.Text          = equipment.VendorName ?? "—";
                lblPurchaseDateValue.Text    = equipment.PurchaseDate?.ToString("yyyy-MM-dd") ?? "—";
                lblWarrantyValue.Text        = equipment.WarrantyExpiry?.ToString("yyyy-MM-dd") ?? "—";
                lblDescriptionValue.Text     = string.IsNullOrWhiteSpace(equipment.Description) ? "—" : equipment.Description;
                lblNotesValue.Text           = string.IsNullOrWhiteSpace(equipment.Notes) ? "—" : equipment.Notes;

                // Color coded status
                lblStatusValue.Text = equipment.CurrentStatus;
                lblStatusValue.ForeColor = equipment.CurrentStatus switch
                {
                    "in_store" => ThemeHelper.Success,
                    "allocated" => ThemeHelper.Warning,
                    "under_repair" => Color.FromArgb(255, 112, 67),
                    "decommissioned" => ThemeHelper.Danger,
                    _ => ThemeHelper.TextPrimary
                };
                lblStatusValue.Font = CardLabelFont;

                // Current location
                var activeAlloc = equipment.Allocations.FirstOrDefault(a => a.Status == "approved");
                lblLocationValue.Text = activeAlloc?.Location?.LocationName ?? "—";

                // Fetch active transaction if allocated
                _activeTransaction = db.Transactions
                    .Where(t => t.EquipmentId == equipment.Id && (t.Status == "Pending" || t.Status == "Overdue"))
                    .OrderByDescending(t => t.TransactionDate)
                    .FirstOrDefault();

                // Pre-fill inputs
                txtOperatorOut.Text = AuthService.CurrentFullName;
                txtOperatorIn.Text = AuthService.CurrentFullName;

                txtIssuedTo.Clear();
                txtDepartment.Clear();
                txtRemarksOut.Clear();
                txtRemarksIn.Clear();
                dtpExpectedReturn.Value = DateTime.Today.AddDays(7);

                if (!string.IsNullOrWhiteSpace(equipment.ConditionGrade))
                {
                    cmbConditionBefore.SelectedItem = equipment.ConditionGrade;
                    cmbConditionAfter.SelectedItem = equipment.ConditionGrade;
                }
                else
                {
                    cmbConditionBefore.SelectedIndex = 1; // Good
                    cmbConditionAfter.SelectedIndex = 1;  // Good
                }

                if (_activeTransaction != null)
                {
                    txtCurrentlyIssuedTo.Text = _activeTransaction.IssuedTo ?? "—";
                }
                else
                {
                    txtCurrentlyIssuedTo.Text = "—";
                }

                // Auto-detect default tab based on status
                if (equipment.CurrentStatus == "allocated")
                {
                    SwitchTab(false); // Check-In
                }
                else
                {
                    SwitchTab(true);  // Check-Out
                }

                // Refresh history grid
                RefreshHistoryGrid(db, equipment.Id);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Search failed: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RefreshHistoryGrid(InventoryDbContext db, int equipmentId)
        {
            dgvRecentTransactions.Rows.Clear();
            var txs = db.Transactions
                .Where(t => t.EquipmentId == equipmentId)
                .OrderByDescending(t => t.TransactionDate)
                .Take(10)
                .ToList();

            foreach (var tx in txs)
            {
                int idx = dgvRecentTransactions.Rows.Add(
                    tx.TransactionDate.ToString("yyyy-MM-dd HH:mm"),
                    tx.TransactionType ?? "—",
                    tx.IssuedTo ?? "—",
                    tx.Department ?? "—",
                    tx.ExpectedReturn?.ToString("yyyy-MM-dd") ?? "—",
                    tx.Status ?? "—",
                    tx.ConditionBefore ?? "—",
                    tx.ConditionAfter ?? "—",
                    tx.Remarks ?? "—"
                );

                var typeCell = dgvRecentTransactions.Rows[idx].Cells["Type"];
                typeCell.Style.Font = ThemeHelper.FontBold;
                typeCell.Style.ForeColor = tx.TransactionType switch
                {
                    "Check-Out" => Color.FromArgb(25, 118, 210),
                    "Check-In" => ThemeHelper.Success,
                    _ => ThemeHelper.TextPrimary
                };

                var statusCell = dgvRecentTransactions.Rows[idx].Cells["Status"];
                statusCell.Style.Font = ThemeHelper.FontBold;
                statusCell.Style.ForeColor = tx.Status switch
                {
                    "Pending" => ThemeHelper.Warning,
                    "Completed" => ThemeHelper.Success,
                    "Overdue" => ThemeHelper.Danger,
                    _ => ThemeHelper.TextPrimary
                };
            }
        }

        private void SwitchTab(bool isCheckOut)
        {
            _isCheckOutTab = isCheckOut;
            if (isCheckOut)
            {
                btnTabCheckOut.BackColor = ThemeHelper.Accent;
                btnTabCheckOut.ForeColor = Color.White;
                btnTabCheckIn.BackColor = Color.FromArgb(243, 244, 246);
                btnTabCheckIn.ForeColor = ThemeHelper.TextSecondary;
                panelCheckOut.Visible = true;
                panelCheckIn.Visible = false;
            }
            else
            {
                btnTabCheckIn.BackColor = ThemeHelper.Accent;
                btnTabCheckIn.ForeColor = Color.White;
                btnTabCheckOut.BackColor = Color.FromArgb(243, 244, 246);
                btnTabCheckOut.ForeColor = ThemeHelper.TextSecondary;
                panelCheckIn.Visible = true;
                panelCheckOut.Visible = false;
            }
        }

        private void btnTabCheckOut_Click(object sender, EventArgs e) => SwitchTab(true);
        private void btnTabCheckIn_Click(object sender, EventArgs e) => SwitchTab(false);

        private void btnConfirmCheckOut_Click(object sender, EventArgs e)
        {
            if (_scannedEquipment == null) return;

            if (_scannedEquipment.CurrentStatus != "in_store")
            {
                MessageBox.Show($"This equipment is currently not in store (status: {_scannedEquipment.CurrentStatus}) and cannot be checked out.",
                    "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(txtIssuedTo.Text))
            {
                MessageBox.Show("Please enter the name of the person this item is being issued to.",
                    "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var tx = new Transaction
            {
                EquipmentId = _scannedEquipment.Id,
                IssuedTo = txtIssuedTo.Text.Trim(),
                Department = txtDepartment.Text.Trim(),
                OperatorName = txtOperatorOut.Text.Trim(),
                ExpectedReturn = DateOnly.FromDateTime(dtpExpectedReturn.Value),
                ConditionBefore = cmbConditionBefore.SelectedItem?.ToString(),
                Remarks = txtRemarksOut.Text.Trim()
            };

            var res = _transactionService.CheckOut(tx);
            if (res.Success)
            {
                MessageBox.Show("Check-Out transaction completed successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                Search(); // Refresh UI
            }
            else
            {
                MessageBox.Show(res.ErrorMessage, "Check-Out Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnConfirmCheckIn_Click(object sender, EventArgs e)
        {
            if (_scannedEquipment == null) return;

            if (_activeTransaction == null)
            {
                MessageBox.Show("No active pending check-out transaction was found for this equipment.",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string condAfter = cmbConditionAfter.SelectedItem?.ToString() ?? "Good";
            string remarks = txtRemarksIn.Text.Trim();

            var res = _transactionService.CheckIn(_activeTransaction.TransactionId, condAfter, remarks);
            if (res.Success)
            {
                MessageBox.Show("Check-In transaction completed successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                Search(); // Refresh UI
            }
            else
            {
                MessageBox.Show(res.ErrorMessage, "Check-In Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            txtQRCode.Clear();
            _lastText = "";
            _scannedEquipment = null;
            _activeTransaction = null;
            panelMainContent.Visible = false;
            lblNoResult.Visible = true;
            lblNoResult.Text = "📋 Scan a QR code or enter an equipment code to start check-in or check-out";
            txtQRCode.Focus();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _suggest?.Dispose();
            base.OnFormClosed(e);
        }
    }
}