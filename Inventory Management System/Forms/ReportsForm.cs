// Forms/ReportsForm.cs
using ClosedXML.Excel;
using Inventory_Management_System.Helpers;
using Inventory_Management_System.Models;
using Inventory_Management_System.Services;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;using System;
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
    public partial class ReportsForm : Form

    {
        private readonly EquipmentService _equipmentService = new();
        private readonly EmployeeService _employeeService = new();
        private readonly AllocationService _allocationService = new();

        // Currently selected report — drives both the on-screen preview grid
        // and what gets exported when PDF/Excel buttons are clicked.
        private string _currentReport = "";
        private string _activeEquipmentCategory = "All Categories";
        private DataGridView _preview = null!;
        private Label _previewTitle = null!;
        private Label _lblWatermark = null!;
        private string _companyName = "Q-Inventory";

        public ReportsForm()
        {
            InitializeComponent();
            ApplyTheme();
            BuildPreviewArea();
            LoadCompanyName();
        }

        private void LoadCompanyName()
        {
            try
            {
                using var db = new InventoryDbContext();
                _companyName = db.AppSettings.FirstOrDefault()?.CompanyName ?? "Q-Inventory";
            }
            catch { /* fall back to default already set */ }
        }

        private void ApplyTheme()
        {
            ThemeHelper.ApplyFormBackground(this);
            ThemeHelper.StyleLabel(lblReportType, bold: true, color: ThemeHelper.Accent);
           

            ThemeHelper.StyleButton(btnEquipmentReport, Color.FromArgb(94, 53, 177));
            ThemeHelper.StyleButton(btnEmployeeReport, Color.FromArgb(123, 31, 162));
            ThemeHelper.StyleButton(btnAllocationReport, Color.FromArgb(245, 124, 0));
            ThemeHelper.StyleButton(btnReturnReport, Color.FromArgb(255, 160, 0));

            ThemeHelper.StyleSecondaryButton(btnExportPDF);
            ThemeHelper.StyleSecondaryButton(btnExportExcel);
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
                _preview.Size = new Size(w, ClientSize.Height - _preview.Top - 8);
            }

            panelTop.SizeChanged += (s, e) => RepositionPreview();
            Resize             += (s, e) => RepositionPreview();
            Shown              += (s, e) => RepositionPreview();
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

        private void PositionWatermark()
        {
            if (_lblWatermark == null) return;
            const int margin = 12;
            _lblWatermark.Location = new Point(
                ClientSize.Width - _lblWatermark.Width - margin,
                ClientSize.Height - _lblWatermark.Height - margin);
        }

        private void btnEquipmentReport_Click(object sender, EventArgs e)
        {
            // Build the ContextMenuStrip
            var menu = new ContextMenuStrip
            {
                Font = ThemeHelper.FontDefault,
                BackColor = Color.White,
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

            // Default to "All Categories" before displaying category menu
            _activeEquipmentCategory = "All Categories";

            // Add "All Categories" item
            var allCatItem = new ToolStripMenuItem("All Categories");
            allCatItem.Click += (s, ev) => LoadEquipmentReport("All Categories");
            menu.Items.Add(allCatItem);

            // Add other category items
            foreach (var cat in categories)
            {
                var item = new ToolStripMenuItem(cat);
                item.Click += (s, ev) => LoadEquipmentReport(cat);
                menu.Items.Add(item);
            }

            // Show menu below the button
            menu.Show(btnEquipmentReport, new Point(0, btnEquipmentReport.Height));
        }

        private void LoadEquipmentReport(string category)
        {
            _currentReport = "Equipment";
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

            foreach (var item in equipmentList)
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
            }
        }

        private void btnEmployeeReport_Click(object sender, EventArgs e)
        {
            _currentReport = "Employee";
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

            foreach (var emp in (empResult.Data ?? new List<Employee>()).Where(e => e.IsActive))
            {
                var empAllocs = allocations.Where(a => a.EmployeeId == emp.Id).ToList();
                int activeCount = empAllocs.Count(a => a.Status == "approved");
                int totalCount = empAllocs.Count;

                if (totalCount == 0) continue; // skip employees with no history

                _preview.Rows.Add(emp.EmployeeCode ?? "—", emp.FullName, emp.Department ?? "—", emp.Email ?? "—", emp.Phone ?? "—", activeCount, totalCount);
            }
        }

        private void btnAllocationReport_Click(object sender, EventArgs e)
        {
            _currentReport = "Allocation";
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

            foreach (var item in (result.Data ?? new List<Allocation>())
                                  .Where(a => a.Status == "approved" || a.Status == "pending"))
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
            }
        }

        private void btnReturnReport_Click(object sender, EventArgs e)
        {
            _currentReport = "Return";
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

            foreach (var item in returnedAllocations)
            {
                var returnDate = item.ActualReturnDate.HasValue
                    ? item.ActualReturnDate.Value.ToDateTime(TimeOnly.MinValue)
                    : DateTime.Today;
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
            }

            // Load Check In/Out completed returns
            var completedCheckIns = db.Transactions
                .Include(t => t.Equipment)
                .Where(t => t.Status == "Completed" && t.ActualReturn.HasValue && t.TransactionType == "Check-Out")
                .OrderByDescending(t => t.ActualReturn)
                .ToList();

            foreach (var t in completedCheckIns)
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
            }
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





        private void btnExportPDF_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_currentReport) || _preview.Columns.Count == 0)
            {
                ToastHelper.Show(this, "Please select a report first.", ToastType.Info);

                return;
            }

            try
            {
                var pd = new PrintDocument();
                int currentRow = 0;

                pd.BeginPrint += (s, ev) => { currentRow = 0; };

                pd.PrintPage += (s, ev) =>
                {
                    var g = ev.Graphics!;
                    float y = 40f;
                    float leftMargin = 40f;
                    int colCount = _preview.Columns.Count;
                    float pageWidth = ev.PageBounds.Width - 80f;
                    float colWidth = pageWidth / colCount;

                    using var titleFont = new Font("Segoe UI", 16, FontStyle.Bold);
                    using var headerFont = new Font("Segoe UI", 9, FontStyle.Bold);
                    using var rowFont = new Font("Segoe UI", 9, FontStyle.Regular);

                    g.DrawString($"{_currentReport} Report", titleFont, Brushes.Black, leftMargin, y);
                    y += 22;
                    g.DrawString(_companyName, headerFont, Brushes.DimGray, leftMargin, y);
                    y += 22;
                    g.DrawString($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm}", rowFont, Brushes.Gray, leftMargin, y);
                    y += 28;

                    float x = leftMargin;
                    for (int c = 0; c < colCount; c++)
                    {
                        g.DrawString(_preview.Columns[c].HeaderText, headerFont, Brushes.Black,
                            new RectangleF(x, y, colWidth - 4, 20));
                        x += colWidth;
                    }
                    y += 22;
                    g.DrawLine(Pens.Black, leftMargin, y, leftMargin + pageWidth, y);
                    y += 6;

                    while (currentRow < _preview.Rows.Count)
                    {
                        if (y > ev.PageBounds.Height - 60)
                        {
                            ev.HasMorePages = true;
                            return;
                        }

                        x = leftMargin;
                        var row = _preview.Rows[currentRow];
                        for (int c = 0; c < colCount; c++)
                        {
                            string val = row.Cells[c].Value?.ToString() ?? "";
                            g.DrawString(val, rowFont, Brushes.Black,
                                new RectangleF(x, y, colWidth - 4, 18));
                            x += colWidth;
                        }
                        y += 20;
                        currentRow++;
                    }

                    ev.HasMorePages = false;
                };

                using var ppd = new PrintPreviewDialog
                {
                    Document = pd,
                    Width = 900,
                    Height = 700,
                    StartPosition = FormStartPosition.CenterParent
                };
                ppd.ShowDialog();
            }
            catch (Exception ex)
            {
               ToastHelper.Show(this, "Could not generate print preview: " + ex.Message, ToastType.Error);
            }
        }

        // ── Actual printer step — errors ONLY here if no printer is connected,
        // exactly matching the original QR-sheet printer-missing behavior.
       
    }
    }