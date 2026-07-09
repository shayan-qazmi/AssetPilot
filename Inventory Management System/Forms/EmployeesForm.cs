// Forms/EmployeesForm.cs
using System;
using System.Drawing;
using System.Windows.Forms;
using Inventory_Management_System.Helpers;
using Inventory_Management_System.Services;

namespace Inventory_Management_System.Forms
{
    public partial class EmployeesForm : Form
    {
        private readonly EmployeeService _employeeService;

        public EmployeesForm()
        {
            InitializeComponent();
            _employeeService = new EmployeeService();
            ApplyTheme();
            LoadEmployees();
        }

        private void ApplyTheme()
        {
            ThemeHelper.ApplyFormBackground(this);
            ThemeHelper.StyleToolbarPanel(panelTop);
            ThemeHelper.StyleButton(btnAdd);
            ThemeHelper.StyleButton(btnRefresh);
            ThemeHelper.StyleDangerButton(btnDelete);
            ThemeHelper.StyleSecondaryButton(btnEdit);
            ThemeHelper.StyleSecondaryButton(btnExport);
            ThemeHelper.StyleTextBox(txtSearch);
            ThemeHelper.StyleDataGridView(dgvEmployees);
        }

        private void LoadEmployees()
        {
            try
            {
                dgvEmployees.Rows.Clear();

                var result = _employeeService.GetAll();
                if (!result.Success)
                {
                    MessageBox.Show(result.ErrorMessage, "Data Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                foreach (var item in result.Data!)
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
                // Auto‑size columns after adding all rows
                dgvEmployees.AutoResizeColumns();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading employees.\n\n" + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            using var form = new AddEditEmployeeForm();
            if (form.ShowDialog() == DialogResult.OK)
            {
                LoadEmployees();
            }
        }

        private void btnEdit_Click(object sender, EventArgs e)
        {
            if (dgvEmployees.SelectedRows.Count == 0) return;
            var code = dgvEmployees.SelectedRows[0].Cells[0].Value.ToString();
            // We need the ID to get the employee, let's find the ID using service
            var resultAll = _employeeService.GetAll();
            if (resultAll.Success && resultAll.Data != null)
            {
                var emp = resultAll.Data.Find(x => x.EmployeeCode == code);
                if (emp != null)
                {
                    using var form = new AddEditEmployeeForm(emp);
                    if (form.ShowDialog() == DialogResult.OK)
                    {
                        LoadEmployees();
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
                            LoadEmployees();
                        }
                        else
                        {
                            MessageBox.Show(result.ErrorMessage, "Delete Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
            }
        }

        private void btnRefresh_Click(object sender, EventArgs e) => LoadEmployees();

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