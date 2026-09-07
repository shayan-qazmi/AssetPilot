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
    public partial class LogsForm : Form, IThemableForm
    {
        private List<EquipmentLog> _allLogs = new();

        private int _currentPage = 1;
        private int _pageSize = 100;
        private int _totalPages = 1;
        private int _totalMatchingCount = 0;

        private string _sortColumn = "CreatedAt";
        private bool _sortAscending = false;

        private readonly Dictionary<string, string> _baseHeaderText = new();
        private System.Windows.Forms.Timer _searchDebounceTimer = null!;

        public LogsForm()
        {
            InitializeComponent();
            SetupPageSizeOptions();
            flowToolbar.SizeChanged += (s, e) => panelTop.Height = flowToolbar.Height;
            ApplyTheme();

            foreach (DataGridViewColumn col in dgvLogs.Columns)
                _baseHeaderText[col.Name] = col.HeaderText;

            _searchDebounceTimer = new System.Windows.Forms.Timer { Interval = 300 };
            _searchDebounceTimer.Tick += (s, e) =>
            {
                _searchDebounceTimer.Stop();
                _currentPage = 1;
                LoadLogs(txtSearch.Text.Trim());
            };

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

        public LogsForm(string equipmentCode) : this()
        {
            txtSearch.Text = equipmentCode;
        }

        private void SetupPageSizeOptions()
        {
            string current = cmbPageSize.SelectedItem?.ToString() ?? "100";
            cmbPageSize.Items.Clear();
            cmbPageSize.Items.AddRange(new object[] { "100", "250", "500", "1000" });
            cmbPageSize.SelectedItem = cmbPageSize.Items.Contains(current) ? current : "100";
        }


        public void ApplyTheme()
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

            panelBottom.BackColor = ThemeHelper.IsDarkMode ? Color.FromArgb(22, 24, 29) : Color.FromArgb(249, 250, 251);
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
                "Name" => "EquipmentName",
                "Make" => "EquipmentMake",
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

        private void UpdateSortIndicators()
        {
            string sortedColumnName = _sortColumn switch
            {
                "Id" => "ID",
                "EquipmentCode" => "Equipment",
                "EquipmentName" => "Name",
                "EquipmentMake" => "Make",
                "QrCode" => "QRCode",
                "ActionType" => "Action",
                "ChangedBy" => "User",
                "OldValue" => "OldValue",
                "NewValue" => "NewValue",
                _ => "Date"
            };

            foreach (DataGridViewColumn col in dgvLogs.Columns)
            {
                if (!_baseHeaderText.TryGetValue(col.Name, out var baseText)) continue;
                col.HeaderText = col.Name == sortedColumnName
                    ? baseText + (_sortAscending ? " ▲" : " ▼")
                    : baseText;
            }
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
                    "EquipmentName" => q => _sortAscending ? q.OrderBy(l => l.Equipment!.Name) : q.OrderByDescending(l => l.Equipment!.Name),
                    "EquipmentMake" => q => _sortAscending ? q.OrderBy(l => l.Equipment!.Make) : q.OrderByDescending(l => l.Equipment!.Make),
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
            dgvLogs.SuspendLayout();
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
                    TimeHelper.ToPakistanTime(item.CreatedAt)?.ToString("dd MMM yyyy, hh:mm tt") ?? "—"
);

                var row = dgvLogs.Rows[rowIdx];
                var actionCell = row.Cells["Action"];

                switch (item.ActionType)
                {
                    // ── Created (Equipment/Employee/Location) → unified green ──
                    case "equipment_created":
                    case "employee_created":
                    case "location_created":
                        actionCell.Style.BackColor = Color.FromArgb(200, 230, 201);
                        actionCell.Style.ForeColor = Color.FromArgb(27, 94, 32);
                        break;

                    // ── Equipment edit ──
                    case "equipment_updated":
                    case "employee_updated":
                    case "location_updated":
                    case "Expected Return Date Updated":
                        actionCell.Style.BackColor = Color.FromArgb(255, 249, 196);
                        actionCell.Style.ForeColor = Color.FromArgb(130, 100, 0);
                        break;

                    // ── Equipment Restore ──
                    case "equipment_restored":
                        actionCell.Style.BackColor = Color.FromArgb(204, 243, 239);
                        actionCell.Style.ForeColor = Color.FromArgb(0, 150, 136); // Teal to match restore button
                        break;

                    // ── Restored from Decommission → lighter teal, distinct
                    // from the general Equipment Restored teal above ─────────
                    case "decommission_undone":
                        actionCell.Style.BackColor = Color.FromArgb(224, 247, 244);
                        actionCell.Style.ForeColor = Color.FromArgb(38, 166, 154);
                        break;

                    // ── Allocation approved → dark orange (distinct from
                    // Overdue's light amber and Edit's yellow) ──────────────────
                    case "allocation_approved":
                        actionCell.Style.BackColor = Color.FromArgb(255, 204, 128);
                        actionCell.Style.ForeColor = Color.FromArgb(230, 81, 0);
                        break;

                    // ── Allocation request deleted → unique orange-red, distinct
                    // from both the deep delete-red (equipment/employee/location)
                    // and the warmer decommission red ───────────────────────────
                    case "Allocation Request Deleted":
                        actionCell.Style.BackColor = Color.FromArgb(255, 171, 145);
                        actionCell.Style.ForeColor = Color.FromArgb(191, 54, 12);
                        break;

                    // ── Return Undone → same orange family as Approved, lighter shade ──
                    case "return_undone":
                        actionCell.Style.BackColor = Color.FromArgb(255, 224, 178);
                        actionCell.Style.ForeColor = Color.FromArgb(239, 108, 0);
                        break;

                    // ── Allocation requested (pending) → dark green, matches the
                    // Pending status pill/Approve button ────────────────────────
                    case "allocation_requested":
                        actionCell.Style.BackColor = Color.FromArgb(46, 125, 50);
                        actionCell.Style.ForeColor = Color.White;
                        break;

              

                    // ── Return → indigo (matches Return/Undo Return buttons) ─
                    case "allocation_returned":
                        actionCell.Style.BackColor = Color.FromArgb(225, 226, 252);
                        actionCell.Style.ForeColor = Color.FromArgb(57, 60, 180);
                        break;

                    // ── Checked Out → blue ─────────────────────────────────
                    case "checked_out":
                        actionCell.Style.BackColor = Color.FromArgb(179, 229, 252);
                        actionCell.Style.ForeColor = Color.FromArgb(1, 87, 155);
                        break;

                    // ── Checked In → same blue family, lighter shade ───────
                    case "checked_in":
                        actionCell.Style.BackColor = Color.FromArgb(209, 240, 253);
                        actionCell.Style.ForeColor = Color.FromArgb(2, 119, 189);
                        break;

                    // ── Decommission / Delete / Settings wipe → red ───────
                    case "allocation_rejected":
                    case "equipment_deleted":
                    case "employee_deleted":
                    case "location_deleted":
                    case "settings_data_delete":
                        actionCell.Style.BackColor = Color.FromArgb(255, 205, 210);
                        actionCell.Style.ForeColor = Color.FromArgb(183, 28, 28);
                        // For settings data wipe also highlight the whole row in light red
                        if (item.ActionType == "settings_data_delete")
                        {
                            row.DefaultCellStyle.BackColor = Color.FromArgb(255, 235, 238);
                            row.DefaultCellStyle.ForeColor = Color.FromArgb(183, 28, 28);
                        }
                        break;

                    // ── Decommission requested/approved → lighter, warmer red,
                    // distinct from the deep delete-red above ──────────────────
                    case "decommission_requested":
                    case "decommission_approved":
                        actionCell.Style.BackColor = Color.FromArgb(255, 224, 220);
                        actionCell.Style.ForeColor = Color.FromArgb(211, 47, 47);
                        break;

                    case "allocation_overdue":
                        actionCell.Style.BackColor = Color.FromArgb(255, 224, 178); // slightly stronger amber tint
                        actionCell.Style.ForeColor = Color.FromArgb(255, 160, 0);   // exact Dashboard Overdue color — unchanged
                        break;

                    // ── Transfer → purple ─────────────────────────────────
                    case "transferred":
                        actionCell.Style.BackColor = Color.FromArgb(225, 190, 231);
                        actionCell.Style.ForeColor = Color.FromArgb(74, 20, 140);
                        break;

                    // ── Location changed / maintenance → amber ────────────
                    case "location_changed":
                        actionCell.Style.BackColor = Color.FromArgb(255, 224, 178);
                        actionCell.Style.ForeColor = Color.FromArgb(130, 60, 0);
                        break;

                    case "maintenance_opened":
                        actionCell.Style.BackColor = Color.FromArgb(187, 222, 251); // distinct light-blue tint, separate from Check In/Out's blue
                        actionCell.Style.ForeColor = Color.FromArgb(33, 150, 243);  // matches Dashboard's Under Repair blue exactly
                        break;

                    case "maintenance_resolved":
                        actionCell.Style.BackColor = Color.FromArgb(178, 235, 242); // unique cyan — distinct from all other greens/teals/blues
                        actionCell.Style.ForeColor = Color.FromArgb(0, 131, 143);
                        break;

                    // ── Maintenance history cleared → unique pinkish-red,
                    // distinct from the reds used for delete/reject/decommission ──
                    case "Maintenance History Cleared":
                        actionCell.Style.BackColor = Color.FromArgb(252, 228, 236);
                        actionCell.Style.ForeColor = Color.FromArgb(194, 24, 91);
                        break;

                    // ── Fallback for any unmapped action type → neutral gray,
                    // so it reads as "uncolored" rather than a rendering bug ──
                    default:
                        actionCell.Style.BackColor = Color.FromArgb(224, 224, 224);
                        actionCell.Style.ForeColor = Color.FromArgb(97, 97, 97);
                        break;
                }
                actionCell.Style.Font = ThemeHelper.FontBold;
            }

            dgvLogs.ClearSelection();
            dgvLogs.CurrentCell = null;

            lblRowCount.Text = $"Showing {filtered.Count} of {totalMatching} records";
            lblPageIndicator.Text = $"Page {_currentPage} of {Math.Max(1, _totalPages)}";
            btnPrevPage.Enabled = _currentPage > 1;
            btnNextPage.Enabled = _currentPage < _totalPages;

            UpdateSortIndicators();
            dgvLogs.ResumeLayout();
        }

        private static string FormatAction(string? raw) => raw switch
        {
            "equipment_created"    => "Equipment Created",
            "equipment_updated"    => "Equipment Updated",
            "equipment_deleted"    => "Equipment Deleted",
            "equipment_restored"   => "Equipment Restored",
            "location_changed"     => "Location Changed",
            "employee_created"     => "Employee Created",
            "employee_updated"     => "Employee Updated",
            "employee_deleted"     => "Employee Deleted",
            "location_created"     => "Location Created",
            "location_updated"     => "Location Updated",
            "location_deleted"     => "Location Deleted",
            "allocation_requested" => "Allocation Requested",
            "allocation_approved"  => "Allocation Approved",
            "allocation_rejected"  => "Allocation Rejected",
            "allocation_returned" => "Returned to Store",
            "Allocation Request Deleted" => "Allocation Request Deleted",
            "allocation_overdue"   => "Allocation Overdue",
            "decommission_requested" => "Decommission Requested",
            "decommission_approved"  => "Decommission Approved",
            "decommission_undone" => "Restored from Decommission",
            "return_undone" => "Returned to Active Allocation",
            "decommission_rejected"  => "Decommission Rejected",
            "transferred"            => "Transferred",
            "checked_out"            => "Checked Out",
            "checked_in"             => "Checked In",
            "maintenance_opened"     => "Maintenance Opened",
            "maintenance_resolved"   => "Maintenance Resolved",
            "settings_data_delete"   => "⚠ Settings Data Deleted",
            "Maintenance History Cleared" => "Maintenance History Cleared",
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
            _searchDebounceTimer.Stop();
            _searchDebounceTimer.Start();
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

            // If the log contains a Source Screen, extract it and append to the Action string
            if (@new.Contains("Source Screen:"))
            {
                var parts = @new.Split(" | ").ToList();
                var screenPart = parts.FirstOrDefault(p => p.Trim().StartsWith("Source Screen:"));
                if (screenPart != null)
                {
                    string screenName = screenPart.Replace("Source Screen:", "").Trim();
                    action = $"{action} (via {screenName})";
                    parts.Remove(screenPart);
                    @new = string.Join(" | ", parts); // Remove it from the popup body
                }
            }

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