// Forms/EmployeesForm.cs
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Inventory_Management_System.Helpers;
using Inventory_Management_System.Models;
using Inventory_Management_System.Services;
using System.Drawing.Drawing2D;

namespace Inventory_Management_System.Forms
{
    public partial class EmployeesForm : Form, IThemableForm
    {
        private readonly EmployeeService _employeeService;

        // ── Pagination state ─────────────────────────────────────────────
        private int _currentPage = 1;
        private int _pageSize = 25;
        private int _totalPages = 1;

        private string? _pendingSelectCode;
        private bool _hasPendingSelection = false;

        public EmployeesForm()
        {
            InitializeComponent();
            flowToolbar.SizeChanged += (s, e) => panelTop.Height = flowToolbar.Height;
            _employeeService = new EmployeeService();
            ApplyTheme();
            dgvEmployees.CellDoubleClick += DgvEmployees_CellDoubleClick;
            LoadEmployees();

            this.Shown += (s, e) =>
            {
                if (!_hasPendingSelection)
                {
                    dgvEmployees.ClearSelection();
                    dgvEmployees.CurrentCell = null;
                }
            };
        }

        public EmployeesForm(string employeeCode) : this()
        {
            _hasPendingSelection = true;
            _pendingSelectCode = employeeCode;
            txtSearch.Text = employeeCode;   // fires TextChanged → LoadEmployees(filtered)
        }






        public void ApplyTheme()
        {
            ThemeHelper.ApplyFormBackground(this);
            ThemeHelper.StyleToolbarPanel(panelTop);
            ThemeHelper.StyleButton(btnAdd);
            ThemeHelper.StyleButton(btnRefresh, Color.FromArgb(33, 150, 243));
            ThemeHelper.StyleDangerButton(btnDelete);
            ThemeHelper.StyleButton(btnEdit, Color.FromArgb(245, 158, 11));
            ThemeHelper.StyleSecondaryButton(btnExport);
            ThemeHelper.StyleSecondaryButton(btnImport);
            ThemeHelper.StyleDataGridView(dgvEmployees);
            ApplyRoundedSearchStyle(txtSearch);
            ThemeHelper.StyleComboBox(cmbPageSize);
            ThemeHelper.StyleSecondaryButton(btnPrevPage);
            ThemeHelper.StyleSecondaryButton(btnNextPage);
        }



        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_CLIENTEDGE = 0x200;

        private static GraphicsPath RoundedRectPath(RectangleF r, float radius)
        {
            var path = new GraphicsPath();
            float d = radius * 2f;
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        private void ApplyRoundedSearchStyle(TextBox txt)
        {
            var parent = txt.Parent;
            if (parent == null) return;

            var originalSize = txt.Size;
            var originalMargin = txt.Margin;
            int idx = parent.Controls.GetChildIndex(txt);
            parent.Controls.Remove(txt);

            var wrapper = new Panel
            {
                Size = originalSize,
                Margin = originalMargin,
                BackColor = Color.White
            };

            Color inputBorder = Color.FromArgb(209, 213, 219);
            Color focusBorder = Color.FromArgb(0x52, 0xA2, 0x3F);
            Color currentBorder = inputBorder;

            void RoundInput(object? s, EventArgs e)
            {
                if (wrapper.Width <= 0 || wrapper.Height <= 0) return;
                using var path = RoundedRectPath(new RectangleF(0, 0, wrapper.Width, wrapper.Height), 8f);
                wrapper.Region = new Region(path);
            }
            wrapper.Resize += RoundInput;

            wrapper.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var path = RoundedRectPath(new RectangleF(0.5f, 0.5f, wrapper.Width - 1, wrapper.Height - 1), 8f);
                using var pen = new Pen(currentBorder, 1f);
                e.Graphics.DrawPath(pen, path);
            };

            var searchIcon = new Label
            {
                Text = "🔍",
                Font = new Font("Segoe UI Emoji", 8.5f),
                ForeColor = ThemeHelper.TextSecondary,
                AutoSize = false,
                Size = new Size(24, wrapper.Height),
                Location = new Point(6, 0),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };

            txt.BorderStyle = BorderStyle.None;
            int exStyle = GetWindowLong(txt.Handle, GWL_EXSTYLE);
            SetWindowLong(txt.Handle, GWL_EXSTYLE, exStyle & ~WS_EX_CLIENTEDGE);
            txt.BackColor = Color.White;
            txt.Location = new Point(30, (wrapper.Height - txt.Height) / 2);
            txt.Width = wrapper.Width - 38;

            wrapper.Controls.Add(searchIcon);
            wrapper.Controls.Add(txt);
            parent.Controls.Add(wrapper);
            parent.Controls.SetChildIndex(wrapper, idx);

            txt.Enter += (s, e) => { currentBorder = focusBorder; wrapper.Invalidate(); };
            txt.Leave += (s, e) => { currentBorder = inputBorder; wrapper.Invalidate(); };

            RoundInput(null, EventArgs.Empty);
        }




        // ── Load — DB-level paging when not searching, in-memory paging over fuzzy matches
        private void LoadEmployees(string searchTerm = "")
        {
            try
            {
                dgvEmployees.Rows.Clear();

                if (string.IsNullOrWhiteSpace(searchTerm))
                {
                    var result = _employeeService.GetPage(_currentPage, _pageSize);
                    if (!result.Success)
                    {
                        ToastHelper.Show(this, result.ErrorMessage, ToastType.Error);
                        return;
                    }

                    _totalPages = Math.Max(1, result.Data!.TotalPages);
                    if (_currentPage > _totalPages) _currentPage = _totalPages;

                    BindEmployeeRows(result.Data.Items);
                    lblRowCount.Text = $"Showing {result.Data.Items.Count} of {result.Data.TotalCount} records";
                }
                else
                {
                    var result = _employeeService.GetAll();
                    if (!result.Success)
                    {
                        ToastHelper.Show(this, result.ErrorMessage, ToastType.Error);
                        return;
                    }

                    var allEmployees = result.Data ?? new List<Employee>();

                    var codeMatches = allEmployees.Where(e =>
                        (!string.IsNullOrEmpty(e.EmployeeCode) && e.EmployeeCode.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)))
                        .ToList();

                    var fullMatches = codeMatches.Count > 0
                        ? codeMatches
                        : allEmployees.Where(e => FuzzySearchHelper.IsFuzzyMatch(e.FullName, searchTerm)
                                                || FuzzySearchHelper.IsFuzzyMatch(e.Department, searchTerm)
                                                || FuzzySearchHelper.IsFuzzyMatch(e.Email, searchTerm)
                                                || FuzzySearchHelper.IsFuzzyMatch(e.Phone, searchTerm))
                            .OrderByDescending(e => Math.Max(
                                FuzzySearchHelper.FuzzyScore(e.FullName, searchTerm),
                                Math.Max(
                                    FuzzySearchHelper.FuzzyScore(e.Department, searchTerm),
                                    Math.Max(
                                        FuzzySearchHelper.FuzzyScore(e.Email, searchTerm),
                                        FuzzySearchHelper.FuzzyScore(e.Phone, searchTerm)))))
                            .ToList();

                    _totalPages = Math.Max(1, (int)Math.Ceiling(fullMatches.Count / (double)_pageSize));
                    if (_currentPage > _totalPages) _currentPage = _totalPages;

                    var pageItems = fullMatches
                        .Skip((_currentPage - 1) * _pageSize)
                        .Take(_pageSize)
                        .ToList();

                    BindEmployeeRows(pageItems);

                    if (!string.IsNullOrWhiteSpace(_pendingSelectCode))
                    {
                        var match = dgvEmployees.Rows.Cast<DataGridViewRow>()
                            .FirstOrDefault(r => string.Equals(r.Cells[0].Value?.ToString(), _pendingSelectCode, StringComparison.OrdinalIgnoreCase));
                        if (match != null)
                        {
                            dgvEmployees.ClearSelection();
                            match.Selected = true;
                            dgvEmployees.CurrentCell = match.Cells[0];
                            dgvEmployees.FirstDisplayedScrollingRowIndex = match.Index;
                        }
                        _pendingSelectCode = null;
                    }

                    lblRowCount.Text = $"Showing {pageItems.Count} of {fullMatches.Count} records";
                }

                lblPageIndicator.Text = $"Page {_currentPage} of {Math.Max(1, _totalPages)}";
                btnPrevPage.Enabled = _currentPage > 1;
                btnNextPage.Enabled = _currentPage < _totalPages;

                dgvEmployees.AutoResizeColumns();
            }
            catch (Exception ex)
            {
                ToastHelper.Show(this, "Error loading employees.\n\n" + ex.Message, ToastType.Error);
            }
        }

        private void BindEmployeeRows(List<Employee> items)
        {
            foreach (var item in items)
            {
                int rowIdx = dgvEmployees.Rows.Add(
                    item.EmployeeCode,
                    item.FullName,
                    item.Department,
                    item.Email,
                    item.Phone,
                    item.IsActive ? "Active" : "Inactive"
                );

                var statusCell = dgvEmployees.Rows[rowIdx].Cells[5];
                statusCell.Style.ForeColor = item.IsActive ? ThemeHelper.Success : ThemeHelper.Danger;
                statusCell.Style.Font = ThemeHelper.FontBold;
            }
        }

        private void DgvEmployees_CellDoubleClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            var code = dgvEmployees.Rows[e.RowIndex].Cells[0].Value?.ToString();
            if (string.IsNullOrEmpty(code)) return;

            var resultAll = _employeeService.GetAll();
            if (!resultAll.Success || resultAll.Data == null) return;

            var emp = resultAll.Data.Find(x => string.Equals(x.EmployeeCode, code, StringComparison.OrdinalIgnoreCase));
            if (emp == null) return;

            using var popup = new EmployeeDetailPopup(emp);
            popup.ShowDialog(this);
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            using var form = new AddEditEmployeeForm();
            if (form.ShowDialog() == DialogResult.OK)
            {
                ToastHelper.Show(this, "Employee added successfully.", ToastType.Success);
                LoadEmployees(txtSearch.Text.Trim());
            }
        }

        private void btnEdit_Click(object sender, EventArgs e)
        {
            if (dgvEmployees.SelectedRows.Count == 0)
            {
                ToastHelper.Show(this, "Please select an employee to edit.", ToastType.Info);
                return;
            }
            var code = dgvEmployees.SelectedRows[0].Cells[0].Value?.ToString();
            if (string.IsNullOrEmpty(code)) return;

            var resultAll = _employeeService.GetAll();
            if (resultAll.Success && resultAll.Data != null)
            {
                var emp = resultAll.Data.Find(x => string.Equals(x.EmployeeCode, code, StringComparison.OrdinalIgnoreCase));
                if (emp != null)
                {
                    using var form = new AddEditEmployeeForm(emp);
                    if (form.ShowDialog() == DialogResult.OK)
                    {
                        ToastHelper.Show(this, "Employee updated successfully.", ToastType.Success);
                        LoadEmployees(txtSearch.Text.Trim());
                    }
                }
            }
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            if (dgvEmployees.SelectedRows.Count == 0)
            {
                ToastHelper.Show(this, "Please select an employee to delete.", ToastType.Info);
                return;
            }
            var code = dgvEmployees.SelectedRows[0].Cells[0].Value?.ToString();
            if (string.IsNullOrEmpty(code)) return;

            var resultAll = _employeeService.GetAll();
            if (resultAll.Success && resultAll.Data != null)
            {
                var emp = resultAll.Data.Find(x => string.Equals(x.EmployeeCode, code, StringComparison.OrdinalIgnoreCase));
                if (emp != null)
                {
                    if (MessageBox.Show("Are you sure you want to delete this employee?", "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    {
                        var result = _employeeService.Delete(emp.Id);
                        if (result.Success)
                        {
                            ToastHelper.Show(this, "Employee deleted successfully.", ToastType.Success);
                            LoadEmployees(txtSearch.Text.Trim());
                        }
                        else
                        {
                            ToastHelper.Show(this, result.ErrorMessage, ToastType.Error);
                        }
                    }
                }
            }
        }

        private void btnImport_Click(object sender, EventArgs e)
        {
            using var ofd = new OpenFileDialog
            {
                Filter = "Excel Files (*.xlsx;*.xls)|*.xlsx;*.xls",
                Title = "Import Employees from Excel"
            };

            if (ofd.ShowDialog() != DialogResult.OK) return;

            try
            {
                var (items, errors) = ExcelHelper.ImportEmployees(ofd.FileName);

                if (errors.Count > 0)
                {
                    string errorMessage = string.Join("\n", errors.Take(10));
                    if (errors.Count > 10) errorMessage += $"\n...and {errors.Count - 10} more errors.";
                    ToastHelper.Show(this, $"Import Warnings:\n{errorMessage}", ToastType.Error);
                }

                if (items != null && items.Count > 0)
                {
                    int importedCount = 0;
                    var importFailures = new List<string>();

                    foreach (var emp in items)
                    {
                        var result = _employeeService.Add(emp);
                        if (result.Success)
                        {
                            importedCount++;
                        }
                        else if (!string.IsNullOrEmpty(result.ErrorMessage))
                        {
                            importFailures.Add($"{emp.FullName}: {result.ErrorMessage}");
                        }
                    }

                    if (importFailures.Count > 0)
                    {
                        string failMsg = string.Join("\n", importFailures.Take(5));
                        ToastHelper.Show(this, $"Imported {importedCount} of {items.Count}.\nErrors:\n{failMsg}", ToastType.Error);
                    }
                    else
                    {
                        ToastHelper.Show(this, $"Successfully imported {importedCount} employees.", ToastType.Success);
                    }

                    // Reset search filter and pagination to show imported records immediately
                    txtSearch.Text = string.Empty;
                    _currentPage = 1;
                    LoadEmployees();
                }
                else if (errors.Count == 0)
                {
                    ToastHelper.Show(this, "No valid employee records found in the selected file.", ToastType.Error);
                }
            }
            catch (Exception ex)
            {
                ToastHelper.Show(this, $"Import failed: {ex.Message}", ToastType.Error);
            }
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            txtSearch.Text = string.Empty;
            _currentPage = 1;
            LoadEmployees();
            ToastHelper.Show(this, "Employees refreshed.", ToastType.Info);
        }

        private void txtSearch_TextChanged(object sender, EventArgs e)
        {
            _currentPage = 1;
            LoadEmployees(txtSearch.Text.Trim());
        }

        private void cmbPageSize_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (int.TryParse(cmbPageSize.SelectedItem?.ToString(), out int size))
            {
                _pageSize = size;
                _currentPage = 1;
                LoadEmployees(txtSearch.Text.Trim());
            }
        }

        private void btnPrevPage_Click(object sender, EventArgs e)
        {
            if (_currentPage > 1)
            {
                _currentPage--;
                LoadEmployees(txtSearch.Text.Trim());
            }
        }

        private void btnNextPage_Click(object sender, EventArgs e)
        {
            if (_currentPage < _totalPages)
            {
                _currentPage++;
                LoadEmployees(txtSearch.Text.Trim());
            }
        }

        private void btnExport_Click(object sender, EventArgs e)
        {
            try
            {
                using var sfd = new SaveFileDialog
                {
                    Filter = "CSV Files|*.csv",
                    Title = "Export Employees",
                    FileName = $"Employees_{DateTime.Now:yyyyMMdd_HHmm}.csv"
                };

                if (sfd.ShowDialog() != DialogResult.OK) return;

                var sb = new System.Text.StringBuilder();
                sb.AppendLine("Employee Code,Full Name,Designation,Email,Phone,Status");

                foreach (DataGridViewRow row in dgvEmployees.Rows)
                {
                    if (row.IsNewRow) continue;

                    sb.AppendLine(string.Join(",",
                        $"\"{row.Cells[0]?.Value}\"",
                        $"\"{row.Cells[1]?.Value}\"",
                        $"\"{row.Cells[2]?.Value}\"",
                        $"\"{row.Cells[3]?.Value}\"",
                        $"\"{row.Cells[4]?.Value}\"",
                        $"\"{row.Cells[5]?.Value}\""));
                }

                System.IO.File.WriteAllText(sfd.FileName, sb.ToString());
                ToastHelper.Show(this, "Employees exported successfully.", ToastType.Success);
            }
            catch (Exception ex)
            {
                ToastHelper.Show(this, $"Export failed: {ex.Message}", ToastType.Error);
            }
        }
    }
}