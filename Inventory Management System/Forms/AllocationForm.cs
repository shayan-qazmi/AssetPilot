// Forms/AllocationForm.cs
using Inventory_Management_System.Helpers;
using Inventory_Management_System.Models;
using Inventory_Management_System.Services;
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
    public partial class AllocationForm : Form
    {
        private readonly AllocationService _allocationService = new();
        private readonly EquipmentService _equipmentService = new();
        private readonly EmployeeService _employeeService = new();
        private readonly LocationService _locationService = new();
        private readonly AuditService _audit = new();

        private List<Allocation> _suggestSourceAllocations = new();

        // Full in-memory set of Allocation + Check-In/Out sourced rows
        // (does NOT include "Available" rows — those are queried fresh
        // each RefreshGrid call via GetAvailableEquipmentRows()).
        private List<AllocationRow> _allSourceRows = new();

        private ComboBox cmbEquipmentFilter = null!;
        private ComboBox cmbSelectMode = null!;          // Select All / Deselect All dropdown
        private ComboBox cmbAllocCategoryFilter = null!;
        private string _activeCategoryFilter = "";       // empty = All Categories
        private Button btnEditReturn = null!;
        private string _activeStatusFilter = "All";
        private List<AllocationRow> _lastRenderedRows = new();

        private EntitySuggestHelper<Allocation>? _suggest;

        private int _currentPage = 1;
        private int _pageSize = 25;
        private int _totalPages = 1;
        private int _totalMatchingCount = 0;

        private bool _userClickedGridRow = false;

        private bool _updatingToolbarHeight = false;


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

        private void RecalcToolbarHeight()
        {
            if (_updatingToolbarHeight) return;

            int needed = flowToolbar.GetPreferredSize(new Size(panelTop.ClientSize.Width, 0)).Height;
            if (panelTop.Height != needed)
            {
                _updatingToolbarHeight = true;
                panelTop.Height = needed;
                _updatingToolbarHeight = false;
            }
        }

        private void FlowToolbar_Layout(object? sender, LayoutEventArgs e)
        {
            RecalcToolbarHeight();
        }

        private class AllocationRow
        {
            public int Id { get; set; }
            public string Source { get; set; } = "Allocation"; // "Allocation" | "Check In/Out" | "Available"
            public string Category { get; set; } = "—";
            public string QrCode { get; set; } = "—";
            public string EquipmentCode { get; set; } = "—";
            public string EquipmentName { get; set; } = "—";
            public string SerialNumber { get; set; } = "—";
            public string Make { get; set; } = "—";
            public string Model { get; set; } = "—";
            public string Employee { get; set; } = "—";
            public string Location { get; set; } = "—";
            public DateTime SortDate { get; set; }
            public string DateAllocated { get; set; } = "—";
            public string ExpectedReturn { get; set; } = "—";
            public string Status { get; set; } = "—";
            public string AllocatedBy { get; set; } = "—";

            public string? PicturePath { get; set; }

            public Allocation? AllocationRef { get; set; }
            public int? TransactionId { get; set; }

            public int? EmployeeId { get; set; }
            public int? LocationId { get; set; }
        }

        public AllocationForm()
        {
            InitializeComponent();

            flowToolbar.Layout += FlowToolbar_Layout;   // ← moved up: must be wired before Setup* adds controls

            ApplyTheme();
            SetupEquipmentFilterDropdown();
            SetupSelectModeDropdown();   // ← replaces plain btnSelectAll with dropdown
            SetupCategoryFilterDropdown();
            SetupEditReturnButton();
            LoadAllocations();
            SetupSuggestions();

            dgvAllocation.SelectionChanged += (s, e) => UpdateActionButtonsForSelection();

            dgvAllocation.CellClick += (s, e) =>
            {
                if (e.RowIndex >= 0) _userClickedGridRow = true;
            };
            dgvAllocation.CellDoubleClick += dgvAllocation_CellDoubleClick;
            this.Shown += (s, e) =>
            {
                dgvAllocation.ClearSelection();
                dgvAllocation.CurrentCell = null;
                RecalcToolbarHeight();   // ← final correction once ClientSize.Width is accurate
            };
        }   

        private void SetupEquipmentFilterDropdown()
        {
            cmbEquipmentFilter = new ComboBox
            {
                Name = "cmbEquipmentFilter",
                DropDownStyle = ComboBoxStyle.DropDownList,
                Size = new Size(150, btnRefresh.Height),
                Margin = btnRefresh.Margin,   // ← match a real toolbar sibling's margin
                Anchor = btnRefresh.Anchor,
                Cursor = Cursors.Hand,
                FlatStyle = FlatStyle.Flat,
                BackColor = ThemeHelper.Accent,
                ForeColor = Color.White,
                Font = ThemeHelper.FontBold,
                DrawMode = DrawMode.OwnerDrawFixed,
                DropDownWidth = 260
            };

            cmbEquipmentFilter.Items.Add("Equipment: All");
            cmbEquipmentFilter.Items.Add("Equipment: Available");
            cmbEquipmentFilter.Items.Add("Equipment: Allocated");
            cmbEquipmentFilter.Items.Add("Equipment: Pending");
            cmbEquipmentFilter.SelectedIndex = 0; // "Equipment: All" by default

            cmbEquipmentFilter.DrawItem += (s, e) =>
            {
                if (e.Index < 0) return;
                bool isHover = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
                using (var brush = new SolidBrush(isHover ? ThemeHelper.Header : ThemeHelper.Accent))
                    e.Graphics.FillRectangle(brush, e.Bounds);
                string text = cmbEquipmentFilter.Items[e.Index]?.ToString() ?? "";
                using (var brush = new SolidBrush(Color.White))
                {
                    var sf = new StringFormat { LineAlignment = StringAlignment.Center, Alignment = StringAlignment.Near };
                    e.Graphics.DrawString(text, cmbEquipmentFilter.Font, brush, e.Bounds, sf);
                }
                e.DrawFocusRectangle();
            };

            cmbEquipmentFilter.SelectedIndexChanged += (s, e) =>
            {
                _activeStatusFilter = cmbEquipmentFilter.SelectedItem?.ToString() switch
                {
                    "Equipment: Available" => "Available",
                    "Equipment: Allocated" => "Allocated",
                    "Equipment: Pending" => "Pending",
                    _ => "All"
                };
                _currentPage = 1;
                RefreshGrid(txtSearch.Text.Trim());
            };

            var flow = panelTop.Controls.OfType<FlowLayoutPanel>().First();
            flow.Controls.Add(cmbEquipmentFilter);
            flow.Controls.SetChildIndex(cmbEquipmentFilter, flow.Controls.IndexOf(btnExport) + 1);
        }

        // ── Category filter dropdown — sits between Select and Equipment: All ──
        private void SetupCategoryFilterDropdown()
        {
            cmbAllocCategoryFilter = new ComboBox
            {
                Name = "cmbAllocCategoryFilter",
                DropDownStyle = ComboBoxStyle.DropDownList,
                Size = new Size(130, btnRefresh.Height),
                Margin = btnRefresh.Margin,
                Anchor = btnRefresh.Anchor,
                Cursor = Cursors.Hand,
                FlatStyle = FlatStyle.Flat,
                BackColor = ThemeHelper.Accent,
                ForeColor = Color.White,
                Font = ThemeHelper.FontBold,
                DrawMode = DrawMode.OwnerDrawFixed,
                DropDownWidth = 200
            };

            LoadAllocCategoryFilterItems();

            cmbAllocCategoryFilter.DrawItem += (s, e) =>
            {
                if (e.Index < 0) return;
                bool isHover = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
                using (var brush = new SolidBrush(isHover ? ThemeHelper.Header : ThemeHelper.Accent))
                    e.Graphics.FillRectangle(brush, e.Bounds);
                string text = cmbAllocCategoryFilter.Items[e.Index]?.ToString() ?? "";
                using (var brush = new SolidBrush(Color.White))
                {
                    var sf = new StringFormat { LineAlignment = StringAlignment.Center, Alignment = StringAlignment.Near };
                    e.Graphics.DrawString(text, cmbAllocCategoryFilter.Font, brush, e.Bounds, sf);
                }
                e.DrawFocusRectangle();
            };

            cmbAllocCategoryFilter.SelectedIndexChanged += (s, e) =>
            {
                var selected = cmbAllocCategoryFilter.SelectedItem?.ToString() ?? "All Categories";
                _activeCategoryFilter = selected == "All Categories" ? "" : selected;
                _currentPage = 1;
                RefreshGrid(txtSearch.Text.Trim());
            };

            var flow = panelTop.Controls.OfType<FlowLayoutPanel>().First();
            flow.Controls.Add(cmbAllocCategoryFilter);
            flow.Controls.SetChildIndex(cmbAllocCategoryFilter, flow.Controls.IndexOf(cmbEquipmentFilter));
        }

        private void LoadAllocCategoryFilterItems()
        {
            try
            {
                using var db = new InventoryDbContext();
                var dbCategories = db.Equipment
                    .Where(e => e.Category != null && e.Category != "")
                    .Select(e => e.Category!)
                    .Distinct()
                    .ToList();

                var categoryNames = new CategoryService().GetAll().Data?
                    .Select(c => c.Name).ToList() ?? new List<string>();

                var allCategories = categoryNames
                    .Union(dbCategories, StringComparer.OrdinalIgnoreCase)
                    .OrderBy(c => c, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                cmbAllocCategoryFilter.Items.Clear();
                cmbAllocCategoryFilter.Items.Add("All Categories");
                foreach (var cat in allCategories)
                    cmbAllocCategoryFilter.Items.Add(cat);

                cmbAllocCategoryFilter.SelectedIndex = 0;
            }
            catch
            {
                cmbAllocCategoryFilter.Items.Clear();
                cmbAllocCategoryFilter.Items.Add("All Categories");
                cmbAllocCategoryFilter.SelectedIndex = 0;
            }
        }

        // ── Select dropdown (Select All / Deselect All) ─────────────────────
        // Matches the Equipment screen's cmbSelectMode pattern.
        private void SetupSelectModeDropdown()
        {
            cmbSelectMode = new ComboBox
            {
                Name = "cmbSelectMode",
                DropDownStyle = ComboBoxStyle.DropDown,
                Text = "Select",
                Size = new Size(95, 32),
                Margin = new Padding(4, 4, 0, 0),
                Cursor = Cursors.Hand,
                FlatStyle = FlatStyle.Flat,
                BackColor = ThemeHelper.Accent,
                ForeColor = Color.White,
                Font = ThemeHelper.FontBold,
                DrawMode = DrawMode.OwnerDrawFixed
            };

            // Prevent the user from typing into the header area
            cmbSelectMode.KeyPress += (s, e) => e.Handled = true;

            cmbSelectMode.Items.Add("Select ✔");
            cmbSelectMode.Items.Add("Select All");
            cmbSelectMode.Items.Add("Deselect All");

            // Owner-draw items: green themed to match other Select dropdowns
            cmbSelectMode.DrawItem += (s, e) =>
            {
                if (e.Index < 0) return;
                bool isHighlighted = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
                using (var bg = new SolidBrush(isHighlighted ? ThemeHelper.Header : ThemeHelper.Accent))
                    e.Graphics.FillRectangle(bg, e.Bounds);
                string text = cmbSelectMode.Items[e.Index]?.ToString() ?? "";
                using (var fg = new SolidBrush(Color.White))
                {
                    var sf = new StringFormat { LineAlignment = StringAlignment.Center, Alignment = StringAlignment.Near };
                    e.Graphics.DrawString(text, cmbSelectMode.Font, fg, e.Bounds, sf);
                }
                e.DrawFocusRectangle();
            };

            cmbSelectMode.SelectedIndexChanged += (s, e) =>
            {
                var chosen = cmbSelectMode.SelectedItem?.ToString() ?? "";
                if (chosen == "Select ✔")
                {
                    if (dgvAllocation.Columns.Contains("ColSelect"))
                    {
                        dgvAllocation.Columns["ColSelect"].Visible = true;
                        dgvAllocation.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
                    }
                    dgvAllocation.ClearSelection();
                }
                else if (chosen == "Select All")
                {
                    if (dgvAllocation.Columns.Contains("ColSelect") && dgvAllocation.Columns["ColSelect"].Visible)
                    {
                        foreach (DataGridViewRow r in dgvAllocation.Rows)
                            r.Cells["ColSelect"].Value = true;
                    }
                    else
                    {
                        dgvAllocation.SelectAll();
                    }
                }
                else if (chosen == "Deselect All")
                {
                    if (dgvAllocation.Columns.Contains("ColSelect") && dgvAllocation.Columns["ColSelect"].Visible)
                    {
                        foreach (DataGridViewRow r in dgvAllocation.Rows)
                            r.Cells["ColSelect"].Value = false;
                    }
                    dgvAllocation.ClearSelection();
                    dgvAllocation.CurrentCell = null;
                }

                // Reset the dropdown header text so it reads "Select" again
                cmbSelectMode.SelectedIndex = -1;
                cmbSelectMode.Text = "Select";
                UpdateActionButtonsForSelection();
            };

            var flow = panelTop.Controls.OfType<FlowLayoutPanel>().First();
            flow.Controls.Add(cmbSelectMode);
            // Insert right after the btnExport button
            flow.Controls.SetChildIndex(cmbSelectMode, flow.Controls.IndexOf(btnExport) + 1);

            // Dynamically append Checkbox Column for manual selection (visibly hidden by default)
            if (!dgvAllocation.Columns.Contains("ColSelect"))
            {
                var chkCol = new DataGridViewCheckBoxColumn
                {
                    Name = "ColSelect",
                    HeaderText = "",
                    Width = 40,
                    ReadOnly = false,
                    Visible = false,
                    AutoSizeMode = DataGridViewAutoSizeColumnMode.None
                };
                dgvAllocation.Columns.Add(chkCol);
                dgvAllocation.Columns["ColSelect"].DisplayIndex = 0;

                // Configure grid to allow editing the Checkbox column while others are read-only
                dgvAllocation.ReadOnly = false;
                foreach (DataGridViewColumn col in dgvAllocation.Columns)
                {
                    if (col.Name == "ColSelect")
                        col.ReadOnly = false;
                    else
                        col.ReadOnly = true;
                }

                // Immediately commit edits when clicking the checkbox cell so value updates instantly
                dgvAllocation.CurrentCellDirtyStateChanged += (s, ev) =>
                {
                    if (dgvAllocation.CurrentCell is DataGridViewCheckBoxCell)
                        dgvAllocation.CommitEdit(DataGridViewDataErrorContexts.Commit);
                };

                dgvAllocation.CellValueChanged += (s, ev) =>
                {
                    if (ev.ColumnIndex >= 0 && dgvAllocation.Columns[ev.ColumnIndex].Name == "ColSelect")
                        UpdateActionButtonsForSelection();
                };
            }
        }

        // Equipment with no active Allocation and no active Check-Out —
        // queried fresh each call rather than cached, so it's always current.

        private void SetupEditReturnButton()
        {
            btnEditReturn = new Button
            {
                Text = "✏  Edit",
                Size = btnApprove.Size,
                Margin = btnApprove.Margin,   // ← match Approve's actual margin, not a guessed one
                Anchor = btnApprove.Anchor    // ← match Approve's anchor so vertical alignment is identical
            };
            ThemeHelper.StyleButton(btnEditReturn, ThemeHelper.Warning);
            btnEditReturn.Click += btnEditReturn_Click;

            var flow = panelTop.Controls.OfType<FlowLayoutPanel>().First();
            flow.Controls.Add(btnEditReturn);
            flow.Controls.SetChildIndex(btnEditReturn, flow.Controls.IndexOf(btnApprove));
        }
        private List<AllocationRow> GetAvailableEquipmentRows()
        {
            try
            {
                using var db = new InventoryDbContext();

                var checkedOutEquipmentIds = new HashSet<int>(
                    db.Transactions
                        .Where(t => t.Status == "Pending" || t.Status == "Overdue")
                        .Select(t => t.EquipmentId));

                var activelyAllocatedEquipmentIds = new HashSet<int>(
                    db.Allocations
                        .Where(a => a.Status == "pending" || a.Status == "approved")
                        .Select(a => a.EquipmentId));

                return db.Equipment
                    .Where(e => e.CurrentStatus != "decommissioned" &&
                                e.CurrentStatus != "under_repair" &&
                                !checkedOutEquipmentIds.Contains(e.Id) &&
                                !activelyAllocatedEquipmentIds.Contains(e.Id))
                    .OrderBy(e => e.EquipmentCode)
                    .ToList()
                    .Select(e => new AllocationRow
                    {
                        Id = -(2_000_000 + e.Id),
                        Source = "Available",
                        Category = string.IsNullOrWhiteSpace(e.Category) ? "—" : e.Category,
                        QrCode = string.IsNullOrWhiteSpace(e.QrCode) ? "—" : e.QrCode,
                        EquipmentCode = e.EquipmentCode,
                        EquipmentName = e.Name,
                        SerialNumber = string.IsNullOrWhiteSpace(e.SerialNumber) ? "—" : e.SerialNumber,
                        Make = string.IsNullOrWhiteSpace(e.Make) ? "—" : e.Make,
                        Model = string.IsNullOrWhiteSpace(e.Model) ? "—" : e.Model,
                        Employee = "—",
                        Location = "—",
                        SortDate = DateTime.MinValue,
                        DateAllocated = "—",
                        ExpectedReturn = "—",
                        Status = "available",
                        AllocationRef = null,
                        TransactionId = null
                    })
                    .ToList();
            }
            catch
            {
                return new List<AllocationRow>();
            }
        }

        // "All" = every equipment item regardless of status — union of
        // active Allocation/Check-In-Out rows and unallocated Available rows.
        // No duplicates are possible since GetAvailableEquipmentRows()
        // already excludes anything with an active allocation/checkout.
        private List<AllocationRow> GetAllCombinedRows()
        {
            return _allSourceRows
                .Concat(GetAvailableEquipmentRows())
                .OrderByDescending(r => r.SortDate)
                .ToList();
        }

        // Formats a Location as "LocationName - Room ..." — mirrors RoomOptionDisplay
        // used in AddAllocationForm's Room dropdown, but returns just the name when
        // there's no room info instead of a placeholder.
        private static string FormatLocationDisplay(Location? loc)
        {
            if (loc == null || string.IsNullOrWhiteSpace(loc.LocationName)) return "—";

            bool hasNum = !string.IsNullOrWhiteSpace(loc.RoomNumber);
            bool hasRoomName = !string.IsNullOrWhiteSpace(loc.RoomName);
            if (!hasNum && !hasRoomName) return loc.LocationName;

            string room = hasNum ? $"Room {loc.RoomNumber}" : "";
            if (hasRoomName) room = string.IsNullOrEmpty(room) ? loc.RoomName! : $"{room} ({loc.RoomName})";

            return $"{loc.LocationName} - {room}";
        }

        // Mirrors the "Name (@username)" identity pattern used elsewhere in the app
        // (see the sidebar profile footer) — falls back gracefully if either piece is missing.
        private static string FormatUserDisplay(string? fullName, string? username)
        {
            bool hasFull = !string.IsNullOrWhiteSpace(fullName);
            bool hasUser = !string.IsNullOrWhiteSpace(username);
            if (!hasFull && !hasUser) return "—";
            if (hasFull && hasUser) return $"{fullName} (@{username})";
            return hasFull ? fullName! : $"@{username}";
        }

        private void ApplyTheme()
        {
            ThemeHelper.ApplyFormBackground(this);
            ThemeHelper.StyleToolbarPanel(panelTop);
            ThemeHelper.StyleButton(btnAdd);
            ThemeHelper.StyleButton(btnApprove, Color.FromArgb(25, 118, 210));
            ThemeHelper.StyleButton(btnReturn, Color.FromArgb(245, 124, 0));
            ThemeHelper.StyleButton(btnRefresh, Color.FromArgb(33, 150, 243));
            ThemeHelper.StyleSecondaryButton(btnExport);
            ThemeHelper.StyleDangerButton(btnDelete);
            ApplyRoundedSearchStyle(txtSearch);
            ThemeHelper.StyleComboBox(cmbPageSize);
            ThemeHelper.StyleSecondaryButton(btnPrevPage);
            ThemeHelper.StyleSecondaryButton(btnNextPage);
            ThemeHelper.StyleDataGridView(dgvAllocation);
            panelBottom.BackColor = Color.FromArgb(249, 250, 251);
            lblRowCount.ForeColor = ThemeHelper.TextSecondary;
            lblRowCount.Font = ThemeHelper.FontSmall;
        }

        private void SetupSuggestions()
        {
            try
            {
                _suggest = new EntitySuggestHelper<Allocation>(
      textBox: txtSearch,
      hostForm: this,
      onSelected: a =>
      {
          var row = _allSourceRows.FirstOrDefault(r => r.AllocationRef != null && r.AllocationRef.Id == a.Id);
          if (row != null) ShowOnlyAllocationRow(row);
      },
                      scoreFn: (a, term) => Math.Max(
                        FuzzySearchHelper.FuzzyScoreCode(a.Equipment?.EquipmentCode, term),
                        Math.Max(
                            FuzzySearchHelper.FuzzyScoreCode(a.Equipment?.QrCode, term),
                            Math.Max(
                                FuzzySearchHelper.FuzzyScore(a.Equipment?.Name, term),
                                Math.Max(
                                    FuzzySearchHelper.FuzzyScore(a.Employee?.FullName, term),
                                    Math.Max(
                                        FuzzySearchHelper.FuzzyScore(a.Location?.LocationName, term),
                                        FuzzySearchHelper.FuzzyScore(a.Status, term)))))),
                    line1Selector: a => $"{a.Equipment?.EquipmentCode} — {a.Equipment?.Name}",
                    line2Selector: a => $"{a.Employee?.FullName ?? "—"} • {a.Status}");
                _suggest.LoadItems(_suggestSourceAllocations);
            }
            catch { /* non-critical UI enhancement, fail silently */ }
        }

        private void SelectAllocationRow(int id)
        {
            foreach (DataGridViewRow row in dgvAllocation.Rows)
            {
                if (row.Cells["AllocationID"].Value != null &&
                    Convert.ToInt32(row.Cells["AllocationID"].Value) == id)
                {
                    dgvAllocation.ClearSelection();
                    row.Selected = true;
                    dgvAllocation.CurrentCell = row.Cells[0];
                    dgvAllocation.FirstDisplayedScrollingRowIndex = row.Index;
                    break;
                }
            }
        }

        private void ShowOnlyAllocationRow(AllocationRow item)
        {
            dgvAllocation.Rows.Clear();
            _lastRenderedRows = new List<AllocationRow> { item };

            int rowIdx = dgvAllocation.Rows.Add(
           item.Id,
           item.QrCode,
           item.EquipmentCode,
           item.EquipmentName,
           item.SerialNumber,
           item.Make,
           item.Model,
           item.Employee == "—" ? "" : item.Employee,
           item.Location == "—" ? "" : item.Location,
           item.DateAllocated,
           item.ExpectedReturn,
           item.Status,
           item.Source,
           item.AllocatedBy
        );

            var statusCell = dgvAllocation.Rows[rowIdx].Cells["Status"];
            switch (item.Status)
            {
                case "approved":
                    statusCell.Style.BackColor = Color.FromArgb(200, 230, 201);
                    statusCell.Style.ForeColor = Color.FromArgb(27, 94, 32);
                    break;
                case "pending":
                case "Pending":
                    statusCell.Style.BackColor = Color.FromArgb(255, 249, 196);
                    statusCell.Style.ForeColor = Color.FromArgb(130, 100, 0);
                    break;
                case "rejected":
                    statusCell.Style.BackColor = Color.FromArgb(255, 205, 210);
                    statusCell.Style.ForeColor = Color.FromArgb(183, 28, 28);
                    break;
                case "returned":
                    statusCell.Style.BackColor = Color.FromArgb(224, 224, 224);
                    statusCell.Style.ForeColor = Color.FromArgb(66, 66, 66);
                    break;
                case "Overdue":
                    statusCell.Style.BackColor = Color.FromArgb(255, 205, 210);
                    statusCell.Style.ForeColor = Color.FromArgb(183, 28, 28);
                    break;
                case "available":
                    statusCell.Style.BackColor = Color.FromArgb(224, 231, 255);
                    statusCell.Style.ForeColor = Color.FromArgb(63, 81, 181);
                    break;
            }
            statusCell.Style.Font = ThemeHelper.FontBold;

            var sourceCell = dgvAllocation.Rows[rowIdx].Cells["Source"];
            if (item.Source == "Check In/Out")
            {
                sourceCell.Style.ForeColor = Color.FromArgb(25, 118, 210);
                sourceCell.Style.Font = ThemeHelper.FontBold;
            }
            else if (item.Source == "Available")
            {
                sourceCell.Style.ForeColor = Color.FromArgb(94, 53, 177);
                sourceCell.Style.Font = ThemeHelper.FontBold;
            }
            else
            {
                sourceCell.Style.ForeColor = Color.FromArgb(107, 114, 128);
                sourceCell.Style.Font = ThemeHelper.FontDefault;
            }

            dgvAllocation.ClearSelection();
            dgvAllocation.Rows[0].Selected = true;

            lblRowCount.Text = "Showing 1 of 1 records";
            lblPageIndicator.Text = "Page 1 of 1";
            btnPrevPage.Enabled = false;
            btnNextPage.Enabled = false;

            UpdateActionButtonsForSelection();
        }

        // Loads the underlying Allocation/Check-In-Out data from the DB.
        // Pagination/filtering/rendering all happen in RefreshGrid, which
        // is the single source of truth for what's on screen.
        private void LoadAllocations(string filter = "")
        {
            try
            {
                using var db = new InventoryDbContext();

                var allocQuery = db.Allocations
                       .Where(a => a.Status != "returned")
                       .Include(a => a.Equipment)
                       .Include(a => a.Employee)
                       .Include(a => a.Location)
                       .Include(a => a.AllocatedByUser)
                       .OrderByDescending(a => a.AllocationDate);

                _suggestSourceAllocations = allocQuery.ToList();

                var approvedEquipmentIds = new HashSet<int>(
                    _suggestSourceAllocations
                        .Where(a => a.Status == "approved")
                        .Select(a => a.EquipmentId));

                var checkInOutRaw = db.Transactions
                    .Include(t => t.Equipment)
                    .Where(t => t.TransactionType == "Check-Out" &&
                                (t.Status == "Pending" || t.Status == "Overdue"))
                    .ToList();

                var checkInOutRows = checkInOutRaw
       .Where(t => t.Equipment != null &&
                   t.Equipment.CurrentStatus == "allocated" &&
                   !approvedEquipmentIds.Contains(t.EquipmentId))
       .Select(t => new AllocationRow
       {
           Id = -t.TransactionId,
           Source = "Check In/Out",
           Category = string.IsNullOrWhiteSpace(t.Equipment?.Category) ? "—" : t.Equipment!.Category,
           QrCode = string.IsNullOrWhiteSpace(t.Equipment?.QrCode) ? "—" : t.Equipment!.QrCode,
           EquipmentCode = t.Equipment?.EquipmentCode ?? "—",
           EquipmentName = t.Equipment?.Name ?? "—",
           SerialNumber = t.Equipment?.SerialNumber ?? "—",
           Make = t.Equipment?.Make ?? "—",
           Model = t.Equipment?.Model ?? "—",
           PicturePath = t.Equipment?.PicturePath,
           Employee = string.IsNullOrWhiteSpace(t.IssuedTo) ? "—" : t.IssuedTo,
           Location = string.IsNullOrWhiteSpace(t.Department) ? "—" : t.Department,
           AllocatedBy = string.IsNullOrWhiteSpace(t.OperatorName) ? "—" : t.OperatorName,   // ← fixed back to t.OperatorName
           SortDate = t.TransactionDate,
           DateAllocated = t.TransactionDate.ToString("yyyy-MM-dd"),
           ExpectedReturn = t.ExpectedReturn?.ToString("yyyy-MM-dd") ?? "—",
           Status = t.Status == "Overdue" ? "Overdue" : "approved",
           AllocationRef = null,
           TransactionId = t.TransactionId
       })
       .ToList();

                var allocationRows = _suggestSourceAllocations.Select(a => new AllocationRow
                {
                    Id = a.Id,
                    Source = "Allocation",
                    Category = string.IsNullOrWhiteSpace(a.Equipment?.Category) ? "—" : a.Equipment!.Category,
                    QrCode = string.IsNullOrWhiteSpace(a.Equipment?.QrCode) ? "—" : a.Equipment!.QrCode,
                    EquipmentCode = a.Equipment?.EquipmentCode ?? "—",
                    EquipmentName = a.Equipment?.Name ?? "—",
                    SerialNumber = a.Equipment?.SerialNumber ?? "—",
                    Make = a.Equipment?.Make ?? "—",
                    Model = a.Equipment?.Model ?? "—",
                    PicturePath = a.Equipment?.PicturePath,
                    Employee = a.Employee?.FullName ?? "—",
                    Location = FormatLocationDisplay(a.Location),
                    AllocatedBy = FormatUserDisplay(a.AllocatedByUser?.FullName, a.AllocatedByUser?.Username),   // ← stays here, uses 'a'
                    SortDate = a.AllocationDate,
                    DateAllocated = a.AllocationDate.ToString("yyyy-MM-dd"),
                    ExpectedReturn = a.ExpectedReturnDate?.ToString("yyyy-MM-dd") ?? "—",
                    Status = (a.Status == "approved" && a.ExpectedReturnDate.HasValue &&
                       a.ExpectedReturnDate.Value < DateOnly.FromDateTime(DateTime.Today))
                       ? "Overdue" : a.Status,
                    AllocationRef = a,
                    TransactionId = null,
                    EmployeeId = a.EmployeeId,
                    LocationId = a.LocationId
                }).ToList();

                _allSourceRows = allocationRows
                    .Concat(checkInOutRows)
                    .OrderByDescending(r => r.SortDate)
                    .ToList();

                _suggest?.LoadItems(_suggestSourceAllocations);
                RefreshGrid(filter);
            }
            catch (Exception ex)
            {
                ToastHelper.Show(this, "Error loading allocations: " + ex.Message, ToastType.Error);
            }
        }

        private void RefreshGrid(string filter = "")
        {
            dgvAllocation.Rows.Clear();

            bool hasSearch = !string.IsNullOrWhiteSpace(filter);

            // A search term overrides the status tab entirely — searching
            // for a QR/equipment code must find it regardless of whether
            // it's currently available, allocated, pending, or checked out.
            List<AllocationRow> baseline;
            if (hasSearch)
            {
                baseline = GetAllCombinedRows();
            }
            else
            {
                baseline = _activeStatusFilter switch
                {
                    "Available" => GetAvailableEquipmentRows(),
                    "Allocated" => _allSourceRows.Where(r => string.Equals(r.Status, "approved", StringComparison.OrdinalIgnoreCase) || string.Equals(r.Status, "Overdue", StringComparison.OrdinalIgnoreCase)).ToList(),
                    "Pending" => _allSourceRows.Where(r => string.Equals(r.Status, "pending", StringComparison.OrdinalIgnoreCase)).ToList(),
                    _ => GetAllCombinedRows() // "All"
                };
            }

            if (!string.IsNullOrEmpty(_activeCategoryFilter))
            {
                baseline = baseline.Where(r =>
                    string.Equals(r.Category, _activeCategoryFilter, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            List<AllocationRow> matched;
            if (hasSearch)
            {
                // Scanned/typed codes need an exact substring match — fuzzy matching
                // lets "QR001" incorrectly match QR002, QR005, etc.
                var codeMatches = baseline.Where(r =>
                    (!string.IsNullOrEmpty(r.QrCode) && r.QrCode.Contains(filter, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrEmpty(r.EquipmentCode) && r.EquipmentCode.Contains(filter, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrEmpty(r.SerialNumber) && r.SerialNumber.Contains(filter, StringComparison.OrdinalIgnoreCase)))
                    .ToList();

                matched = codeMatches.Count > 0
                    ? codeMatches
                    : baseline.Where(r =>
                            FuzzySearchHelper.IsFuzzyMatch(r.EquipmentName, filter) ||
                            FuzzySearchHelper.IsFuzzyMatch(r.Employee, filter) ||
                            FuzzySearchHelper.IsFuzzyMatch(r.Location, filter) ||
                            FuzzySearchHelper.IsFuzzyMatch(r.Status, filter) ||
                            FuzzySearchHelper.IsFuzzyMatch(r.Source, filter))
                        .OrderByDescending(r => Math.Max(
                            FuzzySearchHelper.FuzzyScore(r.EquipmentName, filter),
                            Math.Max(
                                FuzzySearchHelper.FuzzyScore(r.Employee, filter),
                                FuzzySearchHelper.FuzzyScore(r.Location, filter))))
                        .ToList();
            }
            else
            {
                matched = baseline;
            }

            _totalMatchingCount = matched.Count;
            _totalPages = Math.Max(1, (int)Math.Ceiling(_totalMatchingCount / (double)_pageSize));
            if (_currentPage > _totalPages) _currentPage = _totalPages;

            var items = matched
                .Skip((_currentPage - 1) * _pageSize)
                .Take(_pageSize)
                .ToList();

            _lastRenderedRows = items;

            foreach (var item in items)
            {
                int rowIdx = dgvAllocation.Rows.Add(
          item.Id,
          item.QrCode,
          item.EquipmentCode,
          item.EquipmentName,
          item.SerialNumber,
          item.Make,
          item.Model,
          item.Employee == "—" ? "" : item.Employee,
          item.Location == "—" ? "" : item.Location,
          item.DateAllocated,
          item.ExpectedReturn,
          item.Status,
          item.Source,
          item.AllocatedBy
      );

                var statusCell = dgvAllocation.Rows[rowIdx].Cells["Status"];
                switch (item.Status)
                {
                    case "approved":
                        statusCell.Style.BackColor = Color.FromArgb(200, 230, 201);
                        statusCell.Style.ForeColor = Color.FromArgb(27, 94, 32);
                        break;
                    case "pending":
                    case "Pending":
                        statusCell.Style.BackColor = Color.FromArgb(255, 249, 196);
                        statusCell.Style.ForeColor = Color.FromArgb(130, 100, 0);
                        break;
                    case "rejected":
                        statusCell.Style.BackColor = Color.FromArgb(255, 205, 210);
                        statusCell.Style.ForeColor = Color.FromArgb(183, 28, 28);
                        break;
                    case "returned":
                        statusCell.Style.BackColor = Color.FromArgb(224, 224, 224);
                        statusCell.Style.ForeColor = Color.FromArgb(66, 66, 66);
                        break;
                    case "Overdue":
                        statusCell.Style.BackColor = Color.FromArgb(255, 205, 210);
                        statusCell.Style.ForeColor = Color.FromArgb(183, 28, 28);
                        break;
                    case "available":
                        statusCell.Style.BackColor = Color.FromArgb(224, 231, 255);
                        statusCell.Style.ForeColor = Color.FromArgb(63, 81, 181);
                        break;
                }
                statusCell.Style.Font = ThemeHelper.FontBold;

                var sourceCell = dgvAllocation.Rows[rowIdx].Cells["Source"];
                if (item.Source == "Check In/Out")
                {
                    sourceCell.Style.ForeColor = Color.FromArgb(25, 118, 210);
                    sourceCell.Style.Font = ThemeHelper.FontBold;
                }
                else if (item.Source == "Available")
                {
                    sourceCell.Style.ForeColor = Color.FromArgb(94, 53, 177);
                    sourceCell.Style.Font = ThemeHelper.FontBold;
                }
                else
                {
                    sourceCell.Style.ForeColor = Color.FromArgb(107, 114, 128);
                    sourceCell.Style.Font = ThemeHelper.FontDefault;
                }
            }

            dgvAllocation.ClearSelection();
            dgvAllocation.CurrentCell = null;
            _userClickedGridRow = false;

            lblRowCount.Text = $"Showing {items.Count} of {_totalMatchingCount} records";
            lblPageIndicator.Text = $"Page {_currentPage} of {Math.Max(1, _totalPages)}";
            btnPrevPage.Enabled = _currentPage > 1;
            btnNextPage.Enabled = _currentPage < _totalPages;

            UpdateActionButtonsForSelection();
        }

        // ── Selection helpers ───────────────────────────────────────────────

        /// <summary>Returns the single selected row, or null if 0 or multiple rows are selected.</summary>
        private AllocationRow? GetSelectedRow()
        {
            var rows = GetSelectedRows();
            if (rows.Count != 1) return null;
            return rows[0];
        }

        /// <summary>Returns ALL currently selected rows (1 or more).</summary>
        private List<AllocationRow> GetSelectedRows()
        {
            var result = new List<AllocationRow>();
            bool useCheckbox = dgvAllocation.Columns.Contains("ColSelect") && dgvAllocation.Columns["ColSelect"].Visible;

            if (useCheckbox)
            {
                foreach (DataGridViewRow gridRow in dgvAllocation.Rows)
                {
                    if (Convert.ToBoolean(gridRow.Cells["ColSelect"].Value) == true)
                    {
                        if (!int.TryParse(gridRow.Cells["AllocationID"].Value?.ToString(), out int id)) continue;
                        var row = _lastRenderedRows.FirstOrDefault(r => r.Id == id);
                        if (row != null) result.Add(row);
                    }
                }
            }
            else
            {
                foreach (DataGridViewRow gridRow in dgvAllocation.SelectedRows)
                {
                    if (!int.TryParse(gridRow.Cells["AllocationID"].Value?.ToString(), out int id)) continue;
                    var row = _lastRenderedRows.FirstOrDefault(r => r.Id == id);
                    if (row != null) result.Add(row);
                }
            }
            return result;
        }

        private Allocation? GetSelectedAllocation() => GetSelectedRow()?.AllocationRef;

        private void UpdateActionButtonsForSelection()
        {
            // Buttons stay enabled at all times — validation and feedback happen
            // inside each click handler via ToastHelper, so the user always gets
            // a message explaining why an action can't proceed (e.g. selecting
            // "Available" equipment for Approve/Return/Edit/Delete).
            btnApprove.Enabled = true;
            btnReturn.Enabled = true;
            btnDelete.Enabled = true;
            btnEditReturn.Enabled = true;
        }

        private void btnEditReturn_Click(object? sender, EventArgs e)
        {
            var alloc = GetSelectedAllocation();
            if (alloc == null)
            {
                ToastHelper.Show(this, "Select an allocation first.", ToastType.Info);
                return;
            }
            if (alloc.Status == "returned")
            {
                ToastHelper.Show(this, "Cannot edit the return date of a returned allocation.", ToastType.Warning);
                return;
            }

            using var frm = new EditExpectedReturnDialog(alloc.ExpectedReturnDate);
            if (frm.ShowDialog() != DialogResult.OK) return;

            try
            {
                using var db = new InventoryDbContext();
                var a = db.Allocations.FirstOrDefault(x => x.Id == alloc.Id);
                if (a == null) return;

                string oldVal = alloc.ExpectedReturnDate?.ToString("yyyy-MM-dd") ?? "none";
                a.ExpectedReturnDate = frm.SelectedDate;
                db.SaveChanges();

                _audit.Log(alloc.EquipmentId, "Expected Return Date Updated",
                    oldVal, frm.SelectedDate?.ToString("yyyy-MM-dd") ?? "none");

                _audit.Log(alloc.EquipmentId, "Expected Return Date Updated",
    oldVal, frm.SelectedDate?.ToString("yyyy-MM-dd") ?? "none");
                ToastHelper.Show(this, "Expected return date updated.", ToastType.Success);
                LoadAllocations();

                // Overdue/approved status is already derived from ExpectedReturnDate
                // vs. today in LoadAllocations() — reloading picks up the new status
                // automatically, no extra logic needed here.
                LoadAllocations();
            }
            catch (Exception ex)
            {
                ToastHelper.Show(this, "Update failed: " + ex.Message, ToastType.Error);
            }
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            string? preselectCode = null;
            if (_userClickedGridRow)
            {
                var selectedRow = GetSelectedRow();
                preselectCode = selectedRow?.EquipmentCode;
                if (preselectCode == "—") preselectCode = null;
            }

            using var frm = new AddAllocationForm(
                _equipmentService.GetAll().Data ?? new(),
                _employeeService.GetAll().Data ?? new(),
                _locationService.GetAll().Data ?? new(),
                preselectCode);

            if (frm.ShowDialog() != DialogResult.OK) return;

            var result = _allocationService.Allocate(frm.Result!);
            if (result.Success)
            {
                _audit.Log(frm.Result!.EquipmentId, AuditService.ActionAllocationRequested,
                    null, $"Allocated to Employee ID {frm.Result.EmployeeId}");
                LoadAllocations();
                if (result.Success)
                {
                    _audit.Log(frm.Result!.EquipmentId, AuditService.ActionAllocationRequested,
                        null, $"Allocated to Employee ID {frm.Result.EmployeeId}");
                    ToastHelper.Show(this, "Allocation request created.", ToastType.Success);
                    LoadAllocations();
                }
            }
            else
                ToastHelper.Show(this, "Allocation Failed: " + result.ErrorMessage, ToastType.Error);
        }

        private void dgvAllocation_CellDoubleClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= dgvAllocation.Rows.Count) return;

            var clickedRow = dgvAllocation.Rows[e.RowIndex];
            var idValue = clickedRow.Cells["AllocationID"].Value;
            if (idValue == null || !int.TryParse(idValue.ToString(), out int rowId)) return;

            var row = _lastRenderedRows.FirstOrDefault(r => r.Id == rowId);
            if (row == null) return;

            // Anything that isn't "Available" already has allocation details to show.
            if (row.Source != "Available")
            {
                ShowAllocationDetailPopup(row);
                return;
            }

            string? preselectCode = row.EquipmentCode == "—" ? null : row.EquipmentCode;

            using var frm = new AddAllocationForm(
                _equipmentService.GetAll().Data ?? new(),
                _employeeService.GetAll().Data ?? new(),
                _locationService.GetAll().Data ?? new(),
                preselectCode);

            if (frm.ShowDialog() != DialogResult.OK) return;

            var result = _allocationService.Allocate(frm.Result!);
            if (result.Success)
            {
                _audit.Log(frm.Result!.EquipmentId, AuditService.ActionAllocationRequested,
                    null, $"Allocated to Employee ID {frm.Result.EmployeeId}");
                ToastHelper.Show(this, "Allocation request created.", ToastType.Success);
                LoadAllocations();
            }
            else
                ToastHelper.Show(this, "Allocation Failed: " + result.ErrorMessage, ToastType.Error);
        }

        private void ShowAllocationDetailPopup(AllocationRow row)
        {
            var info = new AllocationDetailInfo
            {
                EquipmentCode = row.EquipmentCode,
                EquipmentName = row.EquipmentName,
                QrCode = row.QrCode,
                SerialNumber = row.SerialNumber,
                Make = row.Make,
                Model = row.Model,
                PicturePath = row.PicturePath,
                Employee = row.Employee,
                Location = row.Location,
                DateAllocated = row.DateAllocated,
                ExpectedReturn = row.ExpectedReturn,
                Status = row.Status,
                Source = row.Source,
                AllocatedBy = row.AllocatedBy
            };

            using var popup = new AllocationDetailPopup(info);
            popup.ShowDialog(this);
        }

        // ── Bulk-aware action handlers ──────────────────────────────────────

        private void btnApprove_Click(object sender, EventArgs e)
        {
            var rows = GetSelectedRows()
                  .Where(r => r.AllocationRef != null && r.AllocationRef.Status == "pending")
                  .ToList();

            if (rows.Count == 0)
            {
                ToastHelper.Show(this, "Select one or more pending allocations to approve.", ToastType.Info);
                return;
            }

            // Single-row: use original detailed confirm flow
            if (rows.Count == 1)
            {
                ApproveSingle(rows[0].AllocationRef!);
                return;
            }

            if (MessageBox.Show($"Approve {rows.Count} pending allocations?",
                "Confirm Bulk Approve", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            int ok = 0, fail = 0;
            foreach (var row in rows)
            {
                try
                {
                    using var db = new InventoryDbContext();
                    bool isCheckedOut = db.Transactions.Any(t =>
                        t.EquipmentId == row.AllocationRef!.EquipmentId &&
                        (t.Status == "Pending" || t.Status == "Overdue"));
                    if (isCheckedOut) { fail++; continue; }

                    var a = db.Allocations.Include(x => x.Equipment)
                                          .FirstOrDefault(x => x.Id == row.AllocationRef!.Id);
                    if (a == null) { fail++; continue; }
                    a.Status = "approved";
                    a.ApprovalDate = DateTime.UtcNow;
                    a.ApprovedByUserId = 1;
                    if (a.Equipment != null) a.Equipment.CurrentStatus = "allocated";
                    db.SaveChanges();
                    _audit.Log(a.EquipmentId, AuditService.ActionAllocationApproved, "pending", "approved");
                    ok++;
                }
                catch { fail++; }
            }

            ToastHelper.Show(this,
                fail == 0 ? $"{ok} allocation(s) approved."
                          : $"{ok} approved, {fail} failed (checked-out or error).",
                fail == 0 ? ToastType.Success : ToastType.Warning);
            LoadAllocations();
        }

        private void ApproveSingle(Allocation alloc)
        {
            try
            {
                using var db = new InventoryDbContext();
                bool isCheckedOut = db.Transactions.Any(t =>
                    t.EquipmentId == alloc.EquipmentId &&
                    (t.Status == "Pending" || t.Status == "Overdue"));
                if (isCheckedOut)
                {
                    ToastHelper.Show(this, "This equipment is currently checked out and cannot be approved until it is checked back in.", ToastType.Warning);
                    return;
                }
                var a = db.Allocations.Include(x => x.Equipment).FirstOrDefault(x => x.Id == alloc.Id);
                if (a == null) return;
                a.Status = "approved";
                a.ApprovalDate = DateTime.UtcNow;
                a.ApprovedByUserId = 1;
                if (a.Equipment != null) a.Equipment.CurrentStatus = "allocated";
                db.SaveChanges();
                _audit.Log(alloc.EquipmentId, AuditService.ActionAllocationApproved, "pending", "approved");
                _audit.Log(alloc.EquipmentId, AuditService.ActionAllocationApproved, "pending", "approved");
                ToastHelper.Show(this, "Allocation approved.", ToastType.Success);
                LoadAllocations();
            }
            catch (Exception ex)
            {
                ToastHelper.Show(this, "Approve failed: " + ex.Message, ToastType.Error);
            }
        }

        private void btnReturn_Click(object sender, EventArgs e)
        {
            var rows = GetSelectedRows()
                .Where(r => r.AllocationRef != null && r.AllocationRef.Status != "returned")
                .ToList();

            if (rows.Count == 0)
            {
                ToastHelper.Show(this, "Select one or more active allocations to return.", ToastType.Info);
                return;
            }

            if (rows.Count == 1)
            {
                var alloc = rows[0].AllocationRef!;
                if (MessageBox.Show($"Return '{alloc.Equipment?.Name}' from '{alloc.Employee?.FullName}'?",
                    "Confirm Return", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
                var res = _allocationService.Deallocate(alloc.Id);
                if (res.Success)
                {
                    _audit.Log(alloc.EquipmentId, AuditService.ActionAllocationReturned, "allocated", "in_store");
                    ToastHelper.Show(this, "Return processed.", ToastType.Success);
                    LoadAllocations();
                }
                else ToastHelper.Show(this, "Return Failed: " + res.ErrorMessage, ToastType.Error);
                return;
            }

            if (MessageBox.Show($"Return {rows.Count} allocations?",
                "Confirm Bulk Return", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            int ok = 0, fail = 0;
            foreach (var row in rows)
            {
                var res = _allocationService.Deallocate(row.AllocationRef!.Id);
                if (res.Success) { _audit.Log(row.AllocationRef.EquipmentId, AuditService.ActionAllocationReturned, "allocated", "in_store"); ok++; }
                else fail++;
            }
            ToastHelper.Show(this,
                fail == 0 ? $"{ok} allocation(s) returned."
                          : $"{ok} returned, {fail} failed.",
                fail == 0 ? ToastType.Success : ToastType.Warning);
            LoadAllocations();
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            var rows = GetSelectedRows()
                .Where(r => r.AllocationRef != null && r.AllocationRef.Status != "approved")
                .ToList();

            if (rows.Count == 0)
            {
                ToastHelper.Show(this, "Select one or more non-approved allocations to delete.", ToastType.Info);
                return;
            }

            string msg = rows.Count == 1
                ? "Delete this allocation record?"
                : $"Delete {rows.Count} allocation records?";

            if (MessageBox.Show(msg, "Confirm Delete",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                return;

            int ok = 0, fail = 0;
            foreach (var row in rows)
            {
                try
                {
                    using var db = new InventoryDbContext();
                    var a = db.Allocations.FirstOrDefault(x => x.Id == row.AllocationRef!.Id);
                    if (a != null) { db.Allocations.Remove(a); db.SaveChanges(); ok++; }
                    else fail++;
                }
                catch { fail++; }
            }
            if (fail > 0)
                ToastHelper.Show(this, $"{ok} deleted, {fail} failed.", ToastType.Warning);
            else if (ok > 0)
                ToastHelper.Show(this, ok == 1 ? "Allocation deleted." : $"{ok} allocations deleted.", ToastType.Success);
            LoadAllocations();
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            txtSearch.Text = string.Empty;
            _currentPage = 1;

            if (cmbSelectMode != null)
            {
                cmbSelectMode.SelectedIndex = -1;
                cmbSelectMode.Text = "Select";
            }

            if (dgvAllocation.Columns.Contains("ColSelect"))
                dgvAllocation.Columns["ColSelect"].Visible = false;
            dgvAllocation.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            dgvAllocation.ClearSelection();

            LoadAllocations();
        }



        private void txtSearch_TextChanged(object sender, EventArgs e)
        {
            _currentPage = 1;
            RefreshGrid(txtSearch.Text.Trim());
        }

        private void cmbPageSize_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (int.TryParse(cmbPageSize.SelectedItem?.ToString(), out int size))
            {
                _pageSize = size;
                _currentPage = 1;
                RefreshGrid(txtSearch.Text.Trim());
            }
        }

        private void btnPrevPage_Click(object sender, EventArgs e)
        {
            if (_currentPage <= 1) return;
            _currentPage--;
            RefreshGrid(txtSearch.Text.Trim());
        }

        private void btnNextPage_Click(object sender, EventArgs e)
        {
            if (_currentPage >= _totalPages) return;
            _currentPage++;
            RefreshGrid(txtSearch.Text.Trim());
        }

        private void btnExport_Click(object sender, EventArgs e)
        {
            try
            {
                using var sfd = new SaveFileDialog
                {
                    Filter = "CSV files|*.csv",
                    FileName = $"Allocations_{DateTime.Now:yyyyMMdd_HHmm}.csv"
                };
                if (sfd.ShowDialog() != DialogResult.OK) return;

                var sb = new StringBuilder();
                sb.AppendLine("ID,QR Code,Equipment Code,Equipment Name,Serial Number,Make,Model,Employee,Location,Date Allocated,Expected Return,Status,Source,Allocated By");
                foreach (DataGridViewRow row in dgvAllocation.Rows)
                {
                    string E(object? v) => $"\"{v?.ToString()?.Replace("\"", "\"\"") ?? ""}\"";
                    sb.AppendLine(string.Join(",",
                        E(row.Cells["AllocationID"].Value), E(row.Cells["QRCode"].Value),
                        E(row.Cells["EquipmentCode"].Value), E(row.Cells["EquipmentName"].Value),
                        E(row.Cells["SerialNumber"].Value), E(row.Cells["Make"].Value),
                        E(row.Cells["Model"].Value), E(row.Cells["Employee"].Value),
                        E(row.Cells["Location"].Value), E(row.Cells["DateAllocated"].Value),
                        E(row.Cells["ExpectedReturn"].Value), E(row.Cells["Status"].Value),
                        E(row.Cells["Source"].Value), E(row.Cells["AllocatedBy"].Value)));
                }
                System.IO.File.WriteAllText(sfd.FileName, sb.ToString());
                ToastHelper.Show(this, "Exported successfully.", ToastType.Success);
            }
            catch (Exception ex)
            {
                ToastHelper.Show(this, "Export failed: " + ex.Message, ToastType.Error);
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _suggest?.Dispose();
            base.OnFormClosed(e);
        }
    }

    // ── Add Allocation Dialog ──────────────────────────────────────────────
    // Two-column, fixed-height, non-scrolling layout — matches the visual
    // language of the Add Equipment dialog (two independent columns under
    // one header, sized to fit entirely on a small laptop screen with no
    // scrollbar, so the DateTimePicker's calendar popup is never clipped).
    public class AddAllocationForm : Form
    {
        public Allocation? Result { get; private set; }

        private readonly List<Equipment> _equipment;
        private readonly List<Employee> _employees;
        private readonly List<Location> _locations;
        private readonly string? _preselectEquipmentCode;
        private bool _preselectApplied = false;

        private int? _selectedEquipmentId;
        private int? _selectedEmployeeId;

        private ComboBox cmbEquipment = null!;
        private ComboBox cmbLocation = null!;
        private ComboBox cmbRoom = null!;
        private DateTimePicker dtpExpected = null!;
        private CheckBox chkExpected = null!;
        private Label lblExpectedHint = null!;
        private Button btnSave = null!;
        private Button btnCancel = null!;

        private ComboBox cmbCategoryFilter = null!;
        private TextBox txtEquipmentSearch = null!;
        private TextBox txtEmployeeSearch = null!;
        private EntitySuggestHelper<Equipment>? _equipmentSuggest;
        private EntitySuggestHelper<Employee>? _employeeSuggest;

        private bool _suppressEquipmentSync = false;

        private static readonly Color MutedText = Color.FromArgb(110, 110, 120);
        private static readonly Color CardBorder = Color.FromArgb(224, 224, 224);

        // Location is now picked in two steps: first the location NAME
        // (deduped — a name can have several rows, one per room), then the
        // specific ROOM within that name. The Room dropdown's selected Id
        // is the actual Location row Id that gets saved on the Allocation.

        private class LocationNameOption
        {
            public string? Name { get; set; }        // null = "no location" sentinel
            public string DisplayName { get; set; } = "";
        }

        private class RoomOption
        {
            public int Id { get; set; }
            public string Display { get; set; } = "";
        }

        private static string RoomOptionDisplay(Location l)
        {
            bool hasNum = !string.IsNullOrWhiteSpace(l.RoomNumber);
            bool hasRoomName = !string.IsNullOrWhiteSpace(l.RoomName);
            if (!hasNum && !hasRoomName) return "— Unspecified room —";
            string room = hasNum ? $"Room {l.RoomNumber}" : "";
            if (hasRoomName) room = string.IsNullOrEmpty(room) ? l.RoomName! : $"{room} ({l.RoomName})";
            return room;
        }

        public AddAllocationForm(List<Equipment> equipment, List<Employee> employees, List<Location> locations,
            string? preselectEquipmentCode = null)
        {
            _preselectEquipmentCode = preselectEquipmentCode;

            using (var db = new InventoryDbContext())
            {
                var checkedOutEquipmentIds = new HashSet<int>(
                    db.Transactions
                        .Where(t => t.Status == "Pending" || t.Status == "Overdue")
                        .Select(t => t.EquipmentId));

                var activelyAllocatedEquipmentIds = new HashSet<int>(
                    db.Allocations
                        .Where(a => a.Status == "pending" || a.Status == "approved")
                        .Select(a => a.EquipmentId));

                _equipment = equipment.Where(e =>
                    !checkedOutEquipmentIds.Contains(e.Id) &&
                    !activelyAllocatedEquipmentIds.Contains(e.Id) &&
                    e.CurrentStatus != "decommissioned" &&
                    e.CurrentStatus != "under_repair").ToList();
            }
            _employees = employees.Where(e => e.IsActive).ToList();
            _locations = locations;
            BuildUI();
        }

        private void BuildUI()
        {
            Text = "New Allocation";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            AutoScroll = false;
            Font = new Font("Segoe UI", 9F);
            ThemeHelper.ApplyFormBackground(this);

            const int formWidth = 820;
            const int margin = 28;
            const int colGap = 34;
            int colWidth = (formWidth - margin * 2 - colGap) / 2;
            int leftX = margin;
            int rightX = margin + colWidth + colGap;

            // ── Header ──
            var pnlHeader = new Panel { Location = new Point(0, 0), Size = new Size(formWidth, 10), BackColor = ThemeHelper.Header };
            Controls.Add(pnlHeader);

            var lblTitle = new Label
            {
                Text = "🖥  New Allocation",
                Font = new Font("Segoe UI", 15.5f, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(margin, 18)
            };
            pnlHeader.Controls.Add(lblTitle);

            var subtitleFont = new Font("Segoe UI", 9.5f, FontStyle.Regular);
            string subtitleText = "Assign this equipment to an employee, a location, or both";
            var subtitleSize = TextRenderer.MeasureText(
                subtitleText, subtitleFont, new Size(formWidth - margin * 2, int.MaxValue), TextFormatFlags.WordBreak);
            var lblSubtitle = new Label
            {
                Text = subtitleText,
                Font = subtitleFont,
                ForeColor = Color.FromArgb(235, 245, 233),
                AutoSize = false,
                Size = new Size(formWidth - margin * 2, subtitleSize.Height + 4),
                Location = new Point(margin, lblTitle.Bottom + 8)
            };
            pnlHeader.Controls.Add(lblSubtitle);
            pnlHeader.Height = lblSubtitle.Bottom + 20;

            // ── Body ──
            var body = new Panel { Location = new Point(0, pnlHeader.Height), Size = new Size(formWidth, 10), AutoScroll = false };
            Controls.Add(body);

            // ── Footer — buttons centered ──
            const int footerHeight = 72;
            var pnlFooter = new Panel { Size = new Size(formWidth, footerHeight), BackColor = Color.FromArgb(249, 250, 251) };
            pnlFooter.Controls.Add(new Panel { Location = new Point(0, 0), Size = new Size(formWidth, 1), BackColor = CardBorder });

            btnCancel = new Button { Text = "Cancel", Size = new Size(140, 42) };
            ThemeHelper.StyleSecondaryButton(btnCancel);
            btnCancel.Click += (s, e) => DialogResult = DialogResult.Cancel;

            btnSave = new Button { Text = "✓  Allocate", Size = new Size(190, 42) };
            ThemeHelper.StyleButton(btnSave);
            btnSave.Click += BtnSave_Click;

            int totalButtonsWidth = btnCancel.Width + 12 + btnSave.Width;
            int buttonsStartX = (formWidth - totalButtonsWidth) / 2;
            btnCancel.Location = new Point(buttonsStartX, 15);
            btnSave.Location = new Point(buttonsStartX + btnCancel.Width + 12, 15);

            pnlFooter.Controls.Add(btnCancel);
            pnlFooter.Controls.Add(btnSave);
            Controls.Add(pnlFooter);

            // ── Local layout helpers ──
            void SectionHeader(int x, int width, ref int y, string title, string? description)
            {
                body.Controls.Add(new Panel { BackColor = ThemeHelper.Header, Size = new Size(4, 22), Location = new Point(x, y) });
                body.Controls.Add(new Label
                {
                    Text = title,
                    Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                    ForeColor = ThemeHelper.TextPrimary,
                    AutoSize = true,
                    Location = new Point(x + 12, y - 1)
                });
                y += 28;

                if (description != null)
                {
                    var font = new Font("Segoe UI", 8.5f, FontStyle.Regular);
                    var sz = TextRenderer.MeasureText(description, font, new Size(width - 12, int.MaxValue), TextFormatFlags.WordBreak);
                    body.Controls.Add(new Label
                    {
                        Text = description,
                        Font = font,
                        ForeColor = MutedText,
                        AutoSize = false,
                        Size = new Size(width - 12, sz.Height + 4),
                        Location = new Point(x + 12, y)
                    });
                    y += sz.Height + 4 + 10;
                }
            }

            void FieldLabel(int x, ref int y, string label, bool required = false)
            {
                body.Controls.Add(new Label
                {
                    Text = required ? $"{label}  *" : label,
                    Location = new Point(x, y),
                    AutoSize = true,
                    Font = ThemeHelper.FontBold,
                    ForeColor = ThemeHelper.TextPrimary
                });
                y += 20;
            }

            void PlaceControl(int x, int width, ref int y, Control ctrl, int height = 30)
            {
                ctrl.Location = new Point(x, y);
                ctrl.Width = width;
                if (ctrl.Height < height) ctrl.Height = height;
                body.Controls.Add(ctrl);
                y += ctrl.Height + 4;
            }

            void Hint(int x, int width, ref int y, string text)
            {
                var font = new Font("Segoe UI", 7.8f, FontStyle.Italic);
                var sz = TextRenderer.MeasureText(text, font, new Size(width, int.MaxValue), TextFormatFlags.WordBreak);
                body.Controls.Add(new Label
                {
                    Text = text,
                    Font = font,
                    ForeColor = MutedText,
                    AutoSize = false,
                    Size = new Size(width, sz.Height + 4),
                    Location = new Point(x, y)
                });
                y += sz.Height + 4 + 8;
            }

            int yLeft = 20;
            int yRight = 20;

            // ══════════════════════ LEFT COLUMN — Equipment ══════════════════════
            SectionHeader(leftX, colWidth, ref yLeft, "🖥  Equipment", null);

            cmbCategoryFilter = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
            var categoryNames = new CategoryService().GetAll().Data?.Select(c => c.Name) ?? Enumerable.Empty<string>();
            var equipmentCategories = _equipment.Where(e => !string.IsNullOrWhiteSpace(e.Category)).Select(e => e.Category!).Distinct();
            var allCategoryOptions = new List<string> { "All Categories" };
            allCategoryOptions.AddRange(categoryNames.Union(equipmentCategories, StringComparer.OrdinalIgnoreCase).OrderBy(c => c, StringComparer.OrdinalIgnoreCase));
            cmbCategoryFilter.DataSource = allCategoryOptions;
            ThemeHelper.StyleComboBox(cmbCategoryFilter);
            FieldLabel(leftX, ref yLeft, "Filter by Category");
            PlaceControl(leftX, colWidth, ref yLeft, cmbCategoryFilter);
            Hint(leftX, colWidth, ref yLeft, "Narrows the equipment list below — optional");

            txtEquipmentSearch = new TextBox { PlaceholderText = "Search by code, name, or serial…" };
            ThemeHelper.StyleTextBox(txtEquipmentSearch);
            FieldLabel(leftX, ref yLeft, "Find Equipment", required: true);
            PlaceControl(leftX, colWidth, ref yLeft, txtEquipmentSearch);
            Hint(leftX, colWidth, ref yLeft, "Type to search, then pick a suggestion");

            cmbEquipment = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
            cmbEquipment.DisplayMember = "DisplayText";
            cmbEquipment.ValueMember = "Id";
            ThemeHelper.StyleComboBox(cmbEquipment);
            FieldLabel(leftX, ref yLeft, "Select Equipment", required: true);
            PlaceControl(leftX, colWidth, ref yLeft, cmbEquipment);
            Hint(leftX, colWidth, ref yLeft, "Only unallocated, in-store equipment is listed");

            cmbEquipment.SelectedIndexChanged += CmbEquipment_SelectedIndexChanged;

            RefreshEquipmentDropdown();
            cmbCategoryFilter.SelectedIndexChanged += (s, e) => RefreshEquipmentDropdown();

            _equipmentSuggest = new EntitySuggestHelper<Equipment>(
                textBox: txtEquipmentSearch,
                hostForm: this,
                onSelected: eq =>
                {
                    _selectedEquipmentId = eq.Id;
                    txtEquipmentSearch.Text = $"{eq.EquipmentCode} — {eq.Name}";
                    txtEquipmentSearch.SelectionStart = txtEquipmentSearch.Text.Length;
                },
                scoreFn: (eq, term) => Math.Max(
                    FuzzySearchHelper.FuzzyScoreCode(eq.EquipmentCode, term),
                    Math.Max(
                        FuzzySearchHelper.FuzzyScoreCode(eq.QrCode, term),
                        Math.Max(
                            FuzzySearchHelper.FuzzyScore(eq.Name, term),
                            FuzzySearchHelper.FuzzyScoreCode(eq.SerialNumber, term)))),
                line1Selector: eq => $"{eq.EquipmentCode} — {eq.Name}",
                line2Selector: eq => string.IsNullOrWhiteSpace(eq.SerialNumber) ? (eq.Category ?? "—") : $"S/N: {eq.SerialNumber}");
            _equipmentSuggest.LoadItems(_equipment);

            txtEquipmentSearch.TextChanged += (s, e) => { if (string.IsNullOrWhiteSpace(txtEquipmentSearch.Text)) _selectedEquipmentId = null; };

            if (!string.IsNullOrWhiteSpace(_preselectEquipmentCode))
            {
                var match = _equipment.FirstOrDefault(e => e.EquipmentCode == _preselectEquipmentCode);
                if (match != null)
                {
                    _selectedEquipmentId = match.Id;
                    txtEquipmentSearch.Text = $"{match.EquipmentCode} — {match.Name}";
                    _preselectApplied = true;
                }
            }

            // ── Return Details now lives here, under Select Equipment ──
            yLeft += 6;
            SectionHeader(leftX, colWidth, ref yLeft, "📅  Return Details", null);

            chkExpected = new CheckBox
            {
                Text = "Set an expected return date",
                Location = new Point(leftX, yLeft),
                AutoSize = true,
                Font = ThemeHelper.FontDefault,
                ForeColor = ThemeHelper.TextPrimary
            };
            body.Controls.Add(chkExpected);
            yLeft += 28;

            dtpExpected = new DateTimePicker
            {
                Location = new Point(leftX, yLeft),
                Width = colWidth,
                Height = 30,
                Format = DateTimePickerFormat.Long,
                ShowUpDown = false,
                Enabled = false,
                MinDate = DateTime.Today.AddDays(1),
                Value = DateTime.Today.AddDays(1)
            };
            body.Controls.Add(dtpExpected);
            yLeft += dtpExpected.Height + 6;

            lblExpectedHint = new Label
            {
                Text = "Check the box above, then click the field to pick a date",
                Font = new Font("Segoe UI", 7.8f, FontStyle.Italic),
                ForeColor = MutedText,
                AutoSize = true,
                Location = new Point(leftX, yLeft)
            };
            body.Controls.Add(lblExpectedHint);
            yLeft += 18;

            chkExpected.CheckedChanged += (s, e) =>
            {
                dtpExpected.Enabled = chkExpected.Checked;
                lblExpectedHint.Text = chkExpected.Checked
                    ? "Click the field or its dropdown arrow to open the calendar"
                    : "Check the box above to enable this field";
                lblExpectedHint.ForeColor = chkExpected.Checked ? ThemeHelper.Header : MutedText;
                if (chkExpected.Checked) dtpExpected.Focus();
            };

            // ══════════════════════ RIGHT COLUMN — Assign To ══════════════════════
            SectionHeader(rightX, colWidth, ref yRight, "👤  Assign To",
                "Choose an employee, a location, or both — at least one is required.");

            txtEmployeeSearch = new TextBox { PlaceholderText = "Search by employee code or name…" };
            ThemeHelper.StyleTextBox(txtEmployeeSearch);
            FieldLabel(rightX, ref yRight, "Find Employee");
            PlaceControl(rightX, colWidth, ref yRight, txtEmployeeSearch);
            Hint(rightX, colWidth, ref yRight, "Optional — leave blank if not tied to a person");

            _employeeSuggest = new EntitySuggestHelper<Employee>(
                textBox: txtEmployeeSearch,
                hostForm: this,
                onSelected: emp =>
                {
                    _selectedEmployeeId = emp.Id;
                    txtEmployeeSearch.Text = $"{emp.EmployeeCode} — {emp.FullName}";
                    txtEmployeeSearch.SelectionStart = txtEmployeeSearch.Text.Length;
                },
                scoreFn: (emp, term) => Math.Max(FuzzySearchHelper.FuzzyScoreCode(emp.EmployeeCode, term), FuzzySearchHelper.FuzzyScore(emp.FullName, term)),
                line1Selector: emp => $"{emp.EmployeeCode} — {emp.FullName}",
                line2Selector: emp => emp.Department ?? "—");
            _employeeSuggest.LoadItems(_employees);

            txtEmployeeSearch.TextChanged += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtEmployeeSearch.Text))
                    _selectedEmployeeId = null;
            };

            var lblOr = new Label
            {
                Text = "— or —",
                Font = new Font("Segoe UI", 8f, FontStyle.Bold | FontStyle.Italic),
                ForeColor = MutedText,
                AutoSize = true,
                Location = new Point(rightX, yRight)
            };
            body.Controls.Add(lblOr);
            yRight += 22;

            // ── Location (name only) ──
            cmbLocation = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
            cmbLocation.DisplayMember = "DisplayName";
            cmbLocation.ValueMember = "Name";
            var locationNameOptions = new List<LocationNameOption> { new LocationNameOption { Name = null, DisplayName = "— No location —" } };
            locationNameOptions.AddRange(
                _locations
                    .Select(l => l.LocationName)
                    .Where(n => !string.IsNullOrWhiteSpace(n))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
                    .Select(n => new LocationNameOption { Name = n ?? "", DisplayName = n ?? "" }));
            cmbLocation.DataSource = locationNameOptions;
            ThemeHelper.StyleComboBox(cmbLocation);
            FieldLabel(rightX, ref yRight, "Location");
            PlaceControl(rightX, colWidth, ref yRight, cmbLocation);
            Hint(rightX, colWidth, ref yRight, "Optional — the site or building");

            // ── Room (depends on Location) ──
            cmbRoom = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
            cmbRoom.DisplayMember = "Display";
            cmbRoom.ValueMember = "Id";
            ThemeHelper.StyleComboBox(cmbRoom);
            FieldLabel(rightX, ref yRight, "Room");
            PlaceControl(rightX, colWidth, ref yRight, cmbRoom);
            Hint(rightX, colWidth, ref yRight, "Optional — narrows to a specific room at that location");

            cmbLocation.SelectedIndexChanged += (s, e) => RefreshRoomDropdown();
            if (cmbLocation.Items.Count > 0) cmbLocation.SelectedIndex = 0; // triggers RefreshRoomDropdown → cmbRoom starts disabled

            // ── Final sizing ──
            int contentBottom = Math.Max(yLeft, yRight) + 16;
            body.Size = new Size(formWidth, contentBottom);
            int totalHeight = pnlHeader.Height + contentBottom + footerHeight;
            ClientSize = new Size(formWidth, totalHeight);
            pnlFooter.Location = new Point(0, ClientSize.Height - footerHeight);
        }

        private void CmbEquipment_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (_suppressEquipmentSync) return;
            if (cmbEquipment.SelectedValue == null) return;

            int selectedId = (int)cmbEquipment.SelectedValue;
            var eq = _equipment.FirstOrDefault(x => x.Id == selectedId);
            if (eq == null) return;

            _suppressEquipmentSync = true;
            txtEquipmentSearch.Text = $"{eq.EquipmentCode} — {eq.Name}";
            txtEquipmentSearch.SelectionStart = txtEquipmentSearch.Text.Length;
            _suppressEquipmentSync = false;
        }

        private void RefreshEquipmentDropdown()
        {
            string selectedCategory = cmbCategoryFilter?.SelectedItem?.ToString() ?? "All Categories";

            var filtered = selectedCategory == "All Categories"
                ? _equipment
                : _equipment.Where(e => string.Equals(e.Category, selectedCategory, StringComparison.OrdinalIgnoreCase)).ToList();

            _suppressEquipmentSync = true;
            cmbEquipment.DataSource = filtered.Select(e => new
            {
                e.Id,
                DisplayText = $"{e.EquipmentCode} — {e.Name}" +
                              (string.IsNullOrWhiteSpace(e.SerialNumber) ? "" : $" (S/N: {e.SerialNumber})")
            }).ToList();
            cmbEquipment.SelectedIndex = -1;
            _suppressEquipmentSync = false;

            _equipmentSuggest?.LoadItems(filtered);

            if (!_preselectApplied && !string.IsNullOrWhiteSpace(_preselectEquipmentCode))
            {
                var match = filtered.FirstOrDefault(e => e.EquipmentCode == _preselectEquipmentCode);
                if (match != null)
                {
                    cmbEquipment.SelectedValue = match.Id;
                    _preselectApplied = true;
                }
            }
        }

        private void RefreshRoomDropdown()
        {
            string? selectedName = cmbLocation.SelectedValue as string;

            if (selectedName == null)
            {
                cmbRoom.DataSource = null;
                cmbRoom.Enabled = false;
                return;
            }

            var rooms = _locations
                .Where(l => l.LocationName == selectedName)
                .Select(l => new RoomOption { Id = l.Id, Display = RoomOptionDisplay(l) })
                .ToList();

            cmbRoom.Enabled = rooms.Count > 0;
            cmbRoom.DataSource = rooms;
            if (cmbRoom.Items.Count > 0) cmbRoom.SelectedIndex = 0;
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            if (_selectedEquipmentId == null)
            {
                ToastHelper.Show(this, "Please select an equipment item from the suggestions.", ToastType.Warning);
                return;
            }

            int? locationId = cmbRoom.Enabled && cmbRoom.SelectedValue is int rid ? rid : (int?)null;

            // The actual rule: equipment is required, and AT LEAST ONE of
            // employee / location must be set — not both, not neither.
            if (_selectedEmployeeId == null && locationId == null)
            {
                ToastHelper.Show(this, "Please choose an employee, a location, or both — at least one is required.", ToastType.Warning);
                return;
            }

            Result = new Allocation
            {
                EquipmentId = _selectedEquipmentId.Value,
                EmployeeId = _selectedEmployeeId,
                LocationId = locationId,
                Status = "pending",
                AllocationDate = DateTime.UtcNow,
                AllocatedByUserId = AuthService.CurrentUserId,
                ExpectedReturnDate = chkExpected.Checked ? DateOnly.FromDateTime(dtpExpected.Value) : null
            };
            DialogResult = DialogResult.OK;
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _equipmentSuggest?.Dispose();
            _employeeSuggest?.Dispose();
            base.OnFormClosed(e);
        }
    }

    // ── Edit Expected Return Date Dialog ───────────────────────────────────
    public class EditExpectedReturnDialog : Form
    {
        public DateOnly? SelectedDate { get; private set; }

        private CheckBox chkHasDate = null!;
        private DateTimePicker dtp = null!;

        public EditExpectedReturnDialog(DateOnly? currentDate)
        {
            BuildUI(currentDate);
        }

        private void BuildUI(DateOnly? currentDate)
        {
            Text = "Edit Expected Return Date";
            Size = new Size(360, 260);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ThemeHelper.ApplyFormBackground(this);

            int y = 20;

            chkHasDate = new CheckBox
            {
                Text = "Set expected return date",
                Location = new Point(20, y),
                AutoSize = true,
                Font = ThemeHelper.FontDefault,
                ForeColor = ThemeHelper.TextPrimary,
                Checked = currentDate.HasValue
            };
            chkHasDate.CheckedChanged += (s, e) => dtp.Enabled = chkHasDate.Checked;
            Controls.Add(chkHasDate);
            y += 30;

            dtp = new DateTimePicker
            {
                Location = new Point(20, y),
                Width = 300,
                Format = DateTimePickerFormat.Short,
                Enabled = currentDate.HasValue,
                Value = currentDate.HasValue
                    ? currentDate.Value.ToDateTime(TimeOnly.MinValue)
                    : DateTime.Today
            };
            Controls.Add(dtp);
            y += 50;

            var lblHint = new Label
            {
                Text = "A past date marks this allocation overdue.\nA future (or no) date clears the overdue status.",
                Location = new Point(20, y),
                Size = new Size(300, 40),
                Font = ThemeHelper.FontSmall,
                ForeColor = ThemeHelper.TextSecondary
            };
            Controls.Add(lblHint);
            y += 54;

            var btnSave = new Button { Text = "Save", Location = new Point(20, y), Size = new Size(140, 36) };
            ThemeHelper.StyleButton(btnSave);
            btnSave.Click += (s, e) =>
            {
                SelectedDate = chkHasDate.Checked ? DateOnly.FromDateTime(dtp.Value) : (DateOnly?)null;
                DialogResult = DialogResult.OK;
            };
            Controls.Add(btnSave);

            var btnCancel = new Button { Text = "Cancel", Location = new Point(170, y), Size = new Size(140, 36) };
            ThemeHelper.StyleSecondaryButton(btnCancel);
            btnCancel.Click += (s, e) => DialogResult = DialogResult.Cancel;
            Controls.Add(btnCancel);

            ClientSize = new Size(340, y + 60);
            AcceptButton = btnSave;
            CancelButton = btnCancel;
        }
    }
}