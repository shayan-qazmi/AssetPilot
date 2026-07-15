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
            ThemeHelper.StyleButton(btnRefresh, Color.FromArgb(33, 150, 243));
            ThemeHelper.StyleDangerButton(btnDelete);
            ThemeHelper.StyleButton(btnEdit, Color.FromArgb(245, 158, 11));
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
       private void btnExport_Click(object sender, EventArgs e)
{
    try
    {
        using var sfd = new SaveFileDialog
        {
            Filter   = "CSV Files|*.csv",
            Title    = "Export Locations",
            FileName = $"Locations_{DateTime.Now:yyyyMMdd_HHmm}.csv"
        };

        if (sfd.ShowDialog() != DialogResult.OK) return;

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Location ID,Location Name,Building,Room Number,Allocated Person");

        foreach (DataGridViewRow row in dgvLocations.Rows)
        {
            sb.AppendLine(string.Join(",",
                $"\"{row.Cells["ID"]?.Value}\"",
                $"\"{row.Cells["Name"]?.Value}\"",
                $"\"{row.Cells["Building"]?.Value}\"",
                $"\"{row.Cells["RoomNumber"]?.Value}\"",
                $"\"{row.Cells["AllocatedPerson"]?.Value}\""));
        }

        System.IO.File.WriteAllText(sfd.FileName, sb.ToString());
        MessageBox.Show("Locations exported successfully.", "Export",
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