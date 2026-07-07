// Forms/LocationsForm.cs
using System;
using System.Drawing;
using System.Windows.Forms;
using Inventory_Management_System.Helpers;
using Inventory_Management_System.Services;

namespace Inventory_Management_System.Forms
{
    public partial class LocationsForm : Form
    {
        private readonly LocationService _locationService;

        public LocationsForm()
        {
            InitializeComponent();
            _locationService = new LocationService();
            ApplyTheme();
            LoadLocations();
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
            ThemeHelper.StyleDataGridView(dgvLocations);
        }

        private void LoadLocations()
        {
            dgvLocations.Rows.Clear();

            var result = _locationService.GetAll();
            if (!result.Success)
            {
                MessageBox.Show(result.ErrorMessage, "Data Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            foreach (var item in result.Data!)
            {
                dgvLocations.Rows.Add(
                    item.Id,
                    item.LocationName,
                    item.Building,
                    item.RoomNumber,
                    item.AllocatedPerson ?? ""
                );
            }
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            using var form = new AddEditLocationForm();
            if (form.ShowDialog() == DialogResult.OK)
            {
                LoadLocations();
            }
        }

        private void btnEdit_Click(object sender, EventArgs e)
        {
            if (dgvLocations.SelectedRows.Count == 0) return;
            var id = Convert.ToInt32(dgvLocations.SelectedRows[0].Cells[0].Value);
            var result = _locationService.GetById(id);
            if (result.Success && result.Data != null)
            {
                using var form = new AddEditLocationForm(result.Data);
                if (form.ShowDialog() == DialogResult.OK)
                {
                    LoadLocations();
                }
            }
            else
            {
                MessageBox.Show(result.ErrorMessage ?? "Location not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            if (dgvLocations.SelectedRows.Count == 0) return;
            var id = Convert.ToInt32(dgvLocations.SelectedRows[0].Cells[0].Value);
            if (MessageBox.Show("Are you sure you want to delete this location?", "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                var result = _locationService.Delete(id);
                if (result.Success)
                {
                    LoadLocations();
                }
                else
                {
                    MessageBox.Show(result.ErrorMessage, "Delete Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void btnRefresh_Click(object sender, EventArgs e) => LoadLocations();
        private void btnExport_Click(object sender, EventArgs e) { }
    }
}