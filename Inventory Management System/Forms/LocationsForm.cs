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
            flowToolbar.SizeChanged += (s, e) => panelTop.Height = flowToolbar.Height;
            _locationService = new LocationService();
            ApplyTheme();
            LoadLocations();

            this.Shown += (s, e) =>
            {
                dgvLocations.ClearSelection();
                dgvLocations.CurrentCell = null;
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
            ThemeHelper.StyleSecondaryButton(btnImport);
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
                    item.RoomName ?? ""
                );
            }
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            using var form = new AddEditLocationForm();
            if (form.ShowDialog() == DialogResult.OK)
            {
                LoadLocations();
                ToastHelper.Show(this, "Location added successfully.", ToastType.Success);
            }
        }

        private void btnEdit_Click(object sender, EventArgs e)
        {
            if (dgvLocations.SelectedRows.Count == 0)
            {
                ToastHelper.Show(this, "Please select a location to edit.", ToastType.Info);
                return;
            }
            var id = Convert.ToInt32(dgvLocations.SelectedRows[0].Cells[0].Value);
            var result = _locationService.GetById(id);
            if (result.Success && result.Data != null)
            {
                using var form = new AddEditLocationForm(result.Data);
                if (form.ShowDialog() == DialogResult.OK)
                {
                    LoadLocations();
                    ToastHelper.Show(this, "Location updated successfully.", ToastType.Success);
                }
            }
            else
            {
                ToastHelper.Show(this, result.ErrorMessage ?? "Location not found.", ToastType.Error);
            }
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            if (dgvLocations.SelectedRows.Count == 0)
            {
                ToastHelper.Show(this, "Please select a location to delete.", ToastType.Info);
                return;
            }
            var id = Convert.ToInt32(dgvLocations.SelectedRows[0].Cells[0].Value);
            if (MessageBox.Show("Are you sure you want to delete this location?", "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                var result = _locationService.Delete(id);
                if (result.Success)
                {
                    LoadLocations();
                    ToastHelper.Show(this, "Location deleted successfully.", ToastType.Success);
                }
                else
                {
                    ToastHelper.Show(this, result.ErrorMessage, ToastType.Error);
                }
            }
        }
        private void btnImport_Click(object sender, EventArgs e)
        {
            using var ofd = new OpenFileDialog
            {
                Filter = "Excel Files (*.xlsx;*.xls)|*.xlsx;*.xls",
                Title = "Import Locations from Excel"
            };

            if (ofd.ShowDialog() != DialogResult.OK) return;

            try
            {
                var (items, errors) = ExcelHelper.ImportLocations(ofd.FileName);

                if (errors.Count > 0)
                {
                    string errorMessage = string.Join("\n", errors.Take(10));
                    if (errors.Count > 10) errorMessage += $"\n...and {errors.Count - 10} more errors.";
                    ToastHelper.Show(this, $"Import Warnings:\n{errorMessage}", ToastType.Error);
                }

                if (items.Count > 0)
                {
                    int importedCount = 0;
                    foreach (var loc in items)
                    {
                        var result = _locationService.Add(loc);
                        if (result.Success) importedCount++;
                    }

                    ToastHelper.Show(this, $"Successfully imported {importedCount} locations.", ToastType.Success);
                    LoadLocations();
                }
                else if (errors.Count == 0)
                {
                    ToastHelper.Show(this, "No valid location records found in the selected file.", ToastType.Error);
                }
            }
            catch (Exception ex)
            {
                ToastHelper.Show(this, $"Import failed: {ex.Message}", ToastType.Error);
            }
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            LoadLocations();
            ToastHelper.Show(this, "Locations refreshed.", ToastType.Info);
        }
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
                sb.AppendLine("Location ID,Location Name,Address/Building,Room Number,Allocated Person");    

                foreach (DataGridViewRow row in dgvLocations.Rows)
        {
            sb.AppendLine(string.Join(",",
                $"\"{row.Cells["ID"]?.Value}\"",
                $"\"{row.Cells["Name"]?.Value}\"",
                $"\"{row.Cells["Building"]?.Value}\"",
                $"\"{row.Cells["RoomNumber"]?.Value}\"",
                $"\"{row.Cells["RoomName"]?.Value}\""));
        }

        System.IO.File.WriteAllText(sfd.FileName, sb.ToString());
     ToastHelper.Show(this, "Locations exported successfully.", ToastType.Success);

            }
            catch (Exception ex)
            {
       ToastHelper.Show(this, $"Export failed: {ex.Message}", ToastType.Error);
            }
}
    }
}