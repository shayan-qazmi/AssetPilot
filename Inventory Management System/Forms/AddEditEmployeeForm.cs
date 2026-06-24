using System;
using System.Drawing;
using System.Windows.Forms;
using Inventory_Management_System.Helpers;
using Inventory_Management_System.Models;
using Inventory_Management_System.Services;

namespace Inventory_Management_System.Forms
{
    public class AddEditEmployeeForm : Form
    {
        private Employee _employee;
        private EmployeeService _employeeService;
        private bool _isEditMode;

        private TextBox txtEmployeeCode = null!;
        private TextBox txtFullName = null!;
        private TextBox txtDepartment = null!;
        private TextBox txtEmail = null!;
        private TextBox txtPhone = null!;
        private CheckBox chkIsActive = null!;
        private Button btnSave = null!;
        private Button btnCancel = null!;

        public AddEditEmployeeForm(Employee? employee = null)
        {
            _employee = employee ?? new Employee() { IsActive = true };
            _isEditMode = employee != null;
            _employeeService = new EmployeeService();

            InitializeUI();
            LoadData();
        }

        private void InitializeUI()
        {
            this.Text = _isEditMode ? "Edit Employee" : "Add Employee";
            this.Size = new Size(400, 560);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            ThemeHelper.ApplyFormBackground(this);
            this.AutoScroll = true; // Ensure scrolling when content exceeds form size

            int y = 20;

            // Employee Code
            Label lblCode = new Label() { Text = "Employee Code:", Location = new Point(20, y), AutoSize = true };
            ThemeHelper.StyleLabel(lblCode);
            this.Controls.Add(lblCode);
            y += 25;
            txtEmployeeCode = new TextBox() { Location = new Point(20, y), Width = 340 };
            ThemeHelper.StyleTextBox(txtEmployeeCode);
            this.Controls.Add(txtEmployeeCode);
            y += 40;

            // Full Name
            Label lblName = new Label() { Text = "Full Name:", Location = new Point(20, y), AutoSize = true };
            ThemeHelper.StyleLabel(lblName);
            this.Controls.Add(lblName);
            y += 25;
            txtFullName = new TextBox() { Location = new Point(20, y), Width = 340 };
            ThemeHelper.StyleTextBox(txtFullName);
            this.Controls.Add(txtFullName);
            y += 40;

            // Department
            Label lblDept = new Label() { Text = "Department:", Location = new Point(20, y), AutoSize = true };
            ThemeHelper.StyleLabel(lblDept);
            this.Controls.Add(lblDept);
            y += 25;
            txtDepartment = new TextBox() { Location = new Point(20, y), Width = 340 };
            ThemeHelper.StyleTextBox(txtDepartment);
            this.Controls.Add(txtDepartment);
            y += 40;

            // Email
            Label lblEmail = new Label() { Text = "Email:", Location = new Point(20, y), AutoSize = true };
            ThemeHelper.StyleLabel(lblEmail);
            this.Controls.Add(lblEmail);
            y += 25;
            txtEmail = new TextBox() { Location = new Point(20, y), Width = 340 };
            ThemeHelper.StyleTextBox(txtEmail);
            this.Controls.Add(txtEmail);
            y += 40;

            // Phone
            Label lblPhone = new Label() { Text = "Phone:", Location = new Point(20, y), AutoSize = true };
            ThemeHelper.StyleLabel(lblPhone);
            this.Controls.Add(lblPhone);
            y += 25;
            txtPhone = new TextBox() { Location = new Point(20, y), Width = 340 };
            ThemeHelper.StyleTextBox(txtPhone);
            this.Controls.Add(txtPhone);
            y += 40;

            // IsActive
            chkIsActive = new CheckBox() { Text = "Is Active", Location = new Point(20, y), AutoSize = true };
            chkIsActive.Font = ThemeHelper.FontDefault;
            chkIsActive.ForeColor = ThemeHelper.TextPrimary;
            this.Controls.Add(chkIsActive);
            y += 50;

            // Buttons
            btnSave = new Button() { Text = "Save", Location = new Point(200, y), Width = 75 };
            ThemeHelper.StyleButton(btnSave);
            btnSave.Click += BtnSave_Click;
            this.Controls.Add(btnSave);

            btnCancel = new Button() { Text = "Cancel", Location = new Point(285, y), Width = 75 };
            ThemeHelper.StyleSecondaryButton(btnCancel);
            btnCancel.Click += (s, e) => this.DialogResult = DialogResult.Cancel;
            this.Controls.Add(btnCancel);

            this.AcceptButton = btnSave;
            this.CancelButton = btnCancel;
        }

        private void LoadData()
        {
            if (_isEditMode)
            {
                txtEmployeeCode.Text = _employee.EmployeeCode;
                txtFullName.Text = _employee.FullName;
                txtDepartment.Text = _employee.Department;
                txtEmail.Text = _employee.Email;
                txtPhone.Text = _employee.Phone;
            }
            chkIsActive.Checked = _employee.IsActive;
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtFullName.Text))
            {
                MessageBox.Show("Full Name is required.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _employee.EmployeeCode = txtEmployeeCode.Text.Trim();
            _employee.FullName = txtFullName.Text.Trim();
            _employee.Department = txtDepartment.Text.Trim();
            _employee.Email = txtEmail.Text.Trim();
            _employee.Phone = txtPhone.Text.Trim();
            _employee.IsActive = chkIsActive.Checked;

            var result = _isEditMode ? _employeeService.Update(_employee) : _employeeService.Add(_employee);

            if (result.Success)
            {
                this.DialogResult = DialogResult.OK;
            }
            else
            {
                MessageBox.Show(result.ErrorMessage, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}