// Forms/QRScannerForm.cs
#nullable enable
using Inventory_Management_System.Helpers;
using Inventory_Management_System.Models;
using Inventory_Management_System.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace Inventory_Management_System.Forms
{
    public partial class QRScannerForm : Form
    {
        private readonly EquipmentService _equipmentService = new();
        private System.Windows.Forms.Timer _scanTimer = null!;
        private string _lastText = "";
        private EquipmentSuggestHelper? _suggest;

        private static readonly Font CardLabelFont = new Font("Segoe UI", 8f, FontStyle.Bold);
        private static readonly Font CardValueFont = new Font("Segoe UI", 8f, FontStyle.Regular);

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
                if (e.Graphics is not Graphics g) return;
                using var pen = new System.Drawing.Pen(Color.FromArgb(229, 231, 235), 1f);
                g.DrawRectangle(pen, 0, 0, panelCard.Width - 1, panelCard.Height - 1);
            };

            ThemeHelper.StyleLabel(lblCardTitle, bold: true);

            pnlQrIcon.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;

                using var path = RoundedRect(new Rectangle(0, 0, pnlQrIcon.Width - 1, pnlQrIcon.Height - 1), 8);
                using var bg = new SolidBrush(Color.FromArgb(224, 242, 224));
                g.FillPath(bg, path);

                int pad = (int)(pnlQrIcon.Width * 0.2f);
                DrawQrIcon(g, new Rectangle(pad, pad, pnlQrIcon.Width - pad * 2, pnlQrIcon.Height - pad * 2), ThemeHelper.Accent);
            };

            // Style all label pairs (colors from theme, then compact font applied after)
            var captionLabels = new[]
            {
                lblEquipmentCode, lblQrCode, lblEquipmentName, lblMake, lblModel, lblSerial,
                lblCategory, lblProcurementType, lblStatus, lblCondition, lblLocation,
                lblEmployee, lblPurchaseDate, lblWarranty, lblVendor,
                lblDescriptionTitle, lblNotesTitle
            };
            foreach (var lbl in captionLabels)
            {
                ThemeHelper.StyleLabel(lbl, bold: true);
                lbl.Font = CardLabelFont;
            }

            var valueLabels = new[]
            {
                lblEquipmentCodeValue, lblQrCodeValue, lblEquipmentNameValue, lblMakeValue,
                lblModelValue, lblSerialValue, lblCategoryValue, lblProcurementTypeValue,
                lblStatusValue, lblConditionValue, lblLocationValue, lblEmployeeValue,
                lblPurchaseDateValue, lblWarrantyValue, lblVendorValue,
                lblDescriptionValue, lblNotesValue
            };
            foreach (var lbl in valueLabels)
            {
                ThemeHelper.StyleLabel(lbl);
                lbl.Font = CardValueFont;
            }

            ThemeHelper.StyleLabel(lblHistoryTitle, bold: true);
            lblNoResult.ForeColor = ThemeHelper.TextSecondary;
            lblNoResult.Font = ThemeHelper.FontDefault;
        }

        // ── Fuzzy suggestion dropdown ─────────────────────────────────────
        private void SetupSuggestions()
        {
            try
            {
                var allEquipment = _equipmentService.GetAll().Data ?? new List<Equipment>();

                _suggest = new EquipmentSuggestHelper(
                    textBox: txtQRCode,
                    hostForm: this,
                    onSelected: eq =>
                    {
                        _scanTimer?.Stop();
                        _lastText = eq.EquipmentCode;
                        Search();
                    });

                _suggest.LoadEquipment(allEquipment);
            }
            catch { /* suggestions are non-critical — fail silently */ }
        }

        // ── USB scanner auto-search ───────────────────────────────────────
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

        private void txtQRCode_TextChanged(object? sender, EventArgs e)
        {
            _scanTimer?.Stop();
            _scanTimer?.Start();
        }

        private void txtQRCode_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                _scanTimer?.Stop();
                Search();
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        private void btnSearch_Click(object? sender, EventArgs e) => Search();

        private void Search()
        {
            string code = txtQRCode.Text.Trim();
            if (string.IsNullOrWhiteSpace(code)) return;

            try
            {
                using var db = new InventoryDbContext();

                var equipment = db.Equipment
                    .Include(e => e.Allocations)
                        .ThenInclude(a => a.Employee)
                    .Include(e => e.Allocations)
                        .ThenInclude(a => a.Location)
                    .Include(e => e.EquipmentLogs)
                        .ThenInclude(l => l.ChangedByUser)
                    .Include(e => e.ProcurementType)
                    .FirstOrDefault(e =>
                        e.QrCode == code ||
                        e.EquipmentCode == code ||
                        e.SerialNumber == code);

                if (equipment == null)
                {
                    panelCard.Visible = false;
                    lblNoResult.Visible = true;
                    lblNoResult.Text = $"❌  No equipment found for: \"{code}\"";
                    dgvHistory.Rows.Clear();
                    return;
                }

                // ── Populate card ─────────────────────────────────────────
                lblEquipmentCodeValue.Text = equipment.EquipmentCode;
                lblQrCodeValue.Text = equipment.QrCode ?? "—";
                lblEquipmentNameValue.Text = equipment.Name;
                lblMakeValue.Text = equipment.Make ?? "—";
                lblModelValue.Text = equipment.Model ?? "—";
                lblSerialValue.Text = equipment.SerialNumber ?? "—";
                lblCategoryValue.Text = equipment.Category ?? "—";
                lblProcurementTypeValue.Text = equipment.ProcurementType?.TypeName ?? "—";
                lblConditionValue.Text = equipment.ConditionGrade ?? "—";
                lblPurchaseDateValue.Text = equipment.PurchaseDate?.ToString("yyyy-MM-dd") ?? "—";
                lblWarrantyValue.Text = equipment.WarrantyExpiry?.ToString("yyyy-MM-dd") ?? "—";
                lblVendorValue.Text = equipment.VendorName ?? "—";
                lblDescriptionValue.Text = string.IsNullOrWhiteSpace(equipment.Description) ? "—" : equipment.Description;
                lblNotesValue.Text = string.IsNullOrWhiteSpace(equipment.Notes) ? "—" : equipment.Notes;

                // Status with color
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

                // Current allocation info
                var activeAlloc = equipment.Allocations
                    .FirstOrDefault(a => a.Status == "approved");
                lblLocationValue.Text = activeAlloc?.Location?.LocationName ?? "—";
                lblEmployeeValue.Text = activeAlloc?.Employee?.FullName ?? "Not assigned";

                panelCard.Visible = true;
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
                        "equipment_created" => "Equipment Created",
                        "location_changed" => "Location Changed",
                        "allocation_requested" => "Allocation Requested",
                        "allocation_approved" => "Allocation Approved",
                        "allocation_rejected" => "Allocation Rejected",
                        "allocation_returned" => "Returned to Store",
                        "decommission_requested" => "Decommission Requested",
                        "decommission_approved" => "Decommission Approved",
                        "decommission_rejected" => "Decommission Rejected",
                        "transferred" => "Transferred",
                        _ => log.ActionType ?? "Unknown"
                    };

                    int rowIdx = dgvHistory.Rows.Add(
                        action,
                        log.OldValue ?? "—",
                        log.NewValue ?? "—",
                        log.ChangedByUser?.FullName ?? "System",
                        log.CreatedAt?.ToString("yyyy-MM-dd HH:mm") ?? "—"
                    );

                    var cell = dgvHistory.Rows[rowIdx].Cells["Action"];
                    cell.Style.ForeColor = log.ActionType switch
                    {
                        "equipment_created" => ThemeHelper.Success,
                        "allocation_approved" => Color.FromArgb(25, 118, 210),
                        "allocation_returned" => ThemeHelper.Warning,
                        "allocation_rejected" or
                        "decommission_approved" => ThemeHelper.Danger,
                        "transferred" => Color.FromArgb(123, 31, 162),
                        _ => ThemeHelper.TextPrimary
                    };
                    cell.Style.Font = ThemeHelper.FontBold;
                }
            }
            catch (Exception ex)
            {
                ToastHelper.Show(this, "Search failed: " + ex.Message, ToastType.Error);
            }
        }

        private void btnClear_Click(object? sender, EventArgs e)
        {
            txtQRCode.Clear();
            panelCard.Visible = false;
            lblNoResult.Visible = true;
            lblNoResult.Text = "📷  Scan a QR code or enter an equipment code to see details";
            dgvHistory.Rows.Clear();
            _lastText = "";
            txtQRCode.Focus();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _suggest?.Dispose();
            base.OnFormClosed(e);
        }
    

    private static void DrawQrIcon(Graphics g, Rectangle bounds, Color color)
        {
            const int gridSize = 5;
            float cell = Math.Min(bounds.Width, bounds.Height) / (float)gridSize;

            bool[,] filled = new bool[gridSize, gridSize];
            void Fill(int col, int row) => filled[col, row] = true;

            Fill(0, 0); Fill(1, 0); Fill(0, 1); Fill(1, 1);
            Fill(3, 0); Fill(4, 0); Fill(3, 1); Fill(4, 1);
            Fill(0, 3); Fill(1, 3); Fill(0, 4); Fill(1, 4);
            Fill(2, 2); Fill(3, 2); Fill(3, 3); Fill(4, 4);

            using var brush = new SolidBrush(color);
            for (int col = 0; col < gridSize; col++)
                for (int row = 0; row < gridSize; row++)
                {
                    if (!filled[col, row]) continue;
                    var rect = new RectangleF(bounds.X + col * cell, bounds.Y + row * cell, cell * 0.85f, cell * 0.85f);
                    g.FillRectangle(brush, rect);
                }
        }

        private static GraphicsPath RoundedRect(Rectangle r, int radius)
        {
            var path = new GraphicsPath();
            int d = radius * 2;
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}