// Forms/EquipmentForm.cs
using Inventory_Management_System.Helpers;
using Inventory_Management_System.Models;
using Inventory_Management_System.Services;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.Linq;
using System.Windows.Forms;
using Color = System.Drawing.Color;
using Size = System.Drawing.Size;
using System.Drawing.Drawing2D;


namespace Inventory_Management_System.Forms
{
    public partial class EquipmentForm : Form, IThemableForm
    {
        private readonly EquipmentService _equipmentService = new();
        private List<Equipment> _allEquipment = new();

        private EquipmentSuggestHelper? _suggest;
        private List<Equipment> _fullEquipmentCache = new();

        private int _currentPage = 1;
        private int _pageSize = 25;
        private int _totalPages = 1;

        // ── Gmail-style checkbox header controls ──
        private Panel _chkHeaderPanel = null!;
        private CheckBox _chkHeaderBox = null!;
        private bool _suppressHeaderSync;

        // ── Category filter dropdown ───────────────────────────────────────
        private ComboBox cmbCategory = null!;
        private string _activeCategory = ""; // empty = All
        private string? _pendingSelectCode;
        private bool _hasPendingSelection = false;


        private string? _statusFilter = null;
        private Func<Equipment, bool>? _customFilter = null;
        public EquipmentForm()
        {
            InitializeComponent();
            SetupPageSizeOptions();
            ApplyTheme();
            SetupToolbarLayout();
            LoadCategoryFilter();

            // Force panelTop to match the fully-populated toolbar (it may now
            // wrap to a 2nd row). Without this, panelTop keeps its designer-time
            // height, the 2nd row (search box) renders outside it, and the grid
            // draws over it.
            flowToolbar.AutoSize = true;
            flowToolbar.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            panelTop.Height = flowToolbar.Height;

            LoadEquipment();
            SetupSearchSuggestions();

            flowToolbar.SizeChanged += (s, e) => panelTop.Height = flowToolbar.Height;

            this.Shown += (s, e) =>
            {
                if (!_hasPendingSelection)
                {
                    dgvEquipment.ClearSelection();
                    dgvEquipment.CurrentCell = null;
                }
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


        public void ApplyStatusFilter(string status)
        {
            _statusFilter = status;
            _customFilter = null;
            _currentPage = 1;
            LoadEquipment();
        }

        public void ApplyCustomFilter(Func<Equipment, bool> filter)
        {
            _customFilter = filter;
            _statusFilter = null;
            _currentPage = 1;
            LoadEquipment();
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

        public EquipmentForm(string equipmentCode) : this()
        {
            _hasPendingSelection = true;
            _pendingSelectCode = equipmentCode;
            _suggest?.SetTextWithoutSuggesting(equipmentCode);
        }

        // ── Toolbar layout ─────────────────────────────────────────────────
        // Restores the original designer dimensions for the buttons so text is
        // not truncated, and programmatically replaces the "Select All" button 
        // with a beautifully styled ComboBox (dropdown) in the exact same position.
        private void SetupToolbarLayout()
        {
            // Hide the original btnSelectAll button
            btnSelectAll.Visible = false;

            // ── 1. Create the ColSelect checkbox column (always visible) ──
            if (!dgvEquipment.Columns.Contains("ColSelect"))
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
                dgvEquipment.Columns.Add(chkCol);
                dgvEquipment.Columns["ColSelect"].DisplayIndex = 0;

                // Configure grid to allow editing the Checkbox column while others are read-only
                dgvEquipment.ReadOnly = false;
                foreach (DataGridViewColumn col in dgvEquipment.Columns)
                {
                    if (col.Name == "ColSelect")
                        col.ReadOnly = false;
                    else
                        col.ReadOnly = true;
                }

                // Immediately commit edits when clicking the checkbox cell so value updates instantly
                dgvEquipment.CurrentCellDirtyStateChanged += (s, ev) =>
                {
                    if (dgvEquipment.CurrentCell is DataGridViewCheckBoxCell)
                        dgvEquipment.CommitEdit(DataGridViewDataErrorContexts.Commit);
                };

                dgvEquipment.CellValueChanged += (s, ev) =>
                {
                    if (ev.ColumnIndex >= 0 && dgvEquipment.Columns[ev.ColumnIndex].Name == "ColSelect")
                    {
                        SyncHeaderCheckbox();
                    }
                };
            }

            // ── 2. Build the split [☐ ▾] toolbar control ──
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

            // ── 3. Insert the panel BEFORE btnAdd in the toolbar ──
            var flow = panelTop.Controls.OfType<FlowLayoutPanel>().First();
            flow.Controls.Add(_chkHeaderPanel);
            flow.Controls.SetChildIndex(_chkHeaderPanel, flow.Controls.IndexOf(btnAdd));

            // Not added to the toolbar — kept only as a data source for the
            // Filters button's category submenu (see PopulateCategorySubmenu).
            cmbCategory = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };

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
                    ShowFiltersMenu(btnFilters);
            };
            btnFilters.Click += (s, e) => ShowFiltersMenu(btnFilters);

            flow.Controls.Add(btnFilters);
            flow.Controls.SetChildIndex(btnFilters, flow.Controls.IndexOf(btnGenerateQRSheet) + 1);

            // Re-apply QR button styling after SetupToolbarLayout (ensures correct order)
            StyleQrButton(btnShowQR, "◼️ Show QR");
            StyleQrButton(btnGenerateQRSheet, "📄 QR Sheet");
        }

        private void SetAllCheckboxes(bool check)
        {
            if (!dgvEquipment.Columns.Contains("ColSelect")) return;
            foreach (DataGridViewRow row in dgvEquipment.Rows)
                row.Cells["ColSelect"].Value = check;
            dgvEquipment.RefreshEdit();
        }

        private void SyncHeaderCheckbox()
        {
            if (!dgvEquipment.Columns.Contains("ColSelect") || _chkHeaderBox == null) return;
            _suppressHeaderSync = true;
            int total = dgvEquipment.Rows.Count;
            int checked_ = dgvEquipment.Rows.Cast<DataGridViewRow>()
                .Count(r => Convert.ToBoolean(r.Cells["ColSelect"].Value));
            _chkHeaderBox.Checked = total > 0 && checked_ == total;
            _suppressHeaderSync = false;
        }

        private void ShowFiltersMenu(Button anchor)
        {
            var menu = new ContextMenuStrip { Font = ThemeHelper.FontDefault };

            var statusSub = new ToolStripMenuItem("Status");
            menu.Items.Add(statusSub);
            PopulateStatusSubmenu(statusSub);

            var categorySub = new ToolStripMenuItem("Category");
            menu.Items.Add(categorySub);
            PopulateCategorySubmenu(categorySub);

            menu.Show(anchor, new Point(0, anchor.Height + 2));
            anchor.ContextMenuStrip = menu;

            var hoverTimer = new System.Windows.Forms.Timer { Interval = 40 };
            hoverTimer.Tick += (s, e) =>
            {
                if (menu == null || menu.IsDisposed || !menu.Visible)
                {
                    hoverTimer.Stop();
                    hoverTimer.Dispose();
                    return;
                }
                var cursor = Cursor.Position;
                var btnRect = anchor.RectangleToScreen(anchor.ClientRectangle);
                btnRect.Inflate(4, 12);
                var menuRect = menu.Bounds;
                menuRect.Inflate(4, 4);

                if (!btnRect.Contains(cursor) && !menuRect.Contains(cursor))
                {
                    menu.Close();
                    hoverTimer.Stop();
                    hoverTimer.Dispose();
                }
            };
            hoverTimer.Start();
        }

        private void PopulateStatusSubmenu(ToolStripMenuItem parent)
        {
            parent.DropDownItems.Clear();

            var options = new (string label, string? value)[]
            {
                ("All", null),
                ("In store", "in_store"),
                ("Allocated", "allocated"),
                ("Decommission", "decommissioned"),
                ("Under repair", "under_repair")
            };

            foreach (var (label, value) in options)
            {
                bool isChecked = string.IsNullOrEmpty(_statusFilter) ? value == null : string.Equals(_statusFilter, value, StringComparison.OrdinalIgnoreCase);
                var item = new ToolStripMenuItem(label) { Checked = isChecked };
                item.Click += (s, e) =>
                {
                    _statusFilter = value;
                    _customFilter = null;
                    _currentPage = 1;
                    LoadEquipment(txtSearch.Text.Trim());
                };
                parent.DropDownItems.Add(item);
            }
        }

        private void PopulateCategorySubmenu(ToolStripMenuItem parent)
        {
            parent.DropDownItems.Clear();

            var allItem = new ToolStripMenuItem("All Categories") { Checked = string.IsNullOrEmpty(_activeCategory) };
            allItem.Click += (s, e) => { _activeCategory = ""; _currentPage = 1; LoadEquipment(txtSearch.Text.Trim()); };
            parent.DropDownItems.Add(allItem);
            parent.DropDownItems.Add(new ToolStripSeparator());

            foreach (var cat in cmbCategory?.Items.OfType<string>().Where(c => c != "All Categories") ?? Enumerable.Empty<string>())
            {
                var item = new ToolStripMenuItem(cat) { Checked = _activeCategory == cat };
                item.Click += (s, e) => { _activeCategory = cat; _currentPage = 1; LoadEquipment(txtSearch.Text.Trim()); };
                parent.DropDownItems.Add(item);
            }

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
        // ── Theme ──────────────────────────────────────────────────────────

        public void ApplyTheme()
        {
            ThemeHelper.ApplyFormBackground(this);
            ThemeHelper.StyleToolbarPanel(panelTop);
            ThemeHelper.StyleButton(btnAdd);
            ThemeHelper.StyleButton(btnRefresh, Color.FromArgb(33, 150, 243));
            ThemeHelper.StyleDangerButton(btnDelete);
            ThemeHelper.StyleButton(btnEdit, Color.FromArgb(245, 158, 11));

            // Export and Import Excel: matching secondary style (white bg, dark border/text)
            ThemeHelper.StyleSecondaryButton(btnExport);
            ThemeHelper.StyleSecondaryButton(btnImportExcel);

            // QR buttons: white bg, dark gray (#555) border, dark gray (#333) text
            // Hover: light gray (#F2F2F2) — both identical so they read as a set
            StyleQrButton(btnShowQR,          "◼️ Show QR");
            StyleQrButton(btnGenerateQRSheet,  "📄 QR Sheet");

            ThemeHelper.StyleButton(btnSelectAll);
            ApplyRoundedSearchStyle(txtSearch);
                ThemeHelper.StyleComboBox(cmbPageSize);
            ThemeHelper.StyleSecondaryButton(btnPrevPage);
            ThemeHelper.StyleSecondaryButton(btnNextPage);
            ThemeHelper.StyleDataGridView(dgvEquipment);
            ResetRowStyleOverrides(dgvEquipment);
            panelBottom.BackColor = ThemeHelper.IsDarkMode ? Color.FromArgb(22, 24, 29) : Color.FromArgb(249, 250, 251);
            lblRowCount.ForeColor = ThemeHelper.TextSecondary;
            lblRowCount.Font = ThemeHelper.FontSmall;
        }

        // Applies the shared QR-theme style (white bg, dark gray border + text, hover)
        private static void StyleQrButton(Button btn, string text)
        {
            var borderColor = Color.FromArgb(85, 85, 85);   // #555555
            var textColor   = Color.FromArgb(51, 51, 51);   // #333333
            var hoverBg     = Color.FromArgb(242, 242, 242); // #F2F2F2

            btn.Text      = text;
            btn.FlatStyle = FlatStyle.Flat;
            btn.BackColor = Color.White;
            btn.ForeColor = textColor;
            btn.Font      = ThemeHelper.FontBold;
            btn.Cursor    = Cursors.Hand;
            btn.FlatAppearance.BorderColor     = borderColor;
            btn.FlatAppearance.BorderSize      = 1;
            btn.FlatAppearance.MouseOverBackColor  = hoverBg;
            btn.FlatAppearance.MouseDownBackColor  = Color.FromArgb(230, 230, 230);
        }

        // ── Data ───────────────────────────────────────────────────────────

        private bool MatchesStatusFilter(Equipment eq)
        {
            if (string.IsNullOrEmpty(_statusFilter) || _statusFilter == "All") return true;

            var activeAlloc = eq.Allocations?
                .Where(a => a.Status != null && a.Status.Equals("approved", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(a => a.EmployeeId.HasValue)
                .ThenByDescending(a => a.Id)
                .FirstOrDefault();

            var normalized = (eq.CurrentStatus ?? string.Empty).Trim().ToLowerInvariant();
            string effectiveStatus = activeAlloc != null ? "allocated" : normalized;

            return _statusFilter.ToLowerInvariant() switch
            {
                "in_store" or "in store" => effectiveStatus == "in_store",
                "allocated" => effectiveStatus == "allocated",
                "decommission" or "decommissioned" => effectiveStatus == "decommissioned",
                "under_repair" or "under repair" => effectiveStatus == "under_repair" || effectiveStatus == "under repair",
                _ => string.Equals(effectiveStatus, _statusFilter, StringComparison.OrdinalIgnoreCase)
            };
        }

        private void LoadEquipment(string searchTerm = "")
        {
            if (_statusFilter != null || _customFilter != null)
            {
                if (_fullEquipmentCache.Count == 0)
                    RefreshEquipmentCache();

                Func<Equipment, bool> predicate = _customFilter
                    ?? MatchesStatusFilter;

                var filtered = _fullEquipmentCache.Where(predicate).ToList();

                if (!string.IsNullOrEmpty(_activeCategory))
                    filtered = filtered.Where(eq => string.Equals(eq.Category, _activeCategory, StringComparison.OrdinalIgnoreCase)).ToList();

                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    filtered = filtered
                        .Select(eq => new
                        {
                            Equipment = eq,
                            Score = Math.Max(
                                FuzzySearchHelper.FuzzyScoreCode(eq.EquipmentCode, searchTerm),
                                Math.Max(
                                    FuzzySearchHelper.FuzzyScoreCode(eq.QrCode, searchTerm),
                                    Math.Max(
                                        FuzzySearchHelper.FuzzyScoreCode(eq.SerialNumber, searchTerm),
                                        Math.Max(
                                            FuzzySearchHelper.FuzzyScore(eq.Name, searchTerm),
                                            FuzzySearchHelper.FuzzyScore(eq.Make, searchTerm)))))
                        })
                        .Where(x => x.Score >= 5000)
                        .OrderByDescending(x => x.Score)
                        .Select(x => x.Equipment)
                        .ToList();
                }

                _totalPages = Math.Max(1, (int)Math.Ceiling(filtered.Count / (double)_pageSize));
                if (_currentPage > _totalPages) _currentPage = _totalPages;

                _allEquipment = filtered
                    .Skip((_currentPage - 1) * _pageSize)
                    .Take(_pageSize)
                    .ToList();

                UpdatePaginationControls();
                ApplyFilters();
                return;
            }

            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                var result = _equipmentService.GetPage(_currentPage, _pageSize, _activeCategory);
                if (!result.Success)
                {
                    ToastHelper.Show(this, result.ErrorMessage, ToastType.Error);
                    return;
                }

                _totalPages = Math.Max(1, result.Data!.TotalPages);
                if (_currentPage > _totalPages) _currentPage = _totalPages;

                _allEquipment = result.Data.Items;
            }
            else
            {
                if (_fullEquipmentCache.Count == 0)
                    RefreshEquipmentCache();

                var candidates = string.IsNullOrEmpty(_activeCategory)
                    ? _fullEquipmentCache
                    : _fullEquipmentCache.Where(eq => string.Equals(eq.Category, _activeCategory, StringComparison.OrdinalIgnoreCase)).ToList();

                var fullMatches = candidates
                      .Select(eq => new
                      {
                          Equipment = eq,
                          Score = Math.Max(
                              FuzzySearchHelper.FuzzyScoreCode(eq.EquipmentCode, searchTerm),
                              Math.Max(
                                  FuzzySearchHelper.FuzzyScoreCode(eq.QrCode, searchTerm),
                                  Math.Max(
                                      FuzzySearchHelper.FuzzyScoreCode(eq.SerialNumber, searchTerm),
                                      Math.Max(
                                          FuzzySearchHelper.FuzzyScore(eq.Name, searchTerm),
                                          FuzzySearchHelper.FuzzyScore(eq.Make, searchTerm)))))
                      })
                      .Where(x => x.Score >= 5000)
                      .OrderByDescending(x => x.Score)
                      .Select(x => x.Equipment)
                      .ToList();

                _totalPages = Math.Max(1, (int)Math.Ceiling(fullMatches.Count / (double)_pageSize));
                if (_currentPage > _totalPages) _currentPage = _totalPages;

                _allEquipment = fullMatches
                           .Skip((_currentPage - 1) * _pageSize)
                           .Take(_pageSize)
                           .ToList();
            }

            UpdatePaginationControls();
            ApplyFilters();
        }


        private void UpdatePaginationControls()
        {
            lblPageIndicator.Text = $"Page {_currentPage} of {Math.Max(1, _totalPages)}";
            btnPrevPage.Enabled = _currentPage > 1;
            btnNextPage.Enabled = _currentPage < _totalPages;
        }

        // ── Populate category dropdown from distinct DB values ─────────────
        private void LoadCategoryFilter()
        {
            try
            {
                using var db = new InventoryDbContext();
                var dbCategories = db.Equipment
                    .Where(e => e.Category != null && e.Category != "")
                    .Select(e => e.Category!)
                    .Distinct()
                    .ToList();

                // Union: every category from Settings → Categories table, PLUS
                // any legacy category text already on equipment records (even
                // if it's not in the categories table), so nothing that's
                // actually in use ever disappears.
                var categoryNames = new CategoryService().GetAll().Data?
                    .Select(c => c.Name).ToList() ?? new List<string>();

                var allCategories = categoryNames
                    .Union(dbCategories, StringComparer.OrdinalIgnoreCase)
                    .OrderBy(c => c, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                cmbCategory.Items.Clear();
                cmbCategory.Items.Add("All Categories");
                foreach (var cat in allCategories)
                    cmbCategory.Items.Add(cat);

                cmbCategory.SelectedIndex = 0;
            }
            catch
            {
                cmbCategory.Items.Clear();
                cmbCategory.Items.Add("All Categories");
                cmbCategory.SelectedIndex = 0;
            }
        }

        // ── Fuzzy suggestion dropdown for the search box, same helper as QRScannerForm ──
        private void SetupSearchSuggestions()
        {
            try
            {
                _suggest = new EquipmentSuggestHelper(
                    textBox: txtSearch,
                    hostForm: this,
                    onSelected: eq =>
                    {
                        _currentPage = 1;
                        txtSearch.Text = eq.EquipmentCode;   // fires TextChanged → LoadEquipment
                    });

                RefreshEquipmentCache();
            }
            catch { /* suggestions are non-critical — fail silently */ }
        }

        // Keeps the in-memory fuzzy-search cache (and the dropdown's data) in sync
        // with the DB after any add/edit/delete/import.
        private void RefreshEquipmentCache()
        {
            var allResult = _equipmentService.GetAll();
            _fullEquipmentCache = allResult.Data ?? new List<Equipment>();
            _suggest?.LoadEquipment(_fullEquipmentCache);
        }

        // Import-specific refresh: also clears any active filter/page state so
        // newly imported rows are never hidden behind a stale filter or page.
        private void RefreshAfterImport()
        {
            RefreshEquipmentCache();
            _currentPage = 1;
            _activeCategory = "";
            _statusFilter = null;
            _customFilter = null;
            LoadEquipment();
            LoadCategoryFilter();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _suggest?.Dispose();
            base.OnFormClosed(e);
        }

        private void ApplyFilters()
        {
            RefreshGrid(_allEquipment);
        }

        private void RefreshGrid(List<Equipment> items)
        {
            dgvEquipment.Rows.Clear();

            foreach (var item in items)
            {
                var activeAlloc = item.Allocations
                    .Where(a => a.Status != null && a.Status.Equals("approved", StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(a => a.EmployeeId.HasValue)
                    .ThenByDescending(a => a.Id)
                    .FirstOrDefault();
                string locationName = FormatLocationDisplay(activeAlloc?.Location);

                string procType = item.ProcurementType?.TypeName ?? "";

                var normalizedStatus = (item.CurrentStatus ?? string.Empty).Trim().ToLowerInvariant();
                string formattedStatus;
                // If there is an approved allocation, display Allocated regardless of stored status
                if (activeAlloc != null)
                {
                    formattedStatus = "Allocated";
                }
                else
                {
                    formattedStatus = normalizedStatus switch
                    {
                        "in_store" => "In Store",
                        "allocated" => "Allocated",
                        "decommissioned" => "Decommissioned",
                        "under_repair" => "Under Repair",
                        _ => item.CurrentStatus ?? ""
                    };
                }

                int rowIdx = dgvEquipment.Rows.Add(
      item.EquipmentCode,
      item.QrCode,
      procType,
      item.Name,
      item.Make,
      item.Model,
      item.SerialNumber,
      item.Category ?? "",
      formattedStatus,
      item.ConditionGrade ?? "",
      locationName,
      item.PurchaseDate?.ToString("yyyy-MM-dd") ?? "",
      item.Price.HasValue ? item.Price.Value.ToString("N2") : ""
  );

                var statusCell = dgvEquipment.Rows[rowIdx].Cells["Status"];
                switch (formattedStatus)
                {
                    case "In Store":
                        statusCell.Style.BackColor = ThemeHelper.Success;
                        break;
                    case "Allocated":
                        statusCell.Style.BackColor = ThemeHelper.Warning;
                        break;
                    case "Under Repair":
                        statusCell.Style.BackColor = Color.FromArgb(255, 112, 67);
                        break;
                    case "Decommissioned":
                        statusCell.Style.BackColor = ThemeHelper.Danger;
                        break;
                    default:
                        statusCell.Style.BackColor = ThemeHelper.GridAltRow;
                        break;
                }
                statusCell.Style.ForeColor = Color.White;
                statusCell.Style.Font = ThemeHelper.FontBold;
            }

            if (!string.IsNullOrWhiteSpace(_pendingSelectCode))
            {
                var match = dgvEquipment.Rows.Cast<DataGridViewRow>()
                    .FirstOrDefault(r => string.Equals(r.Cells["Code"].Value?.ToString(), _pendingSelectCode, StringComparison.OrdinalIgnoreCase));
                if (match != null)
                {
                    dgvEquipment.ClearSelection();
                    match.Selected = true;
                    dgvEquipment.CurrentCell = match.Cells[0];
                    dgvEquipment.FirstDisplayedScrollingRowIndex = match.Index;
                }
                _pendingSelectCode = null;
            }
            lblRowCount.Text = $"Showing {dgvEquipment.Rows.Count} records";
            SyncHeaderCheckbox();
        }

        private static string FormatLocationDisplay(Location? loc)
        {
            if (loc == null || string.IsNullOrWhiteSpace(loc.LocationName)) return "";

            bool hasNum = !string.IsNullOrWhiteSpace(loc.RoomNumber);
            bool hasRoomName = !string.IsNullOrWhiteSpace(loc.RoomName);
            if (!hasNum && !hasRoomName) return loc.LocationName;

            string room = hasNum ? $"Room {loc.RoomNumber}" : "";
            if (hasRoomName) room = string.IsNullOrEmpty(room) ? loc.RoomName! : $"{room} ({loc.RoomName})";

            return $"{loc.LocationName} - {room}";
        }

        // ── Selection helpers ──────────────────────────────────────────────

        private Equipment? GetSelectedEquipment()
        {
            if (dgvEquipment.SelectedRows.Count == 0) return null;
            var code = dgvEquipment.SelectedRows[0].Cells["Code"].Value?.ToString();
            return _allEquipment.FirstOrDefault(e => e.EquipmentCode == code);
        }

        private List<Equipment> GetSelectedEquipmentList()
        {
            var selected = new List<Equipment>();

            // First check if any rows have ColSelect checked
            if (dgvEquipment.Columns.Contains("ColSelect"))
            {
                foreach (DataGridViewRow row in dgvEquipment.Rows)
                {
                    var cellValue = row.Cells["ColSelect"].Value;
                    if (cellValue != null && cellValue is bool b && b)
                    {
                        var code = row.Cells["Code"].Value?.ToString();
                        var eq = _allEquipment.FirstOrDefault(e => e.EquipmentCode == code);
                        if (eq != null) selected.Add(eq);
                    }
                }
            }

            // If nothing checked with checkboxes, fall back to row selection
            if (selected.Count == 0)
            {
                foreach (DataGridViewRow row in dgvEquipment.SelectedRows)
                {
                    var code = row.Cells["Code"].Value?.ToString();
                    var eq = _allEquipment.FirstOrDefault(e => e.EquipmentCode == code);
                    if (eq != null) selected.Add(eq);
                }
            }

            return selected;
        }

        // ── Grid events ────────────────────────────────────────────────────

        private void dgvEquipment_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var eq = GetSelectedEquipment();
            if (eq == null) return;
            using var popup = new EquipmentDetailPopup(eq);
            popup.ShowDialog();
        }

        private void RefreshAfterDataChange(string? reselectCode = null)
        {
            bool cacheInUse = !string.IsNullOrWhiteSpace(txtSearch.Text) || _statusFilter != null || _customFilter != null;
            if (cacheInUse)
                RefreshEquipmentCache();

            if (!string.IsNullOrWhiteSpace(reselectCode))
                _pendingSelectCode = reselectCode;

            LoadEquipment(txtSearch.Text.Trim());
            LoadCategoryFilter();
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            using var form = new AddEditEquipmentForm();
            if (form.ShowDialog() == DialogResult.OK)
            {
                ToastHelper.Show(this, "Equipment added successfully.", ToastType.Success);
                RefreshAfterDataChange();
            }
        }

        private void btnEdit_Click(object sender, EventArgs e)
        {
            var eq = GetSelectedEquipment();
            if (eq == null)
            {
                ToastHelper.Show(this, "Please select an equipment row to edit.", ToastType.Info);
                return;
            }
            using var form = new AddEditEquipmentForm(eq);
            if (form.ShowDialog() == DialogResult.OK)
            {
                ToastHelper.Show(this, "Equipment updated successfully.", ToastType.Success);
                RefreshAfterDataChange(eq.EquipmentCode);
            }
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            var selectedList = GetSelectedEquipmentList();

            if (selectedList.Count == 0)
            {
                ToastHelper.Show(this, "Please select at least one equipment row to delete.", ToastType.Info);
                return;
            }

            const int maxListed = 15; // beyond this, the dialog gets too tall and hides the buttons

            string confirmMessage;
            if (selectedList.Count == 1)
            {
                confirmMessage = $"Are you sure you want to delete '{selectedList[0].Name}' ({selectedList[0].EquipmentCode})?";
            }
            else if (selectedList.Count <= maxListed)
            {
                confirmMessage = $"Are you sure you want to delete these {selectedList.Count} items?\n\n" +
                    string.Join("\n", selectedList.Select(eq => $"• {eq.Name} ({eq.EquipmentCode})"));
            }
            else
            {
                var preview = selectedList.Take(maxListed).Select(eq => $"• {eq.Name} ({eq.EquipmentCode})");
                int remaining = selectedList.Count - maxListed;
                confirmMessage = $"Are you sure you want to delete these {selectedList.Count} items?\n\n" +
                    string.Join("\n", preview) +
                    $"\n\n...and {remaining} more item(s).";
            }

            // Kept as MessageBox — this is a destructive Yes/No confirmation.
            if (MessageBox.Show(confirmMessage, "Confirm Delete",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                return;


            var result = _equipmentService.DeleteRange(selectedList.Select(eq => eq.Id));
            RefreshAfterDataChange();

            if (result.Success)
            {
                ToastHelper.Show(this, $"{result.Data} item(s) deleted successfully.", ToastType.Success);
            }
            else
            {
                ToastHelper.Show(this, result.ErrorMessage, ToastType.Warning);
            }
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            txtSearch.Text = string.Empty;
            _currentPage = 1;
            _statusFilter = null;
            _customFilter = null;

            SetAllCheckboxes(false);
            if (_chkHeaderBox != null) _chkHeaderBox.Checked = false;

            dgvEquipment.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvEquipment.ClearSelection();

            LoadEquipment();
        }

        private void btnExport_Click(object sender, EventArgs e)
        {
            if (_allEquipment.Count == 0)
            {
                ToastHelper.Show(this, "No data to export.", ToastType.Info);
                return;
            }

            using var sfd = new SaveFileDialog
            {
                Filter = "Excel Files|*.xlsx",
                Title = "Export Equipment",
                FileName = $"Equipment_{DateTime.Now:yyyyMMdd_HHmm}.xlsx"
            };

            if (sfd.ShowDialog() != DialogResult.OK) return;

            try
            {
                ExcelHelper.ExportEquipment(_allEquipment, sfd.FileName);
                ToastHelper.Show(this, "Equipment exported successfully.", ToastType.Success);
            }
            catch (Exception ex)
            {
                ToastHelper.Show(this, $"Export failed: {ex.Message}", ToastType.Error);
            }
        }

        // Original btnSelectAll click event (kept to avoid potential Designer reference exceptions)
        private void btnSelectAll_Click(object sender, EventArgs e)
        {
            dgvEquipment.MultiSelect = true;
            dgvEquipment.SelectAll();
        }



        private void btnGenerateQRSheet_Click(object sender, EventArgs e)
        {
            var selected = GetSelectedEquipmentList();
            if (selected.Count == 0)
            {
                ToastHelper.Show(this, "Please select at least one equipment item.", ToastType.Info);
                return;
            }

            var qrItems = new List<(Equipment eq, Bitmap bmp)>();
            foreach (var eq in selected)
            {
                string qrContent = eq.QrCode ?? eq.EquipmentCode;
                if (string.IsNullOrWhiteSpace(qrContent)) continue;
                try
                {
                    var bmp = QRHelper.GenerateQRCode(qrContent, 6);
                    qrItems.Add((eq, bmp));
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "EquipmentForm.btnPrintSelectedQR_Click (GenerateQRCode)");
                }
            }

            if (qrItems.Count == 0)
            {
                ToastHelper.Show(this, "None of the selected items have a valid QR code.", ToastType.Warning);
                return;
            }

            const int pageW = 827;
            const int pageH = 1169;
            const int marginL = 40;
            const int marginT = 40;
            const int cellW = 180;
            const int cellH = 200;
            const int cols = 4;
            const int qrSize = 120;

            int currentIndex = 0;

            using var pd = new PrintDocument();
            pd.DefaultPageSettings.PaperSize = new PaperSize("A4", pageW, pageH);
            pd.DefaultPageSettings.Margins = new Margins(marginL, marginL, marginT, marginT);

            pd.BeginPrint += (ps, pe) =>
            {
                currentIndex = 0;
            };

            pd.PrintPage += (ps, pe) =>
            {
                var g = pe.Graphics!;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                string companyName = "Q-Inventory";
                try
                {
                    using var db = new InventoryDbContext();
                    companyName = db.AppSettings.FirstOrDefault()?.CompanyName ?? "Q-Inventory";
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "EquipmentForm.PrintPage (AppSettings query)");
                }

                using var codeFont = new Font("Segoe UI", 7f, FontStyle.Bold);
                using var nameFont = new Font("Segoe UI", 6f, FontStyle.Regular);
                using var coFont = new Font("Segoe UI", 5.5f, FontStyle.Regular);

                int col = 0;
                int row = 0;
                int startX = marginL;
                int startY = marginT;

                while (currentIndex < qrItems.Count)
                {
                    int x = startX + col * (cellW + 10);
                    int y = startY + row * (cellH + 10);

                    if (y + cellH > pageH - marginT)
                    {
                        pe.HasMorePages = true;
                        return;
                    }

                    var (eq, bmp) = qrItems[currentIndex];

                    g.DrawRectangle(Pens.LightGray, x, y, cellW, cellH);

                    int qrX = x + (cellW - qrSize) / 2;
                    int qrY = y + 8;
                    g.DrawImage(bmp, new Rectangle(qrX, qrY, qrSize, qrSize));

                    g.DrawString(companyName, coFont, Brushes.DimGray,
                        new RectangleF(x + 4, qrY + qrSize + 4, cellW - 8, 16));
                    g.DrawString(eq.EquipmentCode, codeFont, Brushes.Black,
                        new RectangleF(x + 4, qrY + qrSize + 18, cellW - 8, 18));

                    string name = eq.Name.Length > 22 ? eq.Name[..22] + "…" : eq.Name;
                    g.DrawString(name, nameFont, Brushes.DimGray,
                        new RectangleF(x + 4, qrY + qrSize + 34, cellW - 8, 16));
                    g.DrawString($"Status: {eq.CurrentStatus}", nameFont, Brushes.DimGray,
                        new RectangleF(x + 4, qrY + qrSize + 48, cellW - 8, 16));

                    currentIndex++;
                    col++;
                    if (col >= cols) { col = 0; row++; }
                }

                pe.HasMorePages = false;
            };

            try
            {
                using var ppd = new PrintPreviewDialog
                {
                    Document = pd,
                    Width = 900,
                    Height = 700,
                    StartPosition = FormStartPosition.CenterParent,
                    Text = $"QR Sheet Preview — {qrItems.Count} items"
                };
                ppd.ShowDialog();
            }
            catch (System.Drawing.Printing.InvalidPrinterException)
            {
                ToastHelper.Show(this,
                    "No printer is installed on this machine, so a print preview can't be shown. Install a printer (a virtual PDF printer works fine for testing) and try again.",
                    ToastType.Warning);
            }
            finally
            {
                foreach (var (_, bmp) in qrItems)
                    bmp.Dispose();
            }
        }

        private void btnShowQR_Click(object sender, EventArgs e)
        {
            var eq = GetSelectedEquipment();
            if (eq == null)
            {
                ToastHelper.Show(this, "Please select an equipment row first.", ToastType.Info);
                return;
            }

            string qrContent = eq.QrCode ?? eq.EquipmentCode;
            if (string.IsNullOrWhiteSpace(qrContent))
            {
                ToastHelper.Show(this, "This equipment has no QR code assigned.", ToastType.Info);
                return;
            }

            Bitmap? qrBitmap = null;
            try
            {
                qrBitmap = QRHelper.GenerateQRCode(qrContent, 10);
            }
            catch (Exception ex)
            {
                ToastHelper.Show(this,
                    $"Could not generate QR code: {ex.Message}. Make sure the QRCoder NuGet package is installed.",
                    ToastType.Error);
                return;
            }

            // Fetch company name once, used for the details block below the QR.
            string companyName = "Q-Inventory";
            try
            {
                using var db0 = new InventoryDbContext();
                companyName = db0.AppSettings.FirstOrDefault()?.CompanyName ?? "Q-Inventory";
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "EquipmentForm.ShowSingleQRPreview (AppSettings query)");
            }

            var frm = new Form
            {
                Text = $"QR Code — {eq.EquipmentCode}",
                Size = new Size(380, 460),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                StartPosition = FormStartPosition.CenterParent,
                BackColor = Color.White
            };

            // ── QR image (slightly smaller to leave room for details below) ────
            var pic = new PictureBox
            {
                Image = qrBitmap,
                SizeMode = PictureBoxSizeMode.Zoom,
                Size = new Size(240, 240),
                Location = new Point((380 - 240) / 2, 18),
                BackColor = Color.White
            };

            var lblCode = new Label
            {
                Text = $"{eq.EquipmentCode}  •  {eq.Name}",
                Font = ThemeHelper.FontBold,
                ForeColor = ThemeHelper.TextPrimary,
                AutoSize = false,
                Width = 344,
                Height = 22,
                Location = new Point(18, 264),
                TextAlign = ContentAlignment.MiddleCenter
            };

            // ── Additional equipment details (moved here from the print popup) ─
            using var smallFont = new Font("Segoe UI", 8f, FontStyle.Regular);
            var lblDetails = new Label
            {
                Text = $"Company: {companyName}\n" +
                            $"Status: {eq.CurrentStatus}\n" +
                            $"Category: {(string.IsNullOrWhiteSpace(eq.Category) ? "—" : eq.Category)}\n" +
                            $"Purchase Date: {eq.PurchaseDate?.ToString("yyyy-MM-dd") ?? "—"}",
                Font = smallFont,
                ForeColor = Color.DimGray,
                AutoSize = false,
                Width = 344,
                Height = 66,
                Location = new Point(18, 288),
                TextAlign = ContentAlignment.TopCenter
            };

            var btnPrint = new Button
            {
                Text = "🖨  Print QR",
                Size = new Size(160, 38),
                Location = new Point(38, 368),
                BackColor = ThemeHelper.Accent,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = ThemeHelper.FontBold,
                Cursor = Cursors.Hand
            };
            btnPrint.FlatAppearance.BorderSize = 0;
            btnPrint.Click += (s, ev) =>
            {
                try
                {
                    using var db = new InventoryDbContext();
                    var setting = db.AppSettings.FirstOrDefault();
                    string printerName = setting?.PrinterName ?? "";
                    int lw = setting?.LabelWidthMm ?? 62;
                    int lh = setting?.LabelHeightMm ?? 29;

                    using var pd = new PrintDocument();
                    if (!string.IsNullOrWhiteSpace(printerName))
                        pd.PrinterSettings.PrinterName = printerName;

                    int wHundredths = (int)(lw * 3.937);
                    int hHundredths = (int)(lh * 3.937);
                    pd.DefaultPageSettings.PaperSize =
                        new PaperSize("Label", wHundredths, hHundredths);
                    pd.DefaultPageSettings.Margins = new Margins(10, 10, 10, 10);

                    // Print ONLY the QR code — no text, no equipment details.
                    pd.PrintPage += (ps, pe) =>
                    {
                        var g = pe.Graphics!;
                        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                        var bounds = pe.PageBounds;
                        int qrSz = Math.Min(bounds.Width - 10, bounds.Height - 10);

                        g.DrawImage(qrBitmap,
                            new Rectangle(
                                bounds.X + (bounds.Width - qrSz) / 2,
                                bounds.Y + (bounds.Height - qrSz) / 2,
                                qrSz, qrSz));

                        pe.HasMorePages = false;
                    };

                    using var ppd = new PrintPreviewDialog
                    {
                        Document = pd,
                        Width = 600,
                        Height = 400,
                        StartPosition = FormStartPosition.CenterParent,
                        Text = "QR Code Print Preview"
                    };
                    ppd.ShowDialog();
                }
                catch (Exception ex)
                {
                    ToastHelper.Show(this, "Printing failed: " + ex.Message, ToastType.Error);
                }
            };

            var btnClose = new Button
            {
                Text = "Close",
                Size = new Size(120, 38),
                Location = new Point(210, 368),
                FlatStyle = FlatStyle.Flat,
                Font = ThemeHelper.FontDefault,
                Cursor = Cursors.Hand
            };
            btnClose.Click += (s, ev) => frm.Close();

            frm.Controls.Add(pic);
            frm.Controls.Add(lblCode);
            frm.Controls.Add(lblDetails);
            frm.Controls.Add(btnPrint);
            frm.Controls.Add(btnClose);
            frm.FormClosed += (s, ev) => qrBitmap?.Dispose();
            frm.ShowDialog();
        }

        private void btnImportExcel_Click(object sender, EventArgs e)
        {
            using var ofd = new OpenFileDialog
            {
                Filter = "Excel Files|*.xlsx;*.xls",
                Title = "Select Excel file to import"
            };

            if (ofd.ShowDialog() != DialogResult.OK) return;

            List<Equipment> items;
            List<string> parseErrors;
            try
            {
                (items, parseErrors) = ExcelHelper.ImportEquipment(ofd.FileName, _equipmentService);
            }
            catch (Exception ex)
            {
                ToastHelper.Show(this,
                    $"Could not read file: {ex.Message}. Make sure ClosedXML NuGet package is installed.",
                    ToastType.Error);
                return;
            }

            if (items.Count == 0)
            {
                string detail = parseErrors.Count > 0
                    ? " Row-level issues: " + string.Join("; ", parseErrors.Take(10))
                    : " Make sure row 1 has header columns 'name' and 'serial_number', and data starts from row 2.";
                ToastHelper.Show(this, "No valid rows found in the Excel file." + detail, ToastType.Info);
                return;
            }

            var result = _equipmentService.AddRange(items);

            // Combine service-level error text with any Excel parse warnings
            var allErrors = new List<string>();
            if (!string.IsNullOrWhiteSpace(result.ErrorMessage))
                allErrors.Add(result.ErrorMessage);
            allErrors.AddRange(parseErrors);

            if (allErrors.Count == 0)
            {
                ToastHelper.Show(this, $"✅ {result.Data} of {items.Count} records imported successfully.", ToastType.Success);
            }
            else
            {
                ToastHelper.Show(this,
                    $"{result.Data} of {items.Count} records imported. Details: " +
                    string.Join("; ", allErrors.Take(20)),
                    ToastType.Warning);
            }

            RefreshAfterImport();
        }

        private void txtSearch_TextChanged(object sender, EventArgs e)
        {
            _currentPage = 1;
            LoadEquipment(txtSearch.Text.Trim());
        }

        private void cmbPageSize_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (int.TryParse(cmbPageSize.SelectedItem?.ToString(), out int size))
            {
                _pageSize = size;
                _currentPage = 1;
                LoadEquipment(txtSearch.Text.Trim());
            }
        }

        private void SetupPageSizeOptions()
        {
            string current = cmbPageSize.SelectedItem?.ToString() ?? "25";
            cmbPageSize.Items.Clear();
            cmbPageSize.Items.AddRange(new object[] { "25", "50", "100", "250", "500", "1000" });
            cmbPageSize.SelectedItem = cmbPageSize.Items.Contains(current) ? current : "25";
        }

        private void btnPrevPage_Click(object sender, EventArgs e)
        {
            if (_currentPage > 1)
            {
                _currentPage--;
                LoadEquipment(txtSearch.Text.Trim());
            }
        }


        private void btnNextPage_Click(object sender, EventArgs e)
        {
            if (_currentPage < _totalPages)
            {
                _currentPage++;
                LoadEquipment(txtSearch.Text.Trim());
            }
        }
    }
}