using Inventory_Management_System.Helpers;
using Inventory_Management_System.Models;
using Inventory_Management_System.Services;
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Inventory_Management_System.Forms
{
    public partial class DeletedItemsForm : Form
    {
        private readonly EquipmentService _equipmentService = new();
        private DataGridView dgv = null!;
        private Panel panelTop = null!;
        private Button btnRestore = null!;
        private Button btnDelete = null!;
        private Button btnRefresh = null!;
        private ComboBox cmbSelectMode = null!;
        private Label lblRowCount = null!;


        private int _currentPage = 1;
        private int _pageSize = 25;
        private int _totalPages = 1;
        private Label lblPageIndicator = null!;
        private Button btnPrevPage = null!;
        private Button btnNextPage = null!;
        private ComboBox cmbPageSize = null!;
        private Panel panelBottom = null!;

        public DeletedItemsForm()
        {
            BuildUI();
            ApplyTheme();
            LoadDeletedItems();

            this.Shown += (s, e) =>
            {
                dgv.ClearSelection();
                dgv.CurrentCell = null;
            };
        }

        private void BuildUI()
        {
            Text = "Deleted Items";
            Dock = DockStyle.Fill;

            panelTop = new Panel { Dock = DockStyle.Top, Height = 56 };

            var flowToolbar = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(8, 8, 8, 8)
            };

            btnRestore = new Button { Text = "↩  Restore Selected", Size = new Size(160, 32), Margin = new Padding(4, 4, 0, 0) };
            btnDelete = new Button { Text = "🗑  Delete Permanently", Size = new Size(170, 32), Margin = new Padding(4, 4, 0, 0) };

            cmbSelectMode = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDown,
                Text = "Select",
                Size = new Size(120, 32),
                Cursor = Cursors.Hand,
                FlatStyle = FlatStyle.Flat,
                Margin = new Padding(4, 4, 0, 0)
            };
            cmbSelectMode.KeyPress += (s, e) => e.Handled = true;
            cmbSelectMode.Items.Add("Select ✔");
            cmbSelectMode.Items.Add("Select All");
            cmbSelectMode.SelectedIndexChanged += CmbSelectMode_SelectedIndexChanged;

            btnRefresh = new Button { Text = "🔄  Refresh", Size = new Size(100, 32), Margin = new Padding(4, 4, 0, 0) };

            btnRestore.Click += BtnRestore_Click;
            btnDelete.Click += BtnDelete_Click;
            btnRefresh.Click += (s, e) => LoadDeletedItems();

            flowToolbar.Controls.Add(btnRestore);
            flowToolbar.Controls.Add(btnDelete);
            flowToolbar.Controls.Add(cmbSelectMode);
            flowToolbar.Controls.Add(btnRefresh);

            panelTop.Controls.Add(flowToolbar);

            dgv = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AllowUserToAddRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = true
            };
            dgv.Columns.Add("Id", "Id");
            dgv.Columns["Id"].Visible = false;
            dgv.Columns.Add("Code", "Equipment Code");
            dgv.Columns.Add("QRCode", "QR Code");
            dgv.Columns.Add("ProcType", "Procurement Type");
            dgv.Columns.Add("Name", "Equipment Name");
            dgv.Columns.Add("Make", "Make");
            dgv.Columns.Add("Model", "Model");
            dgv.Columns.Add("Serial", "Serial Number");
            dgv.Columns.Add("Category", "Category");
            dgv.Columns.Add("PurchaseDate", "Purchase Date");

            var chkCol = new DataGridViewCheckBoxColumn
            {
                Name = "ColSelect",
                HeaderText = "",
                Width = 40,
                Visible = false,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.None
            };
            dgv.Columns.Add(chkCol);

            dgv.ReadOnly = false;
            foreach (DataGridViewColumn col in dgv.Columns)
                col.ReadOnly = col.Name != "ColSelect";

            dgv.CurrentCellDirtyStateChanged += (s, ev) =>
            {
                if (dgv.CurrentCell is DataGridViewCheckBoxCell)
                    dgv.CommitEdit(DataGridViewDataErrorContexts.Commit);
            };

            dgv.CellDoubleClick += Dgv_CellDoubleClick;

            lblRowCount = new Label
            {
                Dock = DockStyle.Bottom,
                Height = 30,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(12, 0, 0, 0)
            };

            panelBottom = new Panel { Dock = DockStyle.Bottom, Height = 44 };

            btnNextPage = new Button { Text = "Next ▶", Size = new Size(80, 28), Anchor = AnchorStyles.Top | AnchorStyles.Right };
            lblPageIndicator = new Label { AutoSize = true, TextAlign = ContentAlignment.MiddleCenter, Anchor = AnchorStyles.Top | AnchorStyles.Right };
            btnPrevPage = new Button { Text = "◀ Prev", Size = new Size(80, 28), Anchor = AnchorStyles.Top | AnchorStyles.Right };
            cmbPageSize = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Size = new Size(70, 28), Anchor = AnchorStyles.Top | AnchorStyles.Right };
            cmbPageSize.Items.AddRange(new object[] { "25", "50", "100", "250", "500", "1000" });
            cmbPageSize.SelectedItem = "25";
            cmbPageSize.SelectedIndexChanged += (s, e) =>
            {
                if (int.TryParse(cmbPageSize.SelectedItem?.ToString(), out int size))
                { _pageSize = size; _currentPage = 1; LoadDeletedItems(); }
            };
            btnPrevPage.Click += (s, e) => { if (_currentPage > 1) { _currentPage--; LoadDeletedItems(); } };
            btnNextPage.Click += (s, e) => { if (_currentPage < _totalPages) { _currentPage++; LoadDeletedItems(); } };

            void RepositionPagination()
            {
                int rightMargin = 12, gap = 8;
                int x = panelBottom.Width - rightMargin;
                x -= btnNextPage.Width; btnNextPage.Location = new Point(x, 8); x -= gap;
                x -= lblPageIndicator.PreferredWidth; lblPageIndicator.Location = new Point(x, 14); x -= gap;
                x -= btnPrevPage.Width; btnPrevPage.Location = new Point(x, 8); x -= gap;
                x -= cmbPageSize.Width; cmbPageSize.Location = new Point(x, 8);
            }
            panelBottom.Resize += (s, e) => RepositionPagination();

            panelBottom.Controls.Add(cmbPageSize);
            panelBottom.Controls.Add(btnPrevPage);
            panelBottom.Controls.Add(lblPageIndicator);
            panelBottom.Controls.Add(btnNextPage);

            Controls.Add(dgv);
            Controls.Add(panelBottom);
            Controls.Add(lblRowCount);
            Controls.Add(panelTop);

            RepositionPagination();
        }

        private void ApplyTheme()
        {
            ThemeHelper.ApplyFormBackground(this);
            ThemeHelper.StyleToolbarPanel(panelTop);
            ThemeHelper.StyleButton(btnRestore, Color.FromArgb(0, 150, 136));
            ThemeHelper.StyleDangerButton(btnDelete);
            ThemeHelper.StyleButton(cmbSelectMode as Control == null ? btnRefresh : btnRefresh); // no-op guard
            ThemeHelper.StyleSecondaryButton(btnRefresh);
            ThemeHelper.StyleComboBox(cmbSelectMode);
            ThemeHelper.StyleDataGridView(dgv);
            ThemeHelper.StyleComboBox(cmbPageSize);
            ThemeHelper.StyleSecondaryButton(btnPrevPage);
            ThemeHelper.StyleSecondaryButton(btnNextPage);
            panelBottom.BackColor = ThemeHelper.IsDarkMode ? Color.FromArgb(22, 24, 29) : Color.FromArgb(249, 250, 251);
            lblRowCount.ForeColor = ThemeHelper.TextSecondary;
            lblRowCount.Font = ThemeHelper.FontSmall;
        }

        private void LoadDeletedItems()
        {
            var result = _equipmentService.GetDeleted();
            dgv.Rows.Clear();

            if (!result.Success)
            {
                ToastHelper.Show(this, result.ErrorMessage, ToastType.Error);
                return;
            }

            var all = result.Data ?? new();
            _totalPages = Math.Max(1, (int)Math.Ceiling(all.Count / (double)_pageSize));
            if (_currentPage > _totalPages) _currentPage = _totalPages;
            var pageItems = all.Skip((_currentPage - 1) * _pageSize).Take(_pageSize).ToList();

            foreach (var eq in pageItems)
            {
                dgv.Rows.Add(
                    eq.Id, eq.EquipmentCode, eq.QrCode, eq.ProcurementType?.TypeName ?? "—",
                    eq.Name, eq.Make ?? "—", eq.Model ?? "—", eq.SerialNumber,
                    eq.Category ?? "—", eq.PurchaseDate?.ToString("yyyy-MM-dd") ?? "—", false);
            }

            if (cmbSelectMode.SelectedItem?.ToString() == "Select All")
            {
                dgv.MultiSelect = true;
                dgv.SelectAll();
            }

            lblRowCount.Text = $"Showing {pageItems.Count} of {all.Count} deleted item(s)";
            lblPageIndicator.Text = $"Page {_currentPage} of {_totalPages}";
            btnPrevPage.Enabled = _currentPage > 1;
            btnNextPage.Enabled = _currentPage < _totalPages;
        }

        private System.Collections.Generic.List<int> GetSelectedIds()
        {
            var ids = new System.Collections.Generic.List<int>();

            if (cmbSelectMode.SelectedItem?.ToString() == "Select ✔")
            {
                foreach (DataGridViewRow row in dgv.Rows)
                {
                    var val = row.Cells["ColSelect"].Value;
                    if (val is bool b && b)
                        ids.Add((int)row.Cells["Id"].Value);
                }
            }
            else
            {
                foreach (DataGridViewRow row in dgv.SelectedRows)
                    ids.Add((int)row.Cells["Id"].Value);
            }

            return ids;
        }

        private void CmbSelectMode_SelectedIndexChanged(object? sender, EventArgs e)
        {
            string mode = cmbSelectMode.SelectedItem?.ToString() ?? "";

            if (mode == "Select ✔")
            {
                dgv.MultiSelect = false;
                dgv.ClearSelection();
                dgv.Columns["ColSelect"].Visible = true;
                dgv.Columns["ColSelect"].DisplayIndex = 0;
            }
            else if (mode == "Select All")
            {
                dgv.Columns["ColSelect"].Visible = false;
                dgv.MultiSelect = true;
                dgv.SelectAll();
            }
            else
            {
                dgv.Columns["ColSelect"].Visible = false;
                dgv.MultiSelect = false;
                dgv.ClearSelection();
            }
        }

        private void BtnRestore_Click(object? sender, EventArgs e)
        {
            var ids = GetSelectedIds();
            if (ids.Count == 0)
            {
                ToastHelper.Show(this, "Please select at least one item to restore.", ToastType.Info);

                return;
            }

            if (MessageBox.Show($"Restore {ids.Count} item(s) back to Equipment?",
                    "Confirm Restore", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            int successCount = ids.Count(id => _equipmentService.Restore(id).Success);

            LoadDeletedItems();
           ToastHelper.Show(this, $"{successCount} of {ids.Count} item(s) restored.", ToastType.Success);

        }

        private void BtnDelete_Click(object? sender, EventArgs e)
        {
            var ids = GetSelectedIds();
            if (ids.Count == 0)
            {
                ToastHelper.Show(this, "Please select at least one item to permanently delete.", ToastType.Info);

                return;
            }

            if (MessageBox.Show(
                    $"Permanently delete {ids.Count} item(s)? This CANNOT be undone.",
                    "Confirm Permanent Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                return;

            int successCount = ids.Count(id => _equipmentService.HardDelete(id).Success);

            LoadDeletedItems();
          ToastHelper.Show(this, $"{successCount} of {ids.Count} item(s) permanently deleted.", ToastType.Success);

        }

        private void Dgv_CellDoubleClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var row = dgv.Rows[e.RowIndex];
            int id = (int)row.Cells["Id"].Value;
            string name = row.Cells["Name"].Value?.ToString() ?? "";
            string code = row.Cells["Code"].Value?.ToString() ?? "";

            using var dlg = new Form
            {
                Text = "Deleted Item",
                Size = new Size(360, 180),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                MaximizeBox = false,
                MinimizeBox = false
            };
            ThemeHelper.ApplyFormBackground(dlg);

            var lbl = new Label
            {
                Text = $"'{name}' ({code})",
                AutoSize = false,
                Size = new Size(320, 40),
                Location = new Point(20, 20),
                Font = ThemeHelper.FontBold
            };

            var btnRestoreRow = new Button { Text = "Restore", Size = new Size(90, 34), Location = new Point(20, 80) };
            var btnDeleteRow = new Button { Text = "Delete", Size = new Size(90, 34), Location = new Point(120, 80) };
            var btnCancelRow = new Button { Text = "Cancel", Size = new Size(90, 34), Location = new Point(220, 80) };

            ThemeHelper.StyleButton(btnRestoreRow, Color.FromArgb(0, 150, 136));
            ThemeHelper.StyleDangerButton(btnDeleteRow);
            ThemeHelper.StyleSecondaryButton(btnCancelRow);

            btnRestoreRow.Click += (s, ev) => { dlg.Tag = "restore"; dlg.Close(); };
            btnDeleteRow.Click += (s, ev) => { dlg.Tag = "delete"; dlg.Close(); };
            btnCancelRow.Click += (s, ev) => dlg.Close();

            dlg.Controls.Add(lbl);
            dlg.Controls.Add(btnRestoreRow);
            dlg.Controls.Add(btnDeleteRow);
            dlg.Controls.Add(btnCancelRow);
            dlg.ShowDialog();

            if (dlg.Tag?.ToString() == "restore")
            {
                _equipmentService.Restore(id);
                LoadDeletedItems();
            }
            else if (dlg.Tag?.ToString() == "delete")
            {
                if (MessageBox.Show($"Permanently delete '{name}' ({code})? This CANNOT be undone.",
                        "Confirm Permanent Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                {
                    _equipmentService.HardDelete(id);
                    LoadDeletedItems();
                }
            }
        }
    }
}