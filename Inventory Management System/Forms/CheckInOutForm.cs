// Forms/CheckInOutForm.cs
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
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
        private readonly EquipmentService _equipmentService = new();
        private System.Windows.Forms.Timer _scanTimer = null!;
        private System.Windows.Forms.Timer? _tabAnimTimer;
        private bool _animating = false;
        private string _lastText = "";
        private Equipment? _scannedEquipment = null;
        private Transaction? _activeTransaction = null;
        private bool _isCheckOutTab = true;
        private EquipmentSuggestHelper? _suggest;

        private const int CardMargin = 10;
        private const int CardRadius = 14;

        private static readonly Font CardLabelFont = new Font("Segoe UI", 8f, FontStyle.Bold);
        private static readonly Font CardValueFont = new Font("Segoe UI", 8f, FontStyle.Regular);

        private Button btnHistory = null!;

        public CheckInOutForm()
        {
            InitializeComponent();
            ApplyTheme();
            this.Load += (s, e) =>
            {
                txtQRCode.Focus();
                SetupScanTimer();
                SetupSuggestions();
                SyncTabPanelSizes();
            };
        }

        private void ApplyTheme()
        {
            ThemeHelper.ApplyFormBackground(this);
            ThemeHelper.StyleToolbarPanel(panelTop);
            ThemeHelper.StyleButton(btnSearch);
            ThemeHelper.StyleSecondaryButton(btnClear);

            btnHistory = new Button
            {
                Text = "🕘  History",
                Size = new Size(110, btnSearch.Height),
                Cursor = Cursors.Hand,
            };
            ThemeHelper.StyleSecondaryButton(btnHistory);
            btnHistory.Click += (s, e) => ShowHistoryView();
            panelTop.Controls.Add(btnHistory);
            btnHistory.BringToFront();

            void PositionHistoryButton()
            {
                btnHistory.Location = new Point(panelTop.ClientSize.Width - btnHistory.Width - 20, btnSearch.Top);
            }
            PositionHistoryButton();

            panelTop.Resize += (s, e) => PositionHistoryButton();
            ThemeHelper.StyleDataGridView(dgvRecentTransactions);

            // ── QR icon badge, left of the search box ──────────────────────
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

            // Left panel (details card) styling
            panelLeft.BackColor = ThemeHelper.CardBg;
            panelLeft.Paint += (s, e) =>
            {
                using var pen = new Pen(ThemeHelper.IsDarkMode ? Color.FromArgb(55, 58, 65) : Color.FromArgb(229, 231, 235), 1f);
                e.Graphics.DrawRectangle(pen, 0, 0, panelLeft.Width - 1, panelLeft.Height - 1);
            };

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

            // ── Tab header buttons — fixed position, rounded corners, color-only state ──
            btnTabCheckOut.FlatStyle = FlatStyle.Flat;
            btnTabCheckOut.FlatAppearance.BorderSize = 0;
            btnTabCheckOut.Font = ThemeHelper.FontBold;
            btnTabCheckOut.Cursor = Cursors.Hand;

            btnTabCheckIn.FlatStyle = FlatStyle.Flat;
            btnTabCheckIn.FlatAppearance.BorderSize = 0;
            btnTabCheckIn.Font = ThemeHelper.FontBold;
            btnTabCheckIn.Cursor = Cursors.Hand;

            ApplyButtonRounding(btnTabCheckOut, 10);
            ApplyButtonRounding(btnTabCheckIn, 10);

            // ── Tab content becomes a floating rounded "card" that slides ──
            panelTabContent.BackColor = Color.FromArgb(241, 243, 241);
            panelCheckOut.BackColor = ThemeHelper.CardBg;
            panelCheckIn.BackColor = ThemeHelper.CardBg;
            panelCheckOut.Paint += CardBorderPaint;
            panelCheckIn.Paint += CardBorderPaint;
            panelTabContent.Resize += (s, e) => SyncTabPanelSizes();

            // Form action labels
            foreach (var lbl in new[] { lblIssuedTo, lblDepartment, lblOperatorOut, lblExpectedReturn, lblConditionBefore, lblRemarksOut })
                ThemeHelper.StyleLabel(lbl, bold: true);
            foreach (var lbl in new[] { lblCurrentlyIssuedTo, lblOperatorIn, lblConditionAfter, lblRemarksIn })
                ThemeHelper.StyleLabel(lbl, bold: true);

            ThemeHelper.StyleButton(btnConfirmCheckOut);
            ThemeHelper.StyleButton(btnConfirmCheckIn);

            // Apply rounded corners to confirm buttons — re-apply on resize so the
            // region always matches the actual button size after layout.
            void RoundConfirmBtn(Button btn)
            {
                ApplyButtonRounding(btn, 10);
                btn.SizeChanged += (s, e) => ApplyButtonRounding(btn, 10);
            }
            RoundConfirmBtn(btnConfirmCheckOut);
            RoundConfirmBtn(btnConfirmCheckIn);

            ThemeHelper.StyleLabel(lblGridTitle, bold: true);
            lblNoResult.ForeColor = ThemeHelper.TextSecondary;
            lblNoResult.Font = ThemeHelper.FontDefault;

            lblEmptyIcon.ForeColor = Color.FromArgb(180, 190, 180);
            lblEmptyTitle.ForeColor = ThemeHelper.TextPrimary;
            lblEmptySubtitle.ForeColor = ThemeHelper.TextSecondary;

            // Hide the labels — text is drawn manually in PnlEmptyState_Paint instead,
            // so it's always centered against the panel's real, current size.
            lblEmptyIcon.Visible = false;
            lblEmptyTitle.Visible = false;
            lblEmptySubtitle.Visible = false;
            pnlEmptyState.Paint += PnlEmptyState_Paint;

            SwitchTab(true);
        }

        private CheckInOutHistoryPanel? _historyPanel;

        private void ShowHistoryView()
        {
            if (_historyPanel == null)
            {
                _historyPanel = new CheckInOutHistoryPanel { Dock = DockStyle.Fill };
                _historyPanel.BackRequested += (s, e) => ShowScanView();
                Controls.Add(_historyPanel);
            }
            _historyPanel.Visible = true;
            _historyPanel.BringToFront();
            _historyPanel.RefreshData();

            panelTop.Visible = false;       // scan bar goes away completely
            panelMainContent.Visible = false;
            lblNoResult.Visible = false;
        }

        private void ShowScanView()
        {
            if (_historyPanel != null) _historyPanel.Visible = false;
            panelTop.Visible = true;
            panelMainContent.Visible = _scannedEquipment != null;
            lblNoResult.Visible = _scannedEquipment == null;
        }

        private static void CardBorderPaint(object? sender, PaintEventArgs e)
        {
            if (sender is not Panel p || p.Width <= 1 || p.Height <= 1) return;
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using var path = RoundedRect(new Rectangle(0, 0, p.Width - 1, p.Height - 1), CardRadius);
            using var pen = new Pen(Color.FromArgb(228, 231, 228), 1.2f);
            g.DrawPath(pen, path);
        }

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

                lblCodeValue.Text = equipment.EquipmentCode;
                lblQrCodeValue.Text = equipment.QrCode ?? "—";
                lblNameValue.Text = equipment.Name;
                lblCategoryValue.Text = equipment.Category ?? "—";
                lblMakeValue.Text = equipment.Make ?? "—";
                lblModelValue.Text = equipment.Model ?? "—";
                lblSerialValue.Text = equipment.SerialNumber ?? "—";
                lblProcurementTypeValue.Text = equipment.ProcurementType?.TypeName ?? "—";
                lblConditionValue.Text = equipment.ConditionGrade ?? "—";
                lblVendorValue.Text = equipment.VendorName ?? "—";
                lblPurchaseDateValue.Text = equipment.PurchaseDate?.ToString("yyyy-MM-dd") ?? "—";
                lblWarrantyValue.Text = equipment.WarrantyExpiry?.ToString("yyyy-MM-dd") ?? "—";
                lblDescriptionValue.Text = string.IsNullOrWhiteSpace(equipment.Description) ? "—" : equipment.Description;
                lblNotesValue.Text = string.IsNullOrWhiteSpace(equipment.Notes) ? "—" : equipment.Notes;

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

                var activeAlloc = equipment.Allocations
                    .Where(a => a.Status == "approved")
                    .OrderByDescending(a => a.EmployeeId.HasValue)
                    .ThenByDescending(a => a.Id)
                    .FirstOrDefault();
                lblLocationValue.Text = activeAlloc?.Location?.LocationName ?? "—";

                _activeTransaction = db.Transactions
                    .Where(t => t.EquipmentId == equipment.Id && (t.Status == "Pending" || t.Status == "Overdue"))
                    .OrderByDescending(t => t.TransactionDate)
                    .FirstOrDefault();

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
                    cmbConditionBefore.SelectedIndex = 1;
                    cmbConditionAfter.SelectedIndex = 1;
                }

                txtCurrentlyIssuedTo.Text = _activeTransaction != null ? (_activeTransaction.IssuedTo ?? "—") : "—";

                if (equipment.CurrentStatus == "allocated")
                    SwitchTab(false);
                else
                    SwitchTab(true);

                RefreshHistoryGrid(db, equipment.Id);
            }
            catch (Exception ex)
            {
                ToastHelper.Show(this, "Search failed: " + ex.Message, ToastType.Error);
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


            pnlEmptyState.Visible = txs.Count == 0;
            if (pnlEmptyState.Visible)
            {
                pnlEmptyState.BringToFront();
                pnlEmptyState.Invalidate();
            }
        }

        // ════════════════════════════════════════════════════════════════
        //  EMPTY-STATE CENTERING
        // ════════════════════════════════════════════════════════════════

        private void PnlEmptyState_Paint(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            var rect = pnlEmptyState.ClientRectangle;
            if (rect.Width <= 0 || rect.Height <= 0) return;

            // Center under the "Issued To" column (index 2)
            int centerX = rect.Width / 2;
            try
            {
                if (dgvRecentTransactions.Columns.Count > 2)
                {
                    var colRect = dgvRecentTransactions.GetColumnDisplayRectangle(2, true);
                    if (colRect.Width > 0)
                    {
                        centerX = colRect.X + (colRect.Width / 2);
                    }
                }
            }
            catch { /* fallback to center if grid isn't fully laid out */ }

            Size iconSize = TextRenderer.MeasureText(g, lblEmptyIcon.Text, lblEmptyIcon.Font);
            Size titleSize = TextRenderer.MeasureText(g, lblEmptyTitle.Text, lblEmptyTitle.Font);
            Size subtitleSize = TextRenderer.MeasureText(g, lblEmptySubtitle.Text, lblEmptySubtitle.Font);

            const int gap1 = 8, gap2 = 4;
            int totalH = iconSize.Height + gap1 + titleSize.Height + gap2 + subtitleSize.Height;
            int startY = rect.Top + Math.Max(0, (rect.Height - totalH) / 2);

            TextRenderer.DrawText(g, lblEmptyIcon.Text, lblEmptyIcon.Font,
                new Point(centerX - iconSize.Width / 2, startY),
                lblEmptyIcon.ForeColor, TextFormatFlags.NoPadding);

            int titleY = startY + iconSize.Height + gap1;
            TextRenderer.DrawText(g, lblEmptyTitle.Text, lblEmptyTitle.Font,
                new Point(centerX - titleSize.Width / 2, titleY),
                lblEmptyTitle.ForeColor, TextFormatFlags.NoPadding);

            int subtitleY = titleY + titleSize.Height + gap2;
            TextRenderer.DrawText(g, lblEmptySubtitle.Text, lblEmptySubtitle.Font,
                new Point(centerX - subtitleSize.Width / 2, subtitleY),
                lblEmptySubtitle.ForeColor, TextFormatFlags.NoPadding);
        }

        // ════════════════════════════════════════════════════════════════
        //  TAB SWITCHING — buttons stay fixed, the rounded card slides
        // ════════════════════════════════════════════════════════════════

        private void SyncTabPanelSizes()
        {
            var avail = panelTabContent.ClientSize;
            if (avail.Width <= 0 || avail.Height <= 0) return;

            int w = Math.Max(10, avail.Width - CardMargin * 2);
            int h = Math.Max(10, avail.Height - CardMargin * 2);
            var size = new Size(w, h);

            if (panelCheckOut.Size != size)
            {
                panelCheckOut.Size = size;
                RoundPanelCorners(panelCheckOut, CardRadius);
            }
            if (panelCheckIn.Size != size)
            {
                panelCheckIn.Size = size;
                RoundPanelCorners(panelCheckIn, CardRadius);
            }
        }

        private static void RoundPanelCorners(Panel p, int radius)
        {
            if (p.Width <= 0 || p.Height <= 0) return;
            using var path = RoundedRect(new Rectangle(0, 0, p.Width, p.Height), radius);
            p.Region = new Region(path);
        }

        private static void ApplyButtonRounding(Button btn, int radius)
        {
            if (btn.Width <= 0 || btn.Height <= 0) return;
            using var path = RoundedRect(new Rectangle(0, 0, btn.Width, btn.Height), radius);
            btn.Region = new Region(path);
        }

        private void SwitchTab(bool toCheckOut)
        {
            btnTabCheckOut.BackColor = toCheckOut ? ThemeHelper.Accent : Color.FromArgb(243, 244, 246);
            btnTabCheckOut.ForeColor = toCheckOut ? Color.White : ThemeHelper.TextSecondary;
            btnTabCheckIn.BackColor = !toCheckOut ? ThemeHelper.Accent : Color.FromArgb(243, 244, 246);
            btnTabCheckIn.ForeColor = !toCheckOut ? Color.White : ThemeHelper.TextSecondary;

            bool firstRun = !panelCheckOut.Visible && !panelCheckIn.Visible;

            if (_isCheckOutTab == toCheckOut && !firstRun && !_animating)
                return;

            _isCheckOutTab = toCheckOut;
            SyncTabPanelSizes();

            Panel incoming = toCheckOut ? panelCheckOut : panelCheckIn;
            Panel outgoing = toCheckOut ? panelCheckIn : panelCheckOut;

            int width = panelTabContent.ClientSize.Width;
            int restLeft = CardMargin;

            if (width <= 1 || firstRun)
            {
                incoming.Left = restLeft;
                incoming.Top = CardMargin;
                incoming.Visible = true;
                outgoing.Visible = false;
                outgoing.Left = restLeft;
                return;
            }

            _tabAnimTimer?.Stop();
            _tabAnimTimer?.Dispose();

            int direction = toCheckOut ? -1 : 1;
            outgoing.Top = CardMargin;
            incoming.Top = CardMargin;
            incoming.Left = restLeft - direction * width;
            incoming.Visible = true;
            incoming.BringToFront();

            int startOutLeft = outgoing.Left;
            int endOutLeft = restLeft + direction * width;
            int startInLeft = incoming.Left;
            int endInLeft = restLeft;

            _animating = true;
            int step = 0;
            const int totalSteps = 12;

            _tabAnimTimer = new System.Windows.Forms.Timer { Interval = 12 };
            _tabAnimTimer.Tick += (s, e) =>
            {
                step++;
                double t = Math.Min(1.0, (double)step / totalSteps);
                double eased = 1 - Math.Pow(1 - t, 3);

                outgoing.Left = startOutLeft + (int)((endOutLeft - startOutLeft) * eased);
                incoming.Left = startInLeft + (int)((endInLeft - startInLeft) * eased);

                if (step >= totalSteps)
                {
                    _tabAnimTimer?.Stop();
                    _tabAnimTimer?.Dispose();
                    _tabAnimTimer = null;
                    outgoing.Visible = false;
                    outgoing.Left = restLeft;
                    incoming.Left = restLeft;
                    _animating = false;
                }
            };
            _tabAnimTimer.Start();
        }

        private void btnTabCheckOut_Click(object sender, EventArgs e) => SwitchTab(true);
        private void btnTabCheckIn_Click(object sender, EventArgs e) => SwitchTab(false);

        private void btnConfirmCheckOut_Click(object sender, EventArgs e)
        {
            if (_scannedEquipment == null) return;

            if (_scannedEquipment.CurrentStatus != "in_store")
            {
                ToastHelper.Show(this, $"This equipment is currently not in store (status: {_scannedEquipment.CurrentStatus}) and cannot be checked out.", ToastType.Warning);
                return;
            }

            using (var checkDb = new InventoryDbContext())
            {
                bool hasPendingAllocation = checkDb.Allocations.Any(a =>
                    a.EquipmentId == _scannedEquipment.Id &&
                    (a.Status == "pending" || a.Status == "approved"));

                if (hasPendingAllocation)
                {
                    ToastHelper.Show(this, "This equipment has a pending or approved formal Allocation and cannot be checked out here until that is resolved.", ToastType.Warning);
                    return;
                }
            }

            if (string.IsNullOrWhiteSpace(txtIssuedTo.Text))
            {
                ToastHelper.Show(this, "Please enter the name of the person this item is being issued to.", ToastType.Warning);
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
                ToastHelper.Show(this, "Check-Out transaction completed successfully!", ToastType.Success);
            else
                ToastHelper.Show(this, res.ErrorMessage, ToastType.Error);
        }

        private void btnConfirmCheckIn_Click(object sender, EventArgs e)
        {
            if (_scannedEquipment == null) return;

            if (_activeTransaction == null)
            {
                ToastHelper.Show(this, "No active pending check-out transaction was found for this equipment.", ToastType.Warning);
                return;
            }

            string condAfter = cmbConditionAfter.SelectedItem?.ToString() ?? "Good";
            string remarks = txtRemarksIn.Text.Trim();

            var res = _transactionService.CheckIn(_activeTransaction.TransactionId, condAfter, remarks);
            if (res.Success)
            {
                ToastHelper.Show(this, "Check-In transaction completed successfully!", ToastType.Success);
                Search();
            }
            else
            {
                ToastHelper.Show(this, res.ErrorMessage, ToastType.Error);
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
            _tabAnimTimer?.Stop();
            _tabAnimTimer?.Dispose();
            _suggest?.Dispose();
            base.OnFormClosed(e);
        }

        // ════════════════════════════════════════════════════════════════
        //  ICON HELPERS
        // ════════════════════════════════════════════════════════════════

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

        private static void DrawPersonIcon(Graphics g, Rectangle bounds, Color color)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using var brush = new SolidBrush(color);

            int headD = (int)(bounds.Height * 0.5f);
            var headRect = new Rectangle(bounds.X + (bounds.Width - headD) / 2, bounds.Y, headD, headD);
            g.FillEllipse(brush, headRect);

            int bodyW = (int)(bounds.Width * 0.85f);
            int bodyH = (int)(bounds.Height * 0.5f);
            var bodyRect = new Rectangle(bounds.X + (bounds.Width - bodyW) / 2, headRect.Bottom - 1, bodyW, bodyH);
            using var path = RoundedRect(bodyRect, bodyH / 2);
            g.FillPath(brush, path);
        }

        /// Builds a rounded background "chip" panel that hosts an existing
        /// input control. The control itself keeps a square, borderless,
        /// matching-color background and sits with a small margin inside
        /// the host — so the host's rounded corners are never fighting the
        /// control's own rendering (no seams, no clipping artifacts). Used
        /// only from the Designer at construction time — never re-parents
        /// a control at runtime, so nothing can go missing.
        private static Panel CreateInputHost(Control inner, Point location, Size size, bool personIcon = false)
        {
            var host = new Panel
            {
                Location = location,
                Size = size,
                BackColor = Color.Transparent,
                Name = (string.IsNullOrEmpty(inner.Name) ? "field" : inner.Name) + "Host"
            };

            Color fieldBg = Color.FromArgb(249, 250, 249);

            if (inner is TextBox tb) tb.BorderStyle = BorderStyle.None;
            if (inner is ComboBox cb) cb.FlatStyle = FlatStyle.Flat;

            inner.BackColor = fieldBg;

            int rightPad = personIcon ? 30 : 10;
            int innerHeight = Math.Min(inner.Height, Math.Max(10, size.Height - 4));
            inner.Height = innerHeight;
            inner.Location = new Point(10, Math.Max(2, (size.Height - innerHeight) / 2));
            inner.Width = Math.Max(20, size.Width - 10 - rightPad);
            inner.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            host.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using var path = RoundedRect(new Rectangle(0, 0, host.Width - 1, host.Height - 1), 8);
                using var bg = new SolidBrush(fieldBg);
                using var border = new Pen(Color.FromArgb(214, 219, 220), 1.2f);
                g.FillPath(bg, path);
                g.DrawPath(border, path);

                if (personIcon)
                {
                    var iconBounds = new Rectangle(host.Width - 27, (host.Height - 16) / 2, 16, 16);
                    DrawPersonIcon(g, iconBounds, Color.FromArgb(150, 165, 150));
                }
            };
            host.Resize += (s, e) => host.Invalidate();

            host.Controls.Add(inner);
            return host;
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