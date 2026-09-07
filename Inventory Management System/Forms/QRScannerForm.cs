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
using System.Text;
using System.Windows.Forms;

namespace Inventory_Management_System.Forms
{
    public partial class QRScannerForm : Form, IThemableForm
    {

        private bool _bulkMode = false;
        private readonly List<(string Code, string Name, DateTime ScannedAt)> _bulkScanCodes = new();
        private readonly EquipmentService _equipmentService = new();
        private System.Windows.Forms.Timer _scanTimer = null!;
        private string _lastText = "";
        private EquipmentSuggestHelper? _suggest;

        private static readonly Font CardLabelFont = new Font("Segoe UI", 8f, FontStyle.Bold);
        private static readonly Font CardValueFont = new Font("Segoe UI", 8f, FontStyle.Regular);

        private Equipment? _currentEquipment;
        private List<EquipmentLog> _currentLogs = new();
        private readonly List<string> _recentScans = new();
        private Image? _currentPictureImage;

        public QRScannerForm()
        {
            InitializeComponent();
            ApplyTheme();
            ThemeHelper.StyleSecondaryButton(btnExportBulk);
            RefreshRecentScansUI();
            this.Load += (s, e) =>
            {
                SetupScanTimer();
                SetupSuggestions();
                txtQRCode.Focus();
            };
        }

        public void ApplyTheme()
        {
            ThemeHelper.ApplyFormBackground(this);
            ThemeHelper.StyleToolbarPanel(panelTop);
            btnSearch.BackColor = Color.FromArgb(56, 142, 60);
            btnSearch.ForeColor = Color.White;
            btnSearch.FlatStyle = FlatStyle.Flat;
            btnSearch.FlatAppearance.BorderSize = 0;
            btnSearch.FlatAppearance.MouseOverBackColor = Color.FromArgb(46, 125, 50);
            ThemeHelper.StyleButton(btnSearch, Color.FromArgb(56, 142, 60));
            ThemeHelper.StyleSecondaryButton(btnClear);
            ThemeHelper.StyleSecondaryButton(btnExportHistory);
            ThemeHelper.StyleSecondaryButton(btnPrintLabel);
            ThemeHelper.StyleDataGridView(dgvHistory, Color.FromArgb(76, 175, 80));
            dgvHistory.BackgroundColor = ThemeHelper.CardBg;
            dgvHistory.EnableHeadersVisualStyles = false;
            dgvHistory.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(56, 142, 60);
            dgvHistory.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvHistory.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(56, 142, 60);
            dgvHistory.ColumnHeadersDefaultCellStyle.SelectionForeColor = Color.White;
            dgvHistory.ColumnHeadersHeight = 36;
            dgvHistory.EnableHeadersVisualStyles = false;
            dgvHistory.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(56, 142, 60);
            dgvHistory.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvHistory.ColumnHeadersDefaultCellStyle.Font = ThemeHelper.FontBold;
            dgvHistory.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(56, 142, 60);
            dgvHistory.ColumnHeadersHeight = 36;
            lblRecentLabel.ForeColor = ThemeHelper.TextSecondary;
            lblRecentLabel.Font = ThemeHelper.FontSmall;

            panelCard.BackColor = ThemeHelper.CardBg;
            panelCard.Paint += (s, e) =>
            {
                if (e.Graphics is not Graphics g) return;
                using var pen = new System.Drawing.Pen(ThemeHelper.IsDarkMode ? Color.FromArgb(55, 58, 65) : Color.FromArgb(229, 231, 235), 1f);
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

            // ── Hero styling ───────────────────────────────────────────────
            ThemeHelper.StyleLabel(lblHeroName, bold: true);
            ThemeHelper.StyleLabel(lblHeroCode);
            lblHeroCode.ForeColor = ThemeHelper.TextSecondary;

            btnViewPicture.BackColor = Color.FromArgb(238, 242, 255);
            btnViewPicture.ForeColor = ThemeHelper.Header;
            btnViewPicture.FlatAppearance.BorderColor = Color.FromArgb(199, 210, 254);
            btnViewPicture.FlatAppearance.MouseOverBackColor = Color.FromArgb(224, 231, 255);
            btnViewPicture.FlatAppearance.BorderSize = 1;

            btnViewLogs.BackColor = Color.FromArgb(238, 242, 255);
            btnViewLogs.ForeColor = ThemeHelper.Header;
            btnViewLogs.FlatAppearance.BorderColor = Color.FromArgb(199, 210, 254);
            btnViewLogs.FlatAppearance.MouseOverBackColor = Color.FromArgb(224, 231, 255);
            btnViewLogs.FlatAppearance.BorderSize = 1;

            btnViewQR.BackColor = Color.FromArgb(238, 242, 255);
            btnViewQR.ForeColor = ThemeHelper.Header;
            btnViewQR.FlatAppearance.BorderColor = Color.FromArgb(199, 210, 254);
            btnViewQR.FlatAppearance.MouseOverBackColor = Color.FromArgb(224, 231, 255);
            btnViewQR.FlatAppearance.BorderSize = 1;

            btnCheckInOut.BackColor = Color.FromArgb(224, 242, 224);
            btnCheckInOut.ForeColor = ThemeHelper.Success;
            btnCheckInOut.FlatAppearance.BorderColor = Color.FromArgb(200, 230, 201);
            btnCheckInOut.FlatAppearance.MouseOverBackColor = Color.FromArgb(200, 230, 201);
            btnCheckInOut.FlatAppearance.BorderSize = 1;

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
                lblConditionValue, lblLocationValue, lblEmployeeValue,
                lblPurchaseDateValue, lblWarrantyValue, lblVendorValue,
                lblDescriptionValue, lblNotesValue
            };
            foreach (var lbl in valueLabels)
            {
                ThemeHelper.StyleLabel(lbl);
                lbl.Font = CardValueFont;
            }
            lblStatusValue.Font = CardValueFont;

            ThemeHelper.StyleLabel(lblHistoryTitle, bold: true);
            lblNoResult.ForeColor = ThemeHelper.TextSecondary;
            lblNoResult.Font = ThemeHelper.FontDefault;
        }

        // ── Recent scans strip ───────────────────────────────────────────
        private void AddRecentScan(string code)
        {
            if (string.IsNullOrWhiteSpace(code)) return;
            _recentScans.RemoveAll(c => string.Equals(c, code, StringComparison.OrdinalIgnoreCase));
            _recentScans.Insert(0, code);
            if (_recentScans.Count > 8) _recentScans.RemoveAt(_recentScans.Count - 1);
            RefreshRecentScansUI();
        }

        private void RefreshRecentScansUI()
        {
            panelRecent.Controls.Clear();
            foreach (var code in _recentScans)
            {
                var chip = new Button
                {
                    Text = code,
                    AutoSize = true,
                    Height = 28,
                    Margin = new Padding(0, 0, 6, 0),
                    FlatStyle = FlatStyle.Flat
                };
                chip.FlatAppearance.BorderColor = ThemeHelper.IsDarkMode
                    ? Color.FromArgb(70, 70, 70)
                    : Color.FromArgb(220, 220, 220);
                chip.Click += (s, e) =>
                {
                    txtQRCode.Text = code;
                    Search();
                };
                panelRecent.Controls.Add(chip);
            }
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
                    _currentEquipment = null;
                    _currentLogs = new List<EquipmentLog>();
                    panelCard.Visible = false;
                    lblNoResult.Visible = true;
                    lblNoResult.Text = $"❌  No equipment found for: \"{code}\"";
                    dgvHistory.Rows.Clear();
                    ShowSuggestions(code, db);
                    return;
                }

                panelSuggestions.Controls.Clear();
                panelSuggestions.Visible = false;

                _currentEquipment = equipment;

                // ── Load picture (same resolution logic AllocationDetailPopup uses) ──
                _currentPictureImage?.Dispose();
                _currentPictureImage = null;
                try
                {
                    _currentPictureImage = AvatarHelper.TryLoadEquipmentImage(equipment.PicturePath);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "QRScannerForm.TryLoadEquipmentImage");
                    _currentPictureImage = null;
                }
                picHero.Invalidate();

                bool hasPicture = _currentPictureImage != null;
                btnViewPicture.Text = hasPicture ? "🖼 View Picture" : "🖼 No Picture";
                btnViewPicture.Enabled = hasPicture;
                btnViewPicture.Cursor = hasPicture ? Cursors.Hand : Cursors.Default;
                if (!hasPicture)
                {
                    btnViewPicture.BackColor = Color.FromArgb(243, 244, 246);
                    btnViewPicture.ForeColor = Color.FromArgb(156, 163, 175);
                    btnViewPicture.FlatAppearance.BorderColor = Color.FromArgb(229, 231, 235);
                }
                else
                {
                    btnViewPicture.BackColor = Color.FromArgb(238, 242, 255);
                    btnViewPicture.ForeColor = ThemeHelper.Header;
                    btnViewPicture.FlatAppearance.BorderColor = Color.FromArgb(199, 210, 254);
                }

                lblQrPreviewValue.Text = equipment.QrCode ?? "—";
                picQrPreview.Invalidate();
                lblHeroName.Text = equipment.Name;
                lblHeroCode.Text = $"{equipment.EquipmentCode} · {equipment.QrCode ?? "—"}";

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
                lblVendorValue.Text = equipment.VendorName ?? "—";
                lblDescriptionValue.Text = string.IsNullOrWhiteSpace(equipment.Description) ? "—" : equipment.Description;
                lblNotesValue.Text = string.IsNullOrWhiteSpace(equipment.Notes) ? "—" : equipment.Notes;

                // Warranty value + expiry status flag
                lblWarrantyValue.Text = equipment.WarrantyExpiry?.ToString("yyyy-MM-dd") ?? "—";
                if (equipment.WarrantyExpiry.HasValue)
                {
                    int daysLeft = equipment.WarrantyExpiry.Value.DayNumber - DateOnly.FromDateTime(DateTime.Now).DayNumber;
                    if (daysLeft < 0)
                    {
                        lblWarrantyValue.Text += "  (Expired)";
                        lblWarrantyValue.ForeColor = ThemeHelper.Danger;
                    }
                    else if (daysLeft <= 30)
                    {
                        lblWarrantyValue.Text += $"  (Expires in {daysLeft}d)";
                        lblWarrantyValue.ForeColor = ThemeHelper.Warning;
                    }
                    else
                    {
                        lblWarrantyValue.ForeColor = ThemeHelper.TextPrimary;
                    }
                }
                else
                {
                    lblWarrantyValue.ForeColor = ThemeHelper.TextPrimary;
                }

                // Status — both the inline field badge and the hero badge
                lblStatusValue.Text = equipment.CurrentStatus;
                var (statusBack, statusFore) = equipment.CurrentStatus switch
                {
                    "in_store" => (Color.FromArgb(200, 230, 201), ThemeHelper.Success),
                    "allocated" => (Color.FromArgb(255, 249, 196), ThemeHelper.Warning),
                    "under_repair" => (Color.FromArgb(255, 224, 178), Color.FromArgb(255, 112, 67)),
                    "decommissioned" => (Color.FromArgb(255, 205, 210), ThemeHelper.Danger),
                    _ => (Color.Transparent, ThemeHelper.TextPrimary)
                };
                lblStatusValue.BackColor = statusBack;
                lblStatusValue.ForeColor = statusFore;
                lblStatusValue.Font = CardLabelFont;

                lblHeroStatus.Text = "●  " + equipment.CurrentStatus;
                lblHeroStatus.BackColor = statusBack;
                lblHeroStatus.ForeColor = statusFore;

                // Current allocation info
                var activeAlloc = equipment.Allocations
                    .FirstOrDefault(a => a.Status == "approved");
                lblLocationValue.Text = activeAlloc?.Location?.LocationName ?? "—";
                lblEmployeeValue.Text = activeAlloc?.Employee?.FullName ?? "Not assigned";

                panelCard.Visible = true;
                lblNoResult.Visible = false;
                panelCard.AutoScrollPosition = new Point(0, 0);
                panelCard.VerticalScroll.Value = 0;
                panelCard.PerformLayout();

                // ── Populate history grid ──────────────────────────────────
                dgvHistory.Rows.Clear();
                var logs = equipment.EquipmentLogs
                    .OrderByDescending(l => l.CreatedAt)
                    .ToList();
                _currentLogs = logs;

                foreach (var log in logs)
                {
                    string rawNew = log.NewValue ?? "";
                    string screenName = ExtractSourceScreen(ref rawNew);
                    string newValueDisplay = string.IsNullOrWhiteSpace(rawNew) ? "—" : rawNew;

                    string action = AuditLogDisplay.FormatActionLabel(log.ActionType);
                    if (!string.IsNullOrEmpty(screenName))
                        action = $"{action} (via {screenName})";

                    string changedBy = log.ChangedByUser != null
                        ? $"{log.ChangedByUser.FullName} (@{log.ChangedByUser.Username})"
                        : "System";

                    int rowIdx = dgvHistory.Rows.Add(
                        action,
                        log.OldValue ?? "—",
                        newValueDisplay,
                        changedBy,
                        TimeHelper.ToPakistanTime(log.CreatedAt)?.ToString("dd MMM yyyy, hh:mm tt") ?? "—"
                    );

                    var (backColor, foreColor) = AuditLogDisplay.GetActionColors(log.ActionType);
                    var row = dgvHistory.Rows[rowIdx];
                    var cell = row.Cells["Action"];
                    cell.Style.BackColor = backColor;
                    cell.Style.ForeColor = foreColor;
                    cell.Style.Font = ThemeHelper.FontBold;

                    row.Cells["OldValue"].ToolTipText = log.OldValue ?? "—";
                    row.Cells["NewValue"].ToolTipText = newValueDisplay;
                }

                lblHistoryTitle.Text = $"📜 Equipment History ({logs.Count})";

                AddRecentScan(equipment.EquipmentCode);
                if (_bulkMode)
                {
                    _bulkScanCodes.Add((equipment.EquipmentCode, equipment.Name, DateTime.Now));
                    ToastHelper.Show(this, $"Added to bulk list ({_bulkScanCodes.Count} scanned)", ToastType.Success);
                }
            }
            catch (Exception ex)
            {
                ToastHelper.Show(this, "Search failed: " + ex.Message, ToastType.Error);
            }
        }

        // ── Hero photo painting — mirrors AllocationDetailPopup.DrawEquipmentIcon ──
        private void picHero_Paint(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            var bounds = new Rectangle(0, 0, picHero.Width - 1, picHero.Height - 1);
            g.SmoothingMode = SmoothingMode.AntiAlias;

            if (_currentPictureImage != null)
            {
                using var path = RoundedRect(bounds, 12);
                var oldClip = g.Clip;
                g.SetClip(path);
                g.DrawImage(_currentPictureImage, bounds);
                g.Clip = oldClip;
                return;
            }

            using (var path = RoundedRect(bounds, 12))
            using (var bg = new SolidBrush(Color.FromArgb(238, 242, 255)))
                g.FillPath(bg, path);

            var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            using var font = new Font("Segoe UI Emoji", 32f);
            using var textBrush = new SolidBrush(ThemeHelper.Header);
            g.DrawString("🖥", font, textBrush, bounds, sf);
        }

        private void picQrPreview_Paint(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var bounds = new Rectangle(4, 4, picQrPreview.Width - 8, picQrPreview.Height - 8);
            DrawQrIcon(g, bounds, Color.Black);
        }
        private void btnViewPicture_Click(object? sender, EventArgs e)
        {
            if (_currentPictureImage == null || _currentEquipment == null) return;

            using var viewer = new Form
            {
                Text = $"QR Code — {_currentEquipment.EquipmentCode}",
                Size = new Size(340, 440),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                StartPosition = FormStartPosition.CenterParent,
                BackColor = Color.White,
                AutoScaleMode = AutoScaleMode.None
            };

            var pic = new PictureBox
            {
                Image = _currentPictureImage,
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Black
            };
            viewer.Controls.Add(pic);

            viewer.ShowDialog(this);
        }

        private void btnViewQR_Click(object? sender, EventArgs e)
        {
            if (_currentEquipment == null) return;

            string qrContent = _currentEquipment.QrCode ?? _currentEquipment.EquipmentCode;
            if (string.IsNullOrWhiteSpace(qrContent))
            {
                ToastHelper.Show(this, "This equipment has no QR code assigned.", ToastType.Info);
                return;
            }

            Bitmap? qrBitmap;
            try
            {
                qrBitmap = QRHelper.GenerateQRCode(qrContent, 10);
            }
            catch (Exception ex)
            {
                ToastHelper.Show(this, "Could not generate QR code: " + ex.Message, ToastType.Error);
                return;
            }

            using var viewer = new Form
            {
                Text = $"QR Code — {_currentEquipment.EquipmentCode}",
                Size = new Size(340, 400),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                StartPosition = FormStartPosition.CenterParent,
                BackColor = Color.White
            };

            var pic = new PictureBox
            {
                Image = qrBitmap,
                SizeMode = PictureBoxSizeMode.Zoom,
                Size = new Size(260, 260),
                Location = new Point((340 - 260) / 2, 20),
                BackColor = Color.White
            };
            viewer.Controls.Add(pic);

            var lbl = new Label
            {
                Text = qrContent,
                AutoSize = false,
                Width = 300,
                Height = 24,
                Location = new Point(20, 290),
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            };
            viewer.Controls.Add(lbl);

            var btnPrint = new Button
            {
                Text = "🖶 Print Label",
                Size = new Size(140, 34),
                Location = new Point(30, 360),
                FlatStyle = FlatStyle.Flat
            };
            btnPrint.Click += (s, ev) => btnPrintLabel_Click(s, EventArgs.Empty);
            viewer.Controls.Add(btnPrint);

            var btnClose = new Button
            {
                Text = "Close",
                Size = new Size(100, 34),
                Location = new Point(190, 360),
                FlatStyle = FlatStyle.Flat
            };
            btnClose.Click += (s, ev) => viewer.Close();
            viewer.Controls.Add(btnClose);

            viewer.FormClosed += (s, ev) => qrBitmap?.Dispose();
            viewer.ShowDialog(this);
        }

        private void btnCheckInOut_Click(object? sender, EventArgs e)
        {
            string code = _currentEquipment?.EquipmentCode ?? txtQRCode.Text.Trim();
            if (string.IsNullOrWhiteSpace(code))
            {
                ToastHelper.Show(this, "Search for an equipment first.", ToastType.Warning);
                return;
            }
            var dashboard = Application.OpenForms.OfType<DashboardForm>().FirstOrDefault();
            dashboard?.NavigateToCheckInOut(code);
        }

        private void btnViewLogs_Click(object? sender, EventArgs e)
        {
            string code = _currentEquipment?.EquipmentCode ?? txtQRCode.Text.Trim();
            if (string.IsNullOrWhiteSpace(code))
            {
                ToastHelper.Show(this, "Search for an equipment first.", ToastType.Warning);
                return;
            }
            var dashboard = Application.OpenForms.OfType<DashboardForm>().FirstOrDefault();
            dashboard?.NavigateToLogs(code);
        }

        private static string ExtractSourceScreen(ref string newValue)
        {
            if (string.IsNullOrEmpty(newValue) || !newValue.Contains("Source Screen:"))
                return "";

            var parts = newValue.Split(" | ").ToList();
            var screenPart = parts.FirstOrDefault(p => p.Trim().StartsWith("Source Screen:"));
            if (screenPart == null) return "";

            string screenName = screenPart.Replace("Source Screen:", "").Trim();
            parts.Remove(screenPart);
            newValue = string.Join(" | ", parts);
            return screenName;
        }

        private void dgvHistory_CellDoubleClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || _currentEquipment == null) return;
            if (e.RowIndex >= _currentLogs.Count) return;

            var row = dgvHistory.Rows[e.RowIndex];

            string action = row.Cells["Action"].Value?.ToString() ?? "";
            string old = row.Cells["OldValue"].Value?.ToString() ?? "";
            string @new = row.Cells["NewValue"].Value?.ToString() ?? "";
            string user = row.Cells["ChangedBy"].Value?.ToString() ?? "";
            string date = row.Cells["Date"].Value?.ToString() ?? "";

            using var popup = new LogDetailPopup(
                action, user, date,
                _currentEquipment.EquipmentCode,
                _currentEquipment.QrCode ?? "—",
                _currentEquipment.Name,
                _currentEquipment.Make ?? "—",
                _currentEquipment.Model ?? "—",
                old, @new);
            popup.ShowDialog(this);
        }

        private void ShowSuggestions(string code, InventoryDbContext db)
        {
            panelSuggestions.Controls.Clear();
            panelSuggestions.Visible = false;

            var candidates = db.Equipment
     .ToList()
     .Where(e => FuzzySearchHelper.IsFuzzyMatch(e.EquipmentCode, code)
              || FuzzySearchHelper.IsFuzzyMatch(e.Name, code))
     .Take(3)
     .ToList();

            if (candidates.Count == 0) return;

            panelSuggestions.Visible = true;

            var hint = new Label
            {
                Text = "Did you mean:",
                AutoSize = true,
                Font = ThemeHelper.FontSmall,
                ForeColor = ThemeHelper.TextSecondary
            };
            panelSuggestions.Controls.Add(hint);

            foreach (var c in candidates)
            {
                var chip = new Button
                {
                    Text = $"{c.EquipmentCode} · {c.Name}",
                    AutoSize = true,
                    Height = 30,
                    Margin = new Padding(0, 4, 0, 0),
                    FlatStyle = FlatStyle.Flat
                };
                chip.Click += (s, e) =>
                {
                    txtQRCode.Text = c.EquipmentCode;
                    Search();
                };
                panelSuggestions.Controls.Add(chip);
            }
        }

        private void btnExportHistory_Click(object? sender, EventArgs e)
        {
            if (_currentEquipment == null)
            {
                ToastHelper.Show(this, "Search for an equipment first.", ToastType.Warning);
                return;
            }

            try
            {
                using var dlg = new SaveFileDialog
                {
                    Filter = "CSV files (*.csv)|*.csv",
                    FileName = $"{_currentEquipment.EquipmentCode}_History_{DateTime.Now:yyyyMMdd_HHmm}.csv"
                };

                if (dlg.ShowDialog() != DialogResult.OK) return;

                var sb = new StringBuilder();
                sb.AppendLine("Action,Previous Value,New Value,Changed By,Timestamp");

                string Escape(object? v) => $"\"{(v?.ToString() ?? "").Replace("\"", "\"\"")}\"";

                foreach (DataGridViewRow row in dgvHistory.Rows)
                {
                    sb.AppendLine(string.Join(",",
                        Escape(row.Cells["Action"].Value),
                        Escape(row.Cells["OldValue"].Value),
                        Escape(row.Cells["NewValue"].Value),
                        Escape(row.Cells["ChangedBy"].Value),
                        Escape(row.Cells["Date"].Value)));
                }

                System.IO.File.WriteAllText(dlg.FileName, sb.ToString());
                ToastHelper.Show(this, $"History exported to:\n{dlg.FileName}", ToastType.Success);
            }
            catch (Exception ex)
            {
                ToastHelper.Show(this, "Export failed: " + ex.Message, ToastType.Error);
            }
        }

        private void btnPrintLabel_Click(object? sender, EventArgs e)
        {
            // Stub — wire this to your existing QR/label printing routine if one
            // exists elsewhere in the app; none was available to integrate against.
            ToastHelper.Show(this, "Label printing isn't wired up yet.", ToastType.Info);
        }

        private void btnClear_Click(object? sender, EventArgs e)
        {
            txtQRCode.Clear();
            panelSuggestions.Visible = false;
            panelCard.Visible = false;
            lblNoResult.Visible = true;
            lblNoResult.Text = "📷  Scan a QR code or enter an equipment code to see details";
            dgvHistory.Rows.Clear();
            lblHistoryTitle.Text = "📜 Equipment History";
            _lastText = "";
            _currentEquipment = null;
            _currentLogs = new List<EquipmentLog>();
            _currentPictureImage?.Dispose();
            _currentPictureImage = null;
            txtQRCode.Focus();
        }

        private void chkBulkMode_CheckedChanged(object? sender, EventArgs e)
        {
            _bulkMode = chkBulkMode.Checked;
            btnExportBulk.Visible = _bulkMode;
            _bulkScanCodes.Clear();
            ToastHelper.Show(this, _bulkMode
                ? "Bulk scan mode on — every scan is added to a list."
                : "Bulk scan mode off.", ToastType.Info);
        }

        private void btnExportBulk_Click(object? sender, EventArgs e)
        {
            if (_bulkScanCodes.Count == 0)
            {
                ToastHelper.Show(this, "No scans recorded yet.", ToastType.Warning);
                return;
            }

            try
            {
                using var dlg = new SaveFileDialog
                {
                    Filter = "CSV files (*.csv)|*.csv",
                    FileName = $"BulkScan_{DateTime.Now:yyyyMMdd_HHmm}.csv"
                };
                if (dlg.ShowDialog() != DialogResult.OK) return;

                var sb = new StringBuilder();
                sb.AppendLine("Equipment Code,Equipment Name,Scanned At");
                foreach (var scan in _bulkScanCodes)
                    sb.AppendLine($"\"{scan.Code}\",\"{scan.Name}\",\"{scan.ScannedAt:yyyy-MM-dd HH:mm:ss}\"");

                System.IO.File.WriteAllText(dlg.FileName, sb.ToString());
                ToastHelper.Show(this, $"Exported {_bulkScanCodes.Count} scans to:\n{dlg.FileName}", ToastType.Success);
            }
            catch (Exception ex)
            {
                ToastHelper.Show(this, "Export failed: " + ex.Message, ToastType.Error);
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _suggest?.Dispose();
            _currentPictureImage?.Dispose();
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

    // Mirrored from LogsForm so both screens render action types identically.
    internal static class AuditLogDisplay
    {
        public static string FormatActionLabel(string? raw) => raw switch
        {
            "equipment_created" => "Equipment Created",
            "equipment_updated" => "Equipment Updated",
            "equipment_deleted" => "Equipment Deleted",
            "equipment_restored" => "Equipment Restored",
            "location_changed" => "Location Changed",
            "employee_created" => "Employee Created",
            "employee_updated" => "Employee Updated",
            "employee_deleted" => "Employee Deleted",
            "location_created" => "Location Created",
            "location_updated" => "Location Updated",
            "location_deleted" => "Location Deleted",
            "allocation_requested" => "Allocation Requested",
            "allocation_approved" => "Allocation Approved",
            "allocation_rejected" => "Allocation Rejected",
            "allocation_returned" => "Returned to Store",
            "Allocation Request Deleted" => "Allocation Request Deleted",
            "allocation_overdue" => "Allocation Overdue",
            "decommission_requested" => "Decommission Requested",
            "decommission_approved" => "Decommission Approved",
            "decommission_undone" => "Restored from Decommission",
            "return_undone" => "Returned to Active Allocation",
            "decommission_rejected" => "Decommission Rejected",
            "transferred" => "Transferred",
            "checked_out" => "Checked Out",
            "checked_in" => "Checked In",
            "maintenance_opened" => "Maintenance Opened",
            "maintenance_resolved" => "Maintenance Resolved",
            "settings_data_delete" => "⚠ Settings Data Deleted",
            "Maintenance History Cleared" => "Maintenance History Cleared",
            "Expected Return Date Updated" => "Expected Return Date Updated",
            _ => raw ?? "Unknown"
        };

        public static (Color Back, Color Fore) GetActionColors(string? actionType) => actionType switch
        {
            "equipment_created" or "employee_created" or "location_created"
                => (Color.FromArgb(200, 230, 201), Color.FromArgb(27, 94, 32)),

            "equipment_updated" or "employee_updated" or "location_updated" or "Expected Return Date Updated"
                => (Color.FromArgb(255, 249, 196), Color.FromArgb(130, 100, 0)),

            "equipment_restored"
                => (Color.FromArgb(204, 243, 239), Color.FromArgb(0, 150, 136)),

            "decommission_undone"
                => (Color.FromArgb(224, 247, 244), Color.FromArgb(38, 166, 154)),

            "allocation_approved"
                => (Color.FromArgb(255, 204, 128), Color.FromArgb(230, 81, 0)),

            "Allocation Request Deleted"
                => (Color.FromArgb(255, 171, 145), Color.FromArgb(191, 54, 12)),

            "return_undone"
                => (Color.FromArgb(255, 224, 178), Color.FromArgb(239, 108, 0)),

            "allocation_requested"
                => (Color.FromArgb(46, 125, 50), Color.White),

            "allocation_returned"
                => (Color.FromArgb(225, 226, 252), Color.FromArgb(57, 60, 180)),

            "checked_out"
                => (Color.FromArgb(179, 229, 252), Color.FromArgb(1, 87, 155)),

            "checked_in"
                => (Color.FromArgb(209, 240, 253), Color.FromArgb(2, 119, 189)),

            "allocation_rejected" or "equipment_deleted" or "employee_deleted"
                or "location_deleted" or "settings_data_delete"
                => (Color.FromArgb(255, 205, 210), Color.FromArgb(183, 28, 28)),

            "decommission_requested" or "decommission_approved"
                => (Color.FromArgb(255, 224, 220), Color.FromArgb(211, 47, 47)),

            "allocation_overdue"
                => (Color.FromArgb(255, 224, 178), Color.FromArgb(255, 160, 0)),

            "transferred"
                => (Color.FromArgb(225, 190, 231), Color.FromArgb(74, 20, 140)),

            "location_changed"
                => (Color.FromArgb(255, 224, 178), Color.FromArgb(130, 60, 0)),

            "maintenance_opened"
                => (Color.FromArgb(187, 222, 251), Color.FromArgb(33, 150, 243)),

            "maintenance_resolved"
                => (Color.FromArgb(178, 235, 242), Color.FromArgb(0, 131, 143)),

            "Maintenance History Cleared"
                => (Color.FromArgb(252, 228, 236), Color.FromArgb(194, 24, 91)),

            _ => (Color.FromArgb(224, 224, 224), Color.FromArgb(97, 97, 97))
        };
    }
}