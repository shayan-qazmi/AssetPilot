// Forms/QRScannerForm.cs
using Inventory_Management_System.Helpers;
using Inventory_Management_System.Models;
using Inventory_Management_System.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Inventory_Management_System.Forms
{
    public partial class QRScannerForm : Form
    {
        private readonly EquipmentService     _equipmentService = new();
        private System.Windows.Forms.Timer    _scanTimer = null!;
        private string                        _lastText  = "";
        private EquipmentSuggestHelper?       _suggest;

        public QRScannerForm()
        {
            InitializeComponent();
            ApplyTheme();
            this.Load += (s, e) =>
            {
                SetupScanTimer();
                SetupSuggestions();
                txtQRCode.Focus();
            };
        }

        private void ApplyTheme()
        {
            ThemeHelper.ApplyFormBackground(this);
            ThemeHelper.StyleToolbarPanel(panelTop);
            ThemeHelper.StyleButton(btnSearch);
            ThemeHelper.StyleSecondaryButton(btnClear);
            ThemeHelper.StyleDataGridView(dgvHistory);

            // Card panel
            panelCard.BackColor = Color.White;
            panelCard.Paint += (s, e) =>
            {
                using var pen = new System.Drawing.Pen(Color.FromArgb(229, 231, 235), 1f);
                e.Graphics.DrawRectangle(pen, 0, 0, panelCard.Width - 1, panelCard.Height - 1);
            };

            // Style all label pairs
            foreach (var lbl in new[] { lblEquipmentCode, lblEquipmentName, lblMake, lblModel,
                                         lblSerial, lblStatus, lblLocation, lblEmployee, lblPurchaseDate })
                ThemeHelper.StyleLabel(lbl, bold: true);

            foreach (var lbl in new[] { lblEquipmentCodeValue, lblEquipmentNameValue, lblMakeValue,
                                         lblModelValue, lblSerialValue, lblStatusValue,
                                         lblLocationValue, lblEmployeeValue, lblPurchaseDateValue })
                ThemeHelper.StyleLabel(lbl);

            ThemeHelper.StyleLabel(lblHistoryTitle, bold: true);
            lblNoResult.ForeColor = ThemeHelper.TextSecondary;
            lblNoResult.Font      = ThemeHelper.FontDefault;
        }

        // ── Fuzzy suggestion dropdown ─────────────────────────────────────
        private void SetupSuggestions()
        {
            try
            {
                // Load all equipment once for the lifetime of the form
                var allEquipment = _equipmentService.GetAll().Data ?? new List<Equipment>();

                _suggest = new EquipmentSuggestHelper(
                    textBox:    txtQRCode,
                    hostForm:   this,
                    onSelected: eq =>
                    {
                        // Stop scan timer so it doesn't double-fire
                        _scanTimer?.Stop();
                        _lastText = eq.EquipmentCode;
                        Search();
                    });

                _suggest.LoadEquipment(allEquipment);
            }
            catch { /* suggestions are non-critical — fail silently */ }
        }

        // ── USB scanner auto-search ───────────────────────────────────────
        // USB barcode/QR scanners type quickly then send Enter — we detect
        // that pattern and auto-search without the user pressing the button
        private void SetupScanTimer()
        {
            _scanTimer          = new System.Windows.Forms.Timer();
            _scanTimer.Interval = 300; // if text stops changing for 300ms after rapid input = scan complete
            _scanTimer.Tick    += (s, e) =>
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
            // Restart timer on each keystroke — fires Search() when typing pauses
            _scanTimer?.Stop();
            _scanTimer?.Start();
        }

        private void txtQRCode_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                _scanTimer?.Stop();
                Search();
                e.Handled          = true;
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

                // Search by QR code OR equipment code
                var equipment = db.Equipment
                    .Include(e => e.Allocations)
                        .ThenInclude(a => a.Employee)
                    .Include(e => e.Allocations)
                        .ThenInclude(a => a.Location)
                    .Include(e => e.EquipmentLogs)
                        .ThenInclude(l => l.ChangedByUser)
                    .FirstOrDefault(e =>
                        e.QrCode        == code ||
                        e.EquipmentCode == code ||
                        e.SerialNumber  == code);

                if (equipment == null)
                {
                    panelCard.Visible   = false;
                    lblNoResult.Visible = true;
                    lblNoResult.Text    = $"❌  No equipment found for: \"{code}\"";
                    dgvHistory.Rows.Clear();
                    return;
                }

                // ── Populate card ─────────────────────────────────────────
                lblEquipmentCodeValue.Text = equipment.EquipmentCode;
                lblEquipmentNameValue.Text = equipment.Name;
                lblMakeValue.Text          = equipment.Make ?? "—";
                lblModelValue.Text         = equipment.Model ?? "—";
                lblSerialValue.Text        = equipment.SerialNumber ?? "—";
                lblPurchaseDateValue.Text  = equipment.PurchaseDate?.ToString("yyyy-MM-dd") ?? "—";

                // Status with color
                lblStatusValue.Text      = equipment.CurrentStatus;
                lblStatusValue.ForeColor = equipment.CurrentStatus switch
                {
                    "in_store"       => ThemeHelper.Success,
                    "allocated"      => ThemeHelper.Warning,
                    "under_repair"   => Color.FromArgb(255, 112, 67),
                    "decommissioned" => ThemeHelper.Danger,
                    _                => ThemeHelper.TextPrimary
                };
                lblStatusValue.Font = ThemeHelper.FontBold;

                // Current allocation info
                var activeAlloc = equipment.Allocations
                    .FirstOrDefault(a => a.Status == "approved");
                lblLocationValue.Text = activeAlloc?.Location?.LocationName ?? "—";
                lblEmployeeValue.Text = activeAlloc?.Employee?.FullName     ?? "Not assigned";

                panelCard.Visible   = true;
                lblNoResult.Visible = false;

                // ── Populate history grid ──────────────────────────────────
                dgvHistory.Rows.Clear();
                var logs = equipment.EquipmentLogs
                    .OrderByDescending(l => l.CreatedAt)
                    .ToList();

                foreach (var log in logs)
                {
                    string action = log.ActionType switch
                    {
                        "equipment_created"      => "Equipment Created",
                        "location_changed"       => "Location Changed",
                        "allocation_requested"   => "Allocation Requested",
                        "allocation_approved"    => "Allocation Approved",
                        "allocation_rejected"    => "Allocation Rejected",
                        "allocation_returned"    => "Returned to Store",
                        "decommission_requested" => "Decommission Requested",
                        "decommission_approved"  => "Decommission Approved",
                        "decommission_rejected"  => "Decommission Rejected",
                        "transferred"            => "Transferred",
                        _                        => log.ActionType ?? "Unknown"
                    };

                    int rowIdx = dgvHistory.Rows.Add(
                        action,
                        log.OldValue ?? "—",
                        log.NewValue ?? "—",
                        log.ChangedByUser?.FullName ?? "System",
                        log.CreatedAt?.ToString("yyyy-MM-dd HH:mm") ?? "—"
                    );

                    // Color code action cell
                    var cell = dgvHistory.Rows[rowIdx].Cells["Action"];
                    cell.Style.ForeColor = log.ActionType switch
                    {
                        "equipment_created"    => ThemeHelper.Success,
                        "allocation_approved"  => Color.FromArgb(25, 118, 210),
                        "allocation_returned"  => ThemeHelper.Warning,
                        "allocation_rejected"  or
                        "decommission_approved"=> ThemeHelper.Danger,
                        "transferred"          => Color.FromArgb(123, 31, 162),
                        _                      => ThemeHelper.TextPrimary
                    };
                    cell.Style.Font = ThemeHelper.FontBold;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Search failed: " + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            txtQRCode.Clear();
            panelCard.Visible   = false;
            lblNoResult.Visible = true;
            lblNoResult.Text    = "📷  Scan a QR code or enter an equipment code to see details";
            dgvHistory.Rows.Clear();
            _lastText = "";
            txtQRCode.Focus();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _suggest?.Dispose();
            base.OnFormClosed(e);
        }
    }
}