// Forms/AddEditEquipmentForm.cs
using System;
using System.Drawing;
using System.Windows.Forms;
using Inventory_Management_System.Helpers;
using Inventory_Management_System.Models;
using Inventory_Management_System.Services;

namespace Inventory_Management_System.Forms
{
    public class AddEditEquipmentForm : Form
    {
        private Equipment _equipment;
        private EquipmentService _equipmentService;
        private bool _isEditMode;

        private TextBox txtEquipmentCode = null!;
        private TextBox txtQrCode = null!;
        private TextBox txtName = null!;
        private TextBox txtMake = null!;
        private TextBox txtModel = null!;
        private TextBox txtSerialNumber = null!;
        private ComboBox cmbStatus = null!;
        private DateTimePicker dtpPurchaseDate = null!;
        private Button btnSave = null!;
        private Button btnCancel = null!;

        public AddEditEquipmentForm(Equipment? equipment = null)
        {
            _equipment = equipment ?? new Equipment() { CurrentStatus = "in_store" };
            _isEditMode = equipment != null;
            _equipmentService = new EquipmentService();

            InitializeUI();
            LoadData();
        }

        private void InitializeUI()
        {
            this.Text = _isEditMode ? "Edit Equipment" : "Add Equipment";
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.AutoScroll = true;

            ThemeHelper.ApplyFormBackground(this);

            int x = 20;
            int w = 340;
            int y = 20;

            // Equipment Code
            var lblCode = new Label() { Text = "Equipment Code:", Location = new Point(x, y), AutoSize = true };
            ThemeHelper.StyleLabel(lblCode);
            this.Controls.Add(lblCode);
            y += 22;
            txtEquipmentCode = new TextBox() { Location = new Point(x, y), Width = w, Height = 26 };
            ThemeHelper.StyleTextBox(txtEquipmentCode);
            if (_isEditMode) txtEquipmentCode.Enabled = false;
            this.Controls.Add(txtEquipmentCode);
            y += 34;

            // QR Code
            var lblQr = new Label() { Text = "QR Code:", Location = new Point(x, y), AutoSize = true };
            ThemeHelper.StyleLabel(lblQr);
            this.Controls.Add(lblQr);
            y += 22;
            txtQrCode = new TextBox() { Location = new Point(x, y), Width = w, Height = 26 };
            ThemeHelper.StyleTextBox(txtQrCode);
            if (_isEditMode) txtQrCode.Enabled = false;
            this.Controls.Add(txtQrCode);
            y += 34;

            // Name
            var lblName = new Label() { Text = "Name:", Location = new Point(x, y), AutoSize = true };
            ThemeHelper.StyleLabel(lblName);
            this.Controls.Add(lblName);
            y += 22;
            txtName = new TextBox() { Location = new Point(x, y), Width = w, Height = 26 };
            ThemeHelper.StyleTextBox(txtName);
            this.Controls.Add(txtName);
            y += 34;

            // Make
            var lblMake = new Label() { Text = "Make:", Location = new Point(x, y), AutoSize = true };
            ThemeHelper.StyleLabel(lblMake);
            this.Controls.Add(lblMake);
            y += 22;
            txtMake = new TextBox() { Location = new Point(x, y), Width = w, Height = 26 };
            ThemeHelper.StyleTextBox(txtMake);
            this.Controls.Add(txtMake);
            y += 34;

            // Model
            var lblModel = new Label() { Text = "Model:", Location = new Point(x, y), AutoSize = true };
            ThemeHelper.StyleLabel(lblModel);
            this.Controls.Add(lblModel);
            y += 22;
            txtModel = new TextBox() { Location = new Point(x, y), Width = w, Height = 26 };
            ThemeHelper.StyleTextBox(txtModel);
            this.Controls.Add(txtModel);
            y += 34;

            // Serial Number
            var lblSerial = new Label() { Text = "Serial Number:", Location = new Point(x, y), AutoSize = true };
            ThemeHelper.StyleLabel(lblSerial);
            this.Controls.Add(lblSerial);
            y += 22;
            txtSerialNumber = new TextBox() { Location = new Point(x, y), Width = w, Height = 26 };
            ThemeHelper.StyleTextBox(txtSerialNumber);
            this.Controls.Add(txtSerialNumber);
            y += 34;

            // Status
            var lblStatus = new Label() { Text = "Current Status:", Location = new Point(x, y), AutoSize = true };
            ThemeHelper.StyleLabel(lblStatus);
            this.Controls.Add(lblStatus);
            y += 22;
            cmbStatus = new ComboBox() { Location = new Point(x, y), Width = w, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbStatus.Items.AddRange(new string[] { "in_store", "allocated", "decommissioned", "under_repair" });
            ThemeHelper.StyleComboBox(cmbStatus);
            this.Controls.Add(cmbStatus);
            y += 34;

            // Purchase Date
            var lblDate = new Label() { Text = "Purchase Date:", Location = new Point(x, y), AutoSize = true };
            ThemeHelper.StyleLabel(lblDate);
            this.Controls.Add(lblDate);
            y += 22;
            dtpPurchaseDate = new DateTimePicker() { Location = new Point(x, y), Width = w, Format = DateTimePickerFormat.Short };
            this.Controls.Add(dtpPurchaseDate);
            y += 40;

            // Buttons — 16 px gap below last control
            y += 16;
            btnSave = new Button() { Text = "Save", Location = new Point(x + w - 160, y), Width = 75, Height = 30 };
            ThemeHelper.StyleButton(btnSave);
            btnSave.Click += BtnSave_Click;
            this.Controls.Add(btnSave);

            btnCancel = new Button() { Text = "Cancel", Location = new Point(x + w - 76, y), Width = 76, Height = 30 };
            ThemeHelper.StyleSecondaryButton(btnCancel);
            btnCancel.Click += (s, e) => this.DialogResult = DialogResult.Cancel;
            this.Controls.Add(btnCancel);

            // Size form so buttons are fully visible with 24 px bottom margin
            this.ClientSize = new Size(w + 2 * x, y + 30 + 24);

            this.AcceptButton = btnSave;
            this.CancelButton = btnCancel;
        }

        private void LoadData()
        {
            if (_isEditMode)
            {
                txtEquipmentCode.Text = _equipment.EquipmentCode;
                txtQrCode.Text        = _equipment.QrCode;
                txtName.Text          = _equipment.Name;
                txtMake.Text          = _equipment.Make;
                txtModel.Text         = _equipment.Model;
                txtSerialNumber.Text  = _equipment.SerialNumber;
                cmbStatus.SelectedItem = _equipment.CurrentStatus;

                if (_equipment.PurchaseDate.HasValue)
                    dtpPurchaseDate.Value = _equipment.PurchaseDate.Value.ToDateTime(TimeOnly.MinValue);
            }
            else
            {
                cmbStatus.SelectedIndex = 0;
            }
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtName.Text) ||
                string.IsNullOrWhiteSpace(txtEquipmentCode.Text) ||
                string.IsNullOrWhiteSpace(txtQrCode.Text))
            {
                MessageBox.Show("Equipment Code, QR Code, and Name are required.",
                    "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _equipment.EquipmentCode  = txtEquipmentCode.Text.Trim();
            _equipment.QrCode         = txtQrCode.Text.Trim();
            _equipment.Name           = txtName.Text.Trim();
            _equipment.Make           = txtMake.Text.Trim();
            _equipment.Model          = txtModel.Text.Trim();
            _equipment.SerialNumber   = txtSerialNumber.Text.Trim();
            _equipment.CurrentStatus  = cmbStatus.SelectedItem?.ToString() ?? "in_store";
            _equipment.PurchaseDate   = DateOnly.FromDateTime(dtpPurchaseDate.Value);

            if (!_isEditMode)
                _equipment.CreatedByUserId = 1;

            var result = _isEditMode
                ? _equipmentService.Update(_equipment)
                : _equipmentService.Add(_equipment);

            if (result.Success)
                this.DialogResult = DialogResult.OK;
            else
                MessageBox.Show(result.ErrorMessage, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}