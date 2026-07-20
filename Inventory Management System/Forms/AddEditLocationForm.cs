// Forms/AddEditLocationForm.cs
using System;
using System.Drawing;
using System.Windows.Forms;
using Inventory_Management_System.Helpers;
using Inventory_Management_System.Models;
using Inventory_Management_System.Services;

namespace Inventory_Management_System.Forms
{
    public class AddEditLocationForm : Form
    {
        private Location _location;
        private LocationService _locationService;
        private bool _isEditMode;

        private TextBox txtName = null!;
        private TextBox txtBuilding = null!;
        private TextBox txtRoomNumber = null!;
        private TextBox txtRoomName = null!;
        private Button btnSave = null!;
        private Button btnCancel = null!;

        public AddEditLocationForm(Location? location = null)
        {
            _location = location ?? new Location();
            _isEditMode = location != null;
            _locationService = new LocationService();

            InitializeUI();
            LoadData();
        }

        private void InitializeUI()
        {
            this.Text = _isEditMode ? "Edit Location" : "Add Location";
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            ThemeHelper.ApplyFormBackground(this);
            this.AutoScroll = true;

            int y = 20;

            // ── Location Name ─────────────────────────────────────────────
            Label lblName = new Label() { Text = "Location Name:", Location = new Point(20, y), AutoSize = true };
            ThemeHelper.StyleLabel(lblName);
            this.Controls.Add(lblName);
            y += 25;
            txtName = new TextBox() { Location = new Point(20, y), Width = 340 };
            ThemeHelper.StyleTextBox(txtName);
            this.Controls.Add(txtName);
            y += 40;

            // ── Building ──────────────────────────────────────────────────
            Label lblBuilding = new Label() { Text = "Building:", Location = new Point(20, y), AutoSize = true };
            ThemeHelper.StyleLabel(lblBuilding);
            this.Controls.Add(lblBuilding);
            y += 25;
            txtBuilding = new TextBox() { Location = new Point(20, y), Width = 340 };
            ThemeHelper.StyleTextBox(txtBuilding);
            this.Controls.Add(txtBuilding);
            y += 40;

            // ── Room Number ───────────────────────────────────────────────
            Label lblRoomNumber = new Label() { Text = "Room Number:", Location = new Point(20, y), AutoSize = true };
            ThemeHelper.StyleLabel(lblRoomNumber);
            this.Controls.Add(lblRoomNumber);
            y += 25;
            txtRoomNumber = new TextBox() { Location = new Point(20, y), Width = 340 };
            ThemeHelper.StyleTextBox(txtRoomNumber);
            this.Controls.Add(txtRoomNumber);
            y += 40;

         // ── Room Name ─────────────────────────────────────────────────
            Label lblRoomName = new Label() { Text = "Room Name:", Location = new Point(20, y), AutoSize = true };
            ThemeHelper.StyleLabel(lblRoomName);
            this.Controls.Add(lblRoomName);
            y += 25;
            txtRoomName = new TextBox() { Location = new Point(20, y), Width = 340 };
            ThemeHelper.StyleTextBox(txtRoomName);
            this.Controls.Add(txtRoomName);
            y += 50;

            // ── Buttons ───────────────────────────────────────────────────
            btnSave = new Button() { Text = "Save", Location = new Point(200, y), Width = 75 };
            ThemeHelper.StyleButton(btnSave);
            btnSave.Click += BtnSave_Click;
            this.Controls.Add(btnSave);

            btnCancel = new Button() { Text = "Cancel", Location = new Point(285, y), Width = 75 };
            ThemeHelper.StyleSecondaryButton(btnCancel);
            btnCancel.Click += (s, e) => this.DialogResult = DialogResult.Cancel;
            this.Controls.Add(btnCancel);

            this.ClientSize = new Size(380, y + 40);
            this.AcceptButton = btnSave;
            this.CancelButton = btnCancel;
        }

        private void LoadData()
        {
            if (_isEditMode)
            {
                txtName.Text            = _location.LocationName;
                txtBuilding.Text        = _location.Building;
                txtRoomNumber.Text      = _location.RoomNumber;
                txtRoomName.Text = _location.RoomName ?? "";
            }
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                MessageBox.Show("Location Name is required.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _location.LocationName    = txtName.Text.Trim();
            _location.Building        = txtBuilding.Text.Trim();
            _location.RoomNumber      = txtRoomNumber.Text.Trim();
            _location.RoomName = string.IsNullOrWhiteSpace(txtRoomName.Text)
            ? null
            : txtRoomName.Text.Trim();

            var result = _isEditMode ? _locationService.Update(_location) : _locationService.Add(_location);

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