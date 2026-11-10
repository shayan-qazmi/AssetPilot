// Forms/ReportsForm.cs
using ClosedXML.Excel;
using Inventory_Management_System.Helpers;
using Inventory_Management_System.Models;
using Inventory_Management_System.Services;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Color = System.Drawing.Color;
using Size = System.Drawing.Size;

namespace Inventory_Management_System.Forms
{
    public partial class ReportsForm : Form, IThemableForm
    {
        private readonly EquipmentService _equipmentService = new();
        private readonly EmployeeService _employeeService = new();
        private readonly AllocationService _allocationService = new();

        private string _currentReport = "Equipment";
        private string _companyName = "AssetPilot";
        private Color _activeHeaderColor = Color.FromArgb(94, 53, 177); // Deep Purple default

        // ── Data Cache & Filtering ──────────────────────────────────────────
        private class ReportRowData
        {
            public Dictionary<string, string> Values { get; } = new(StringComparer.OrdinalIgnoreCase);
            public DateTime? Date { get; set; }
            public string Status { get; set; } = "";
            public string Location { get; set; } = "";
            public string Category { get; set; } = "";

            public string GetValue(string key) => Values.TryGetValue(key, out var v) ? v : "—";
            public void SetValue(string key, string val) => Values[key] = val;
        }

        private List<ReportRowData> _allMasterRows = new();
        private List<ReportRowData> _filteredRows = new();

        // ── Filter State ───────────────────────────────────────────────────
        private string _activeDateMode = "thisyear"; // "today", "7days", "30days", "thisyear", "custom"
        private DateTime? _customStartDate = null;
        private DateTime? _customEndDate = null;
        private string _selectedLocation = "All locations";
        private string _selectedCategory = "All categories";
        private string _selectedStatus = "Any status";
        private string _searchFilterText = "";

        // ── Pagination State ────────────────────────────────────────────────
        private int _currentPage = 1;
        private int _pageSize = 25;
        private int _totalPages = 1;

        // ── Sort State ──────────────────────────────────────────────────────
        private string? _currentSortColumn = null;
        private bool _currentSortAscending = true;

        // ── Card Accent Colors ─────────────────────────────────────────────
        private static readonly Color Card1Color = Color.FromArgb(94, 53, 177);  // Purple
        private static readonly Color Card2Color = Color.FromArgb(245, 124, 0);  // Orange
        private static readonly Color Card3Color = Color.FromArgb(16, 185, 129); // Green
        private static readonly Color Card4Color = Color.FromArgb(239, 68, 68);  // Red

        public ReportsForm()
        {
            InitializeComponent();
            LoadCompanyName();
            SetupStyling();
            SetupFilterControls();
            SetupSummaryCards();
            SetupDataGrid();
            SetupPagination();
            SetupWatermark();

            this.Load += (s, e) =>
            {
                ShowDefaultEmptyState();
            };
        }

        private Label _lblWatermark = null!;

        private void SetupWatermark()
        {
            _lblWatermark = new Label
            {
                Text = "Designed and Developed by\n🐙@shayan-qazmi/📷@shayanqazmi",
                Font = new Font("Segoe UI", 8.25f, FontStyle.Italic),
                ForeColor = Color.FromArgb(160, 160, 160),
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleRight,
                AutoSize = true,
                Visible = true
            };
            Controls.Add(_lblWatermark);
            _lblWatermark.BringToFront();
            PositionWatermark();

            this.Resize += (s, e) => PositionWatermark();
            _lblWatermark.SizeChanged += (s, e) => PositionWatermark();
        }

        private void PositionWatermark()
        {
            if (_lblWatermark == null || !_lblWatermark.Visible) return;
            const int marginX = 24;
            const int marginY = 12;
            _lblWatermark.Location = new Point(
                ClientSize.Width - _lblWatermark.Width - marginX,
                ClientSize.Height - _lblWatermark.Height - marginY);
        }

        private void LoadCompanyName()
        {
            try
            {
                using var db = new InventoryDbContext();
                _companyName = db.AppSettings.FirstOrDefault()?.CompanyName ?? "AssetPilot";
            }
            catch { /* fallback to default */ }
        }

        private void ShowDefaultEmptyState()
        {
            _currentReport = "";
            _allMasterRows.Clear();
            _filteredRows.Clear();

            dgvReport.Columns.Clear();
            dgvReport.Rows.Clear();

            lblGridReportTitle.Text = "Reports";
            lblGridReportSubtitle.Text = "Select a report above to view details";

            UpdateSummaryCards("Total rows", "—", "Allocated", "—", "In store", "—", "Needs attention", "—");
            
            pnlPagination.Visible = false;
            if (_lblWatermark != null)
            {
                _lblWatermark.Visible = true;
                _lblWatermark.BringToFront();
                PositionWatermark();
            }

            UpdateTabButtonStyles();
        }

        // ════════════════════════════════════════════════════════════════════
        //  STYLING & THEME
        // ════════════════════════════════════════════════════════════════════

        public void ApplyTheme()
        {
            ThemeHelper.ApplyFormBackground(this);
            panelTop.BackColor = ThemeHelper.IsDarkMode ? Color.FromArgb(18, 20, 24) : Color.FromArgb(250, 250, 250);
            pnlPagination.BackColor = ThemeHelper.IsDarkMode ? Color.FromArgb(18, 20, 24) : Color.FromArgb(250, 250, 250);

            ThemeHelper.StyleDataGridView(dgvReport, _activeHeaderColor);

            UpdateTabButtonStyles();
            UpdateDatePillStyles();
            UpdateCardTheme();

            ThemeHelper.StyleComboBox(cmbFilterLocation);
            ThemeHelper.StyleComboBox(cmbFilterStatus);
            ThemeHelper.StyleComboBox(cmbPageSize);

            dgvReport.Invalidate();
        }

        private void SetupStyling()
        {
            ApplyTheme();

            // Style Export buttons
            StyleOutlineButton(btnPrint);
            StyleOutlineButton(btnExportPDF);
            StyleOutlineButton(btnExportExcel);
            StyleOutlineButton(btnExportCSV);

            // Style Header action buttons
            StyleOutlineButton(btnColumns);
            StyleOutlineButton(btnSort);
            StyleOutlineButton(btnResetFilters);

            // Style Apply button (Green)
            btnApplyFilters.BackColor = Color.FromArgb(46, 125, 50);
            btnApplyFilters.ForeColor = Color.White;
            btnApplyFilters.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            btnApplyFilters.FlatStyle = FlatStyle.Flat;
            btnApplyFilters.FlatAppearance.BorderSize = 0;
            ApplyButtonRounding(btnApplyFilters, 6);

            // Style search textbox
            txtFilterSearch.BorderStyle = BorderStyle.FixedSingle;
            txtFilterSearch.Font = new Font("Segoe UI", 9f);
            txtFilterSearch.PlaceholderText = "Filter rows...";
            txtFilterSearch.TextChanged += (s, e) =>
            {
                _searchFilterText = txtFilterSearch.Text.Trim();
                ApplyCurrentFilters();
            };
            txtFilterSearch.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    ApplyCurrentFilters();
                }
            };

            btnApplyFilters.Click += (s, e) => ApplyCurrentFilters();
            btnResetFilters.Click += (s, e) => ResetFilters();
        }

        private void StyleOutlineButton(Button b)
        {
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderSize = 1;
            b.FlatAppearance.BorderColor = Color.FromArgb(209, 213, 219);
            b.BackColor = ThemeHelper.IsDarkMode ? Color.FromArgb(30, 32, 38) : Color.White;
            b.ForeColor = ThemeHelper.IsDarkMode ? Color.FromArgb(220, 225, 230) : Color.FromArgb(55, 65, 81);
            b.Font = new Font("Segoe UI", 8.75f, FontStyle.Regular);
            b.Cursor = Cursors.Hand;
            ApplyButtonRounding(b, 6);

            b.MouseEnter += (s, e) => b.BackColor = ThemeHelper.IsDarkMode ? Color.FromArgb(45, 48, 56) : Color.FromArgb(243, 244, 246);
            b.MouseLeave += (s, e) => b.BackColor = ThemeHelper.IsDarkMode ? Color.FromArgb(30, 32, 38) : Color.White;
        }

        private static void ApplyButtonRounding(Button btn, int radius)
        {
            if (btn.Width <= 0 || btn.Height <= 0) return;
            using var path = RoundedRect(new Rectangle(0, 0, btn.Width, btn.Height), radius);
            btn.Region = new Region(path);
            btn.SizeChanged += (s, e) =>
            {
                if (btn.Width <= 0 || btn.Height <= 0) return;
                using var p = RoundedRect(new Rectangle(0, 0, btn.Width, btn.Height), radius);
                btn.Region = new Region(p);
            };
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

        // ════════════════════════════════════════════════════════════════════
        //  TAB BUTTONS & STYLING
        // ════════════════════════════════════════════════════════════════════

        private void UpdateTabButtonStyles()
        {
            // Reset all tab buttons
            SetTabButtonStyle(btnEquipmentReport, Color.FromArgb(94, 53, 177), _currentReport == "Equipment");
            SetTabButtonStyle(btnEmployeeReport, Color.FromArgb(123, 31, 162), _currentReport == "Employee");
            SetTabButtonStyle(btnAllocationReport, Color.FromArgb(245, 124, 0), _currentReport == "Allocation");
            SetTabButtonStyle(btnReturnReport, Color.FromArgb(92, 107, 192), _currentReport == "Return");
            SetTabButtonStyle(btnCheckInOutReport, Color.FromArgb(3, 169, 244), _currentReport == "CheckInOut");
        }

        private void SetTabButtonStyle(Button btn, Color themeColor, bool isSelected)
        {
            btn.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            btn.BackColor = themeColor;
            btn.ForeColor = Color.White;
            ApplyButtonRounding(btn, 6);

            if (isSelected)
            {
                btn.FlatAppearance.BorderSize = 2;
                btn.FlatAppearance.BorderColor = Color.White;
            }
            else
            {
                btn.FlatAppearance.BorderSize = 0;
            }
        }

        // ════════════════════════════════════════════════════════════════════
        //  DATE PILLS & FILTER CONTROLS
        // ════════════════════════════════════════════════════════════════════

        private void SetupFilterControls()
        {
            btnDateToday.Click += (s, e) => SetDateMode("today");
            btnDate7Days.Click += (s, e) => SetDateMode("7days");
            btnDate30Days.Click += (s, e) => SetDateMode("30days");
            btnDateThisYear.Click += (s, e) => SetDateMode("thisyear");
            btnDateCustom.Click += (s, e) => OpenCustomDateDialog();

            UpdateDatePillStyles();

            cmbFilterLocation.SelectedIndexChanged += (s, e) =>
            {
                _selectedLocation = cmbFilterLocation.SelectedItem?.ToString() ?? "All locations";
                ApplyCurrentFilters();
            };

            cmbFilterStatus.SelectedIndexChanged += (s, e) =>
            {
                _selectedStatus = cmbFilterStatus.SelectedItem?.ToString() ?? "Any status";
                ApplyCurrentFilters();
            };
        }

        private void SetDateMode(string mode)
        {
            _activeDateMode = mode;
            btnDateCustom.Text = "Custom...";
            UpdateDatePillStyles();
            ApplyCurrentFilters();
        }

        private void UpdateDatePillStyles()
        {
            StylePill(btnDateToday, _activeDateMode == "today");
            StylePill(btnDate7Days, _activeDateMode == "7days");
            StylePill(btnDate30Days, _activeDateMode == "30days");
            StylePill(btnDateThisYear, _activeDateMode == "thisyear");
            StylePill(btnDateCustom, _activeDateMode == "custom");
        }

        private void StylePill(Button b, bool isActive)
        {
            b.FlatStyle = FlatStyle.Flat;
            b.Font = new Font("Segoe UI", 8.5f, isActive ? FontStyle.Bold : FontStyle.Regular);

            if (isActive)
            {
                b.BackColor = Color.FromArgb(31, 41, 55); // Dark charcoal pill
                b.ForeColor = Color.White;
                b.FlatAppearance.BorderSize = 0;
            }
            else
            {
                b.BackColor = ThemeHelper.IsDarkMode ? Color.FromArgb(30, 32, 38) : Color.White;
                b.ForeColor = ThemeHelper.IsDarkMode ? Color.FromArgb(200, 205, 215) : Color.FromArgb(75, 85, 99);
                b.FlatAppearance.BorderSize = 1;
                b.FlatAppearance.BorderColor = Color.FromArgb(229, 231, 235);
            }
            ApplyButtonRounding(b, 14);
        }

        private void OpenCustomDateDialog()
        {
            using var dlg = new Form
            {
                Text = "Custom Date Range",
                ClientSize = new Size(340, 230),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = ThemeHelper.CardBg,
                ForeColor = ThemeHelper.TextPrimary
            };

            var lblFrom = new Label { Text = "From Date:", Location = new Point(24, 18), AutoSize = true, Font = ThemeHelper.FontBold };
            var dtpFrom = new DateTimePicker { Location = new Point(24, 42), Width = 290, Height = 28, Value = _customStartDate ?? DateTime.Today.AddDays(-30) };

            var lblTo = new Label { Text = "To Date:", Location = new Point(24, 82), AutoSize = true, Font = ThemeHelper.FontBold };
            var dtpTo = new DateTimePicker { Location = new Point(24, 106), Width = 290, Height = 28, Value = _customEndDate ?? DateTime.Today };

            var btnOk = new Button { Text = "Apply Range", Location = new Point(95, 160), Size = new Size(150, 36), DialogResult = DialogResult.OK };
            ThemeHelper.StyleButton(btnOk, Color.FromArgb(46, 125, 50));
            btnOk.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);

            dlg.Controls.AddRange(new Control[] { lblFrom, dtpFrom, lblTo, dtpTo, btnOk });

            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                _customStartDate = dtpFrom.Value.Date;
                _customEndDate = dtpTo.Value.Date.AddDays(1).AddTicks(-1);
                _activeDateMode = "custom";
                btnDateCustom.Text = $"{dtpFrom.Value:MMM dd} - {dtpTo.Value:MMM dd}";
                UpdateDatePillStyles();
                ApplyCurrentFilters();
            }
        }

        private void ResetFilters()
        {
            _activeDateMode = "thisyear";
            _customStartDate = null;
            _customEndDate = null;
            btnDateCustom.Text = "Custom...";

            txtFilterSearch.Text = "";
            _searchFilterText = "";

            if (cmbFilterLocation.Items.Count > 0) cmbFilterLocation.SelectedIndex = 0;
            if (cmbFilterStatus.Items.Count > 0) cmbFilterStatus.SelectedIndex = 0;

            _selectedLocation = "All locations";
            _selectedCategory = "All categories";
            _selectedStatus = "Any status";

            // Reset sort
            _currentSortColumn = null;
            _currentSortAscending = true;

            // Reset columns: make all columns visible
            foreach (DataGridViewColumn col in dgvReport.Columns)
            {
                col.Visible = true;
            }

            UpdateDatePillStyles();

            if (!string.IsNullOrEmpty(_currentReport))
            {
                ApplyCurrentFilters();
            }
        }

        // ════════════════════════════════════════════════════════════════════
        //  SUMMARY KPI CARDS
        // ════════════════════════════════════════════════════════════════════

        private void SetupSummaryCards()
        {
            AttachCardPaint(pnlCard1, Card1Color);
            AttachCardPaint(pnlCard2, Card2Color);
            AttachCardPaint(pnlCard3, Card3Color);
            AttachCardPaint(pnlCard4, Card4Color);

            UpdateCardTheme();
        }

        private void AttachCardPaint(Panel card, Color accentColor)
        {
            card.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;

                var rect = new Rectangle(0, 0, card.Width - 1, card.Height - 1);
                using var path = RoundedRect(rect, 8);

                Color bg = ThemeHelper.IsDarkMode ? Color.FromArgb(28, 30, 36) : Color.White;
                Color border = ThemeHelper.IsDarkMode ? Color.FromArgb(50, 54, 64) : Color.FromArgb(229, 231, 235);

                using var bgBrush = new SolidBrush(bg);
                using var borderPen = new Pen(border, 1.2f);
                g.FillPath(bgBrush, path);
                g.DrawPath(borderPen, path);

                // Left 4px accent bar
                using var accentBrush = new SolidBrush(accentColor);
                var accentRect = new Rectangle(0, 4, 4, card.Height - 8);
                using var barPath = RoundedRect(accentRect, 2);
                g.FillPath(accentBrush, barPath);
            };
        }

        private void UpdateCardTheme()
        {
            Color labelColor = Color.FromArgb(107, 114, 128);
            Color valColor = ThemeHelper.IsDarkMode ? Color.FromArgb(240, 245, 250) : Color.FromArgb(17, 24, 39);

            lblCard1Title.ForeColor = labelColor;
            lblCard2Title.ForeColor = labelColor;
            lblCard3Title.ForeColor = labelColor;
            lblCard4Title.ForeColor = labelColor;

            lblCard1Value.ForeColor = valColor;
            lblCard2Value.ForeColor = valColor;
            lblCard3Value.ForeColor = valColor;
            lblCard4Value.ForeColor = valColor;

            pnlCard1.Invalidate();
            pnlCard2.Invalidate();
            pnlCard3.Invalidate();
            pnlCard4.Invalidate();
        }

        private void UpdateSummaryCards(string title1, string val1, string title2, string val2, string title3, string val3, string title4, string val4)
        {
            lblCard1Title.Text = title1;
            lblCard1Value.Text = val1;

            lblCard2Title.Text = title2;
            lblCard2Value.Text = val2;

            lblCard3Title.Text = title3;
            lblCard3Value.Text = val3;

            lblCard4Title.Text = title4;
            lblCard4Value.Text = val4;
        }

        // ════════════════════════════════════════════════════════════════════
        //  DATA GRID & PILL BADGES
        // ════════════════════════════════════════════════════════════════════

        private void SetupDataGrid()
        {
            dgvReport.ColumnHeadersHeight = 38;
            dgvReport.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            dgvReport.RowTemplate.Height = 36;
            dgvReport.CellPainting += DgvReport_CellPainting;
        }

        private void DgvReport_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;

            string colName = dgvReport.Columns[e.ColumnIndex].Name;
            if ((colName == "Status" || colName == "Overdue") && e.Value != null)
            {
                e.PaintBackground(e.ClipBounds, true);

                string raw = (e.Value.ToString() ?? "").Trim().Replace("_", " ");
                string status = raw.ToLowerInvariant();

                Color pillBg;
                Color pillText;

                if (colName == "Overdue")
                {
                    bool isOverdue = string.Equals(raw, "Yes", StringComparison.OrdinalIgnoreCase);
                    pillBg = isOverdue
                        ? (ThemeHelper.IsDarkMode ? Color.FromArgb(65, 20, 20) : Color.FromArgb(254, 226, 226))
                        : (ThemeHelper.IsDarkMode ? Color.FromArgb(20, 45, 25) : Color.FromArgb(209, 250, 229));
                    pillText = isOverdue
                        ? (ThemeHelper.IsDarkMode ? Color.FromArgb(248, 113, 113) : Color.FromArgb(220, 38, 38))
                        : (ThemeHelper.IsDarkMode ? Color.FromArgb(52, 211, 153) : Color.FromArgb(5, 150, 105));
                }
                else
                {
                    if (status.Contains("repair"))
                    {
                        raw = "Under Repair";
                        // Light blue pill badge as requested!
                        pillBg = ThemeHelper.IsDarkMode ? Color.FromArgb(12, 45, 72) : Color.FromArgb(224, 242, 254);
                        pillText = ThemeHelper.IsDarkMode ? Color.FromArgb(56, 189, 248) : Color.FromArgb(2, 132, 199);
                    }
                    else if (status.Contains("in store") || status.Contains("active") || status.Contains("completed"))
                    {
                        if (status.Contains("in store")) raw = "In Store";
                        pillBg = ThemeHelper.IsDarkMode ? Color.FromArgb(18, 55, 32) : Color.FromArgb(209, 250, 229);
                        pillText = ThemeHelper.IsDarkMode ? Color.FromArgb(52, 211, 153) : Color.FromArgb(5, 150, 105);
                    }
                    else if (status.Contains("allocated") || status.Contains("pending"))
                    {
                        if (status.Contains("allocated")) raw = "Allocated";
                        pillBg = ThemeHelper.IsDarkMode ? Color.FromArgb(60, 45, 15) : Color.FromArgb(254, 243, 199);
                        pillText = ThemeHelper.IsDarkMode ? Color.FromArgb(251, 191, 36) : Color.FromArgb(217, 119, 6);
                    }
                    else if (status.Contains("decommission") || status.Contains("overdue") || status.Contains("inactive"))
                    {
                        if (status.Contains("decommission")) raw = "Decommissioned";
                        pillBg = ThemeHelper.IsDarkMode ? Color.FromArgb(65, 20, 20) : Color.FromArgb(254, 226, 226);
                        pillText = ThemeHelper.IsDarkMode ? Color.FromArgb(248, 113, 113) : Color.FromArgb(220, 38, 38);
                    }
                    else
                    {
                        pillBg = ThemeHelper.IsDarkMode ? Color.FromArgb(40, 45, 55) : Color.FromArgb(243, 244, 246);
                        pillText = ThemeHelper.IsDarkMode ? Color.FromArgb(180, 185, 195) : Color.FromArgb(107, 114, 128);
                    }
                }

                using var font = new Font("Segoe UI", 8.25f, FontStyle.Bold);
                var size = TextRenderer.MeasureText(raw, font);
                int badgeW = Math.Max(60, size.Width + 16);
                int badgeH = 22;
                int x = e.CellBounds.X + (e.CellBounds.Width - badgeW) / 2;
                int y = e.CellBounds.Y + (e.CellBounds.Height - badgeH) / 2;

                var badgeRect = new Rectangle(x, y, badgeW, badgeH);

                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var path = RoundedRect(badgeRect, 11);
                using var bgBrush = new SolidBrush(pillBg);
                e.Graphics.FillPath(bgBrush, path);

                TextRenderer.DrawText(e.Graphics, raw, font, badgeRect, pillText,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

                e.Handled = true;
            }
        }

        // ════════════════════════════════════════════════════════════════════
        //  PAGINATION CONTROLS
        // ════════════════════════════════════════════════════════════════════

        private void SetupPagination()
        {
            cmbPageSize.Items.Clear();
            cmbPageSize.Items.AddRange(new object[] { "10 per page", "25 per page", "50 per page", "100 per page", "All rows" });
            cmbPageSize.SelectedItem = "25 per page";
            cmbPageSize.SelectedIndexChanged += (s, e) =>
            {
                string sel = cmbPageSize.SelectedItem?.ToString() ?? "25";
                _pageSize = sel.StartsWith("All") ? int.MaxValue : int.Parse(sel.Split(' ')[0]);
                _currentPage = 1;
                RenderReportPage();
            };
        }

        private void PrevPage()
        {
            if (_currentPage > 1)
            {
                _currentPage--;
                RenderReportPage();
            }
        }

        private void NextPage()
        {
            if (_currentPage < _totalPages)
            {
                _currentPage++;
                RenderReportPage();
            }
        }

        private void RenderReportPage()
        {
            dgvReport.Rows.Clear();

            int total = _filteredRows.Count;
            _totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)_pageSize));
            if (_currentPage > _totalPages) _currentPage = _totalPages;

            var slice = _filteredRows.Skip((_currentPage - 1) * _pageSize).Take(_pageSize).ToList();

            var visibleCols = dgvReport.Columns.Cast<DataGridViewColumn>().ToList();

            foreach (var r in slice)
            {
                object[] values = new object[visibleCols.Count];
                for (int i = 0; i < visibleCols.Count; i++)
                {
                    values[i] = r.GetValue(visibleCols[i].Name);
                }
                dgvReport.Rows.Add(values);
            }

            int start = total == 0 ? 0 : (_currentPage - 1) * _pageSize + 1;
            int end = Math.Min(total, _currentPage * _pageSize);

            lblPaginationShowing.Text = $"Showing {start}–{end} of {total}";
            btnPrevPage.Enabled = _currentPage > 1;
            btnNextPage.Enabled = _currentPage < _totalPages;

            bool hasReport = !string.IsNullOrEmpty(_currentReport);
            pnlPagination.Visible = hasReport;
            if (_lblWatermark != null)
            {
                _lblWatermark.Visible = !hasReport;
                if (!hasReport) PositionWatermark();
            }
        }

        // ════════════════════════════════════════════════════════════════════
        //  FILTER APPLICATION LOGIC
        // ════════════════════════════════════════════════════════════════════

        private void ApplyCurrentFilters()
        {
            DateTime now = DateTime.Now;
            DateTime? startDate = null;
            DateTime? endDate = null;

            switch (_activeDateMode)
            {
                case "today":
                    startDate = now.Date;
                    endDate = now.Date.AddDays(1).AddTicks(-1);
                    break;
                case "7days":
                    startDate = now.Date.AddDays(-7);
                    endDate = now.Date.AddDays(1).AddTicks(-1);
                    break;
                case "30days":
                    startDate = now.Date.AddDays(-30);
                    endDate = now.Date.AddDays(1).AddTicks(-1);
                    break;
                case "thisyear":
                    startDate = new DateTime(now.Year, 1, 1);
                    endDate = now.Date.AddDays(1).AddTicks(-1);
                    break;
                case "custom":
                    startDate = _customStartDate;
                    endDate = _customEndDate;
                    break;
            }

            _filteredRows = _allMasterRows.Where(r =>
            {
                // Date filter (only apply if the row has a date and date mode is restrictive)
                if (startDate.HasValue && endDate.HasValue && r.Date.HasValue)
                {
                    // For equipment report, we don't hide items created prior to 30 days unless custom or specific
                    if (_currentReport != "Equipment" || _activeDateMode == "custom")
                    {
                        if (r.Date.Value < startDate.Value || r.Date.Value > endDate.Value)
                            return false;
                    }
                }

                // Location filter
                if (!string.Equals(_selectedLocation, "All locations", StringComparison.OrdinalIgnoreCase))
                {
                    if (!r.Location.Contains(_selectedLocation, StringComparison.OrdinalIgnoreCase))
                        return false;
                }

                // Category filter
                if (!string.Equals(_selectedCategory, "All categories", StringComparison.OrdinalIgnoreCase))
                {
                    if (!string.Equals(r.Category, _selectedCategory, StringComparison.OrdinalIgnoreCase))
                        return false;
                }

                // Status filter
                if (!string.Equals(_selectedStatus, "Any status", StringComparison.OrdinalIgnoreCase))
                {
                    if (!string.Equals(r.Status, _selectedStatus, StringComparison.OrdinalIgnoreCase))
                        return false;
                }

                // Search text filter
                if (!string.IsNullOrWhiteSpace(_searchFilterText))
                {
                    bool match = r.Values.Values.Any(v => v.Contains(_searchFilterText, StringComparison.OrdinalIgnoreCase));
                    if (!match) return false;
                }

                return true;
            }).ToList();

            // Apply sort if any
            if (!string.IsNullOrEmpty(_currentSortColumn))
            {
                _filteredRows = _currentSortAscending
                    ? _filteredRows.OrderBy(r => r.GetValue(_currentSortColumn)).ToList()
                    : _filteredRows.OrderByDescending(r => r.GetValue(_currentSortColumn)).ToList();
            }

            // Update KPI cards based on filtered rows
            RefreshSummaryCardMetrics();

            // Subtitle
            lblGridReportSubtitle.Text = $"{_filteredRows.Count} records · updated just now";

            _currentPage = 1;
            RenderReportPage();
        }

        private void RefreshSummaryCardMetrics()
        {
            if (_currentReport == "Equipment")
            {
                int total = _filteredRows.Count;
                int allocated = _filteredRows.Count(r => r.Status.Equals("allocated", StringComparison.OrdinalIgnoreCase));
                int inStore = _filteredRows.Count(r => r.Status.Equals("in_store", StringComparison.OrdinalIgnoreCase) || r.Status.Equals("in store", StringComparison.OrdinalIgnoreCase));
                int attention = _filteredRows.Count(r => r.Status.Equals("under_repair", StringComparison.OrdinalIgnoreCase) ||
                                                        r.Status.Equals("under repair", StringComparison.OrdinalIgnoreCase) ||
                                                        r.Status.Equals("decommissioned", StringComparison.OrdinalIgnoreCase));

                UpdateSummaryCards("Total rows", total.ToString(), "Allocated", allocated.ToString(), "In store", inStore.ToString(), "Needs attention", attention.ToString());
            }
            else if (_currentReport == "Employee")
            {
                int total = _filteredRows.Count;
                int held = _filteredRows.Count(r => int.TryParse(r.GetValue("ActiveCount"), out int c) && c > 0);
                int totalItems = _filteredRows.Sum(r => int.TryParse(r.GetValue("ActiveCount"), out int c) ? c : 0);
                int depts = _filteredRows.Select(r => r.Category).Distinct(StringComparer.OrdinalIgnoreCase).Count();

                UpdateSummaryCards("Total rows", total.ToString(), "With items held", held.ToString(), "Total items held", totalItems.ToString(), "Departments", depts.ToString());
            }
            else if (_currentReport == "Allocation")
            {
                int total = _filteredRows.Count;
                int approved = _filteredRows.Count(r => r.Status.Equals("approved", StringComparison.OrdinalIgnoreCase));
                int pending = _filteredRows.Count(r => r.Status.Equals("pending", StringComparison.OrdinalIgnoreCase));
                int returned = _filteredRows.Count(r => r.Status.Equals("returned", StringComparison.OrdinalIgnoreCase));

                UpdateSummaryCards("Total rows", total.ToString(), "Approved", approved.ToString(), "Pending", pending.ToString(), "Returned", returned.ToString());
            }
            else if (_currentReport == "Return")
            {
                int total = _filteredRows.Count;
                int allocations = _filteredRows.Count(r => r.GetValue("Source").Equals("Allocation", StringComparison.OrdinalIgnoreCase));
                int transfers = _filteredRows.Count(r => r.GetValue("Source").Equals("Transferred", StringComparison.OrdinalIgnoreCase));
                int overdue = _filteredRows.Count(r => r.GetValue("Overdue").Equals("Yes", StringComparison.OrdinalIgnoreCase));

                UpdateSummaryCards("Total rows", total.ToString(), "Allocation returns", allocations.ToString(), "Transfers", transfers.ToString(), "Overdue returns", overdue.ToString());
            }
            else if (_currentReport == "CheckInOut")
            {
                int total = _filteredRows.Count;
                int checkOuts = _filteredRows.Count(r => r.GetValue("Type").Equals("Check-Out", StringComparison.OrdinalIgnoreCase));
                int checkIns = _filteredRows.Count(r => r.GetValue("Type").Equals("Check-In", StringComparison.OrdinalIgnoreCase));
                int overdue = _filteredRows.Count(r => r.Status.Equals("Overdue", StringComparison.OrdinalIgnoreCase) || r.Status.Equals("Pending", StringComparison.OrdinalIgnoreCase));

                UpdateSummaryCards("Total rows", total.ToString(), "Check-Outs", checkOuts.ToString(), "Check-Ins", checkIns.ToString(), "Overdue / Pending", overdue.ToString());
            }
        }

        private List<string> _availableCategories = new();

        private void PopulateFilterDropdowns(IEnumerable<string> locations, IEnumerable<string> categories, IEnumerable<string> statuses)
        {
            cmbFilterLocation.Items.Clear();
            cmbFilterLocation.Items.Add("All locations");
            foreach (var loc in locations.Where(l => !string.IsNullOrWhiteSpace(l) && l != "—").Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(l => l))
                cmbFilterLocation.Items.Add(loc);
            cmbFilterLocation.SelectedIndex = 0;

            _availableCategories = categories.Where(c => !string.IsNullOrWhiteSpace(c) && c != "—").Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(c => c).ToList();

            cmbFilterStatus.Items.Clear();
            cmbFilterStatus.Items.Add("Any status");
            foreach (var st in statuses.Where(s => !string.IsNullOrWhiteSpace(s) && s != "—").Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(s => s))
                cmbFilterStatus.Items.Add(st);
            cmbFilterStatus.SelectedIndex = 0;
        }

        // ════════════════════════════════════════════════════════════════════
        //  REPORTS LOADING
        // ════════════════════════════════════════════════════════════════════

        private void btnEquipmentReport_Click(object? sender, EventArgs e)
        {
            if (_currentReport != "Equipment")
            {
                LoadEquipmentReport("All Categories");
            }
            else
            {
                ShowEquipmentCategoryMenu();
            }
        }

        private void ShowEquipmentCategoryMenu()
        {
            if (_availableCategories.Count == 0) return;

            var menu = new ContextMenuStrip
            {
                Font = ThemeHelper.FontDefault,
                BackColor = ThemeHelper.CardBg,
                ForeColor = ThemeHelper.TextPrimary
            };

            var allCatItem = new ToolStripMenuItem("All Categories")
            {
                Checked = string.Equals(_selectedCategory, "All categories", StringComparison.OrdinalIgnoreCase)
            };
            allCatItem.Click += (s, ev) =>
            {
                _selectedCategory = "All categories";
                btnEquipmentReport.Text = "Equipment  ▾";
                ApplyCurrentFilters();
            };
            menu.Items.Add(allCatItem);

            foreach (var item in _availableCategories)
            {
                var mi = new ToolStripMenuItem(item)
                {
                    Checked = string.Equals(_selectedCategory, item, StringComparison.OrdinalIgnoreCase)
                };
                string catName = item;
                mi.Click += (s, ev) =>
                {
                    _selectedCategory = catName;
                    btnEquipmentReport.Text = $"Equipment ({catName})  ▾";
                    ApplyCurrentFilters();
                };
                menu.Items.Add(mi);
            }

            menu.Show(btnEquipmentReport, new Point(0, btnEquipmentReport.Height));
        }

        private void LoadEquipmentReport(string category)
        {
            _currentReport = "Equipment";
            _activeHeaderColor = Color.FromArgb(94, 53, 177); // Deep Purple
            lblGridReportTitle.Text = "Equipment Report";
            btnEquipmentReport.Text = "Equipment  ▾";
            UpdateTabButtonStyles();
            ApplyTheme();

            try
            {
                using var db = new InventoryDbContext();
                var list = db.Equipment
                    .Where(e => !e.IsDeleted)
                    .Include(e => e.Allocations).ThenInclude(a => a.Employee)
                    .Include(e => e.Allocations).ThenInclude(a => a.Location)
                    .Include(e => e.ProcurementType)
                    .OrderBy(e => e.EquipmentCode)
                    .ToList();

                var activeTx = db.Transactions
                    .Where(t => t.Status == "Pending" || t.Status == "Overdue")
                    .ToList();

                var masterRows = new List<ReportRowData>();
                var locations = new HashSet<string>();
                var categories = new HashSet<string>();
                var statuses = new HashSet<string>();

                foreach (var eq in list)
                {
                    var alloc = eq.Allocations.FirstOrDefault(a => a.Status == "approved");
                    var tx = activeTx.FirstOrDefault(t => t.EquipmentId == eq.Id);

                    string assignedTo = alloc?.Employee?.FullName ?? tx?.IssuedTo ?? "—";
                    string loc = ReturnsForm.FormatLocation(alloc?.Location) ?? (string.IsNullOrWhiteSpace(tx?.Department) ? "—" : tx.Department);
                    string makeModel = string.Join(" ", new[] { eq.Make, eq.Model }.Where(s => !string.IsNullOrWhiteSpace(s)));
                    if (string.IsNullOrWhiteSpace(makeModel)) makeModel = "—";

                    string rawStatus = eq.CurrentStatus ?? "in_store";
                    string status = rawStatus.Replace("_", " ");
                    if (string.Equals(status, "under repair", StringComparison.OrdinalIgnoreCase)) status = "Under Repair";
                    else if (string.Equals(status, "in store", StringComparison.OrdinalIgnoreCase)) status = "In Store";
                    else if (string.Equals(status, "allocated", StringComparison.OrdinalIgnoreCase)) status = "Allocated";
                    else if (string.Equals(status, "decommissioned", StringComparison.OrdinalIgnoreCase)) status = "Decommissioned";

                    string cat = eq.Category ?? "—";

                    var row = new ReportRowData
                    {
                        Date = eq.PurchaseDate?.ToDateTime(TimeOnly.MinValue),
                        Status = status,
                        Location = loc,
                        Category = cat
                    };

                    row.SetValue("Code", eq.EquipmentCode);
                    row.SetValue("Name", eq.Name);
                    row.SetValue("Category", cat);
                    row.SetValue("MakeModel", makeModel);
                    row.SetValue("AssignedTo", assignedTo);
                    row.SetValue("Location", loc);
                    row.SetValue("Status", status);
                    row.SetValue("QRCode", eq.QrCode ?? "—");
                    row.SetValue("Serial", eq.SerialNumber ?? "—");

                    masterRows.Add(row);

                    if (loc != "—") locations.Add(loc);
                    if (cat != "—") categories.Add(cat);
                    statuses.Add(status);
                }

                _allMasterRows = masterRows;

                // Setup Columns matching Image 1
                dgvReport.Columns.Clear();
                dgvReport.Columns.Add("Code", "Code");
                dgvReport.Columns.Add("Name", "Name");
                dgvReport.Columns.Add("Category", "Category");
                dgvReport.Columns.Add("MakeModel", "Make / Model");
                dgvReport.Columns.Add("AssignedTo", "Assigned to");
                dgvReport.Columns.Add("Location", "Location");
                dgvReport.Columns.Add("Status", "Status");

                dgvReport.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

                PopulateFilterDropdowns(locations, categories, statuses);
                ApplyCurrentFilters();
            }
            catch (Exception ex)
            {
                ToastHelper.Show(this, "Error loading equipment report: " + ex.Message, ToastType.Error);
            }
        }

        private void btnEmployeeReport_Click(object? sender, EventArgs e)
        {
            _currentReport = "Employee";
            _activeHeaderColor = Color.FromArgb(123, 31, 162); // Magenta
            lblGridReportTitle.Text = "Employee Summary Report";
            UpdateTabButtonStyles();
            ApplyTheme();

            try
            {
                using var db = new InventoryDbContext();
                var employees = db.Employees.Where(emp => emp.IsActive).ToList();
                var allocations = db.Allocations.Where(a => a.Status == "approved").ToList();

                var masterRows = new List<ReportRowData>();
                var depts = new HashSet<string>();

                foreach (var emp in employees)
                {
                    int activeCount = allocations.Count(a => a.EmployeeId == emp.Id);
                    string dept = emp.Department ?? "—";
                    string status = emp.IsActive ? "Active" : "Inactive";

                    var row = new ReportRowData
                    {
                        Date = emp.CreatedAt,
                        Status = status,
                        Category = dept,
                        Location = "—"
                    };

                    row.SetValue("EmpCode", emp.EmployeeCode ?? "—");
                    row.SetValue("Employee", emp.FullName);
                    row.SetValue("Designation", dept);
                    row.SetValue("Email", emp.Email ?? "—");
                    row.SetValue("Phone", emp.Phone ?? "—");
                    row.SetValue("ActiveCount", activeCount.ToString());
                    row.SetValue("Status", status);

                    masterRows.Add(row);
                    if (dept != "—") depts.Add(dept);
                }

                _allMasterRows = masterRows;

                dgvReport.Columns.Clear();
                dgvReport.Columns.Add("EmpCode", "Employee Code");
                dgvReport.Columns.Add("Employee", "Employee");
                dgvReport.Columns.Add("Designation", "Department");
                dgvReport.Columns.Add("Email", "Email");
                dgvReport.Columns.Add("Phone", "Phone");
                dgvReport.Columns.Add("ActiveCount", "Items Held");
                dgvReport.Columns.Add("Status", "Status");

                dgvReport.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

                PopulateFilterDropdowns(Enumerable.Empty<string>(), depts, new[] { "Active", "Inactive" });
                ApplyCurrentFilters();
            }
            catch (Exception ex)
            {
                ToastHelper.Show(this, "Error loading employee report: " + ex.Message, ToastType.Error);
            }
        }

        private void btnAllocationReport_Click(object? sender, EventArgs e)
        {
            _currentReport = "Allocation";
            _activeHeaderColor = Color.FromArgb(245, 124, 0); // Orange
            lblGridReportTitle.Text = "Active Allocations Report";
            UpdateTabButtonStyles();
            ApplyTheme();

            try
            {
                using var db = new InventoryDbContext();
                var list = db.Allocations
                    .Include(a => a.Equipment)
                    .Include(a => a.Employee)
                    .Include(a => a.Location)
                    .OrderByDescending(a => a.AllocationDate)
                    .ToList();

                var masterRows = new List<ReportRowData>();
                var locations = new HashSet<string>();
                var categories = new HashSet<string>();
                var statuses = new HashSet<string>();

                foreach (var item in list)
                {
                    string fullLoc = ReturnsForm.FormatLocation(item.Location) ?? "—";
                    string status = item.Status ?? "pending";
                    string cat = item.Equipment?.Category ?? "—";

                    var row = new ReportRowData
                    {
                        Date = item.AllocationDate,
                        Status = status,
                        Location = fullLoc,
                        Category = cat
                    };

                    row.SetValue("Code", item.Equipment?.EquipmentCode ?? "—");
                    row.SetValue("Name", item.Equipment?.Name ?? "—");
                    row.SetValue("Employee", item.Employee?.FullName ?? "—");
                    row.SetValue("Location", fullLoc);
                    row.SetValue("Date", item.AllocationDate.ToString("yyyy-MM-dd"));
                    row.SetValue("Expected", item.ExpectedReturnDate?.ToString("yyyy-MM-dd") ?? "—");
                    row.SetValue("Status", status);

                    masterRows.Add(row);

                    if (fullLoc != "—") locations.Add(fullLoc);
                    if (cat != "—") categories.Add(cat);
                    statuses.Add(status);
                }

                _allMasterRows = masterRows;

                dgvReport.Columns.Clear();
                dgvReport.Columns.Add("Code", "Equipment Code");
                dgvReport.Columns.Add("Name", "Equipment Name");
                dgvReport.Columns.Add("Employee", "Employee");
                dgvReport.Columns.Add("Location", "Location");
                dgvReport.Columns.Add("Date", "Allocated On");
                dgvReport.Columns.Add("Expected", "Expected Return");
                dgvReport.Columns.Add("Status", "Status");

                dgvReport.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

                PopulateFilterDropdowns(locations, categories, statuses);
                ApplyCurrentFilters();
            }
            catch (Exception ex)
            {
                ToastHelper.Show(this, "Error loading allocation report: " + ex.Message, ToastType.Error);
            }
        }

        private void btnReturnReport_Click(object? sender, EventArgs e)
        {
            _currentReport = "Return";
            _activeHeaderColor = Color.FromArgb(92, 107, 192); // Indigo
            lblGridReportTitle.Text = "Returns History Report";
            UpdateTabButtonStyles();
            ApplyTheme();

            try
            {
                using var db = new InventoryDbContext();
                var transferredAllocationIds = db.Allocations
                    .Where(a => a.PreviousAllocationId != null)
                    .Select(a => a.PreviousAllocationId!.Value)
                    .Distinct()
                    .ToHashSet();

                var returnedAllocations = db.Allocations
                    .Where(a => a.Status == "returned")
                    .Include(a => a.Equipment)
                    .Include(a => a.Employee)
                    .Include(a => a.Location)
                    .OrderByDescending(a => a.ActualReturnDate)
                    .ToList();

                var completedCheckIns = db.Transactions
                    .Include(t => t.Equipment)
                    .Where(t => t.Status == "Completed" && t.ActualReturn.HasValue && t.TransactionType == "Check-Out")
                    .OrderByDescending(t => t.ActualReturn)
                    .ToList();

                var masterRows = new List<ReportRowData>();
                var locations = new HashSet<string>();

                foreach (var item in returnedAllocations)
                {
                    bool isTransfer = transferredAllocationIds.Contains(item.Id);
                    bool isOverdue = item.ExpectedReturnDate.HasValue && item.ActualReturnDate.HasValue &&
                                     item.ActualReturnDate.Value > item.ExpectedReturnDate.Value;
                    string fullLoc = ReturnsForm.FormatLocation(item.Location) ?? "—";

                    var row = new ReportRowData
                    {
                        Date = item.ActualReturnDate?.ToDateTime(TimeOnly.MinValue) ?? item.AllocationDate,
                        Status = "Returned",
                        Location = fullLoc,
                        Category = item.Equipment?.Category ?? "—"
                    };

                    row.SetValue("Code", item.Equipment?.EquipmentCode ?? "—");
                    row.SetValue("Name", item.Equipment?.Name ?? "—");
                    row.SetValue("Employee", item.Employee?.FullName ?? "—");
                    row.SetValue("Location", fullLoc);
                    row.SetValue("Returned", item.ActualReturnDate?.ToString("yyyy-MM-dd") ?? "—");
                    row.SetValue("Condition", item.Equipment?.ConditionGrade ?? "—");
                    row.SetValue("Source", isTransfer ? "Transferred" : "Allocation");
                    row.SetValue("Overdue", isOverdue ? "Yes" : "No");

                    masterRows.Add(row);
                    if (fullLoc != "—") locations.Add(fullLoc);
                }

                foreach (var t in completedCheckIns)
                {
                    bool isOverdue = t.ExpectedReturn.HasValue && t.ActualReturn.HasValue &&
                                     t.ActualReturn.Value > t.ExpectedReturn.Value;
                    string loc = string.IsNullOrWhiteSpace(t.Department) ? "—" : t.Department;

                    var row = new ReportRowData
                    {
                        Date = t.ActualReturn?.ToDateTime(TimeOnly.MinValue) ?? t.TransactionDate,
                        Status = "Returned",
                        Location = loc,
                        Category = t.Equipment?.Category ?? "—"
                    };

                    row.SetValue("Code", t.Equipment?.EquipmentCode ?? "—");
                    row.SetValue("Name", t.Equipment?.Name ?? "—");
                    row.SetValue("Employee", string.IsNullOrWhiteSpace(t.IssuedTo) ? "—" : t.IssuedTo);
                    row.SetValue("Location", loc);
                    row.SetValue("Returned", t.ActualReturn?.ToString("yyyy-MM-dd") ?? "—");
                    row.SetValue("Condition", t.ConditionAfter ?? "—");
                    row.SetValue("Source", "Check In/Out");
                    row.SetValue("Overdue", isOverdue ? "Yes" : "No");

                    masterRows.Add(row);
                    if (loc != "—") locations.Add(loc);
                }

                _allMasterRows = masterRows;

                dgvReport.Columns.Clear();
                dgvReport.Columns.Add("Code", "Equipment Code");
                dgvReport.Columns.Add("Name", "Equipment Name");
                dgvReport.Columns.Add("Employee", "Employee / Issued To");
                dgvReport.Columns.Add("Location", "Location");
                dgvReport.Columns.Add("Returned", "Return Date");
                dgvReport.Columns.Add("Condition", "Condition");
                dgvReport.Columns.Add("Source", "Source");
                dgvReport.Columns.Add("Overdue", "Overdue");

                dgvReport.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

                PopulateFilterDropdowns(locations, new[] { "Allocation", "Transferred", "Check In/Out" }, new[] { "Returned" });
                ApplyCurrentFilters();
            }
            catch (Exception ex)
            {
                ToastHelper.Show(this, "Error loading returns report: " + ex.Message, ToastType.Error);
            }
        }

        private void btnCheckInOutReport_Click(object? sender, EventArgs e)
        {
            _currentReport = "CheckInOut";
            _activeHeaderColor = Color.FromArgb(3, 169, 244); // Sky Blue
            lblGridReportTitle.Text = "Check In/Out History Report";
            UpdateTabButtonStyles();
            ApplyTheme();

            try
            {
                using var db = new InventoryDbContext();
                var transactions = db.Transactions
                    .Include(t => t.Equipment)
                    .OrderByDescending(t => t.TransactionDate)
                    .ToList();

                var masterRows = new List<ReportRowData>();
                var locations = new HashSet<string>();
                var statuses = new HashSet<string>();

                foreach (var t in transactions)
                {
                    string status = t.Status ?? "Completed";
                    string loc = string.IsNullOrWhiteSpace(t.Department) ? "—" : t.Department;

                    var row = new ReportRowData
                    {
                        Date = t.TransactionDate,
                        Status = status,
                        Location = loc,
                        Category = t.TransactionType ?? "Check-Out"
                    };

                    row.SetValue("Code", t.Equipment?.EquipmentCode ?? "—");
                    row.SetValue("Name", t.Equipment?.Name ?? "—");
                    row.SetValue("Type", t.TransactionType ?? "Check-Out");
                    row.SetValue("IssuedTo", t.IssuedTo ?? "—");
                    row.SetValue("Department", loc);
                    row.SetValue("Operator", t.OperatorName ?? "—");
                    row.SetValue("Date", t.TransactionDate.ToString("yyyy-MM-dd HH:mm"));
                    row.SetValue("ExpectedReturn", t.ExpectedReturn?.ToString("yyyy-MM-dd") ?? "—");
                    row.SetValue("Status", status);

                    masterRows.Add(row);

                    if (loc != "—") locations.Add(loc);
                    statuses.Add(status);
                }

                _allMasterRows = masterRows;

                dgvReport.Columns.Clear();
                dgvReport.Columns.Add("Code", "Equipment Code");
                dgvReport.Columns.Add("Name", "Equipment Name");
                dgvReport.Columns.Add("Type", "Type");
                dgvReport.Columns.Add("IssuedTo", "Issued To");
                dgvReport.Columns.Add("Department", "Department / Location");
                dgvReport.Columns.Add("Operator", "Operator");
                dgvReport.Columns.Add("Date", "Transaction Date");
                dgvReport.Columns.Add("ExpectedReturn", "Expected Return");
                dgvReport.Columns.Add("Status", "Status");

                dgvReport.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

                PopulateFilterDropdowns(locations, new[] { "Check-Out", "Check-In" }, statuses);
                ApplyCurrentFilters();
            }
            catch (Exception ex)
            {
                ToastHelper.Show(this, "Error loading check in/out report: " + ex.Message, ToastType.Error);
            }
        }

        // ════════════════════════════════════════════════════════════════════
        //  COLUMNS & SORT MENUS
        // ════════════════════════════════════════════════════════════════════

        private void btnColumns_Click(object? sender, EventArgs e)
        {
            if (dgvReport.Columns.Count == 0) return;

            var menu = new ContextMenuStrip
            {
                Font = ThemeHelper.FontDefault,
                BackColor = ThemeHelper.CardBg,
                ForeColor = ThemeHelper.TextPrimary
            };

            foreach (DataGridViewColumn col in dgvReport.Columns)
            {
                var item = new ToolStripMenuItem(col.HeaderText)
                {
                    Checked = col.Visible,
                    CheckOnClick = true
                };

                string colName = col.Name;
                item.CheckedChanged += (s, ev) =>
                {
                    dgvReport.Columns[colName].Visible = item.Checked;
                };

                menu.Items.Add(item);
            }

            menu.Show(btnColumns, new Point(0, btnColumns.Height));
        }

        private void btnSort_Click(object? sender, EventArgs e)
        {
            if (dgvReport.Columns.Count == 0) return;

            var menu = new ContextMenuStrip
            {
                Font = ThemeHelper.FontDefault,
                BackColor = ThemeHelper.CardBg,
                ForeColor = ThemeHelper.TextPrimary
            };

            foreach (DataGridViewColumn col in dgvReport.Columns.Cast<DataGridViewColumn>().Where(c => c.Visible))
            {
                string colName = col.Name;
                string header = col.HeaderText;

                var asc = new ToolStripMenuItem($"{header} (A-Z / Asc)");
                asc.Click += (s, ev) =>
                {
                    _currentSortColumn = colName;
                    _currentSortAscending = true;
                    ApplyCurrentFilters();
                };
                menu.Items.Add(asc);

                var desc = new ToolStripMenuItem($"{header} (Z-A / Desc)");
                desc.Click += (s, ev) =>
                {
                    _currentSortColumn = colName;
                    _currentSortAscending = false;
                    ApplyCurrentFilters();
                };
                menu.Items.Add(desc);
            }

            menu.Show(btnSort, new Point(0, btnSort.Height));
        }

        // ════════════════════════════════════════════════════════════════════
        //  EXPORTS: PRINT, PDF, EXCEL, CSV
        // ════════════════════════════════════════════════════════════════════

        private void btnPrint_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_currentReport) || dgvReport.Columns.Count == 0 || _filteredRows.Count == 0)
            {
                ToastHelper.Show(this, "No data available to print.", ToastType.Info);
                return;
            }

            try
            {
                using var pd = new PrintDocument();
                pd.DefaultPageSettings.Landscape = true;
                int rowIdx = 0;
                var visibleCols = dgvReport.Columns.Cast<DataGridViewColumn>().Where(c => c.Visible).OrderBy(c => c.DisplayIndex).ToList();

                pd.PrintPage += (s, ev) =>
                {
                    var g = ev.Graphics!;
                    int left = ev.MarginBounds.Left;
                    int top = ev.MarginBounds.Top;
                    int width = ev.MarginBounds.Width;

                    using var titleFont = new Font("Segoe UI", 14f, FontStyle.Bold);
                    using var subFont = new Font("Segoe UI", 9f);
                    using var boldFont = new Font("Segoe UI", 8.5f, FontStyle.Bold);
                    using var regFont = new Font("Segoe UI", 8.5f);

                    g.DrawString($"{lblGridReportTitle.Text} — {_companyName}", titleFont, Brushes.Black, left, top);
                    top += 25;
                    g.DrawString($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm} | Total Records: {_filteredRows.Count}", subFont, Brushes.Gray, left, top);
                    top += 25;

                    int colWidth = width / Math.Max(1, visibleCols.Count);
                    int headerH = 26;

                    using var headerBg = new SolidBrush(_activeHeaderColor);
                    g.FillRectangle(headerBg, left, top, width, headerH);

                    for (int i = 0; i < visibleCols.Count; i++)
                    {
                        var r = new Rectangle(left + i * colWidth + 4, top + 4, colWidth - 8, headerH - 8);
                        g.DrawString(visibleCols[i].HeaderText, boldFont, Brushes.White, r);
                    }
                    top += headerH;

                    int rowH = 22;
                    while (rowIdx < _filteredRows.Count)
                    {
                        if (top + rowH > ev.MarginBounds.Bottom)
                        {
                            ev.HasMorePages = true;
                            return;
                        }

                        var row = _filteredRows[rowIdx];
                        bool alt = (rowIdx % 2 == 1);
                        if (alt) g.FillRectangle(Brushes.WhiteSmoke, left, top, width, rowH);

                        for (int i = 0; i < visibleCols.Count; i++)
                        {
                            string txt = row.GetValue(visibleCols[i].Name);
                            var r = new Rectangle(left + i * colWidth + 4, top + 2, colWidth - 8, rowH - 4);
                            g.DrawString(txt, regFont, Brushes.Black, r);
                        }

                        using var linePen = new Pen(Color.FromArgb(230, 230, 230));
                        g.DrawLine(linePen, left, top + rowH, left + width, top + rowH);

                        top += rowH;
                        rowIdx++;
                    }
                    ev.HasMorePages = false;
                };

                using var ppd = new PrintPreviewDialog
                {
                    Document = pd,
                    Width = 950,
                    Height = 650,
                    StartPosition = FormStartPosition.CenterParent,
                    Text = $"{_currentReport} Report Preview"
                };
                ppd.ShowDialog(this);
            }
            catch (Exception ex)
            {
                ToastHelper.Show(this, "Print preview failed: " + ex.Message, ToastType.Error);
            }
        }

        private void btnExportPDF_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_currentReport) || dgvReport.Columns.Count == 0 || _filteredRows.Count == 0)
            {
                ToastHelper.Show(this, "No data available to export.", ToastType.Info);
                return;
            }

            using var sfd = new SaveFileDialog
            {
                Filter = "PDF Files|*.pdf",
                FileName = $"{_currentReport}_Report_{DateTime.Now:yyyyMMdd_HHmm}.pdf"
            };
            if (sfd.ShowDialog() != DialogResult.OK) return;

            try
            {
                QuestPDF.Settings.License = LicenseType.Community;

                var visibleCols = dgvReport.Columns.Cast<DataGridViewColumn>().Where(c => c.Visible).OrderBy(c => c.DisplayIndex).ToList();
                int colCount = visibleCols.Count;
                var headers = visibleCols.Select(c => c.HeaderText).ToList();
                string title = lblGridReportTitle.Text;
                string company = _companyName;
                string generated = DateTime.Now.ToString("yyyy-MM-dd HH:mm");

                Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4.Landscape());
                        page.Margin(30);
                        page.DefaultTextStyle(x => x.FontSize(9));

                        page.Header().Column(col =>
                        {
                            col.Item().Text($"{title}").FontSize(16).Bold();
                            col.Item().Text(company).FontSize(10).FontColor(QuestPDF.Helpers.Colors.Grey.Darken1);
                            col.Item().PaddingBottom(8).Text($"Generated: {generated} · Records: {_filteredRows.Count}").FontSize(8).FontColor(QuestPDF.Helpers.Colors.Grey.Medium);
                        });

                        page.Content().Table(table =>
                        {
                            table.ColumnsDefinition(cols =>
                            {
                                for (int c = 0; c < colCount; c++) cols.RelativeColumn();
                            });

                            table.Header(header =>
                            {
                                foreach (var h in headers)
                                {
                                    header.Cell().Background(QuestPDF.Helpers.Colors.Purple.Darken2)
                                        .Padding(5)
                                        .Text(h).FontColor(QuestPDF.Helpers.Colors.White).Bold();
                                }
                            });

                            bool alt = false;
                            foreach (var r in _filteredRows)
                            {
                                var bg = alt ? QuestPDF.Helpers.Colors.Grey.Lighten4 : QuestPDF.Helpers.Colors.White;
                                foreach (var col in visibleCols)
                                {
                                    table.Cell().Background(bg).Padding(4).Text(r.GetValue(col.Name));
                                }
                                alt = !alt;
                            }
                        });

                        page.Footer().AlignCenter().Text(x =>
                        {
                            x.CurrentPageNumber();
                            x.Span(" / ");
                            x.TotalPages();
                        });
                    });
                }).GeneratePdf(sfd.FileName);

                ToastHelper.Show(this, "Report exported to PDF successfully.", ToastType.Success);
            }
            catch (Exception ex)
            {
                ToastHelper.Show(this, "PDF Export failed: " + ex.Message, ToastType.Error);
            }
        }

        private void btnExportExcel_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_currentReport) || dgvReport.Columns.Count == 0 || _filteredRows.Count == 0)
            {
                ToastHelper.Show(this, "No data available to export.", ToastType.Info);
                return;
            }

            using var sfd = new SaveFileDialog
            {
                Filter = "Excel Files|*.xlsx",
                FileName = $"{_currentReport}_Report_{DateTime.Now:yyyyMMdd_HHmm}.xlsx"
            };
            if (sfd.ShowDialog() != DialogResult.OK) return;

            try
            {
                using var wb = new XLWorkbook();
                var ws = wb.Worksheets.Add(_currentReport);

                var visibleCols = dgvReport.Columns.Cast<DataGridViewColumn>().Where(c => c.Visible).OrderBy(c => c.DisplayIndex).ToList();

                ws.Cell(1, 1).Value = $"{_companyName} — {lblGridReportTitle.Text}";
                ws.Range(1, 1, 1, visibleCols.Count).Merge();
                ws.Cell(1, 1).Style.Font.Bold = true;
                ws.Cell(1, 1).Style.Font.FontSize = 13;

                for (int c = 0; c < visibleCols.Count; c++)
                    ws.Cell(2, c + 1).Value = visibleCols[c].HeaderText;

                var headerRange = ws.Range(2, 1, 2, visibleCols.Count);
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = XLColor.FromArgb(94, 53, 177);
                headerRange.Style.Font.FontColor = XLColor.White;

                for (int r = 0; r < _filteredRows.Count; r++)
                {
                    for (int c = 0; c < visibleCols.Count; c++)
                    {
                        ws.Cell(r + 3, c + 1).Value = _filteredRows[r].GetValue(visibleCols[c].Name);
                    }
                }

                ws.Columns().AdjustToContents();
                wb.SaveAs(sfd.FileName);

                ToastHelper.Show(this, "Report exported to Excel successfully.", ToastType.Success);
            }
            catch (Exception ex)
            {
                ToastHelper.Show(this, "Excel Export failed: " + ex.Message, ToastType.Error);
            }
        }

        private void btnExportCSV_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_currentReport) || dgvReport.Columns.Count == 0 || _filteredRows.Count == 0)
            {
                ToastHelper.Show(this, "No data available to export.", ToastType.Info);
                return;
            }

            using var sfd = new SaveFileDialog
            {
                Filter = "CSV Files|*.csv",
                FileName = $"{_currentReport}_Report_{DateTime.Now:yyyyMMdd_HHmm}.csv"
            };
            if (sfd.ShowDialog() != DialogResult.OK) return;

            try
            {
                var sb = new StringBuilder();
                var visibleCols = dgvReport.Columns.Cast<DataGridViewColumn>().Where(c => c.Visible).OrderBy(c => c.DisplayIndex).ToList();

                sb.AppendLine(string.Join(",", visibleCols.Select(c => EscapeCsv(c.HeaderText))));

                foreach (var r in _filteredRows)
                {
                    sb.AppendLine(string.Join(",", visibleCols.Select(c => EscapeCsv(r.GetValue(c.Name)))));
                }

                File.WriteAllText(sfd.FileName, sb.ToString(), Encoding.UTF8);
                ToastHelper.Show(this, "Report exported to CSV successfully.", ToastType.Success);
            }
            catch (Exception ex)
            {
                ToastHelper.Show(this, "CSV Export failed: " + ex.Message, ToastType.Error);
            }
        }

        private static string EscapeCsv(string? val)
        {
            if (string.IsNullOrEmpty(val)) return "\"\"";
            return "\"" + val.Replace("\"", "\"\"") + "\"";
        }
    }
}
