// Forms/LogsForm.cs
using Inventory_Management_System.Helpers;
using Inventory_Management_System.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing.Drawing2D;


namespace Inventory_Management_System.Forms
{
    public partial class LogsForm : Form
    {
        private List<EquipmentLog> _allLogs = new();

        private int _currentPage = 1;
        private int _pageSize = 25;
        private int _totalPages = 1;
        private int _totalMatchingCount = 0;

        private string _sortColumn = "CreatedAt";
        private bool _sortAscending = false;

        public LogsForm()
        {
            InitializeComponent();
            flowToolbar.SizeChanged += (s, e) => panelTop.Height = flowToolbar.Height;
            ApplyTheme();
            LoadLogs();

            this.Shown += (s, e) =>
            {
                dgvLogs.ClearSelection();
                dgvLogs.CurrentCell = null;
            };
        }


        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_CLIENTEDGE = 0x200;

        private static GraphicsPath RoundedRectPath(RectangleF r, float radius)
        {
            var path = new GraphicsPath();
            float d = radius * 2f;
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        private void ApplyRoundedSearchStyle(TextBox txt)
        {
            var parent = txt.Parent;
            if (parent == null) return;

            var originalSize = txt.Size;
            var originalMargin = txt.Margin;
            int idx = parent.Controls.GetChildIndex(txt);
            parent.Controls.Remove(txt);

            var wrapper = new Panel
            {
                Size = originalSize,
                Margin = originalMargin,
                BackColor = Color.White
            };

            Color inputBorder = Color.FromArgb(209, 213, 219);
            Color focusBorder = Color.FromArgb(0x52, 0xA2, 0x3F);
            Color currentBorder = inputBorder;

            void RoundInput(object? s, EventArgs e)
            {
                if (wrapper.Width <= 0 || wrapper.Height <= 0) return;
                using var path = RoundedRectPath(new RectangleF(0, 0, wrapper.Width, wrapper.Height), 8f);
                wrapper.Region = new Region(path);
            }
            wrapper.Resize += RoundInput;

            wrapper.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var path = RoundedRectPath(new RectangleF(0.5f, 0.5f, wrapper.Width - 1, wrapper.Height - 1), 8f);
                using var pen = new Pen(currentBorder, 1f);
                e.Graphics.DrawPath(pen, path);
            };

            var searchIcon = new Label
            {
                Text = "🔍",
                Font = new Font("Segoe UI Emoji", 8.5f),
                ForeColor = ThemeHelper.TextSecondary,
                AutoSize = false,
                Size = new Size(24, wrapper.Height),
                Location = new Point(6, 0),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };

            txt.BorderStyle = BorderStyle.None;
            int exStyle = GetWindowLong(txt.Handle, GWL_EXSTYLE);
            SetWindowLong(txt.Handle, GWL_EXSTYLE, exStyle & ~WS_EX_CLIENTEDGE);
            txt.BackColor = Color.White;
            txt.Location = new Point(30, (wrapper.Height - txt.Height) / 2);
            txt.Width = wrapper.Width - 38;

            wrapper.Controls.Add(searchIcon);
            wrapper.Controls.Add(txt);
            parent.Controls.Add(wrapper);
            parent.Controls.SetChildIndex(wrapper, idx);

            txt.Enter += (s, e) => { currentBorder = focusBorder; wrapper.Invalidate(); };
            txt.Leave += (s, e) => { currentBorder = inputBorder; wrapper.Invalidate(); };

            RoundInput(null, EventArgs.Empty);
        }


        private void ApplyTheme()
        {
            ThemeHelper.ApplyFormBackground(this);
            ThemeHelper.StyleToolbarPanel(panelTop);
            ThemeHelper.StyleButton(btnRefresh, Color.FromArgb(33, 150, 243));
            ThemeHelper.StyleSecondaryButton(btnExport);
            ApplyRoundedSearchStyle(txtSearch); 
            ThemeHelper.StyleComboBox(cmbPageSize);
            ThemeHelper.StyleSecondaryButton(btnPrevPage);
            ThemeHelper.StyleSecondaryButton(btnNextPage);
            ThemeHelper.StyleDataGridView(dgvLogs);

            panelBottom.BackColor = Color.FromArgb(249, 250, 251);
            lblRowCount.ForeColor = ThemeHelper.TextSecondary;
            lblRowCount.Font = ThemeHelper.FontSmall;

            if (dgvLogs.Columns["Action"] != null) dgvLogs.Columns["Action"]!.FillWeight = 22;
            if (dgvLogs.Columns["OldValue"] != null) dgvLogs.Columns["OldValue"]!.FillWeight = 18;
            if (dgvLogs.Columns["NewValue"] != null) dgvLogs.Columns["NewValue"]!.FillWeight = 18;
            if (dgvLogs.Columns["ID"] != null) dgvLogs.Columns["ID"].Visible = false;
        }

        private void dgvLogs_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            string columnName = dgvLogs.Columns[e.ColumnIndex].Name;

            string sortKey = columnName switch
            {
                "ID" => "Id",
                "Equipment" => "EquipmentCode",
                "QRCode" => "QrCode",
                "Action" => "ActionType",
                "User" => "ChangedBy",
                "OldValue" => "OldValue",
                "NewValue" => "NewValue",
                "Date" => "CreatedAt",
                _ => "CreatedAt"
            };

            if (_sortColumn == sortKey)
                _sortAscending = !_sortAscending;
            else
            {
                _sortColumn = sortKey;
                _sortAscending = false;
            }

            _currentPage = 1;
            if (string.IsNullOrWhiteSpace(txtSearch.Text))
                LoadLogs();
            else
                RefreshGrid(txtSearch.Text.Trim());
        }

        private void LoadLogs(string filter = "")
        {
            try
            {
                using var db = new InventoryDbContext();

                IQueryable<EquipmentLog> baseQuery = db.EquipmentLogs
                    .Include(l => l.Equipment)
                    .Include(l => l.ChangedByUser);

                Func<IQueryable<EquipmentLog>, IOrderedQueryable<EquipmentLog>> sortFunc = _sortColumn switch
                {
                    "Id" => q => _sortAscending ? q.OrderBy(l => l.Id) : q.OrderByDescending(l => l.Id),
                    "EquipmentCode" => q => _sortAscending ? q.OrderBy(l => l.Equipment!.EquipmentCode) : q.OrderByDescending(l => l.Equipment!.EquipmentCode),
                    "QrCode" => q => _sortAscending ? q.OrderBy(l => l.Equipment!.QrCode) : q.OrderByDescending(l => l.Equipment!.QrCode),
                    "ActionType" => q => _sortAscending ? q.OrderBy(l => l.ActionType) : q.OrderByDescending(l => l.ActionType),
                    "ChangedBy" => q => _sortAscending ? q.OrderBy(l => l.ChangedByUser!.FullName) : q.OrderByDescending(l => l.ChangedByUser!.FullName),
                    "OldValue" => q => _sortAscending ? q.OrderBy(l => l.OldValue) : q.OrderByDescending(l => l.OldValue),
                    "NewValue" => q => _sortAscending ? q.OrderBy(l => l.NewValue) : q.OrderByDescending(l => l.NewValue),
                    _ => q => _sortAscending ? q.OrderBy(l => l.CreatedAt) : q.OrderByDescending(l => l.CreatedAt)
                };

                var query = sortFunc(baseQuery);

                if (string.IsNullOrWhiteSpace(filter))
                {
                    _totalMatchingCount = query.Count();
                    _totalPages = Math.Max(1, (int)Math.Ceiling(_totalMatchingCount / (double)_pageSize));
                    if (_currentPage > _totalPages) _currentPage = _totalPages;

                    _allLogs = query
                        .Skip((_currentPage - 1) * _pageSize)
                        .Take(_pageSize)
                        .ToList();
                }
                else
                {
                    _allLogs = query.ToList();
                }
            }
            catch
            {
                _allLogs = new List<EquipmentLog>();
                _totalMatchingCount = 0;
                _totalPages = 1;
            }

            RefreshGrid(filter);
        }

        private void RefreshGrid(string filter = "")
        {
            dgvLogs.Rows.Clear();

            List<EquipmentLog> filtered;
            int totalMatching;

            if (string.IsNullOrWhiteSpace(filter))
            {
                filtered = _allLogs;
                totalMatching = _totalMatchingCount;
                _totalPages = Math.Max(1, (int)Math.Ceiling(totalMatching / (double)_pageSize));
            }
            else
            {
                // Scanned/typed codes need an exact substring match — fuzzy matching
                // lets "EQ003" incorrectly match EQ001, EQ002, etc.
                var codeMatches = _allLogs.Where(l =>
                    (!string.IsNullOrEmpty(l.Equipment?.EquipmentCode) &&
                        l.Equipment.EquipmentCode.Contains(filter, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrEmpty(l.Equipment?.QrCode) &&
                        l.Equipment.QrCode.Contains(filter, StringComparison.OrdinalIgnoreCase)))
                    .ToList();

                filtered = codeMatches.Count > 0
                    ? codeMatches
                    : _allLogs.Where(l =>
                            FuzzySearchHelper.IsFuzzyMatch(l.ActionType, filter) ||
                            FuzzySearchHelper.IsFuzzyMatch(l.ChangedByUser?.FullName, filter) ||
                            FuzzySearchHelper.IsFuzzyMatch(l.OldValue, filter) ||
                            FuzzySearchHelper.IsFuzzyMatch(l.NewValue, filter))
                        .OrderByDescending(l => Math.Max(
                            FuzzySearchHelper.FuzzyScore(l.ActionType, filter),
                            Math.Max(
                                FuzzySearchHelper.FuzzyScore(l.ChangedByUser?.FullName, filter),
                                Math.Max(
                                    FuzzySearchHelper.FuzzyScore(l.OldValue, filter),
                                    FuzzySearchHelper.FuzzyScore(l.NewValue, filter)))))
                        .ToList();

                totalMatching = filtered.Count;
                _totalPages = Math.Max(1, (int)Math.Ceiling(totalMatching / (double)_pageSize));
                if (_currentPage > _totalPages) _currentPage = _totalPages;

                filtered = filtered
                    .Skip((_currentPage - 1) * _pageSize)
                    .Take(_pageSize)
                    .ToList();
            }

            foreach (var item in filtered)
            {
                int rowIdx = dgvLogs.Rows.Add(
                    item.Id,
                    item.Equipment?.EquipmentCode ?? "—",
                    item.Equipment?.Name ?? "—",
                    item.Equipment?.Make ?? "—",
                    item.Equipment?.QrCode ?? "—",
                    FormatAction(item.ActionType),
                  item.ChangedByUser != null ? $"{item.ChangedByUser.FullName} (@{item.ChangedByUser.Username})" : "System",
                    item.OldValue ?? "",
                    item.NewValue ?? "",
                  TimeHelper.ToPakistanTime(item.CreatedAt)?.ToString("yyyy-MM-dd HH:mm:ss") ?? ""
                );

                var row = dgvLogs.Rows[rowIdx];
                var actionCell = row.Cells["Action"];

                switch (item.ActionType)
                {
                    case "equipment_created":
                        actionCell.Style.BackColor = Color.FromArgb(200, 230, 201);
                        actionCell.Style.ForeColor = Color.FromArgb(27, 94, 32);
                        break;
                    case "allocation_approved":
                    case "allocation_requested":
                        actionCell.Style.BackColor = Color.FromArgb(187, 222, 251);
                        actionCell.Style.ForeColor = Color.FromArgb(13, 71, 161);
                        break;
                    case "allocation_returned":
                        actionCell.Style.BackColor = Color.FromArgb(255, 249, 196);
                        actionCell.Style.ForeColor = Color.FromArgb(130, 100, 0);
                        break;
                    case "allocation_rejected":
                    case "decommission_requested":
                    case "decommission_approved":
                        actionCell.Style.BackColor = Color.FromArgb(255, 205, 210);
                        actionCell.Style.ForeColor = Color.FromArgb(183, 28, 28);
                        break;
                    case "transferred":
                        actionCell.Style.BackColor = Color.FromArgb(225, 190, 231);
                        actionCell.Style.ForeColor = Color.FromArgb(74, 20, 140);
                        break;
                    case "location_changed":
                        actionCell.Style.BackColor = Color.FromArgb(255, 224, 178);
                        actionCell.Style.ForeColor = Color.FromArgb(130, 60, 0);
                        break;
                    case "maintenance_opened":
                        actionCell.Style.BackColor = Color.FromArgb(255, 224, 130);
                        actionCell.Style.ForeColor = Color.FromArgb(130, 90, 0);
                        break;
                    case "maintenance_resolved":
                        actionCell.Style.BackColor = Color.FromArgb(197, 225, 165);
                        actionCell.Style.ForeColor = Color.FromArgb(51, 105, 30);
                        break;
                }
                actionCell.Style.Font = ThemeHelper.FontBold;
            }

            lblRowCount.Text = $"Showing {filtered.Count} of {totalMatching} records";
            lblPageIndicator.Text = $"Page {_currentPage} of {Math.Max(1, _totalPages)}";
            btnPrevPage.Enabled = _currentPage > 1;
            btnNextPage.Enabled = _currentPage < _totalPages;
        }

        private static string FormatAction(string? raw) => raw switch
        {
            "equipment_created" => "Equipment Created",
            "location_changed" => "Location Changed",
            "allocation_requested" => "Allocation Requested",
            "allocation_approved" => "Allocation Approved",
            "allocation_rejected" => "Allocation Rejected",
            "allocation_returned" => "Returned to Store",
            "decommission_requested" => "Decommission Requested",
            "decommission_approved" => "Decommission Approved",
            "decommission_rejected" => "Decommission Rejected",
            "transferred" => "Transferred",
            "maintenance_opened" => "Maintenance Opened",
            "maintenance_resolved" => "Maintenance Resolved",
            _ => raw ?? "Unknown"
        };

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            txtSearch.Text = string.Empty;
            _currentPage = 1;
            LoadLogs();
        }

        private void txtSearch_TextChanged(object sender, EventArgs e)
        {
            _currentPage = 1;
            LoadLogs(txtSearch.Text.Trim());
        }

        private void dgvLogs_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var row = dgvLogs.Rows[e.RowIndex];

            string action = row.Cells["Action"].Value?.ToString() ?? "";
            string user = row.Cells["User"].Value?.ToString() ?? "";
            string date = row.Cells["Date"].Value?.ToString() ?? "";
            string eqCode = row.Cells["Equipment"].Value?.ToString() ?? "";
            string qrCode = row.Cells["QRCode"].Value?.ToString() ?? "";
            string eqName = row.Cells["Name"].Value?.ToString() ?? "";
            string make = row.Cells["Make"].Value?.ToString() ?? "";
            string old = row.Cells["OldValue"].Value?.ToString() ?? "";
            string @new = row.Cells["NewValue"].Value?.ToString() ?? "";

            // Model isn't a grid column — pull it from the underlying log by ID
            string model = "—";
            if (int.TryParse(row.Cells["ID"].Value?.ToString(), out int logId))
            {
                var log = _allLogs.FirstOrDefault(l => l.Id == logId);
                model = log?.Equipment?.Model ?? "—";
            }

            using var popup = new LogDetailPopup(action, user, date, eqCode, qrCode, eqName, make, model, old, @new);
            popup.ShowDialog(this);
        }

        private void cmbPageSize_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (int.TryParse(cmbPageSize.SelectedItem?.ToString(), out int size))
            {
                _pageSize = size;
                _currentPage = 1;
                if (string.IsNullOrWhiteSpace(txtSearch.Text))
                    LoadLogs();
                else
                    RefreshGrid(txtSearch.Text.Trim());
            }
        }

        private void btnPrevPage_Click(object sender, EventArgs e)
        {
            if (_currentPage <= 1) return;
            _currentPage--;
            if (string.IsNullOrWhiteSpace(txtSearch.Text))
                LoadLogs();
            else
                RefreshGrid(txtSearch.Text.Trim());
        }

        private void btnNextPage_Click(object sender, EventArgs e)
        {
            if (_currentPage >= _totalPages) return;
            _currentPage++;
            if (string.IsNullOrWhiteSpace(txtSearch.Text))
                LoadLogs();
            else
                RefreshGrid(txtSearch.Text.Trim());
        }

        private void btnExport_Click(object sender, EventArgs e)
        {
            try
            {
                using var dlg = new SaveFileDialog
                {
                    Filter = "CSV files (*.csv)|*.csv",
                    FileName = $"AuditLogs_{DateTime.Now:yyyyMMdd_HHmm}.csv"
                };

                if (dlg.ShowDialog() != DialogResult.OK) return;

                var sb = new StringBuilder();
                sb.AppendLine("Log ID,Equipment Code,QR Code,Action,Changed By,Previous Value,New Value,Timestamp");

                foreach (DataGridViewRow row in dgvLogs.Rows)
                {
                    string Escape(object? v) => $"\"{(v?.ToString() ?? "").Replace("\"", "\"\"")}\"";
                    sb.AppendLine(string.Join(",",
                        Escape(row.Cells["ID"].Value),
                        Escape(row.Cells["Equipment"].Value),
                        Escape(row.Cells["QRCode"].Value),
                        Escape(row.Cells["Action"].Value),
                        Escape(row.Cells["User"].Value),
                        Escape(row.Cells["OldValue"].Value),
                        Escape(row.Cells["NewValue"].Value),
                        Escape(row.Cells["Date"].Value)));
                }

                System.IO.File.WriteAllText(dlg.FileName, sb.ToString());
                ToastHelper.Show(this, $"Logs exported successfully to:\n{dlg.FileName}", ToastType.Success);
            }
            catch (Exception ex)
            {
                ToastHelper.Show(this, "Export failed: " + ex.Message, ToastType.Error);
            }
        }
    }
}