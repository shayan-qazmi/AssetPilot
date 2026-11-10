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
        // Filter state
        private string _activeActionCategory = "All";
        private int? _selectedUserId = null;
        private DateTime? _filterStartDate = null;
        private DateTime? _filterEndDate = null;
        private readonly Dictionary<string, List<string>> _categoryActionMap = new()
        {
            { "All", new List<string>() },
            { "Created", new List<string>{"equipment_created","employee_created","location_created"} },
            { "Updated", new List<string>{"equipment_updated","employee_updated","location_updated","Expected Return Date Updated"} },
            { "Allocation", new List<string>{"allocation_requested","allocation_approved","allocation_rejected","allocation_returned","allocation_overdue","return_undone","Allocation Request Deleted"} },
            { "CheckInOut", new List<string>{"checked_out","checked_in"} },
            { "Transfers", new List<string>{"transferred"} },
            { "Decommission", new List<string>{"decommission_requested","decommission_approved","decommission_undone","decommission_rejected"} },
            { "Deleted", new List<string>{"equipment_deleted","employee_deleted","location_deleted","equipment_restored","settings_data_delete"} }
        };

        private int _currentPage = 1;
        private int _pageSize = 100;
        private int _totalPages = 1;
        private int _totalMatchingCount = 0;

        private string _sortColumn = "CreatedAt";
        private bool _sortAscending = false;

        private readonly Dictionary<string, string> _baseHeaderText = new();
        private System.Windows.Forms.Timer _searchDebounceTimer = null!;
        private EntitySuggestHelper<EquipmentLog>? _suggest;
        private List<EquipmentLog> _logSuggestSource = new();

        public LogsForm()
        {
            InitializeComponent();
            SetupPageSizeOptions();
            flowToolbar.SizeChanged += (s, e) =>
            {
                panelTop.Height = flowToolbar.Height + (flowFilterRow?.Height ?? 0);
            };
            flowFilterRow.SizeChanged += (s, e) =>
            {
                panelTop.Height = flowToolbar.Height + flowFilterRow.Height;
            };
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

            SetupSuggestions();
            PopulateUserFilter();

            // Set default date filter to All time (no restriction)
            _filterStartDate = null;
            _filterEndDate   = null;

            // Style the "All" pill as active by default (charcoal), rest inactive
            StylePill(btnFilterAll,          true,  Color.FromArgb(31,  41,  55));
            StylePill(btnFilterCreated,      false, Color.FromArgb(27,  94,  32));
            StylePill(btnFilterUpdated,      false, Color.FromArgb(202, 138, 4));
            StylePill(btnFilterAllocation,   false, Color.FromArgb(234, 88,  12));
            StylePill(btnFilterCheckInOut,   false, Color.FromArgb(1,   87, 155));
            StylePill(btnFilterTransfers,    false, Color.FromArgb(74,  20, 140));
            StylePill(btnFilterDecommission, false, Color.FromArgb(211, 47,  47));
            StylePill(btnFilterDeleted,      false, Color.FromArgb(183, 28,  28));

            LoadLogs();

            this.Shown += (s, e) =>
            {
                dgvLogs.ClearSelection();
                dgvLogs.CurrentCell = null;
            };
        }

        private void SetupSuggestions()
        {
            try
            {
                using var db = new InventoryDbContext();
                _logSuggestSource = db.EquipmentLogs
                    .Include(l => l.Equipment)
                    .Include(l => l.ChangedByUser)
                    .OrderByDescending(l => l.CreatedAt)
                    .Take(2000)
                    .ToList();

                _suggest = new EntitySuggestHelper<EquipmentLog>(
                    textBox: txtSearch,
                    hostForm: this,
                    onSelected: log => { txtSearch.Text = log.Equipment?.EquipmentCode ?? FormatAction(log.ActionType); },
                    scoreFn: (log, term) => Math.Max(
                        FuzzySearchHelper.FuzzyScoreCode(log.Equipment?.EquipmentCode, term),
                        Math.Max(
                            FuzzySearchHelper.FuzzyScoreCode(log.Equipment?.QrCode, term),
                            Math.Max(
                                FuzzySearchHelper.FuzzyScore(FormatAction(log.ActionType), term),
                                FuzzySearchHelper.FuzzyScore(log.ChangedByUser?.FullName, term)))),
                    line1Selector: log => $"{FormatAction(log.ActionType)} — {log.Equipment?.EquipmentCode ?? "—"}",
                    line2Selector: log => log.ChangedByUser?.FullName ?? "System");
                _suggest.LoadItems(_logSuggestSource);
            }
            catch { /* non-critical UI enhancement, fail silently */ }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _suggest?.Dispose();
            base.OnFormClosed(e);
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
            ThemeHelper.StyleSecondaryButton(btnReset);
            ApplyRoundedSearchStyle(txtSearch);
            ThemeHelper.StyleComboBox(cmbPageSize);
            ThemeHelper.StyleComboBox(cmbFilterUser);
            ThemeHelper.StyleComboBox(cmbFilterDate);
            ThemeHelper.StyleComboBox(cmbDensity);
            ThemeHelper.StyleSecondaryButton(btnPrevPage);
            ThemeHelper.StyleSecondaryButton(btnNextPage);
            ThemeHelper.StyleDataGridView(dgvLogs);

            // Style filter row background
            flowFilterRow.BackColor = ThemeHelper.IsDarkMode
                ? Color.FromArgb(28, 32, 38)
                : Color.FromArgb(248, 250, 252);

            // Live status label
            lblLiveStatus.ForeColor = Color.FromArgb(0, 128, 0);
            lblLiveStatus.Font = ThemeHelper.FontSmall;

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

                // ── Category filter ───────────────────────────────────────────
                if (_activeActionCategory != "All" && _categoryActionMap.TryGetValue(_activeActionCategory, out var actions) && actions.Count > 0)
                    baseQuery = baseQuery.Where(l => actions.Contains(l.ActionType));

                // ── User filter ───────────────────────────────────────────────
                if (_selectedUserId.HasValue)
                    baseQuery = baseQuery.Where(l => l.ChangedByUserId == _selectedUserId.Value);

                // ── Date filter ───────────────────────────────────────────────
                if (_filterStartDate.HasValue)
                    baseQuery = baseQuery.Where(l => l.CreatedAt >= _filterStartDate.Value);
                if (_filterEndDate.HasValue)
                    baseQuery = baseQuery.Where(l => l.CreatedAt <= _filterEndDate.Value);

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
            UpdateLiveStatus();
        }

        private void UpdateLiveStatus()
        {
            var pkt = TimeHelper.ToPakistanTime(DateTime.UtcNow);
            lblLiveStatus.Text = $"● Live – updated {pkt.ToString("hh:mm tt")}";
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
            SetupSuggestions();
        }

        // ── Filter pill handlers ──────────────────────────────────────────────
        private void SetActiveCategory(string category)
        {
            _activeActionCategory = category;
            _currentPage = 1;

            // Category → active pill color (matches log row colors)
            var pillColors = new Dictionary<string, Color>
            {
                { "All",          Color.FromArgb(31,  41,  55)  },  // charcoal
                { "Created",      Color.FromArgb(27,  94,  32)  },  // dark green
                { "Updated",      Color.FromArgb(202, 138, 4)   },  // golden yellow (#CA8A04)
                { "Allocation",   Color.FromArgb(234, 88,  12)  },  // orange (#EA580C)
                { "CheckInOut",   Color.FromArgb(1,   87, 155)  },  // dark blue
                { "Transfers",    Color.FromArgb(74,  20, 140)  },  // deep purple
                { "Decommission", Color.FromArgb(211, 47,  47)  },  // red
                { "Deleted",      Color.FromArgb(183, 28,  28)  },  // deep red
            };

            var pillMap = new Dictionary<string, Button>
            {
                { "All",          btnFilterAll },
                { "Created",      btnFilterCreated },
                { "Updated",      btnFilterUpdated },
                { "Allocation",   btnFilterAllocation },
                { "CheckInOut",   btnFilterCheckInOut },
                { "Transfers",    btnFilterTransfers },
                { "Decommission", btnFilterDecommission },
                { "Deleted",      btnFilterDeleted }
            };

            foreach (var kvp in pillMap)
            {
                bool isActive = kvp.Key == category;
                Color activeColor = pillColors.TryGetValue(kvp.Key, out var c) ? c : Color.FromArgb(31, 41, 55);
                StylePill(kvp.Value, isActive, activeColor);
            }

            LoadLogs(txtSearch.Text.Trim());
        }

        private void StylePill(Button btn, bool active, Color activeBackColor)
        {
            if (active)
            {
                btn.BackColor = activeBackColor;
                btn.ForeColor = Color.White;
                btn.FlatStyle = FlatStyle.Flat;
                btn.FlatAppearance.BorderSize = 0;
                btn.Font = new Font(btn.Font, FontStyle.Bold);
            }
            else
            {
                btn.BackColor = Color.White;
                btn.ForeColor = Color.FromArgb(75, 85, 99);
                btn.FlatStyle = FlatStyle.Flat;
                btn.FlatAppearance.BorderColor = Color.FromArgb(229, 231, 235);
                btn.FlatAppearance.BorderSize = 1;
                btn.Font = new Font(btn.Font, FontStyle.Regular);
            }
        }

        private void btnFilterAll_Click(object sender, EventArgs e)         => SetActiveCategory("All");
        private void btnFilterCreated_Click(object sender, EventArgs e)     => SetActiveCategory("Created");
        private void btnFilterUpdated_Click(object sender, EventArgs e)     => SetActiveCategory("Updated");
        private void btnFilterAllocation_Click(object sender, EventArgs e)  => SetActiveCategory("Allocation");
        private void btnFilterCheckInOut_Click(object sender, EventArgs e)  => SetActiveCategory("CheckInOut");
        private void btnFilterTransfers_Click(object sender, EventArgs e)   => SetActiveCategory("Transfers");
        private void btnFilterDecommission_Click(object sender, EventArgs e)=> SetActiveCategory("Decommission");
        private void btnFilterDeleted_Click(object sender, EventArgs e)     => SetActiveCategory("Deleted");

        // ── User / Date filter handlers ───────────────────────────────────────
        private void cmbFilterUser_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbFilterUser.SelectedItem is UserFilterItem item)
                _selectedUserId = item.UserId > 0 ? item.UserId : (int?)null;
            else
                _selectedUserId = null;

            _currentPage = 1;
            LoadLogs(txtSearch.Text.Trim());
        }

        private void cmbFilterDate_SelectedIndexChanged(object sender, EventArgs e)
        {
            var now = DateTime.UtcNow;
            switch (cmbFilterDate.SelectedItem?.ToString())
            {
                case "Last 7 days":
                    _filterStartDate = now.AddDays(-7);
                    _filterEndDate   = null;
                    break;
                case "Last 30 days":
                    _filterStartDate = now.AddDays(-30);
                    _filterEndDate   = null;
                    break;
                case "All time":
                    _filterStartDate = null;
                    _filterEndDate   = null;
                    break;
                case "Custom...":
                    OpenCustomDateRangeDialog();
                    return; // handler inside method will trigger LoadLogs
            }
            _currentPage = 1;
            LoadLogs(txtSearch.Text.Trim());
        }

        private void OpenCustomDateRangeDialog()
        {
            var dlg = new Form
            {
                Text = "Custom Date Range",
                Size = new Size(360, 230),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false
            };
            var lblFrom  = new Label { Text = "From Date:",   Location = new Point(24, 16),  AutoSize = true };
            var dtpFrom  = new DateTimePicker { Location = new Point(24, 36),  Width = 290, Height = 28,
                             Value = DateTime.Today.AddDays(-30) };
            var lblTo    = new Label { Text = "To Date:",     Location = new Point(24, 76),  AutoSize = true };
            var dtpTo    = new DateTimePicker { Location = new Point(24, 96),  Width = 290, Height = 28,
                             Value = DateTime.Today };
            var btnApply = new Button { Text = "Apply", DialogResult = DialogResult.OK,
                             Location = new Point(24, 148), Width = 136, Height = 32 };
            var btnCancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel,
                             Location = new Point(176, 148), Width = 136, Height = 32 };
            dlg.Controls.AddRange(new Control[]{ lblFrom, dtpFrom, lblTo, dtpTo, btnApply, btnCancel });
            dlg.AcceptButton = btnApply;
            dlg.CancelButton = btnCancel;

            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                _filterStartDate = dtpFrom.Value.Date.ToUniversalTime();
                _filterEndDate   = dtpTo.Value.Date.AddDays(1).AddTicks(-1).ToUniversalTime();
                // Update combobox label
                cmbFilterDate.SelectedIndexChanged -= cmbFilterDate_SelectedIndexChanged;
                cmbFilterDate.Items[3] = $"{dtpFrom.Value:MMM dd} – {dtpTo.Value:MMM dd}";
                cmbFilterDate.SelectedIndex = 3;
                cmbFilterDate.SelectedIndexChanged += cmbFilterDate_SelectedIndexChanged;
                _currentPage = 1;
                LoadLogs(txtSearch.Text.Trim());
            }
            else
            {
                // Revert to "Last 30 days"
                cmbFilterDate.SelectedIndexChanged -= cmbFilterDate_SelectedIndexChanged;
                cmbFilterDate.SelectedItem = "Last 30 days";
                cmbFilterDate.SelectedIndexChanged += cmbFilterDate_SelectedIndexChanged;
            }
            dlg.Dispose();
        }

        // ── Reset handler ─────────────────────────────────────────────────────
        private void btnReset_Click(object sender, EventArgs e)
        {
            // Reset all filter state
            _activeActionCategory = "All";
            _selectedUserId       = null;
            _filterStartDate      = null;
            _filterEndDate        = null;
            _currentPage          = 1;
            _sortColumn           = "CreatedAt";
            _sortAscending        = false;

            // Reset UI controls without triggering re-queries
            txtSearch.TextChanged -= txtSearch_TextChanged;
            txtSearch.Text = string.Empty;
            txtSearch.TextChanged += txtSearch_TextChanged;

            cmbFilterUser.SelectedIndexChanged -= cmbFilterUser_SelectedIndexChanged;
            if (cmbFilterUser.Items.Count > 0) cmbFilterUser.SelectedIndex = 0;
            cmbFilterUser.SelectedIndexChanged += cmbFilterUser_SelectedIndexChanged;

            cmbFilterDate.SelectedIndexChanged -= cmbFilterDate_SelectedIndexChanged;
            cmbFilterDate.SelectedIndex = 2; // All time
            _filterStartDate = null;
            _filterEndDate   = null;
            cmbFilterDate.SelectedIndexChanged += cmbFilterDate_SelectedIndexChanged;
            // Reset pill styles
            SetActiveCategory("All");
        }

        // ── Populate the user filter dropdown ─────────────────────────────────
        private void PopulateUserFilter()
        {
            try
            {
                cmbFilterUser.SelectedIndexChanged -= cmbFilterUser_SelectedIndexChanged;
                cmbFilterUser.Items.Clear();
                cmbFilterUser.Items.Add(new UserFilterItem(0, "All users"));

                using var db = new InventoryDbContext();
                var users = db.Users.OrderBy(u => u.FullName).ToList();
                foreach (var u in users)
                    cmbFilterUser.Items.Add(new UserFilterItem(u.Id, u.FullName ?? u.Username));

                cmbFilterUser.SelectedIndex = 0;
                cmbFilterUser.SelectedIndexChanged += cmbFilterUser_SelectedIndexChanged;
            }
            catch { /* non-critical */ }
        }

        // Helper wrapper for user combo items
        private sealed class UserFilterItem
        {
            public int    UserId   { get; }
            public string Name     { get; }
            public UserFilterItem(int id, string name) { UserId = id; Name = name; }
            public override string ToString() => Name;
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