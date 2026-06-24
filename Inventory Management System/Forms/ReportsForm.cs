// Forms/ReportsForm.cs
using ClosedXML.Excel;
using Inventory_Management_System.Helpers;
using Inventory_Management_System.Models;
using Inventory_Management_System.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.Linq;
using System.Windows.Forms;

namespace Inventory_Management_System.Forms
{
    public partial class ReportsForm : Form
    {
        private readonly EquipmentService  _equipmentService  = new();
        private readonly EmployeeService   _employeeService   = new();
        private readonly AllocationService _allocationService = new();

        // Currently selected report — drives both the on-screen preview grid
        // and what gets exported when PDF/Excel buttons are clicked.
        private string _currentReport = "";
        private DataGridView _preview = null!;
        private Label _previewTitle = null!;

        public ReportsForm()
        {
            InitializeComponent();
            ApplyTheme();
            BuildPreviewArea();
        }

        private void ApplyTheme()
        {
            ThemeHelper.ApplyFormBackground(this);
            ThemeHelper.StyleLabel(lblReportType, bold: true, color: ThemeHelper.Accent);
            ThemeHelper.StyleLabel(lblExport,     bold: true, color: ThemeHelper.Accent);

            ThemeHelper.StyleButton(btnEquipmentReport);
            ThemeHelper.StyleButton(btnEmployeeReport);
            ThemeHelper.StyleButton(btnAllocationReport);
            ThemeHelper.StyleButton(btnReturnReport);

            ThemeHelper.StyleSecondaryButton(btnExportPDF);
            ThemeHelper.StyleSecondaryButton(btnExportExcel);
        }

        // ── Preview grid — built in code since the Designer only has buttons ──
        private void BuildPreviewArea()
        {
            _previewTitle = new Label
            {
                Location  = new Point(60, 330),
                AutoSize  = true,
                Text      = "Select a report above to preview it here.",
                Font      = ThemeHelper.FontBold,
                ForeColor = ThemeHelper.TextPrimary
            };
            Controls.Add(_previewTitle);

            _preview = new DataGridView
            {
                Location = new Point(60, 360),
                Size     = new Size(1080, 300),
                Anchor   = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
                AllowUserToAddRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };
            ThemeHelper.StyleDataGridView(_preview);
            Controls.Add(_preview);
            _preview.BringToFront();
        }

        // ── Report builders ─────────────────────────────────────────────
        private void btnEquipmentReport_Click(object sender, EventArgs e)
        {
            _currentReport = "Equipment";
            _previewTitle.Text = "Equipment Inventory Report";

            var result = _equipmentService.GetAll();
            if (!result.Success)
            {
                MessageBox.Show(result.ErrorMessage, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            _preview.Columns.Clear();
            _preview.Rows.Clear();
            _preview.Columns.Add("Code",     "Equipment Code");
            _preview.Columns.Add("Name",     "Name");
            _preview.Columns.Add("Make",     "Make");
            _preview.Columns.Add("Model",    "Model");
            _preview.Columns.Add("Serial",   "Serial Number");
            _preview.Columns.Add("Status",   "Status");
            _preview.Columns.Add("Location", "Location");
            _preview.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            foreach (var item in result.Data ?? new List<Equipment>())
            {
                string location = item.Allocations
                    .FirstOrDefault(a => a.Status == "approved")?.Location?.LocationName ?? "—";

                int rowIdx = _preview.Rows.Add(
                    item.EquipmentCode, item.Name, item.Make ?? "—", item.Model ?? "—",
                    item.SerialNumber, item.CurrentStatus, location);

                var statusCell = _preview.Rows[rowIdx].Cells["Status"];
                statusCell.Style.ForeColor = item.CurrentStatus switch
                {
                    "in_store"       => ThemeHelper.Success,
                    "allocated"      => ThemeHelper.Warning,
                    "under_repair"   => Color.FromArgb(255, 112, 67),
                    "decommissioned" => ThemeHelper.Danger,
                    _                => ThemeHelper.TextPrimary
                };
                statusCell.Style.Font = ThemeHelper.FontBold;
            }
        }

        private void btnEmployeeReport_Click(object sender, EventArgs e)
        {
            _currentReport = "Employee";
            _previewTitle.Text = "Employee Allocation Summary";

            var empResult = _employeeService.GetAll();
            var allocResult = _allocationService.GetAll();
            if (!empResult.Success || !allocResult.Success)
            {
                MessageBox.Show("Could not load employee/allocation data.", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            _preview.Columns.Clear();
            _preview.Rows.Clear();
            _preview.Columns.Add("Employee",   "Employee");
            _preview.Columns.Add("Department", "Department");
            _preview.Columns.Add("ActiveCount","Items Currently Held");
            _preview.Columns.Add("TotalCount", "Total Ever Allocated");
            _preview.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            var allocations = allocResult.Data ?? new List<Allocation>();

            foreach (var emp in (empResult.Data ?? new List<Employee>()).Where(e => e.IsActive))
            {
                var empAllocs = allocations.Where(a => a.EmployeeId == emp.Id).ToList();
                int activeCount = empAllocs.Count(a => a.Status == "approved");
                int totalCount  = empAllocs.Count;

                if (totalCount == 0) continue; // skip employees with no history

                _preview.Rows.Add(emp.FullName, emp.Department ?? "—", activeCount, totalCount);
            }
        }

        private void btnAllocationReport_Click(object sender, EventArgs e)
        {
            _currentReport = "Allocation";
            _previewTitle.Text = "Active Allocations Report";

            var result = _allocationService.GetAll();
            if (!result.Success)
            {
                MessageBox.Show(result.ErrorMessage, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            _preview.Columns.Clear();
            _preview.Rows.Clear();
            _preview.Columns.Add("Code",     "Equipment Code");
            _preview.Columns.Add("Name",     "Equipment Name");
            _preview.Columns.Add("Employee", "Allocated To");
            _preview.Columns.Add("Location", "Location");
            _preview.Columns.Add("Date",     "Allocated On");
            _preview.Columns.Add("Expected", "Expected Return");
            _preview.Columns.Add("Status",   "Status");
            _preview.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            foreach (var item in (result.Data ?? new List<Allocation>())
                                  .Where(a => a.Status == "approved" || a.Status == "pending"))
            {
                int rowIdx = _preview.Rows.Add(
                    item.Equipment?.EquipmentCode ?? "—",
                    item.Equipment?.Name          ?? "—",
                    item.Employee?.FullName        ?? "—",
                    item.Location?.LocationName    ?? "—",
                    item.AllocationDate.ToString("yyyy-MM-dd"),
                    item.ExpectedReturnDate?.ToString("yyyy-MM-dd") ?? "—",
                    item.Status);

                var statusCell = _preview.Rows[rowIdx].Cells["Status"];
                statusCell.Style.ForeColor = item.Status == "approved" ? ThemeHelper.Success : ThemeHelper.Warning;
                statusCell.Style.Font = ThemeHelper.FontBold;
            }
        }

        private void btnReturnReport_Click(object sender, EventArgs e)
        {
            _currentReport = "Return";
            _previewTitle.Text = "Returns History Report";

            var result = _allocationService.GetAll();
            if (!result.Success)
            {
                MessageBox.Show(result.ErrorMessage, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            _preview.Columns.Clear();
            _preview.Rows.Clear();
            _preview.Columns.Add("Code",     "Equipment Code");
            _preview.Columns.Add("Name",     "Equipment Name");
            _preview.Columns.Add("Employee", "Returned By");
            _preview.Columns.Add("Allocated","Allocated On");
            _preview.Columns.Add("Returned", "Return Date");
            _preview.Columns.Add("Duration", "Duration (days)");
            _preview.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            foreach (var item in (result.Data ?? new List<Allocation>()).Where(a => a.Status == "returned"))
            {
                var returnDate = item.ActualReturnDate.HasValue
                    ? item.ActualReturnDate.Value.ToDateTime(TimeOnly.MinValue)
                    : DateTime.Today;
                int duration = (returnDate - item.AllocationDate).Days;

                int rowIdx = _preview.Rows.Add(
                    item.Equipment?.EquipmentCode ?? "—",
                    item.Equipment?.Name          ?? "—",
                    item.Employee?.FullName        ?? "—",
                    item.AllocationDate.ToString("yyyy-MM-dd"),
                    item.ActualReturnDate?.ToString("yyyy-MM-dd") ?? "—",
                    duration);

                if (duration > 30)
                    _preview.Rows[rowIdx].Cells["Duration"].Style.ForeColor = ThemeHelper.Warning;
            }
        }

        // ── Export: Excel (ClosedXML — already used elsewhere in project) ──
        private void btnExportExcel_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_currentReport) || _preview.Columns.Count == 0)
            {
                MessageBox.Show("Please select a report first.", "No Report Selected",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var sfd = new SaveFileDialog
            {
                Filter   = "Excel Files|*.xlsx",
                FileName = $"{_currentReport}_Report_{DateTime.Now:yyyyMMdd_HHmm}.xlsx"
            };
            if (sfd.ShowDialog() != DialogResult.OK) return;

            try
            {
                using var wb = new XLWorkbook();
                var ws = wb.Worksheets.Add(_currentReport);

                for (int c = 0; c < _preview.Columns.Count; c++)
                    ws.Cell(1, c + 1).Value = _preview.Columns[c].HeaderText;

                var headerRange = ws.Range(1, 1, 1, _preview.Columns.Count);
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = XLColor.FromArgb(46, 125, 50);
                headerRange.Style.Font.FontColor = XLColor.White;

                for (int r = 0; r < _preview.Rows.Count; r++)
                    for (int c = 0; c < _preview.Columns.Count; c++)
                        ws.Cell(r + 2, c + 1).Value = _preview.Rows[r].Cells[c].Value?.ToString() ?? "";

                ws.Columns().AdjustToContents();
                wb.SaveAs(sfd.FileName);

                MessageBox.Show("Report exported successfully.", "Export Complete",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Export failed: " + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ── Export: PDF via Print Preview ──────────────────────────────
        // Uses Windows' built-in "Microsoft Print to PDF" driver — no new
        // NuGet package needed. Opens a real print-preview dialog the user
        // can either print physically or save as a PDF file.
        private void btnExportPDF_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_currentReport) || _preview.Columns.Count == 0)
            {
                MessageBox.Show("Please select a report first.", "No Report Selected",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                var pd = new PrintDocument();
                int currentRow = 0;

                pd.PrintPage += (s, ev) =>
                {
                    var g = ev.Graphics!;
                    float y = 40f;
                    float leftMargin = 40f;
                    int colCount = _preview.Columns.Count;
                    float pageWidth = ev.PageBounds.Width - 80f;
                    float colWidth  = pageWidth / colCount;

                    using var titleFont  = new Font("Segoe UI", 16, FontStyle.Bold);
                    using var headerFont = new Font("Segoe UI", 9, FontStyle.Bold);
                    using var rowFont    = new Font("Segoe UI", 9, FontStyle.Regular);

                    g.DrawString($"{_currentReport} Report", titleFont, Brushes.Black, leftMargin, y);
                    y += 30;
                    g.DrawString($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm}", rowFont, Brushes.Gray, leftMargin, y);
                    y += 28;

                    // Header row
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

                    // Data rows — paginate when reaching page bottom
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
                    Document    = pd,
                    Width       = 900,
                    Height      = 700,
                    StartPosition = FormStartPosition.CenterParent
                };
                ppd.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not generate print preview: " + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}