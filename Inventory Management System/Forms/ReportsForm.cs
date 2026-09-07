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
using System.Drawing.Printing;
using System.Linq;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;
using Color = System.Drawing.Color;
using Size = System.Drawing.Size;


namespace Inventory_Management_System.Forms
{
    public partial class ReportsForm : Form, IThemableForm

    {
        private readonly EquipmentService _equipmentService = new();
        private readonly EmployeeService _employeeService = new();
        private readonly AllocationService _allocationService = new();

        // Currently selected report — drives both the on-screen preview grid
        // and what gets exported when PDF/Excel buttons are clicked.
        private string _currentReport = "";
        private string _activeEquipmentCategory = "All Categories";
        private Color? _previewHeaderColor = null; // null = default theme header color
        private DataGridView _preview = null!;
        private ContextMenuStrip? _equipmentMenu;
        private System.Windows.Forms.Timer? _equipmentMenuCloseTimer;
        private Label _previewTitle = null!;
        private Label _lblWatermark = null!;
        private string _companyName = "AssetPilot";

        // ── Pagination — built once, only shown once a report is selected ──
        private Panel _pnlPagination = null!;
        private ComboBox cmbPageSize = null!;
        private Button btnPrevPage = null!;
        private Button btnNextPage = null!;
        private Label lblPageIndicator = null!;
        private Label lblRowCount = null!;
        private int _currentPage = 1;
        private int _pageSize = 25;
        private int _totalPages = 1;
        // Each entry adds ONE fully-built row (with its own styling) to _preview —
        // built once per report selection, then only the current page's slice is
        // actually invoked/rendered, instead of dumping every row at once.
        private List<Action> _reportRowBuilders = new();

        public ReportsForm()
        {
            InitializeComponent();
            btnCheckInOutReport.Click += btnCheckInOutReport_Click;
            ApplyTheme();
            BuildPreviewArea();
            LoadCompanyName();
            SetupEquipmentDropdown();
        }

        private void LoadCompanyName()
        {
            try
            {
                using var db = new InventoryDbContext();
                _companyName = db.AppSettings.FirstOrDefault()?.CompanyName ?? "AssetPilot";
            }
            catch { /* fall back to default already set */ }
        }

        public void ApplyTheme()
        {
            ThemeHelper.ApplyFormBackground(this);
            ThemeHelper.StyleLabel(lblReportType, bold: true, color: ThemeHelper.Accent);


            ThemeHelper.StyleButton(btnEquipmentReport, Color.FromArgb(94, 53, 177));
            ThemeHelper.StyleButton(btnEmployeeReport, Color.FromArgb(123, 31, 162));
            ThemeHelper.StyleButton(btnAllocationReport, Color.FromArgb(245, 124, 0));
            ThemeHelper.StyleButton(btnReturnReport, Color.FromArgb(99, 102, 241));
            ThemeHelper.StyleButton(btnCheckInOutReport, Color.FromArgb(33, 150, 243));

            ThemeHelper.StyleSecondaryButton(btnExportPDF);
            ThemeHelper.StyleSecondaryButton(btnExportExcel);

            // Preview area is built once in BuildPreviewArea() and never re-styled
            // otherwise — re-apply here so the grid/title/watermark pick up the
            // new theme immediately instead of staying stuck on the old colors.
            if (_preview != null)
            {
                if (_previewHeaderColor.HasValue)
                    ThemeHelper.StyleDataGridView(_preview, _previewHeaderColor.Value);
                else
                    ThemeHelper.StyleDataGridView(_preview);
                // Rebuild the current page's rows so per-cell colors (Status,
                // Overdue, Source) get reapplied for the new theme — Invalidate()
                // alone just repaints with the old cell-level Style values,
                // which is what caused the half-updated flash and flat colors.
                if (_reportRowBuilders.Count > 0)
                    RenderReportPage();
                else
                    _preview.Invalidate();
            }
            if (_previewTitle != null)
                _previewTitle.ForeColor = ThemeHelper.TextPrimary;

            if (_pnlPagination != null)
            {
                _pnlPagination.BackColor = ThemeHelper.IsDarkMode ? Color.FromArgb(22, 24, 29) : Color.FromArgb(249, 250, 251);
                lblRowCount.ForeColor = ThemeHelper.TextSecondary;
                lblPageIndicator.ForeColor = ThemeHelper.TextSecondary;
                ThemeHelper.StyleComboBox(cmbPageSize);
                ThemeHelper.StyleSecondaryButton(btnPrevPage);
                ThemeHelper.StyleSecondaryButton(btnNextPage);
            }
        }

        // ── Preview grid — built in code since the Designer only has buttons ──
        private void BuildPreviewArea()
        {
            _previewTitle = new Label
            {
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Text = "Select a report above to preview it here.",
                Font = ThemeHelper.FontBold,
                ForeColor = ThemeHelper.TextPrimary,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Height = 30
            };
            Controls.Add(_previewTitle);

            _preview = new DataGridView
            {
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                AllowUserToAddRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };
            ThemeHelper.StyleDataGridView(_preview);
            _preview.ColumnHeadersHeight = 40;
            _preview.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            _preview.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.True;
            _preview.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            Controls.Add(_preview);
            _preview.BringToFront();

            // Reposition preview area below panelTop whenever it changes size
            void RepositionPreview()
            {
                int top = panelTop.Bottom + 4;
                int left = 20;
                int w = ClientSize.Width - left * 2;
                _previewTitle.Location = new Point(left, top);
                _previewTitle.Width = w;
                _preview.Location = new Point(left, top + _previewTitle.Height + 2);

                int paginationSpace = _pnlPagination != null && _pnlPagination.Visible ? _pnlPagination.Height + 4 : 8;
                _preview.Size = new Size(w, ClientSize.Height - _preview.Top - paginationSpace);

                if (_pnlPagination != null)
                {
                    _pnlPagination.Location = new Point(left, ClientSize.Height - _pnlPagination.Height - 4);
                    _pnlPagination.Width = w;
                }
            }

            BuildPaginationBar();

            panelTop.SizeChanged += (s, e) => RepositionPreview();
            Resize += (s, e) => RepositionPreview();
            Shown += (s, e) => RepositionPreview();
            RepositionPreview(); // initial placement

            _lblWatermark = new Label
            {
                Text = "Designed and Developed by\n🐙@shayan-qazmi/📷@shayanqazmi",
                Font = new Font("Segoe UI", 8f, FontStyle.Italic),
                ForeColor = Color.FromArgb(160, 160, 160),
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleRight,
                AutoSize = true,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right
            };
            Controls.Add(_lblWatermark);
            _lblWatermark.BringToFront();
            PositionWatermark();

            this.Resize += (s, e) => PositionWatermark();
            _lblWatermark.SizeChanged += (s, e) => PositionWatermark();
        }

        private void RepositionPaginationControls()
        {
            if (_pnlPagination == null) return;
            int rightMargin = 4, gap = 8, ctrlY = 5;
            int x = _pnlPagination.Width - rightMargin;
            x -= btnNextPage.Width; btnNextPage.Location = new Point(x, ctrlY); x -= gap;
            x -= lblPageIndicator.PreferredWidth; lblPageIndicator.Location = new Point(x, ctrlY + 3); x -= gap;
            x -= btnPrevPage.Width; btnPrevPage.Location = new Point(x, ctrlY); x -= gap;
            x -= cmbPageSize.Width; cmbPageSize.Location = new Point(x, ctrlY);
        }

        private void PositionWatermark()
        {
            if (_lblWatermark == null) return;
            const int margin = 12;
            _lblWatermark.Location = new Point(
                ClientSize.Width - _lblWatermark.Width - margin,
                ClientSize.Height - _lblWatermark.Height - margin);
        }

        private void BuildPaginationBar()
        {
            _pnlPagination = new Panel
            {
                Height = 36,
                Visible = false, // only shown once a report is actually selected
                BackColor = ThemeHelper.IsDarkMode ? Color.FromArgb(22, 24, 29) : Color.FromArgb(249, 250, 251)
            };
            Controls.Add(_pnlPagination);

            lblRowCount = new Label
            {
                AutoSize = true,
                Font = ThemeHelper.FontSmall,
                ForeColor = ThemeHelper.TextSecondary,
                Location = new Point(4, 10)
            };
            _pnlPagination.Controls.Add(lblRowCount);

            cmbPageSize = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Size = new Size(85, 26) };
            cmbPageSize.Items.AddRange(new object[] { "25", "50", "100", "250", "500", "1000", "2000" });
            cmbPageSize.SelectedItem = "25";
            ThemeHelper.StyleComboBox(cmbPageSize);
            cmbPageSize.SelectedIndexChanged += (s, e) =>
            {
                if (int.TryParse(cmbPageSize.SelectedItem?.ToString(), out int size))
                {
                    _pageSize = size;
                    _currentPage = 1;
                    RenderReportPage();
                }
            };

            btnPrevPage = new Button { Text = "◀ Prev", Size = new Size(90, 26), Font = ThemeHelper.FontSmall };
            ThemeHelper.StyleSecondaryButton(btnPrevPage);
            btnPrevPage.Click += (s, e) => { if (_currentPage > 1) { _currentPage--; RenderReportPage(); } };

            lblPageIndicator = new Label { AutoSize = true, Font = ThemeHelper.FontSmall, ForeColor = ThemeHelper.TextSecondary, TextAlign = ContentAlignment.MiddleCenter };

            btnNextPage = new Button { Text = "Next ▶", Size = new Size(90, 26), Font = ThemeHelper.FontSmall };
            ThemeHelper.StyleSecondaryButton(btnNextPage);
            btnNextPage.Click += (s, e) => { if (_currentPage < _totalPages) { _currentPage++; RenderReportPage(); } };

            _pnlPagination.Controls.Add(cmbPageSize);
            _pnlPagination.Controls.Add(btnPrevPage);
            _pnlPagination.Controls.Add(lblPageIndicator);
            _pnlPagination.Controls.Add(btnNextPage);

            _pnlPagination.Resize += (s, e) => RepositionPaginationControls();
            RepositionPaginationControls();
        }

        // Shows the pagination bar (and correspondingly hides the watermark,
        // since they occupy the same bottom strip) once a report is selected;
        // hides it again if there's ever nothing to page through.
        private void ShowPaginationBar(bool show)
        {
            _pnlPagination.Visible = show;
            if (_lblWatermark != null) _lblWatermark.Visible = !show;

            // Re-run the preview's own resize logic (defined as a local function
            // inside BuildPreviewArea) so the grid reclaims/cedes the bottom strip.
            _preview.Size = new Size(_preview.Width,
                ClientSize.Height - _preview.Top - (show ? _pnlPagination.Height + 4 : 8));
        }

        // Clears the grid and renders only the current page's slice of
        // _reportRowBuilders — call after (re)building the full builder list.
        private void RenderReportPage()
        {
            _preview.Rows.Clear();

            _totalPages = Math.Max(1, (int)Math.Ceiling(_reportRowBuilders.Count / (double)_pageSize));
            if (_currentPage > _totalPages) _currentPage = _totalPages;

            var slice = _reportRowBuilders.Skip((_currentPage - 1) * _pageSize).Take(_pageSize);
            foreach (var build in slice) build();

            int shown = Math.Max(0, Math.Min(_pageSize, _reportRowBuilders.Count - (_currentPage - 1) * _pageSize));
            lblRowCount.Text = $"Showing {shown} of {_reportRowBuilders.Count} records";
            lblPageIndicator.Text = $"Page {_currentPage} of {_totalPages}";
            btnPrevPage.Enabled = _currentPage > 1;
            btnNextPage.Enabled = _currentPage < _totalPages;

            ShowPaginationBar(_reportRowBuilders.Count > 0);
            RepositionPaginationControls();
        }

        // Kept as the Click handler the Designer already wires up — also acts as
        // a fallback for touch/keyboard activation, since the primary trigger is
        // now hover (see SetupEquipmentDropdown).
        private void btnEquipmentReport_Click(object sender, EventArgs e)
        {
            ShowEquipmentCategoryMenu();
        }

        // ── Hover-driven category dropdown ──────────────────────────────
        // Menu opens on MouseEnter over the button and closes automatically
        // once the cursor leaves BOTH the button and the open menu — a short
        // timer debounces the gap between "left the button" and "entered the
        // menu" so it doesn't flicker shut while crossing that boundary.
    

        private void SetupEquipmentDropdown()
        {
            // Visual affordance — same idea as the dropdown caret on the
            // profile icon in the top-right corner.
            if (!btnEquipmentReport.Text.TrimEnd().EndsWith("▾"))
                btnEquipmentReport.Text = btnEquipmentReport.Text.TrimEnd() + "  ▾";

            _equipmentMenuCloseTimer = new System.Windows.Forms.Timer { Interval = 150 };
            _equipmentMenuCloseTimer.Tick += (s, e) =>
            {
                _equipmentMenuCloseTimer!.Stop();
                if (_equipmentMenu == null || !_equipmentMenu.Visible) return;

                var cursorPos = Cursor.Position;
                bool overButton = btnEquipmentReport.RectangleToScreen(btnEquipmentReport.ClientRectangle).Contains(cursorPos);
                bool overMenu = _equipmentMenu.Bounds.Contains(cursorPos);

                if (!overButton && !overMenu)
                    _equipmentMenu.Close();
            };

            btnEquipmentReport.MouseEnter += (s, e) => ShowEquipmentCategoryMenu();
            btnEquipmentReport.MouseLeave += (s, e) => _equipmentMenuCloseTimer!.Start();
        }

        private void ShowEquipmentCategoryMenu()
        {
            _equipmentMenuCloseTimer?.Stop();
            if (_equipmentMenu != null && _equipmentMenu.Visible) return;

            _equipmentMenu = BuildEquipmentCategoryMenu();
            _equipmentMenu.MouseEnter += (s, e) => _equipmentMenuCloseTimer!.Stop();
            _equipmentMenu.MouseLeave += (s, e) => _equipmentMenuCloseTimer!.Start();
            _equipmentMenu.Closed += (s, e) => _equipmentMenu = null;

            _equipmentMenu.Show(btnEquipmentReport, new Point(0, btnEquipmentReport.Height));
        }

        private ContextMenuStrip BuildEquipmentCategoryMenu()
        {
            var menu = new ContextMenuStrip
            {
                Font = ThemeHelper.FontDefault,
                BackColor = ThemeHelper.CardBg,
                ForeColor = ThemeHelper.TextPrimary
            };

            // Load categories matching EquipmentForm.LoadCategoryFilter() union & sorting logic
            List<string> categories = new List<string>();
            try
            {
                using var db = new InventoryDbContext();
                var dbCategories = db.Equipment
                    .Where(eq => eq.Category != null && eq.Category != "")
                    .Select(eq => eq.Category!)
                    .Distinct()
                    .ToList();

                var categoryNames = new CategoryService().GetAll().Data?
                    .Select(c => c.Name) ?? Enumerable.Empty<string>();

                categories = categoryNames
                    .Union(dbCategories, StringComparer.OrdinalIgnoreCase)
                    .OrderBy(c => c, StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }
            catch
            {
                categories = (new CategoryService().GetAll().Data?.Select(c => c.Name) ?? Enumerable.Empty<string>())
                    .OrderBy(c => c, StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }

            _activeEquipmentCategory = "All Categories";

            var allCatItem = new ToolStripMenuItem("All Categories");
            allCatItem.Click += (s, ev) => { LoadEquipmentReport("All Categories"); _equipmentMenu?.Close(); };
            menu.Items.Add(allCatItem);

            foreach (var cat in categories)
            {
                var item = new ToolStripMenuItem(cat);
                item.Click += (s, ev) => { LoadEquipmentReport(cat); _equipmentMenu?.Close(); };
                menu.Items.Add(item);
            }

            return menu;
        }

        private void LoadEquipmentReport(string category)
        {
            _currentReport = "Equipment";
            _previewHeaderColor = null;
            _activeEquipmentCategory = category;
            _previewTitle.Text = category == "All Categories"
             ? $"Equipment Inventory Report — {_companyName}"
             : $"Equipment Inventory Report - {category} — {_companyName}";

            var result = _equipmentService.GetAll();
            if (!result.Success)
            {
                ToastHelper.Show(this, result.ErrorMessage, ToastType.Error);
                return;
            }

            _preview.Columns.Clear();
            _preview.Rows.Clear();
            _preview.Columns.Add("Code", "Equipment Code");
            _preview.Columns.Add("QRCode", "QR Code");
            _preview.Columns.Add("Name", "Name");
            _preview.Columns.Add("Make", "Make");
            _preview.Columns.Add("Model", "Model");
            _preview.Columns.Add("Serial", "Serial Number");
            _preview.Columns.Add("Status", "Status");
            _preview.Columns.Add("Location", "Location");
            _preview.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            var equipmentList = result.Data ?? new List<Equipment>();

            if (category != "All Categories")
            {
                equipmentList = equipmentList
                    .Where(e => string.Equals(e.Category, category, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            _reportRowBuilders = equipmentList.Select(item => (Action)(() =>
            {
                string location = ReturnsForm.FormatLocation(
                    item.Allocations.FirstOrDefault(a => a.Status == "approved")?.Location) ?? "—";

                int rowIdx = _preview.Rows.Add(
                    item.EquipmentCode, item.QrCode ?? "—", item.Name, item.Make ?? "—", item.Model ?? "—",
                    item.SerialNumber, item.CurrentStatus, location);

                var statusCell = _preview.Rows[rowIdx].Cells["Status"];
                statusCell.Style.ForeColor = item.CurrentStatus switch
                {
                    "in_store" => ThemeHelper.Success,
                    "allocated" => ThemeHelper.Warning,
                    "under_repair" => Color.FromArgb(255, 112, 67),
                    "decommissioned" => ThemeHelper.Danger,
                    _ => ThemeHelper.TextPrimary
                };
                statusCell.Style.Font = ThemeHelper.FontBold;
            })).ToList();

            _currentPage = 1;
            RenderReportPage();
        }

        private void btnEmployeeReport_Click(object sender, EventArgs e)
        {
            _currentReport = "Employee";
            _previewHeaderColor = null;
            _previewTitle.Text = $"Employee Allocation Summary — {_companyName}";

            var empResult = _employeeService.GetAll();
            var allocResult = _allocationService.GetAll();
            if (!empResult.Success || !allocResult.Success)
            {
               ToastHelper.Show(this, "Could not load employee/allocation data.", ToastType.Error);

                return;
            }

            _preview.Columns.Clear();
            _preview.Rows.Clear();
            _preview.Columns.Add("EmpCode", "Employee Code");
            _preview.Columns.Add("Employee", "Employee");
            _preview.Columns.Add("Designation", "Designation");
            _preview.Columns.Add("Email", "Email");
            _preview.Columns.Add("Phone", "Phone");
            _preview.Columns.Add("ActiveCount", "Items Currently Held");
            _preview.Columns.Add("TotalCount", "Total Ever Allocated");
            _preview.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            var allocations = allocResult.Data ?? new List<Allocation>();

            _reportRowBuilders = (empResult.Data ?? new List<Employee>())
                .Where(e => e.IsActive)
                .Select(emp =>
                {
                    var empAllocs = allocations.Where(a => a.EmployeeId == emp.Id).ToList();
                    int activeCount = empAllocs.Count(a => a.Status == "approved");
                    int totalCount = empAllocs.Count;
                    return (emp, activeCount, totalCount);
                })
                .Where(x => x.totalCount > 0) // skip employees with no history
                .Select(x => (Action)(() =>
                {
                    _preview.Rows.Add(x.emp.EmployeeCode ?? "—", x.emp.FullName, x.emp.Department ?? "—",
                        x.emp.Email ?? "—", x.emp.Phone ?? "—", x.activeCount, x.totalCount);
                }))
                .ToList();

            _currentPage = 1;
            RenderReportPage();
        }

        private void btnAllocationReport_Click(object sender, EventArgs e)
        {
            _currentReport = "Allocation";
            _previewHeaderColor = ThemeHelper.Header;
            _previewTitle.Text = $"Active Allocations Report — {_companyName}";

            var result = _allocationService.GetAll();
            if (!result.Success)
            {
                ToastHelper.Show(this, result.ErrorMessage, ToastType.Error);

                return;
            }

            _preview.Columns.Clear();
            _preview.Rows.Clear();
            _preview.Columns.Add("Code", "Equipment Code");
            _preview.Columns.Add("QRCode", "QR Code");
            _preview.Columns.Add("Name", "Equipment Name");
            _preview.Columns.Add("Employee", "Employee");
            _preview.Columns.Add("Location", "Location");
            _preview.Columns.Add("Date", "Allocated On");
            _preview.Columns.Add("Expected", "Expected Return");
            _preview.Columns.Add("Status", "Status");
            _preview.Columns.Add("Source", "Source");
            _preview.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            _reportRowBuilders = (result.Data ?? new List<Allocation>())
         .Where(a => a.Status == "approved" || a.Status == "pending")
         .Select(item => (Action)(() =>
         {
             string fullLocation = ReturnsForm.FormatLocation(item.Location);

             int rowIdx = _preview.Rows.Add(
                 item.Equipment?.EquipmentCode ?? "—",
                 item.Equipment?.QrCode ?? "—",
                 item.Equipment?.Name ?? "—",
                 item.Employee?.FullName ?? "—",
                 fullLocation,
                 item.AllocationDate.ToString("yyyy-MM-dd"),
                 item.ExpectedReturnDate?.ToString("yyyy-MM-dd") ?? "—",
                 item.Status,
                 "Allocation");

             var statusCell = _preview.Rows[rowIdx].Cells["Status"];
             statusCell.Style.ForeColor = item.Status == "approved" ? ThemeHelper.Success : ThemeHelper.Warning;
             statusCell.Style.Font = ThemeHelper.FontBold;
         }))
         .ToList();

            _currentPage = 1;
            RenderReportPage();
        }

        private void btnReturnReport_Click(object sender, EventArgs e)
        {
            _currentReport = "Return";
            _previewHeaderColor = Color.FromArgb(99, 102, 241);
            _previewTitle.Text = $"Returns History Report — {_companyName}";

            try
            {
            _preview.Columns.Clear();
            _preview.Rows.Clear();
            _preview.Columns.Add("QRCode", "QR Code");
            _preview.Columns.Add("Code", "Equipment Code");
            _preview.Columns.Add("Name", "Equipment Name");
            _preview.Columns.Add("Employee", "Employee");
            _preview.Columns.Add("Location", "Location");
            _preview.Columns.Add("Returned", "Return Date");
            _preview.Columns.Add("Condition", "Condition");
            _preview.Columns.Add("Source", "Source");
            _preview.Columns.Add("Overdue", "Overdue");
            _preview.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            // Load allocation-based returns
            using var db = new InventoryDbContext();
            var returnedAllocations = db.Allocations
                .Where(a => a.Status == "returned")
                .Include(a => a.Equipment)
                .Include(a => a.Employee)
                .Include(a => a.Location)
                .OrderByDescending(a => a.ActualReturnDate)
                .ToList();

                var builders = new List<Action>();

                foreach (var item in returnedAllocations)
                {
                    builders.Add(() =>
                    {
                        bool isOverdue = item.ExpectedReturnDate.HasValue && item.ActualReturnDate.HasValue &&
                                         item.ActualReturnDate.Value > item.ExpectedReturnDate.Value;
                        string fullLocation = ReturnsForm.FormatLocation(item.Location);

                        int rowIdx = _preview.Rows.Add(
                            item.Equipment?.QrCode ?? "—",
                            item.Equipment?.EquipmentCode ?? "—",
                            item.Equipment?.Name ?? "—",
                            item.Employee?.FullName ?? "—",
                            fullLocation,
                            item.ActualReturnDate?.ToString("yyyy-MM-dd") ?? "—",
                            item.Equipment?.ConditionGrade ?? "—",
                            "Allocation",
                            isOverdue ? "Yes" : "No");

                        var overdueCell = _preview.Rows[rowIdx].Cells["Overdue"];
                        overdueCell.Style.Font = ThemeHelper.FontBold;
                        overdueCell.Style.ForeColor = isOverdue ? ThemeHelper.Danger : ThemeHelper.TextSecondary;

                        var sourceCell = _preview.Rows[rowIdx].Cells["Source"];
                        sourceCell.Style.ForeColor = Color.FromArgb(107, 114, 128);
                    });
                }

                // Load Check In/Out completed returns
                var completedCheckIns = db.Transactions
                    .Include(t => t.Equipment)
                    .Where(t => t.Status == "Completed" && t.ActualReturn.HasValue && t.TransactionType == "Check-Out")
                    .OrderByDescending(t => t.ActualReturn)
                    .ToList();

                foreach (var t in completedCheckIns)
                {
                    builders.Add(() =>
                    {
                        bool isOverdue = t.ExpectedReturn.HasValue && t.ActualReturn.HasValue &&
                                         t.ActualReturn.Value > t.ExpectedReturn.Value;

                        int rowIdx = _preview.Rows.Add(
                            t.Equipment?.QrCode ?? "—",
                            t.Equipment?.EquipmentCode ?? "—",
                            t.Equipment?.Name ?? "—",
                            string.IsNullOrWhiteSpace(t.IssuedTo) ? "—" : t.IssuedTo,
                            string.IsNullOrWhiteSpace(t.Department) ? "—" : t.Department,
                            t.ActualReturn?.ToString("yyyy-MM-dd") ?? "—",
                            t.ConditionAfter ?? "—",
                            "Check In/Out",
                            isOverdue ? "Yes" : "No");

                        var overdueCell = _preview.Rows[rowIdx].Cells["Overdue"];
                        overdueCell.Style.Font = ThemeHelper.FontBold;
                        overdueCell.Style.ForeColor = isOverdue ? ThemeHelper.Danger : ThemeHelper.TextSecondary;

                        var sourceCell = _preview.Rows[rowIdx].Cells["Source"];
                        sourceCell.Style.ForeColor = Color.FromArgb(25, 118, 210);
                        sourceCell.Style.Font = ThemeHelper.FontBold;
                    });
                }

                _reportRowBuilders = builders;
                _currentPage = 1;
                RenderReportPage();
            }
            catch (Exception ex)
            {
                ToastHelper.Show(this, "Error loading returns report: " + ex.Message, ToastType.Error);
            }
        }

        // ── Export: Excel (ClosedXML — already used elsewhere in project) ──
        private void btnExportExcel_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_currentReport) || _preview.Columns.Count == 0)
            {
               ToastHelper.Show(this, "Please select a report first.", ToastType.Info);

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

                ws.Cell(1, 1).Value = _companyName;
                ws.Range(1, 1, 1, _preview.Columns.Count).Merge();
                ws.Cell(1, 1).Style.Font.Bold = true;
                ws.Cell(1, 1).Style.Font.FontSize = 13;

                for (int c = 0; c < _preview.Columns.Count; c++)
                    ws.Cell(2, c + 1).Value = _preview.Columns[c].HeaderText;

                var headerRange = ws.Range(2, 1, 2, _preview.Columns.Count);
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = XLColor.FromArgb(46, 125, 50);
                headerRange.Style.Font.FontColor = XLColor.White;

                for (int r = 0; r < _preview.Rows.Count; r++)
                    for (int c = 0; c < _preview.Columns.Count; c++)
                        ws.Cell(r + 3, c + 1).Value = _preview.Rows[r].Cells[c].Value?.ToString() ?? "";

                ws.Columns().AdjustToContents();
                wb.SaveAs(sfd.FileName);

                ToastHelper.Show(this, "Report exported successfully.", ToastType.Success);

            }
            catch (Exception ex)
            {
               ToastHelper.Show(this, "Export failed: " + ex.Message, ToastType.Error);

            }
        }


        private void btnCheckInOutReport_Click(object sender, EventArgs e)
        {
            _currentReport = "CheckInOut";
            _previewHeaderColor = Color.FromArgb(33, 150, 243);
            _previewTitle.Text = $"Check In/Out History Report — {_companyName}";

            try
            {
                _preview.Columns.Clear();
                _preview.Rows.Clear();
                _preview.Columns.Add("EquipmentCode", "Equipment Code");
                _preview.Columns.Add("EquipmentName", "Equipment Name");
                _preview.Columns.Add("Type", "Type");
                _preview.Columns.Add("IssuedTo", "Issued To");
                _preview.Columns.Add("Department", "Department");
                _preview.Columns.Add("Operator", "Operator");
                _preview.Columns.Add("ExpectedReturn", "Expected Return");
                _preview.Columns.Add("Status", "Status");
                _preview.Columns.Add("ConditionBefore", "Condition Before");
                _preview.Columns.Add("ConditionAfter", "Condition After");
                _preview.Columns.Add("Date", "Date");
                _preview.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

                using var db = new InventoryDbContext();
                var transactions = db.Transactions
                    .Include(t => t.Equipment)
                    .OrderByDescending(t => t.TransactionDate)
                    .ToList();

                var checkInTxEquipmentIds = transactions
                    .Where(t => string.Equals(t.TransactionType, "Check-In", StringComparison.OrdinalIgnoreCase))
                    .Select(t => t.EquipmentId)
                    .ToHashSet();

                var entries = new List<(Transaction Tx, bool IsCheckIn)>();
                foreach (var t in transactions)
                {
                    if (string.Equals(t.TransactionType, "Check-In", StringComparison.OrdinalIgnoreCase))
                    {
                        entries.Add((t, true));
                    }
                    else
                    {
                        entries.Add((t, false));
                        if (t.ActualReturn.HasValue && !checkInTxEquipmentIds.Contains(t.EquipmentId))
                            entries.Add((t, true));
                    }
                }

                entries = entries
                    .OrderByDescending(en => en.IsCheckIn
                        ? (en.Tx.ReturnProcessedAt ?? en.Tx.ActualReturn!.Value.ToDateTime(TimeOnly.MinValue))
                        : en.Tx.TransactionDate)
                    .ToList();

                _reportRowBuilders = entries.Select(en => (Action)(() =>
                {
                    var t = en.Tx;
                    string typeLabel = en.IsCheckIn ? "Check-In" : (t.TransactionType ?? "—");
                    string dateStr = en.IsCheckIn
                        ? t.ActualReturn!.Value.ToDateTime(TimeOnly.MinValue).ToString("yyyy-MM-dd")
                        : t.TransactionDate.ToString("yyyy-MM-dd HH:mm");
                    string statusStr = en.IsCheckIn ? "Completed" : (t.Status ?? "—");

                    int rowIdx = _preview.Rows.Add(
                        t.Equipment?.EquipmentCode ?? "—",
                        t.Equipment?.Name ?? "—",
                        typeLabel,
                        t.IssuedTo ?? "—",
                        t.Department ?? "—",
                        t.OperatorName ?? "—",
                        t.ExpectedReturn?.ToString("yyyy-MM-dd") ?? "—",
                        statusStr,
                        t.ConditionBefore ?? "—",
                        t.ConditionAfter ?? "—",
                        dateStr);

                    var typeCell = _preview.Rows[rowIdx].Cells["Type"];
                    typeCell.Style.Font = ThemeHelper.FontBold;
                    typeCell.Style.ForeColor = typeLabel switch
                    {
                        "Check-Out" => Color.FromArgb(25, 118, 210),
                        "Check-In" => ThemeHelper.Success,
                        _ => ThemeHelper.TextPrimary
                    };

                    var statusCell = _preview.Rows[rowIdx].Cells["Status"];
                    statusCell.Style.Font = ThemeHelper.FontBold;
                    statusCell.Style.ForeColor = statusStr switch
                    {
                        "Pending" => ThemeHelper.Warning,
                        "Completed" => ThemeHelper.Success,
                        "Overdue" => ThemeHelper.Danger,
                        _ => ThemeHelper.TextPrimary
                    };
                })).ToList();

                _currentPage = 1;
                RenderReportPage();
            }
            catch (Exception ex)
            {
                ToastHelper.Show(this, "Error loading check in/out report: " + ex.Message, ToastType.Error);
            }
        }


        // Writes an actual .pdf file to disk via QuestPDF, matching the behavior
        // of "Export Excel" (which really does save an .xlsx) instead of only
        // opening a print preview dialog.
        private void btnExportPDF_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_currentReport) || _preview.Columns.Count == 0)
            {
                ToastHelper.Show(this, "Please select a report first.", ToastType.Info);
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

                int colCount = _preview.Columns.Count;
                var headers = _preview.Columns.Cast<DataGridViewColumn>().Select(c => c.HeaderText).ToList();
                var rows = _preview.Rows.Cast<DataGridViewRow>()
                    .Select(r => r.Cells.Cast<DataGridViewCell>().Select(c => c.Value?.ToString() ?? "").ToList())
                    .ToList();
                string reportTitle = _currentReport;
                string company = _companyName;
                string generatedOn = DateTime.Now.ToString("yyyy-MM-dd HH:mm");

                Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4.Landscape());
                        page.Margin(30);
                        page.DefaultTextStyle(x => x.FontSize(9));

                        page.Header().Column(col =>
                        {
                            col.Item().Text($"{reportTitle} Report").FontSize(16).Bold();
                            col.Item().Text(company).FontSize(10).FontColor(QuestPDF.Helpers.Colors.Grey.Darken1);
                            col.Item().PaddingBottom(8).Text($"Generated: {generatedOn}").FontSize(8).FontColor(QuestPDF.Helpers.Colors.Grey.Medium);
                        });

                        page.Content().Table(table =>
                        {
                            table.ColumnsDefinition(cols =>
                            {
                                for (int c = 0; c < colCount; c++)
                                    cols.RelativeColumn();
                            });

                            table.Header(header =>
                            {
                                foreach (var h in headers)
                                {
                                    header.Cell().Background(QuestPDF.Helpers.Colors.Green.Darken2)
                                        .Padding(4)
                                        .Text(h).FontColor(QuestPDF.Helpers.Colors.White).Bold();
                                }
                                header.Cell().ColumnSpan((uint)colCount).PaddingTop(1).BorderBottom(1)
                                    .BorderColor(QuestPDF.Helpers.Colors.Grey.Lighten1);
                            });

                            bool alt = false;
                            foreach (var row in rows)
                            {
                                var bg = alt ? QuestPDF.Helpers.Colors.Grey.Lighten4 : QuestPDF.Helpers.Colors.White;
                                foreach (var cell in row)
                                {
                                    table.Cell().Background(bg).Padding(4).Text(cell);
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

                ToastHelper.Show(this, "Report exported successfully.", ToastType.Success);
            }
            catch (Exception ex)
            {
                ToastHelper.Show(this, "Export failed: " + ex.Message, ToastType.Error);
            }
        }

        // ── Actual printer step — errors ONLY here if no printer is connected,
        // exactly matching the original QR-sheet printer-missing behavior.

    }
    }