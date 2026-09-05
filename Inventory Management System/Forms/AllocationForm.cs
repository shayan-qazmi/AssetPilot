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
    public partial class AllocationForm : Form, IThemableForm
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
        // Gmail-style checkbox header controls
        private Panel  _chkHeaderPanel = null!;   // container: checkbox + arrow button
        private CheckBox _chkHeaderBox = null!;   // the header checkbox (select-all toggle)
        private bool   _suppressHeaderSync;       // prevent feedback loop
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

        private void SetupPageSizeOptions()
        {
            cmbPageSize.Items.Clear();
            cmbPageSize.Items.Add("10");
            cmbPageSize.Items.Add("25");
            cmbPageSize.Items.Add("50");
            cmbPageSize.Items.Add("100");
            cmbPageSize.Items.Add("250");
            cmbPageSize.Items.Add("500");
            cmbPageSize.Items.Add("1000");
            cmbPageSize.SelectedIndex = 1;
        }

        public AllocationForm()
        {
            InitializeComponent();
            flowToolbar.SizeChanged += (s, e) => panelTop.Height = flowToolbar.Height;

            ApplyTheme();
            SetupEquipmentFilterDropdown();
            SetupCheckboxSelectHeader();
            SetupCategoryFilterDropdown();
            SetupCombinedFiltersButton();
            SetupEditReturnButton();
            SetupPageSizeOptions();

            // Same fix as EquipmentForm — sync panelTop's height explicitly once
            // all toolbar controls exist, instead of relying only on SizeChanged.
            flowToolbar.AutoSize = true;
            flowToolbar.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            panelTop.Height = flowToolbar.Height;

            LoadAllocations();
            SetupSuggestions();

            dgvAllocation.SelectionChanged += (s, e) => UpdateActionButtonsForSelection();
            dgvAllocation.CellDoubleClick += dgvAllocation_CellDoubleClick;
            this.Shown += (s, e) =>
            {
                dgvAllocation.ClearSelection();
                dgvAllocation.CurrentCell = null;
            };
        }

        public AllocationForm(string initialFilter = null) : this()
        {
            if (!string.IsNullOrEmpty(initialFilter))
            {
                if (string.Equals(initialFilter, "Available", StringComparison.OrdinalIgnoreCase))
                    initialFilter = "In Store";
                int idx = cmbEquipmentFilter.Items.IndexOf($"Equipment: {initialFilter}");
                if (idx >= 0) cmbEquipmentFilter.SelectedIndex = idx;
            }
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
            cmbEquipmentFilter.Items.Add("Equipment: In Store");
            cmbEquipmentFilter.Items.Add("Equipment: Allocated");
            cmbEquipmentFilter.Items.Add("Equipment: Pending");
            cmbEquipmentFilter.Items.Add("Equipment: Overdue");
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
                    "Equipment: In Store" => "In Store",
                    "Equipment: Allocated" => "Allocated",
                    "Equipment: Pending" => "Pending",
                    "Equipment: Overdue" => "Overdue",
                    _ => "All"
                };
                _currentPage = 1;
                RefreshGrid(txtSearch.Text.Trim());
            };

            // Not added to toolbar directly — consumed by the combined Filters button instead.
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

            // Not added to toolbar directly — consumed by the combined Filters button instead.
        }

        private void SetupCombinedFiltersButton()
        {
            var btnFilters = new Button
            {
                Name = "btnFilters",
                Text = "🔽 Filters",
                Size = new Size(110, 32),
                Margin = new Padding(4, 4, 0, 0),
                Cursor = Cursors.Hand,
                FlatStyle = FlatStyle.Flat,
                BackColor = ThemeHelper.Accent,
                ForeColor = Color.White,
                Font = ThemeHelper.FontBold
            };
            btnFilters.FlatAppearance.BorderSize = 0;
            
            btnFilters.MouseEnter += (s, e) =>
            {
                if (!btnFilters.ContextMenuStrip?.Visible ?? true)
                    ShowAllocFiltersMenu(btnFilters);
            };
            btnFilters.Click += (s, e) => ShowAllocFiltersMenu(btnFilters);

            var flow = panelTop.Controls.OfType<FlowLayoutPanel>().First();
            flow.Controls.Add(btnFilters);
            flow.Controls.SetChildIndex(btnFilters, flow.Controls.IndexOf(btnExport) + 1);
        }

        private void ShowAllocFiltersMenu(Button anchor)
        {
            var allocFiltersMenu = new ContextMenuStrip { Font = ThemeHelper.FontDefault };

            var statusSub = new ToolStripMenuItem("Status");
            foreach (var (label, value) in new[] { ("All", "All"), ("In Store", "In Store"), ("Allocated", "Allocated"), ("Pending", "Pending"), ("Overdue", "Overdue") })
            {
                var item = new ToolStripMenuItem(label) { Checked = _activeStatusFilter == value };
                item.Click += (s, e) => { _activeStatusFilter = value; _currentPage = 1; RefreshGrid(txtSearch.Text.Trim()); };
                statusSub.DropDownItems.Add(item);
            }
            allocFiltersMenu.Items.Add(statusSub);

            var categorySub = new ToolStripMenuItem("Category");
            var allCat = new ToolStripMenuItem("All Categories") { Checked = string.IsNullOrEmpty(_activeCategoryFilter) };
            allCat.Click += (s, e) => { _activeCategoryFilter = ""; _currentPage = 1; RefreshGrid(txtSearch.Text.Trim()); };
            categorySub.DropDownItems.Add(allCat);
            categorySub.DropDownItems.Add(new ToolStripSeparator());
            foreach (var cat in cmbAllocCategoryFilter?.Items.OfType<string>().Where(c => c != "All Categories") ?? Enumerable.Empty<string>())
            {
                var item = new ToolStripMenuItem(cat) { Checked = _activeCategoryFilter == cat };
                item.Click += (s, e) => { _activeCategoryFilter = cat; _currentPage = 1; RefreshGrid(txtSearch.Text.Trim()); };
                categorySub.DropDownItems.Add(item);
            }
            allocFiltersMenu.Items.Add(categorySub);

            allocFiltersMenu.Show(anchor, new Point(0, anchor.Height + 2));
            anchor.ContextMenuStrip = allocFiltersMenu;

            var hoverTimer = new System.Windows.Forms.Timer { Interval = 40 };
            hoverTimer.Tick += (s, e) =>
            {
                if (allocFiltersMenu == null || allocFiltersMenu.IsDisposed || !allocFiltersMenu.Visible)
                {
                    hoverTimer.Stop();
                    hoverTimer.Dispose();
                    return;
                }
                var cursor = Cursor.Position;
                var btnRect = anchor.RectangleToScreen(anchor.ClientRectangle);
                btnRect.Inflate(4, 12);

                // Include the bounds of any currently-open submenu (Status/Category),
                // not just the top-level menu — otherwise moving into the submenu
                // reads as "left the menu" and closes it.
                bool insideAnyMenu = GetOpenMenuScreenBounds(allocFiltersMenu)
                    .Any(r => { r.Inflate(4, 4); return r.Contains(cursor); });

                if (!btnRect.Contains(cursor) && !insideAnyMenu)
                {
                    allocFiltersMenu.Close();
                    hoverTimer.Stop();
                    hoverTimer.Dispose();
                }
            };
            hoverTimer.Start();
        }

        // Recursively collects screen bounds of a menu and any of its open submenus,
        // so hover-tracking treats the whole chain as one continuous area.
        private static IEnumerable<Rectangle> GetOpenMenuScreenBounds(ToolStripDropDown menu)
        {
            if (menu == null || menu.IsDisposed || !menu.Visible) yield break;
            yield return menu.Bounds;

            foreach (ToolStripItem item in menu.Items)
            {
                if (item is ToolStripMenuItem mi && mi.HasDropDownItems && mi.DropDown.Visible)
                {
                    foreach (var r in GetOpenMenuScreenBounds(mi.DropDown))
                        yield return r;
                }
            }
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

        // ── Gmail-style checkbox header ────────────────────────────────────
        // Replaces the old Select/Select All/Deselect All combobox with:
        //   • A permanent checkbox column (ColSelect) that is always visible.
        //   • A split control in the toolbar: [☐ v] where ☐ toggles all rows
        //     and [v] opens an All / None context menu.
        private void SetupCheckboxSelectHeader()
        {
            // ── 1. Create the ColSelect checkbox column (always visible) ──
            if (!dgvAllocation.Columns.Contains("ColSelect"))
            {
                var chkCol = new DataGridViewCheckBoxColumn
                {
                    Name             = "ColSelect",
                    HeaderText       = "",
                    Width            = 40,
                    ReadOnly         = false,
                    Visible          = true,
                    AutoSizeMode     = DataGridViewAutoSizeColumnMode.None,
                    TrueValue        = true,
                    FalseValue       = false,
                    IndeterminateValue = false
                };
                dgvAllocation.Columns.Add(chkCol);
                dgvAllocation.Columns["ColSelect"].DisplayIndex = 0;

                dgvAllocation.ReadOnly = false;
                foreach (DataGridViewColumn col in dgvAllocation.Columns)
                    col.ReadOnly = col.Name != "ColSelect";

                // Commit checkbox edits immediately so value is readable.
                dgvAllocation.CurrentCellDirtyStateChanged += (s, ev) =>
                {
                    if (dgvAllocation.CurrentCell is DataGridViewCheckBoxCell)
                        dgvAllocation.CommitEdit(DataGridViewDataErrorContexts.Commit);
                };

                dgvAllocation.CellValueChanged += (s, ev) =>
                {
                    if (ev.ColumnIndex >= 0 && dgvAllocation.Columns[ev.ColumnIndex].Name == "ColSelect")
                    {
                        SyncHeaderCheckbox();
                        UpdateActionButtonsForSelection();
                    }
                };
            }

            // ── 2. Build the split [☐ ▾] toolbar control ──
            // Outer container (renders as a small rounded pill)
            _chkHeaderPanel = new Panel
            {
                Name    = "pnlCheckHeader",
                Size    = new Size(62, 32),
                Margin  = new Padding(4, 4, 0, 0),
                Cursor  = Cursors.Hand,
                BackColor = ThemeHelper.IsDarkMode
                    ? Color.FromArgb(40, 44, 52)
                    : Color.FromArgb(241, 243, 244)
            };

            // The header checkbox (left portion of the split control)
            _chkHeaderBox = new CheckBox
            {
                Name      = "chkHeaderBox",
                Size      = new Size(40, 32),
                Location  = new Point(0, 0),
                Appearance = Appearance.Normal,
                FlatStyle  = FlatStyle.Flat,
                BackColor  = Color.Transparent,
                ForeColor  = ThemeHelper.TextPrimary,
                Text       = "",
                Cursor     = Cursors.Hand,
                TextAlign  = ContentAlignment.MiddleCenter,
                CheckAlign = ContentAlignment.MiddleCenter,
                ThreeState = false
            };
            _chkHeaderBox.FlatAppearance.BorderSize  = 0;
            _chkHeaderBox.FlatAppearance.MouseOverBackColor  = Color.Transparent;
            _chkHeaderBox.FlatAppearance.CheckedBackColor    = Color.Transparent;
            _chkHeaderBox.CheckedChanged += (s, e) =>
            {
                if (_suppressHeaderSync) return;
                SetAllCheckboxes(_chkHeaderBox.Checked);
            };

            // The small arrow button (right portion, shows All/None menu)
            var btnArrow = new Button
            {
                Name      = "btnCheckArrow",
                Text      = "▾",
                Size      = new Size(22, 32),
                Location  = new Point(40, 0),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                ForeColor = ThemeHelper.TextPrimary,
                Font      = new Font("Segoe UI", 8f),
                Cursor    = Cursors.Hand,
                Padding   = Padding.Empty,
                Margin    = Padding.Empty
            };
            btnArrow.FlatAppearance.BorderSize = 0;
            btnArrow.FlatAppearance.MouseOverBackColor = ThemeHelper.IsDarkMode
                ? Color.FromArgb(55, 60, 70)
                : Color.FromArgb(218, 220, 224);
            btnArrow.Click += (s, e) =>
            {
                var menu = new ContextMenuStrip { Font = ThemeHelper.FontDefault };
                var itemAll  = new ToolStripMenuItem("All");
                var itemNone = new ToolStripMenuItem("None");
                itemAll.Click  += (_, __) => { SetAllCheckboxes(true);  _chkHeaderBox.Checked = true; };
                itemNone.Click += (_, __) => { SetAllCheckboxes(false); _chkHeaderBox.Checked = false; };
                menu.Items.Add(itemAll);
                menu.Items.Add(itemNone);
                menu.Show(btnArrow, new Point(0, btnArrow.Height));
            };

            // Draw a subtle 1-px separator between checkbox and arrow
            _chkHeaderPanel.Paint += (s, e) =>
            {
                using var pen = new Pen(ThemeHelper.IsDarkMode
                    ? Color.FromArgb(65, 70, 80)
                    : Color.FromArgb(200, 202, 206), 1f);
                e.Graphics.DrawLine(pen, 40, 4, 40, _chkHeaderPanel.Height - 4);
            };

            _chkHeaderPanel.Controls.Add(_chkHeaderBox);
            _chkHeaderPanel.Controls.Add(btnArrow);

            // ── 3. Insert the panel BEFORE btnAdd in the toolbar ──
            var flow = panelTop.Controls.OfType<FlowLayoutPanel>().First();
            flow.Controls.Add(_chkHeaderPanel);
            flow.Controls.SetChildIndex(_chkHeaderPanel, flow.Controls.IndexOf(btnAdd));
        }

        /// <summary>Check or uncheck every row in ColSelect.</summary>
        private void SetAllCheckboxes(bool check)
        {
            if (!dgvAllocation.Columns.Contains("ColSelect")) return;
            foreach (DataGridViewRow row in dgvAllocation.Rows)
                row.Cells["ColSelect"].Value = check;
            dgvAllocation.RefreshEdit();
            UpdateActionButtonsForSelection();
        }

        /// <summary>
        /// Syncs the header checkbox to reflect all/none/mixed state of row checkboxes.
        /// </summary>
        private void SyncHeaderCheckbox()
        {
            if (!dgvAllocation.Columns.Contains("ColSelect") || _chkHeaderBox == null) return;
            _suppressHeaderSync = true;
            int total   = dgvAllocation.Rows.Count;
            int checked_ = dgvAllocation.Rows.Cast<DataGridViewRow>()
                .Count(r => Convert.ToBoolean(r.Cells["ColSelect"].Value));
            _chkHeaderBox.Checked = total > 0 && checked_ == total;
            _suppressHeaderSync = false;
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

                return db.Equipment
                    .Where(e => !e.IsDeleted && e.CurrentStatus != "allocated" && e.CurrentStatus != "under_repair" && e.CurrentStatus != "decommissioned")
                    .OrderBy(e => e.QrCode)
                    .ThenBy(e => e.EquipmentCode)
                    .ToList()
                    .Select(e => new AllocationRow
                    {
                        Id = -(2_000_000 + e.Id),
                        Source = "In Store",
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
                        Status = "In Store",
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
            var activeCodes = new HashSet<string>(
                _allSourceRows.Where(r => r.AllocationRef != null &&
                    (r.AllocationRef.Status == "approved" || r.AllocationRef.Status == "pending"))
                    .Select(r => r.EquipmentCode), StringComparer.OrdinalIgnoreCase);

            var availableRows = GetAvailableEquipmentRows()
                .Where(r => !activeCodes.Contains(r.EquipmentCode))
                .ToList();

            return _allSourceRows
                .Concat(availableRows)
                .OrderBy(r => r.QrCode)
                .ThenBy(r => r.EquipmentCode)
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

        public void ApplyTheme()
        {
            ThemeHelper.ApplyFormBackground(this);
            ThemeHelper.StyleToolbarPanel(panelTop);
            ThemeHelper.StyleButton(btnAdd, Color.FromArgb(245, 124, 0));
            ThemeHelper.StyleButton(btnApprove, ThemeHelper.Accent);
            ThemeHelper.StyleButton(btnReturn, Color.FromArgb(99, 102, 241)); // indigo — matches Undo Return in Returns screen
            ThemeHelper.StyleButton(btnRefresh, Color.FromArgb(33, 150, 243));
            ThemeHelper.StyleSecondaryButton(btnExport);
            ThemeHelper.StyleDangerButton(btnDelete);
            ApplyRoundedSearchStyle(txtSearch);
            ThemeHelper.StyleComboBox(cmbPageSize);
            ThemeHelper.StyleSecondaryButton(btnPrevPage);
            ThemeHelper.StyleSecondaryButton(btnNextPage);
            ThemeHelper.StyleDataGridView(dgvAllocation, Color.FromArgb(245, 124, 0));
            panelBottom.BackColor = ThemeHelper.IsDarkMode ? Color.FromArgb(22, 24, 29) : Color.FromArgb(249, 250, 251);
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
                    statusCell.Style.BackColor = Color.FromArgb(255, 224, 178);
                    statusCell.Style.ForeColor = Color.FromArgb(230, 81, 0);
                    break;
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
                    statusCell.Style.BackColor = Color.FromArgb(255, 243, 224);
                    statusCell.Style.ForeColor = Color.FromArgb(255, 160, 0);
                    break;
                case "in_store":
                case "In Store":
                    statusCell.Style.BackColor = Color.FromArgb(200, 230, 201);
                    statusCell.Style.ForeColor = Color.FromArgb(27, 94, 32);
                    break;
            }
            statusCell.Style.Font = ThemeHelper.FontBold;

            var sourceCell = dgvAllocation.Rows[rowIdx].Cells["Source"];
            if (item.Source == "Check In/Out")
            {
                sourceCell.Style.ForeColor = Color.FromArgb(25, 118, 210);
                sourceCell.Style.Font = ThemeHelper.FontBold;
            }
            else if (item.Source == "In Store")
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
                       ? "Overdue" : (a.Status == "approved" ? "Approved" : a.Status),
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
                    "In Store" => GetAvailableEquipmentRows(),
                    "Allocated" => _allSourceRows.Where(r => string.Equals(r.Status, "approved", StringComparison.OrdinalIgnoreCase) || string.Equals(r.Status, "Overdue", StringComparison.OrdinalIgnoreCase)).ToList(),
                    "Pending" => _allSourceRows.Where(r => string.Equals(r.Status, "pending", StringComparison.OrdinalIgnoreCase)).ToList(),
                    "Overdue" => _allSourceRows.Where(r => string.Equals(r.Status, "Overdue", StringComparison.OrdinalIgnoreCase)).ToList(),
                    _ => GetAllCombinedRows() // "All"
                };
            }

            if (!string.IsNullOrEmpty(_activeCategoryFilter))
            {
                baseline = baseline.Where(r =>
                    string.Equals(r.Category, _activeCategoryFilter, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            baseline = baseline
                .OrderBy(r => r.QrCode)
                .ThenBy(r => r.EquipmentCode)
                .ToList();

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
                    case "Approved":
                        statusCell.Style.BackColor = Color.FromArgb(255, 224, 178);
                        statusCell.Style.ForeColor = Color.FromArgb(230, 81, 0);
                        break;
                    case "pending":
                    case "Pending":
                        statusCell.Style.BackColor = Color.FromArgb(46, 125, 50); // dark green — distinct from In Store's pale green
                        statusCell.Style.ForeColor = Color.White;
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
                        statusCell.Style.BackColor = Color.FromArgb(255, 243, 224);
                        statusCell.Style.ForeColor = Color.FromArgb(255, 160, 0);
                        break;
                    case "In Store":
                        statusCell.Style.BackColor = Color.FromArgb(200, 230, 201);
                        statusCell.Style.ForeColor = Color.FromArgb(27, 94, 32);
                        break;
                }
                statusCell.Style.Font = ThemeHelper.FontBold;

                var sourceCell = dgvAllocation.Rows[rowIdx].Cells["Source"];
                if (item.Source == "Check In/Out")
                {
                    sourceCell.Style.ForeColor = Color.FromArgb(25, 118, 210);
                    sourceCell.Style.Font = ThemeHelper.FontBold;
                }
                else if (item.Source == "In Store")
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

            lblRowCount.Text = $"Showing {items.Count} of {_totalMatchingCount} records";
            lblPageIndicator.Text = $"Page {_currentPage} of {Math.Max(1, _totalPages)}";
            btnPrevPage.Enabled = _currentPage > 1;
            btnNextPage.Enabled = _currentPage < _totalPages;

            SyncHeaderCheckbox();
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

        /// <summary>Returns ALL currently selected rows (checkbox-checked or row-selected).</summary>
        private List<AllocationRow> GetSelectedRows()
        {
            var result = new List<AllocationRow>();

            // 1. Check if any row has ColSelect checkbox ticked
            if (dgvAllocation.Columns.Contains("ColSelect"))
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

            // 2. If nothing is checked via checkbox, fall back to row selection
            if (result.Count == 0)
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

        private static void ShowToast(Form form, string message, ToastType type = ToastType.Success, bool isEdit = false, bool isDelete = false, bool isReturn = false, bool isApprove = false)
        {
            Color color = isDelete
                ? Color.FromArgb(211, 47, 47)
                : isReturn
                    ? Color.FromArgb(99, 102, 241)
                    : isEdit
                        ? Color.FromArgb(245, 158, 11) // Yellow for edit
                                         : isApprove
                            ? ThemeHelper.Accent // Dark green — matches Approve/Filters button
                            : Color.FromArgb(245, 124, 0); // Orange — matches Allocate button
            ToastHelper.Show(form, message, type, 3000, color);
        }

        private void btnEditReturn_Click(object? sender, EventArgs e)
        {
            var allSelected = GetSelectedRows();
            if (allSelected.Count > 1)
            {
                ShowToast(this, "Multiple allocations can't be edited at once — please select only one.", ToastType.Warning, isEdit: true);
                return;
            }

            var selectedRow = GetSelectedRow();
            var alloc = selectedRow?.AllocationRef;
            if (alloc == null)
            {
                ShowToast(this, "Select an allocation record first.", ToastType.Info, isEdit: true);
                return;
            }

            using var frm = new EditExpectedReturnDialog(
                alloc.ExpectedReturnDate,
                selectedRow?.EquipmentCode,
                selectedRow?.QrCode,
                selectedRow?.EquipmentName);
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
                 oldVal, (frm.SelectedDate?.ToString("yyyy-MM-dd") ?? "none") + " | Source Screen: Allocation Screen");
                ShowToast(this, "Expected return date updated.", ToastType.Success, isEdit: true);
                LoadAllocations();
            }
            catch (Exception ex)
            {
                ShowToast(this, "Update failed: " + ex.Message, ToastType.Error, isEdit: true);
            }
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            var selectedRow = GetSelectedRow();
            string? preselectCode = selectedRow?.EquipmentCode;
            if (preselectCode == "—") preselectCode = null;

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
      null, $"Allocated to Employee ID {frm.Result.EmployeeId} | Source Screen: Allocation Screen");
                ShowToast(this, "Allocation request created.", ToastType.Success);
                LoadAllocations();
            }
            else
                ShowToast(this, "Allocation Failed: " + result.ErrorMessage, ToastType.Error);
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
            if (row.Source != "In Store")
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
            var allSelected = GetSelectedRows();
            if (allSelected.Count == 0)
            {
                ShowToast(this, "Please select at least one allocation to approve.", ToastType.Info);
                return;
            }

            var allocRows = allSelected
                .Where(r => r.AllocationRef != null)
                .ToList();

            if (allocRows.Count == 0)
            {
                ShowToast(this, "The selected row(s) are available equipment items, not pending allocations.", ToastType.Warning);
                return;
            }

            var pendingRows = allocRows
                .Where(r => r.AllocationRef!.Status == "pending")
                .ToList();

            if (pendingRows.Count == 0)
            {
                ShowToast(this, "The selected allocation(s) are already approved or returned.", ToastType.Info);
                return;
            }

            // Single-row: use detailed confirm flow
            if (pendingRows.Count == 1)
            {
                ApproveSingle(pendingRows[0].AllocationRef!);
                return;
            }

            if (MessageBox.Show($"Approve {pendingRows.Count} pending allocations?",
                "Confirm Bulk Approve", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            int ok = 0, fail = 0;
            foreach (var row in pendingRows)
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
                    _audit.Log(a.EquipmentId, AuditService.ActionAllocationApproved, "pending", "approved | Source Screen: Allocation Screen");
                    ok++;
                }
                catch { fail++; }
            }

            ShowToast(this,
                fail == 0 ? $"{ok} allocation(s) approved successfully."
                          : $"{ok} approved, {fail} failed (checked-out or error).",
                fail == 0 ? ToastType.Success : ToastType.Warning, isApprove: true);
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
                    ShowToast(this, "This equipment is currently checked out and cannot be approved until it is checked back in.", ToastType.Warning);
                    return;
                }
                var a = db.Allocations.Include(x => x.Equipment).FirstOrDefault(x => x.Id == alloc.Id);
                if (a == null) return;
                a.Status = "approved";
                a.ApprovalDate = DateTime.UtcNow;
                a.ApprovedByUserId = 1;
                if (a.Equipment != null) a.Equipment.CurrentStatus = "allocated";
                db.SaveChanges();
                _audit.Log(alloc.EquipmentId, AuditService.ActionAllocationApproved, "pending", "approved | Source Screen: Allocation Screen");
                ShowToast(this, "Allocation approved successfully.", ToastType.Success, isApprove: true);
                LoadAllocations();
            }
            catch (Exception ex)
            {
                ShowToast(this, "Approve failed: " + ex.Message, ToastType.Error, isApprove: true);
            }
        }

        private void btnReturn_Click(object sender, EventArgs e)
        {
            var allSelected = GetSelectedRows();
            if (allSelected.Count == 0)
            {
                ShowToast(this, "Please select at least one allocation to return.", ToastType.Info, isReturn: true);
                return;
            }

            bool hasCheckInOut = allSelected.Any(r => string.Equals(r.Source, "Check In/Out", StringComparison.OrdinalIgnoreCase) || (r.AllocationRef == null && r.TransactionId != null));

            var activeRows = allSelected
                .Where(r => r.AllocationRef != null && r.AllocationRef.Status != "returned")
                .ToList();

            if (activeRows.Count == 0)
            {
                if (hasCheckInOut)
                {
                    ShowToast(this, "This item was issued via Check In/Out. Please check it in from the Check In/Out screen.", ToastType.Warning, isReturn: true);
                }
                else
                {
                    ShowToast(this, "No active or overdue allocations to return in selection.", ToastType.Info, isReturn: true);
                }
                return;
            }

            if (hasCheckInOut)
            {
                ShowToast(this, "Selected item(s) include equipment issued via Check In/Out. Please check them in from the Check In/Out screen.", ToastType.Warning, isReturn: true);
                return;
            }

            if (activeRows.Count == 1)
            {
                if (MessageBox.Show("Are you sure you want to mark this allocation as returned?",
                    "Confirm Return", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                    return;
            }
            else
            {
                if (MessageBox.Show($"Mark {activeRows.Count} allocations as returned?",
                    "Confirm Bulk Return", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                    return;
            }

            int ok = 0, fail = 0;
            foreach (var row in activeRows)
            {
                var res = _allocationService.Deallocate(row.AllocationRef!.Id);
                if (res.Success)
                {
                    _audit.Log(row.AllocationRef.EquipmentId, AuditService.ActionAllocationReturned, "allocated", "in_store | Source Screen: Allocation Screen");
                    ok++;
                }
                else fail++;
            }

            ShowToast(this,
                fail == 0 ? $"{ok} allocation(s) returned successfully."
                          : $"{ok} returned, {fail} failed.",
                fail == 0 ? ToastType.Success : ToastType.Warning, isReturn: true);
            LoadAllocations();
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            var allSelected = GetSelectedRows();
            if (allSelected.Count == 0)
            {
                ShowToast(this, "Please select at least one allocation to delete.", ToastType.Info, isDelete: true);
                return;
            }

            var allocRows = allSelected
                .Where(r => r.AllocationRef != null)
                .ToList();

            if (allocRows.Count == 0)
            {
                ShowToast(this, "The selected row(s) are available equipment items, not allocation records.", ToastType.Warning);
                return;
            }

            string msg = allocRows.Count == 1
                ? "Are you sure you want to delete this allocation record?"
                : $"Are you sure you want to delete these {allocRows.Count} allocation records?";

            if (MessageBox.Show(msg, "Confirm Delete",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                return;

            int ok = 0, fail = 0;
            foreach (var row in allocRows)
            {
                try
                {
                    using var db = new InventoryDbContext();
                    var a = db.Allocations.Include(x => x.Equipment).FirstOrDefault(x => x.Id == row.AllocationRef!.Id);
                    if (a != null)
                    {
                        if (a.Equipment != null && a.Equipment.CurrentStatus == "allocated")
                        {
                            a.Equipment.CurrentStatus = "in_store";
                        }
                        db.Allocations.Remove(a);
                        db.SaveChanges();
                        _audit.Log(a.EquipmentId, "Allocation Deleted", a.Status, "Deleted | Source Screen: Allocation Screen");
                        ok++;
                    }
                    else fail++;
                }
                catch { fail++; }
            }

            if (fail > 0)
                ShowToast(this, $"{ok} deleted, {fail} failed.", ToastType.Warning, isDelete: true);
            else if (ok > 0)
                ShowToast(this, ok == 1 ? "Allocation record deleted successfully." : $"{ok} allocation records deleted successfully.", ToastType.Success, isDelete: true);

            LoadAllocations();
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            txtSearch.Text = string.Empty;
            _currentPage = 1;

            // Uncheck all rows and reset the header checkbox.
            SetAllCheckboxes(false);
            if (_chkHeaderBox != null) _chkHeaderBox.Checked = false;

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
                ShowToast(this, "Exported successfully.", ToastType.Success);
            }
            catch (Exception ex)
            {
                ShowToast(this, "Export failed: " + ex.Message, ToastType.Error);
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
        private static Color CardBorder => ThemeHelper.IsDarkMode ? Color.FromArgb(55, 58, 65) : Color.FromArgb(224, 224, 224);

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
                {
                    if (activelyAllocatedEquipmentIds.Contains(e.Id)) return false;

                    bool isAllocated = string.Equals(e.CurrentStatus, "allocated", StringComparison.OrdinalIgnoreCase);
                    if (isAllocated) return !checkedOutEquipmentIds.Contains(e.Id);

                    bool isDecomm = string.Equals(e.CurrentStatus, "decommissioned", StringComparison.OrdinalIgnoreCase);
                    bool isRepair = string.Equals(e.CurrentStatus, "under_repair", StringComparison.OrdinalIgnoreCase) ||
                                    string.Equals(e.CurrentStatus, "under repair", StringComparison.OrdinalIgnoreCase);

                    return !isDecomm && !isRepair;
                }).ToList();
            }

            // Ensure preselected equipment is present in _equipment list
            if (!string.IsNullOrWhiteSpace(preselectEquipmentCode))
            {
                var preselected = equipment.FirstOrDefault(e => e.EquipmentCode == preselectEquipmentCode);
                if (preselected != null && !_equipment.Any(e => e.Id == preselected.Id))
                {
                    _equipment.Insert(0, preselected);
                }
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
            var pnlHeader = new Panel { Location = new Point(0, 0), Size = new Size(formWidth, 10), BackColor = Color.FromArgb(245, 124, 0) };
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
            var pnlFooter = new Panel { Size = new Size(formWidth, footerHeight), BackColor = ThemeHelper.IsDarkMode ? Color.FromArgb(22, 24, 29) : Color.FromArgb(249, 250, 251) };
            pnlFooter.Controls.Add(new Panel { Location = new Point(0, 0), Size = new Size(formWidth, 1), BackColor = CardBorder });

            btnCancel = new Button { Text = "Cancel", Size = new Size(140, 42) };
            ThemeHelper.StyleSecondaryButton(btnCancel);
            btnCancel.Click += (s, e) => DialogResult = DialogResult.Cancel;

            btnSave = new Button { Text = "✓  Allocate", Size = new Size(190, 42) };
            ThemeHelper.StyleButton(btnSave, Color.FromArgb(245, 124, 0));
            btnSave.Click += BtnSave_Click;

            int totalButtonsWidth = btnCancel.Width + 12 + btnSave.Width;
            int buttonsStartX = (formWidth - totalButtonsWidth) / 2;
            btnSave.Location = new Point(buttonsStartX, 15);
            btnCancel.Location = new Point(buttonsStartX + btnSave.Width + 12, 15);

            pnlFooter.Controls.Add(btnCancel);
            pnlFooter.Controls.Add(btnSave);
            Controls.Add(pnlFooter);

            // ── Local layout helpers ──
            void SectionHeader(int x, int width, ref int y, string title, string? description)
            {
                body.Controls.Add(new Panel { BackColor = Color.FromArgb(245, 124, 0), Size = new Size(4, 22), Location = new Point(x, y) });
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
                    _suppressEquipmentSync = true;
                    txtEquipmentSearch.Text = $"{match.EquipmentCode} — {match.Name}";
                    if (!string.IsNullOrWhiteSpace(match.Category) && cmbCategoryFilter.Items.Contains(match.Category))
                    {
                        cmbCategoryFilter.SelectedItem = match.Category;
                    }
                    RefreshEquipmentDropdown();
                    cmbEquipment.SelectedValue = match.Id;
                    _suppressEquipmentSync = false;
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
                lblExpectedHint.ForeColor = chkExpected.Checked ? Color.FromArgb(245, 124, 0) : MutedText;
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

            if (_selectedEquipmentId.HasValue && filtered.Any(e => e.Id == _selectedEquipmentId.Value))
            {
                cmbEquipment.SelectedValue = _selectedEquipmentId.Value;
            }
            else if (!string.IsNullOrWhiteSpace(_preselectEquipmentCode))
            {
                var match = filtered.FirstOrDefault(e => e.EquipmentCode == _preselectEquipmentCode);
                if (match != null)
                {
                    _selectedEquipmentId = match.Id;
                    cmbEquipment.SelectedValue = match.Id;
                }
                else
                {
                    cmbEquipment.SelectedIndex = -1;
                }
            }
            else
            {
                cmbEquipment.SelectedIndex = -1;
            }
            _suppressEquipmentSync = false;

            _equipmentSuggest?.LoadItems(filtered);
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
            if (_selectedEquipmentId == null && cmbEquipment.SelectedValue is int cmbEqId && cmbEqId > 0)
            {
                _selectedEquipmentId = cmbEqId;
            }

            if (_selectedEquipmentId == null)
            {
                ToastHelper.Show(this, "Please select an equipment item.", ToastType.Warning, 3000, Color.FromArgb(245, 124, 0));
                return;
            }

            int? locationId = cmbRoom.Enabled && cmbRoom.SelectedValue is int rid ? rid : (int?)null;

            // The actual rule: equipment is required, and AT LEAST ONE of
            // employee / location must be set — not both, not neither.
            if (_selectedEmployeeId == null && locationId == null)
            {
                ToastHelper.Show(this, "Please choose an employee, a location, or both — at least one is required.", ToastType.Warning, 3000, Color.FromArgb(245, 124, 0));
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
        private Button btnSave = null!;
        private Button btnCancel = null!;

        private static Color EditAccent => Color.FromArgb(245, 158, 11);
        private static Color CardBorder => ThemeHelper.IsDarkMode ? Color.FromArgb(55, 58, 65) : Color.FromArgb(224, 224, 224);
        private static readonly Color MutedText = Color.FromArgb(110, 110, 120);

        public EditExpectedReturnDialog(DateOnly? currentDate, string? equipmentCode = null, string? qrCode = null, string? equipmentName = null)
        {
            BuildUI(currentDate, equipmentCode, qrCode, equipmentName);
        }

        private void BuildUI(DateOnly? currentDate, string? equipmentCode, string? qrCode, string? equipmentName)
        {
            Text = "Edit Return Date";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            Font = new Font("Segoe UI", 9F);
            ThemeHelper.ApplyFormBackground(this);

            const int formWidth = 470;
            const int margin = 26;
            int contentWidth = formWidth - margin * 2;

            // ── Header ──
            var pnlHeader = new Panel { Location = new Point(0, 0), Size = new Size(formWidth, 10), BackColor = EditAccent };
            Controls.Add(pnlHeader);

            var lblTitle = new Label
            {
                Text = "📅  Edit Expected Return Date",
                Font = new Font("Segoe UI", 14f, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(margin, 16)
            };
            pnlHeader.Controls.Add(lblTitle);

            var subtitleFont = new Font("Segoe UI", 9f, FontStyle.Regular);
            string subtitleText = "Update or clear the expected return date for this item";
            var lblSubtitle = new Label
            {
                Text = subtitleText,
                Font = subtitleFont,
                ForeColor = Color.FromArgb(255, 245, 230),
                AutoSize = true,
                Location = new Point(margin, lblTitle.Bottom + 4)
            };
            pnlHeader.Controls.Add(lblSubtitle);
            pnlHeader.Height = lblSubtitle.Bottom + 16;

            // ── Body ──
            var body = new Panel { Location = new Point(0, pnlHeader.Height), Size = new Size(formWidth, 10) };
            Controls.Add(body);

            int y = 16;

            // ── Equipment / QR details info card ──
            if (!string.IsNullOrWhiteSpace(equipmentCode) || !string.IsNullOrWhiteSpace(qrCode))
            {
                var pnlInfo = new Panel
                {
                    Location = new Point(margin, y),
                    Size = new Size(contentWidth, 48),
                    BackColor = ThemeHelper.IsDarkMode ? Color.FromArgb(35, 38, 45) : Color.FromArgb(245, 247, 250)
                };
                pnlInfo.Paint += (s, e) =>
                {
                    using var pen = new Pen(CardBorder, 1f);
                    e.Graphics.DrawRectangle(pen, 0, 0, pnlInfo.Width - 1, pnlInfo.Height - 1);
                };

                string eqLabel = string.IsNullOrWhiteSpace(equipmentName) || equipmentName == "—"
                    ? $"Equipment: {equipmentCode ?? "—"}"
                    : $"{equipmentCode} — {equipmentName}";

                var lblEq = new Label
                {
                    Text = eqLabel,
                    Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                    ForeColor = ThemeHelper.TextPrimary,
                    Location = new Point(12, 6),
                    AutoSize = true
                };
                var lblQr = new Label
                {
                    Text = $"QR Code: {qrCode ?? "—"}",
                    Font = new Font("Segoe UI", 8.5f),
                    ForeColor = ThemeHelper.TextSecondary,
                    Location = new Point(12, 26),
                    AutoSize = true
                };
                pnlInfo.Controls.Add(lblEq);
                pnlInfo.Controls.Add(lblQr);
                body.Controls.Add(pnlInfo);
                y += pnlInfo.Height + 16;
            }

            chkHasDate = new CheckBox
            {
                Text = "Set expected return date",
                Location = new Point(margin, y),
                AutoSize = true,
                Font = ThemeHelper.FontBold,
                ForeColor = ThemeHelper.TextPrimary,
                Checked = currentDate.HasValue
            };
            chkHasDate.CheckedChanged += (s, e) => dtp.Enabled = chkHasDate.Checked;
            body.Controls.Add(chkHasDate);
            y += 30;

            dtp = new DateTimePicker
            {
                Location = new Point(margin, y),
                Width = contentWidth,
                Height = 32,
                Format = DateTimePickerFormat.Long,
                Enabled = currentDate.HasValue,
                Value = currentDate.HasValue
                    ? currentDate.Value.ToDateTime(TimeOnly.MinValue)
                    : DateTime.Today.AddDays(7)
            };
            body.Controls.Add(dtp);
            y += 42;

            var lblHint = new Label
            {
                Text = "A past date marks this allocation overdue.\nA future (or no) date clears the overdue status.",
                Location = new Point(margin, y),
                Size = new Size(contentWidth, 36),
                Font = ThemeHelper.FontSmall,
                ForeColor = MutedText
            };
            body.Controls.Add(lblHint);
            y += 44;

            body.Height = y;

            // ── Footer ──
            const int footerHeight = 64;
            var pnlFooter = new Panel
            {
                Size = new Size(formWidth, footerHeight),
                BackColor = ThemeHelper.IsDarkMode ? Color.FromArgb(22, 24, 29) : Color.FromArgb(249, 250, 251)
            };
            pnlFooter.Controls.Add(new Panel { Location = new Point(0, 0), Size = new Size(formWidth, 1), BackColor = CardBorder });

            btnCancel = new Button { Text = "Cancel", Size = new Size(130, 38) };
            ThemeHelper.StyleSecondaryButton(btnCancel);
            btnCancel.Click += (s, e) => DialogResult = DialogResult.Cancel;

            btnSave = new Button { Text = "✓  Save", Size = new Size(130, 38) };
            ThemeHelper.StyleButton(btnSave, EditAccent);
            btnSave.Click += (s, e) =>
            {
                SelectedDate = chkHasDate.Checked ? DateOnly.FromDateTime(dtp.Value) : (DateOnly?)null;
                DialogResult = DialogResult.OK;
            };

            int totalButtonsWidth = btnCancel.Width + 12 + btnSave.Width;
            int buttonsStartX = (formWidth - totalButtonsWidth) / 2;
            btnSave.Location = new Point(buttonsStartX, 13);
            btnCancel.Location = new Point(buttonsStartX + btnSave.Width + 12, 13);

            pnlFooter.Controls.Add(btnCancel);
            pnlFooter.Controls.Add(btnSave);
            Controls.Add(pnlFooter);

            int totalHeight = pnlHeader.Height + body.Height + footerHeight;
            ClientSize = new Size(formWidth, totalHeight);
            pnlFooter.Location = new Point(0, ClientSize.Height - footerHeight);

            AcceptButton = btnSave;
            CancelButton = btnCancel;
        }
    }
}
