// Forms/EmployeesForm.cs
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Inventory_Management_System.Helpers;
using Inventory_Management_System.Models;
using Inventory_Management_System.Services;

namespace Inventory_Management_System.Forms
{
    public partial class EmployeesForm : Form
    {
        private readonly EmployeeService _employeeService;

        // ── Pagination state ─────────────────────────────────────────────
        private int _currentPage = 1;
        private int _pageSize = 25;
        private int _totalPages = 1;

        public EmployeesForm()
        {
            InitializeComponent();
            _employeeService = new EmployeeService();
            ApplyTheme();
            LoadEmployees();

            this.Shown += (s, e) =>
            {
                dgvEmployees.ClearSelection();
                dgvEmployees.CurrentCell = null;
            };

        }

        private void ApplyTheme()
        {
            ThemeHelper.ApplyFormBackground(this);
            ThemeHelper.StyleToolbarPanel(panelTop);
            ThemeHelper.StyleButton(btnAdd);
            ThemeHelper.StyleButton(btnRefresh, Color.FromArgb(33, 150, 243));
            ThemeHelper.StyleDangerButton(btnDelete);
            ThemeHelper.StyleButton(btnEdit, Color.FromArgb(245, 158, 11));
            ThemeHelper.StyleSecondaryButton(btnExport);
            ThemeHelper.StyleDataGridView(dgvEmployees);
            ThemeHelper.StyleTextBox(txtSearch);
            ThemeHelper.StyleComboBox(cmbPageSize);
            ThemeHelper.StyleSecondaryButton(btnPrevPage);
            ThemeHelper.StyleSecondaryButton(btnNextPage);
        }

        // ── Load — DB-level paging when not searching, in-memory paging
        //    over the fuzzy-matched set when searching (fuzzy match can't
        //    run as SQL, so it needs the full set loaded first). ──────────
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
                        MessageBox.Show(result.ErrorMessage, "Data Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                        MessageBox.Show(result.ErrorMessage, "Data Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    var allEmployees = result.Data ?? new List<Employee>();

                    // Scanned/typed codes need an exact substring match — fuzzy matching
                    // lets "EMP004" incorrectly match EMP001, EMP002, etc.
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
                    lblRowCount.Text = $"Showing {pageItems.Count} of {fullMatches.Count} records";
                }

                lblPageIndicator.Text = $"Page {_currentPage} of {Math.Max(1, _totalPages)}";
                btnPrevPage.Enabled = _currentPage > 1;
                btnNextPage.Enabled = _currentPage < _totalPages;

                dgvEmployees.AutoResizeColumns();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading employees.\n\n" + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
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

                var statusCell = dgvEmployees.Rows[rowIdx].Cells["Status"];
                statusCell.Style.ForeColor = item.IsActive ? ThemeHelper.Success : ThemeHelper.Danger;
                statusCell.Style.Font      = ThemeHelper.FontBold;
            }
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            using var form = new AddEditEmployeeForm();
            if (form.ShowDialog() == DialogResult.OK)
            {
                LoadEmployees(txtSearch.Text.Trim());
            }
        }

        private void btnEdit_Click(object sender, EventArgs e)
        {
            if (dgvEmployees.SelectedRows.Count == 0) return;
            var code = dgvEmployees.SelectedRows[0].Cells[0].Value.ToString();
            var resultAll = _employeeService.GetAll();
            if (resultAll.Success && resultAll.Data != null)
            {
                var emp = resultAll.Data.Find(x => x.EmployeeCode == code);
                if (emp != null)
                {
                    using var form = new AddEditEmployeeForm(emp);
                    if (form.ShowDialog() == DialogResult.OK)
                    {
                        LoadEmployees(txtSearch.Text.Trim());
                    }
                }
            }
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            if (dgvEmployees.SelectedRows.Count == 0) return;
            var code = dgvEmployees.SelectedRows[0].Cells[0].Value.ToString();
            var resultAll = _employeeService.GetAll();
            if (resultAll.Success && resultAll.Data != null)
            {
                var emp = resultAll.Data.Find(x => x.EmployeeCode == code);
                if (emp != null)
                {
                    if (MessageBox.Show("Are you sure you want to delete this employee?", "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    {
                        var result = _employeeService.Delete(emp.Id);
                        if (result.Success)
                        {
                            LoadEmployees(txtSearch.Text.Trim());
                        }
                        else
                        {
                            MessageBox.Show(result.ErrorMessage, "Delete Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
            }
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            txtSearch.Text = string.Empty;
            _currentPage = 1;
            LoadEmployees();
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
                    Filter   = "CSV Files|*.csv",
                    Title    = "Export Employees",
                    FileName = $"Employees_{DateTime.Now:yyyyMMdd_HHmm}.csv"
                };

                if (sfd.ShowDialog() != DialogResult.OK) return;

                var sb = new System.Text.StringBuilder();
                sb.AppendLine("Employee Code,Full Name,Department,Email,Phone,Status");

                foreach (DataGridViewRow row in dgvEmployees.Rows)
                {
                    sb.AppendLine(string.Join(",",
                        $"\"{row.Cells["ID"]?.Value}\"",
                        $"\"{row.Cells["Name"]?.Value}\"",
                        $"\"{row.Cells["Department"]?.Value}\"",
                        $"\"{row.Cells["Email"]?.Value}\"",
                        $"\"{row.Cells["Phone"]?.Value}\"",
                        $"\"{row.Cells["Status"]?.Value}\""));
                }

                System.IO.File.WriteAllText(sfd.FileName, sb.ToString());
                MessageBox.Show("Employees exported successfully.", "Export",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Export failed: {ex.Message}", "Export Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}