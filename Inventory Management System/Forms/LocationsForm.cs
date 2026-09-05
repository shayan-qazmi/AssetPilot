// Forms/LocationsForm.cs
using System;
using System.Drawing;
using System.Windows.Forms;
using Inventory_Management_System.Helpers;
using Inventory_Management_System.Services;

namespace Inventory_Management_System.Forms
{
    public partial class LocationsForm : Form, IThemableForm
    {
        private readonly LocationService _locationService;
        // ── Gmail-style checkbox header controls ──
        private Panel _chkHeaderPanel = null!;
        private CheckBox _chkHeaderBox = null!;
        private bool _suppressHeaderSync;

        public LocationsForm()
        {
            InitializeComponent();
            dgvLocations.Columns["ID"].Visible = false;
            flowToolbar.SizeChanged += (s, e) => panelTop.Height = flowToolbar.Height;
            _locationService = new LocationService();
            ApplyTheme();
            SetupCheckboxSelectHeader();
            LoadLocations();

            this.Shown += (s, e) =>
            {
                dgvLocations.ClearSelection();
                dgvLocations.CurrentCell = null;
            };
        }
        // Hover handlers in ThemeHelper bake an explicit row.DefaultCellStyle.BackColor
        // onto whichever rows were hovered, using IsDarkMode at that instant. On a
        // theme toggle those baked colors go stale and don't follow the new theme —
        // clearing them lets rows fall back to the grid's (freshly updated) default
        // and alternating-row styles instead.
        private static void ResetRowStyleOverrides(DataGridView dgv)
        {
            foreach (DataGridViewRow row in dgv.Rows)
                row.DefaultCellStyle.BackColor = Color.Empty;
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
            ThemeHelper.StyleDataGridView(dgvLocations);
            ResetRowStyleOverrides(dgvLocations);
        }

        private void SetupCheckboxSelectHeader()
        {
            if (!dgvLocations.Columns.Contains("ColSelect"))
            {
                var chkCol = new DataGridViewCheckBoxColumn
                {
                    Name = "ColSelect",
                    HeaderText = "",
                    Width = 40,
                    ReadOnly = false,
                    Visible = true,
                    AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
                    TrueValue = true,
                    FalseValue = false,
                    IndeterminateValue = false
                };
                dgvLocations.Columns.Add(chkCol);
                dgvLocations.Columns["ColSelect"].DisplayIndex = 0;

                dgvLocations.ReadOnly = false;
                foreach (DataGridViewColumn col in dgvLocations.Columns)
                    col.ReadOnly = col.Name != "ColSelect";

                dgvLocations.CurrentCellDirtyStateChanged += (s, ev) =>
                {
                    if (dgvLocations.CurrentCell is DataGridViewCheckBoxCell)
                        dgvLocations.CommitEdit(DataGridViewDataErrorContexts.Commit);
                };

                dgvLocations.CellValueChanged += (s, ev) =>
                {
                    if (ev.ColumnIndex >= 0 && dgvLocations.Columns[ev.ColumnIndex].Name == "ColSelect")
                    {
                        SyncHeaderCheckbox();
                    }
                };
            }

            _chkHeaderPanel = new Panel
            {
                Name = "pnlCheckHeader",
                Size = new Size(62, 32),
                Margin = new Padding(4, 4, 0, 0),
                Cursor = Cursors.Hand,
                BackColor = ThemeHelper.IsDarkMode
                    ? Color.FromArgb(40, 44, 52)
                    : Color.FromArgb(241, 243, 244)
            };

            _chkHeaderBox = new CheckBox
            {
                Name = "chkHeaderBox",
                Size = new Size(40, 32),
                Location = new Point(0, 0),
                Appearance = Appearance.Normal,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                ForeColor = ThemeHelper.TextPrimary,
                Text = "",
                Cursor = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleCenter,
                CheckAlign = ContentAlignment.MiddleCenter,
                ThreeState = false
            };
            _chkHeaderBox.FlatAppearance.BorderSize = 0;
            _chkHeaderBox.FlatAppearance.MouseOverBackColor = Color.Transparent;
            _chkHeaderBox.FlatAppearance.CheckedBackColor = Color.Transparent;
            _chkHeaderBox.CheckedChanged += (s, e) =>
            {
                if (_suppressHeaderSync) return;
                SetAllCheckboxes(_chkHeaderBox.Checked);
            };

            var btnArrow = new Button
            {
                Name = "btnCheckArrow",
                Text = "▾",
                Size = new Size(22, 32),
                Location = new Point(40, 0),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                ForeColor = ThemeHelper.TextPrimary,
                Font = new Font("Segoe UI", 8f),
                Cursor = Cursors.Hand,
                Padding = Padding.Empty,
                Margin = Padding.Empty
            };
            btnArrow.FlatAppearance.BorderSize = 0;
            btnArrow.FlatAppearance.MouseOverBackColor = ThemeHelper.IsDarkMode
                ? Color.FromArgb(55, 60, 70)
                : Color.FromArgb(218, 220, 224);
            btnArrow.Click += (s, e) =>
            {
                var menu = new ContextMenuStrip { Font = ThemeHelper.FontDefault };
                var itemAll = new ToolStripMenuItem("All");
                var itemNone = new ToolStripMenuItem("None");
                itemAll.Click += (_, __) => { SetAllCheckboxes(true); _chkHeaderBox.Checked = true; };
                itemNone.Click += (_, __) => { SetAllCheckboxes(false); _chkHeaderBox.Checked = false; };
                menu.Items.Add(itemAll);
                menu.Items.Add(itemNone);
                menu.Show(btnArrow, new Point(0, btnArrow.Height));
            };

            _chkHeaderPanel.Paint += (s, e) =>
            {
                using var pen = new Pen(ThemeHelper.IsDarkMode
                    ? Color.FromArgb(65, 70, 80)
                    : Color.FromArgb(200, 202, 206), 1f);
                e.Graphics.DrawLine(pen, 40, 4, 40, _chkHeaderPanel.Height - 4);
            };

            _chkHeaderPanel.Controls.Add(_chkHeaderBox);
            _chkHeaderPanel.Controls.Add(btnArrow);

            flowToolbar.Controls.Add(_chkHeaderPanel);
            flowToolbar.Controls.SetChildIndex(_chkHeaderPanel, flowToolbar.Controls.IndexOf(btnAdd));
        }

        private void SetAllCheckboxes(bool check)
        {
            if (!dgvLocations.Columns.Contains("ColSelect")) return;
            foreach (DataGridViewRow row in dgvLocations.Rows)
                row.Cells["ColSelect"].Value = check;
            dgvLocations.RefreshEdit();
        }

        private void SyncHeaderCheckbox()
        {
            if (!dgvLocations.Columns.Contains("ColSelect") || _chkHeaderBox == null) return;
            _suppressHeaderSync = true;
            int total = dgvLocations.Rows.Count;
            int checked_ = dgvLocations.Rows.Cast<DataGridViewRow>()
                .Count(r => Convert.ToBoolean(r.Cells["ColSelect"].Value));
            _chkHeaderBox.Checked = total > 0 && checked_ == total;
            _suppressHeaderSync = false;
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
            SyncHeaderCheckbox();
        }

        // ── Centralised toast helper — delete=red, edit=yellow, everything else=green ──
        private void ShowToast(string message, ToastType type = ToastType.Success, ToastIntent intent = ToastIntent.Normal)
        {
            Color? col = intent switch
            {
                ToastIntent.Delete => Color.FromArgb(211, 47, 47),
                ToastIntent.Edit   => Color.FromArgb(245, 158, 11),
                _                  => Color.FromArgb(56, 142, 60)
            };
            ToastHelper.Show(this, message, type, 3000, col);
        }
        private enum ToastIntent { Normal, Edit, Delete }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            using var form = new AddEditLocationForm();
            if (form.ShowDialog() == DialogResult.OK)
            {
                LoadLocations();
                ShowToast("Location added successfully.");
            }
        }

        private void btnEdit_Click(object sender, EventArgs e)
        {
            var selectedIds = GetSelectedLocationIds();
            if (selectedIds.Count > 1)
            {
                ShowToast("Multiple locations can't be edited at once — please select only one.", ToastType.Warning, ToastIntent.Edit);
                return;
            }

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
                    ShowToast("Location updated successfully.", ToastType.Success, ToastIntent.Edit);
                }
            }
            else
            {
                ToastHelper.Show(this, result.ErrorMessage ?? "Location not found.", ToastType.Error);
            }
        }

        private List<int> GetSelectedLocationIds()
        {
            var ids = new List<int>();

            if (dgvLocations.Columns.Contains("ColSelect"))
            {
                foreach (DataGridViewRow row in dgvLocations.Rows)
                {
                    var val = row.Cells["ColSelect"].Value;
                    if (val is bool b && b)
                    {
                        if (int.TryParse(row.Cells["ID"].Value?.ToString(), out int id))
                            ids.Add(id);
                    }
                }
            }

            if (ids.Count == 0)
            {
                foreach (DataGridViewRow row in dgvLocations.SelectedRows)
                {
                    if (int.TryParse(row.Cells["ID"].Value?.ToString(), out int id))
                        ids.Add(id);
                }
            }

            return ids;
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            var selectedIds = GetSelectedLocationIds();
            if (selectedIds.Count == 0)
            {
                ShowToast("Please select at least one location to delete.", ToastType.Info, ToastIntent.Delete);
                return;
            }

            string confirmMsg = selectedIds.Count == 1
                ? "Are you sure you want to delete this location?"
                : $"Are you sure you want to delete these {selectedIds.Count} locations?";

            if (MessageBox.Show(confirmMsg, "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            int deletedCount = 0;
            string? firstError = null;

            foreach (var id in selectedIds)
            {
                var result = _locationService.Delete(id);
                if (result.Success)
                    deletedCount++;
                else if (firstError == null)
                    firstError = result.ErrorMessage;
            }

            if (deletedCount > 0)
            {
                LoadLocations();
                ShowToast($"{deletedCount} location(s) deleted successfully.", ToastType.Success, ToastIntent.Delete);
            }
            else if (firstError != null)
            {
                ShowToast(firstError, ToastType.Error, ToastIntent.Delete);
            }
        }
        private void btnImport_Click(object sender, EventArgs e)
        {
            using var ofd = new OpenFileDialog
            {
                Filter = "Excel & CSV Files (*.xlsx;*.xls;*.csv)|*.xlsx;*.xls;*.csv|All Files (*.*)|*.*",
                Title = "Import Locations"
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

                    ShowToast($"Successfully imported {importedCount} locations.");
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
            SetAllCheckboxes(false);
            if (_chkHeaderBox != null) _chkHeaderBox.Checked = false;
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
                sb.AppendLine("Location Name,Address/Building,Room Number,Room Name");    

                foreach (DataGridViewRow row in dgvLocations.Rows)
        {
            sb.AppendLine(string.Join(",",
                $"\"{row.Cells["Name"]?.Value}\"",
                $"\"{row.Cells["Building"]?.Value}\"",
                $"\"{row.Cells["RoomNumber"]?.Value}\"",
                $"\"{row.Cells["RoomName"]?.Value}\""));
        }

        System.IO.File.WriteAllText(sfd.FileName, sb.ToString());
     ShowToast("Locations exported successfully.");

            }
            catch (Exception ex)
            {
       ToastHelper.Show(this, $"Export failed: {ex.Message}", ToastType.Error);
            }
}
    }
}