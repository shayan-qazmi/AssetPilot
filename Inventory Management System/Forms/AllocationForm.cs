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

        private void FlowToolbar_Layout(object? sender, LayoutEventArgs e)
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

        private class AllocationRow
        {
            public int Id { get; set; }
            public string Source { get; set; } = "Allocation"; // "Allocation" | "Check In/Out" | "Available"
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

            public Allocation? AllocationRef { get; set; }
            public int? TransactionId { get; set; }
        }

        public AllocationForm()
        {
            InitializeComponent();
            ApplyTheme();
            SetupEquipmentFilterDropdown();
            SetupEditReturnButton();
            LoadAllocations();
            SetupSuggestions();

            dgvAllocation.SelectionChanged += (s, e) => UpdateActionButtonsForSelection();

            dgvAllocation.CellClick += (s, e) =>
            {
                if (e.RowIndex >= 0) _userClickedGridRow = true;
            };
            this.Shown += (s, e) =>
            {
                dgvAllocation.ClearSelection();
                dgvAllocation.CurrentCell = null;
            };
            flowToolbar.Layout += FlowToolbar_Layout;
        }

        private void SetupEquipmentFilterDropdown()
        {
            cmbEquipmentFilter = new ComboBox
            {
                Name = "cmbEquipmentFilter",
                DropDownStyle = ComboBoxStyle.DropDownList,
                Size = new Size(150, 32),
                Margin = new Padding(4, 4, 0, 0),
                Cursor = Cursors.Hand,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(94, 53, 177),
                ForeColor = Color.White,
                Font = ThemeHelper.FontBold,
                DrawMode = DrawMode.OwnerDrawFixed,
                DropDownWidth = 230
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
                using (var brush = new SolidBrush(isHover ? ThemeHelper.Header : Color.FromArgb(94, 53, 177)))
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
            flow.Controls.SetChildIndex(cmbEquipmentFilter, flow.Controls.IndexOf(btnRefresh) + 1);
        }

        // Equipment with no active Allocation and no active Check-Out —
        // queried fresh each call rather than cached, so it's always current.

        private void SetupEditReturnButton()
        {
            btnEditReturn = new Button
            {
                Text = "✏  Edit",
                Size = btnApprove.Size,           // ← match Approve's exact size instead of guessing
                Margin = new Padding(4, 4, 0, 0)
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

        private void ApplyTheme()
        {
            ThemeHelper.ApplyFormBackground(this);
            ThemeHelper.StyleToolbarPanel(panelTop);
            ThemeHelper.StyleButton(btnAdd);
            ThemeHelper.StyleButton(btnApprove, Color.FromArgb(25, 118, 210));
            ThemeHelper.StyleButton(btnReturn, Color.FromArgb(245, 124, 0));
            ThemeHelper.StyleButton(btnRefresh, Color.FromArgb(33, 150, 243));
            ThemeHelper.StyleSecondaryButton(btnSelectAll);
            ThemeHelper.StyleSecondaryButton(btnExport);
            ThemeHelper.StyleDangerButton(btnDelete);
            ThemeHelper.StyleTextBox(txtSearch);
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
                       item.Id, item.QrCode, item.EquipmentCode, item.EquipmentName,
                       item.SerialNumber, item.Make, item.Model, item.Employee, item.Location,
                       item.DateAllocated, item.ExpectedReturn, item.Status, item.Source, item.AllocatedBy);

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
                        QrCode = string.IsNullOrWhiteSpace(t.Equipment?.QrCode) ? "—" : t.Equipment!.QrCode,
                        EquipmentCode = t.Equipment?.EquipmentCode ?? "—",
                        EquipmentName = t.Equipment?.Name ?? "—",
                        SerialNumber = t.Equipment?.SerialNumber ?? "—",
                        Make = t.Equipment?.Make ?? "—",
                        Model = t.Equipment?.Model ?? "—",
                        Employee = string.IsNullOrWhiteSpace(t.IssuedTo) ? "—" : t.IssuedTo,
                        Location = string.IsNullOrWhiteSpace(t.Department) ? "—" : t.Department,
                        AllocatedBy = string.IsNullOrWhiteSpace(t.OperatorName) ? "—" : t.OperatorName,
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
                    QrCode = string.IsNullOrWhiteSpace(a.Equipment?.QrCode) ? "—" : a.Equipment!.QrCode,
                    EquipmentCode = a.Equipment?.EquipmentCode ?? "—",
                    EquipmentName = a.Equipment?.Name ?? "—",
                    SerialNumber = a.Equipment?.SerialNumber ?? "—",
                    Make = a.Equipment?.Make ?? "—",
                    Model = a.Equipment?.Model ?? "—",
                    Employee = a.Employee?.FullName ?? "—",
                    Location = a.Location?.LocationName ?? "—",
                    AllocatedBy = a.AllocatedByUser?.FullName ?? "—",
                    SortDate = a.AllocationDate,
                    DateAllocated = a.AllocationDate.ToString("yyyy-MM-dd"),
                    ExpectedReturn = a.ExpectedReturnDate?.ToString("yyyy-MM-dd") ?? "—",
                    Status = (a.Status == "approved" && a.ExpectedReturnDate.HasValue &&
                              a.ExpectedReturnDate.Value < DateOnly.FromDateTime(DateTime.Today))
                              ? "Overdue" : a.Status,
                    AllocationRef = a,
                    TransactionId = null
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
                       item.Employee,
                       item.Location,
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

        private AllocationRow? GetSelectedRow()
        {
            if (dgvAllocation.SelectedRows.Count == 0) return null;
            if (!int.TryParse(dgvAllocation.SelectedRows[0].Cells["AllocationID"].Value?.ToString(), out int id)) return null;
            return _lastRenderedRows.FirstOrDefault(r => r.Id == id);
        }

        private Allocation? GetSelectedAllocation()
        {
            return GetSelectedRow()?.AllocationRef;
        }

        private void UpdateActionButtonsForSelection()
        {
            var row = GetSelectedRow();
            bool isCheckInOutSelected = row != null && row.AllocationRef == null;
            bool nothingSelected = row == null;

            btnApprove.Enabled = !isCheckInOutSelected;
            btnReturn.Enabled = !isCheckInOutSelected;
            btnDelete.Enabled = !isCheckInOutSelected;

            // Editing the return date only makes sense for a real Allocation record
            // that hasn't already been returned.
            btnEditReturn.Enabled = !nothingSelected && !isCheckInOutSelected &&
                !string.Equals(row?.Status, "returned", StringComparison.OrdinalIgnoreCase);
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
            }
            else
                ToastHelper.Show(this, "Allocation Failed: " + result.ErrorMessage, ToastType.Error);
        }

        private void btnApprove_Click(object sender, EventArgs e)
        {
            var alloc = GetSelectedAllocation();
            if (alloc == null) { ToastHelper.Show(this, "Select an allocation first.", ToastType.Info); return; }
            if (alloc.Status != "pending") { ToastHelper.Show(this, "Only pending allocations can be approved.", ToastType.Warning); return; }

            try
            {
                using var db = new InventoryDbContext();

                bool isCheckedOut = db.Transactions.Any(t =>
                    t.EquipmentId == alloc.EquipmentId &&
                    (t.Status == "Pending" || t.Status == "Overdue"));

                if (isCheckedOut)
                {
                    ToastHelper.Show(this, "This equipment is currently checked out via Check In/Out and cannot be approved until it is checked back in.", ToastType.Warning);
                    return;
                }

                var a = db.Allocations
                    .Include(x => x.Equipment)
                    .FirstOrDefault(x => x.Id == alloc.Id);
                if (a == null) return;
                a.Status = "approved";
                a.ApprovalDate = DateTime.UtcNow;
                a.ApprovedByUserId = 1;
                if (a.Equipment != null)
                    a.Equipment.CurrentStatus = "allocated";
                db.SaveChanges();
                _audit.Log(alloc.EquipmentId, AuditService.ActionAllocationApproved, "pending", "approved");
                LoadAllocations();
            }
            catch (Exception ex)
            {
                ToastHelper.Show(this, "Approve failed: " + ex.Message, ToastType.Error);
            }
        }

        private void btnReturn_Click(object sender, EventArgs e)
        {
            var alloc = GetSelectedAllocation();
            if (alloc == null) { ToastHelper.Show(this, "Select an allocation first.", ToastType.Info); return; }
            if (alloc.Status == "returned") { ToastHelper.Show(this, "This allocation is already returned.", ToastType.Warning); return; }

            if (MessageBox.Show($"Return '{alloc.Equipment?.Name}' from '{alloc.Employee?.FullName}'?",
    "Confirm Return", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

            var result = _allocationService.Deallocate(alloc.Id);
            if (result.Success)
            {
                _audit.Log(alloc.EquipmentId, AuditService.ActionAllocationReturned, "allocated", "in_store");
                LoadAllocations();
            }
            else
                ToastHelper.Show(this, "Return Failed: " + result.ErrorMessage, ToastType.Error);
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            var alloc = GetSelectedAllocation();
            if (alloc == null) { ToastHelper.Show(this, "Select an allocation first.", ToastType.Info); return; }
            if (alloc.Status == "approved") { ToastHelper.Show(this, "Cannot delete an approved allocation. Return it first.", ToastType.Warning); return; }

            if (MessageBox.Show("Delete this allocation record?", "Confirm Delete",
     MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                return;

            try
            {
                using var db = new InventoryDbContext();
                var a = db.Allocations.FirstOrDefault(x => x.Id == alloc.Id);
                if (a != null) { db.Allocations.Remove(a); db.SaveChanges(); }
                LoadAllocations();
            }
            catch (Exception ex)
            {
                ToastHelper.Show(this, "Delete failed: " + ex.Message, ToastType.Error);
            }
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            txtSearch.Text = string.Empty;
            _currentPage = 1;
            LoadAllocations();
        }

        private void btnSelectAll_Click(object sender, EventArgs e)
            => dgvAllocation.SelectAll();

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
    // ── Add Allocation Dialog ──────────────────────────────────────────────
    public class AddAllocationForm : Form
    {
        public Allocation? Result { get; private set; }

        private readonly List<Equipment> _equipment;
        private readonly List<Employee> _employees;
        private readonly List<Location> _locations;
        private readonly string? _preselectEquipmentCode;
        private bool _preselectApplied = false;

        private int? _selectedEmployeeId;

        private ComboBox cmbEquipment = null!;
        private ComboBox cmbLocation = null!;
        private DateTimePicker dtpExpected = null!;
        private CheckBox chkExpected = null!;
        private Button btnSave = null!;
        private Button btnCancel = null!;

        private ComboBox cmbCategoryFilter = null!;
        private TextBox txtEquipmentSearch = null!;
        private TextBox txtEmployeeSearch = null!;
        private EntitySuggestHelper<Equipment>? _equipmentSuggest;
        private EntitySuggestHelper<Employee>? _employeeSuggest;

        private Panel _body = null!;

        private bool _suppressEquipmentSync = false;

        // Wrapper so the location dropdown can offer a genuine "no location"
        // option — binding directly to List<Location> always has something
        // selected, which made "location optional" impossible before.
        private class LocationOption
        {
            public int? Id { get; set; }
            public string LocationName { get; set; } = "";
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
            ThemeHelper.ApplyFormBackground(this);
            ClientSize = new Size(480, 600);

            // ── Header ──────────────────────────────────────────────
            var pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 64,
                BackColor = Color.FromArgb(94, 53, 177)
            };
            pnlHeader.Controls.Add(new Label
            {
                Text = "New Allocation",
                Font = new Font("Segoe UI", 14f, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(24, 10)
            });
            pnlHeader.Controls.Add(new Label
            {
                Text = "Assign equipment to an employee, a location, or both",
                Font = new Font("Segoe UI", 9f, FontStyle.Regular),
                ForeColor = Color.FromArgb(230, 224, 245),
                AutoSize = true,
                Location = new Point(24, 39)
            });

            // ── Footer (always visible, independent of scroll) ───────
            var pnlFooter = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 74,
                BackColor = Color.FromArgb(249, 250, 251)
            };
            pnlFooter.Controls.Add(new Panel { Dock = DockStyle.Top, Height = 1, BackColor = Color.FromArgb(224, 224, 224) });

            btnCancel = new Button { Text = "Cancel", Size = new Size(150, 40) };
            ThemeHelper.StyleSecondaryButton(btnCancel);
            btnCancel.Click += (s, e) => DialogResult = DialogResult.Cancel;

            btnSave = new Button { Text = "✓  Allocate", Size = new Size(180, 40) };
            ThemeHelper.StyleButton(btnSave);
            btnSave.Click += BtnSave_Click;

            pnlFooter.Controls.Add(btnCancel);
            pnlFooter.Controls.Add(btnSave);
            btnSave.Location = new Point(ClientSize.Width - btnSave.Width - 24, 17);
            btnCancel.Location = new Point(btnSave.Left - btnCancel.Width - 12, 17);

            // ── Scrollable body ────────────────────────────────────
            _body = new Panel { Dock = DockStyle.Fill, AutoScroll = true };

            Controls.Add(pnlHeader);
            Controls.Add(pnlFooter);
            Controls.Add(_body);

            int y = 20;

            Label SectionHeader(string text)
            {
                _body.Controls.Add(new Panel
                {
                    BackColor = Color.FromArgb(94, 53, 177),
                    Size = new Size(4, 20),
                    Location = new Point(20, y + 1)
                });
                var lbl = new Label
                {
                    Text = text,
                    Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                    ForeColor = ThemeHelper.TextPrimary,
                    AutoSize = true,
                    Location = new Point(32, y)
                };
                _body.Controls.Add(lbl);
                y += 32;
                return lbl;
            }

            void Hint(string text)
            {
                var lbl = new Label
                {
                    Text = text,
                    Font = new Font("Segoe UI", 8f, FontStyle.Italic),
                    ForeColor = ThemeHelper.TextSecondary,
                    AutoSize = false,
                    MaximumSize = new Size(420, 0),
                    Size = new Size(420, 32),
                    Location = new Point(20, y)
                };
                _body.Controls.Add(lbl);
                int textH = TextRenderer.MeasureText(text, lbl.Font, new Size(420, int.MaxValue), TextFormatFlags.WordBreak).Height;
                lbl.Height = textH;
                y += textH + 10;
            }

            Control Field(string label, Control ctrl, string? hint = null, bool required = false)
            {
                var lbl = new Label { Text = required ? $"{label} *" : label, Location = new Point(20, y), AutoSize = true };
                ThemeHelper.StyleLabel(lbl, bold: true);
                _body.Controls.Add(lbl);
                y += 22;

                ctrl.Location = new Point(20, y);
                ctrl.Width = 400;
                _body.Controls.Add(ctrl);
                y += ctrl.Height + 6;

                if (hint != null) Hint(hint);
                else y += 8;

                return ctrl;
            }

            // ── Equipment section ──────────────────────────────────
            SectionHeader("Equipment");

            cmbCategoryFilter = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
            var categoryNames = new CategoryService().GetAll().Data?.Select(c => c.Name)
                ?? Enumerable.Empty<string>();
            var equipmentCategories = _equipment
                .Where(e => !string.IsNullOrWhiteSpace(e.Category))
                .Select(e => e.Category!)
                .Distinct();
            var allCategoryOptions = new List<string> { "All Categories" };
            allCategoryOptions.AddRange(
                categoryNames.Union(equipmentCategories, StringComparer.OrdinalIgnoreCase)
                             .OrderBy(c => c, StringComparer.OrdinalIgnoreCase));
            cmbCategoryFilter.DataSource = allCategoryOptions;
            ThemeHelper.StyleComboBox(cmbCategoryFilter);
            Field("Filter by Category", cmbCategoryFilter, "Narrows the equipment list below — optional");

            txtEquipmentSearch = new TextBox { PlaceholderText = "Search by equipment code, name, or serial…" };
            ThemeHelper.StyleTextBox(txtEquipmentSearch);
            Field("Find Equipment", txtEquipmentSearch, "Start typing to search, then pick from the suggestions", required: true);

            cmbEquipment = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
            cmbEquipment.DisplayMember = "DisplayText";
            cmbEquipment.ValueMember = "Id";
            ThemeHelper.StyleComboBox(cmbEquipment);
            Field("Select Equipment", cmbEquipment, "Only unallocated, in-store equipment is listed", required: true);

            cmbEquipment.SelectedIndexChanged += CmbEquipment_SelectedIndexChanged;

            RefreshEquipmentDropdown();
            cmbCategoryFilter.SelectedIndexChanged += (s, e) => RefreshEquipmentDropdown();

            _equipmentSuggest = new EntitySuggestHelper<Equipment>(
                textBox: txtEquipmentSearch,
                hostForm: this,
                onSelected: eq =>
                {
                    _suppressEquipmentSync = true;
                    if (cmbCategoryFilter.SelectedItem?.ToString() != "All Categories")
                        cmbCategoryFilter.SelectedItem = "All Categories";
                    cmbEquipment.SelectedValue = eq.Id;

                    txtEquipmentSearch.Text = $"{eq.EquipmentCode} — {eq.Name}";
                    txtEquipmentSearch.SelectionStart = txtEquipmentSearch.Text.Length;
                    _suppressEquipmentSync = false;
                },
                scoreFn: (eq, term) => Math.Max(
                    FuzzySearchHelper.FuzzyScoreCode(eq.EquipmentCode, term),
                    Math.Max(
                        FuzzySearchHelper.FuzzyScoreCode(eq.QrCode, term),
                        Math.Max(
                            FuzzySearchHelper.FuzzyScore(eq.Name, term),
                            FuzzySearchHelper.FuzzyScoreCode(eq.SerialNumber, term)))),
                line1Selector: eq => $"{eq.EquipmentCode} — {eq.Name}",
                line2Selector: eq => string.IsNullOrWhiteSpace(eq.SerialNumber)
                    ? (eq.Category ?? "—") : $"S/N: {eq.SerialNumber}");
            _equipmentSuggest.LoadItems(_equipment);

            // ── Assign To section ───────────────────────────────────
            y += 6;
            SectionHeader("Assign To");
            Hint("Choose an employee, a location, or both — at least one is required.");

            txtEmployeeSearch = new TextBox { PlaceholderText = "Search by employee code or name…" };
            ThemeHelper.StyleTextBox(txtEmployeeSearch);
            Field("Find Employee", txtEmployeeSearch, "Optional — leave blank if this equipment isn't tied to a person");

            _employeeSuggest = new EntitySuggestHelper<Employee>(
                textBox: txtEmployeeSearch,
                hostForm: this,
                onSelected: emp =>
                {
                    _selectedEmployeeId = emp.Id;
                    txtEmployeeSearch.Text = $"{emp.EmployeeCode} — {emp.FullName}";
                    txtEmployeeSearch.SelectionStart = txtEmployeeSearch.Text.Length;
                },
                scoreFn: (emp, term) => Math.Max(
                    FuzzySearchHelper.FuzzyScoreCode(emp.EmployeeCode, term),
                    FuzzySearchHelper.FuzzyScore(emp.FullName, term)),
                line1Selector: emp => $"{emp.EmployeeCode} — {emp.FullName}",
                line2Selector: emp => emp.Department ?? "—");
            _employeeSuggest.LoadItems(_employees);

            // Clearing the search box clears the selected employee too, so
            // "optional" actually behaves as optional once text is removed.
            txtEmployeeSearch.TextChanged += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtEmployeeSearch.Text))
                    _selectedEmployeeId = null;
            };
            cmbLocation = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
            cmbLocation.DisplayMember = "LocationName";
            cmbLocation.ValueMember = "Id";
            var locationOptions = new List<LocationOption> { new LocationOption { Id = null, LocationName = "— No location —" } };
            locationOptions.AddRange(_locations.Select(l => new LocationOption { Id = l.Id, LocationName = l.LocationName }));
            cmbLocation.DataSource = locationOptions;
            ThemeHelper.StyleComboBox(cmbLocation);
            Field("Location", cmbLocation, "Optional — where this equipment will be kept");
            if (cmbLocation.Items.Count > 0) cmbLocation.SelectedIndex = 0;   // ← moved here, after it's parented

            // ── Return Details section ──────────────────────────────
            y += 6;
            SectionHeader("Return Details");

            chkExpected = new CheckBox
            {
                Text = "Set expected return date",
                Location = new Point(20, y),
                AutoSize = true,
                Font = ThemeHelper.FontDefault,
                ForeColor = ThemeHelper.TextPrimary
            };
            chkExpected.CheckedChanged += (s, e) => dtpExpected.Enabled = chkExpected.Checked;
            _body.Controls.Add(chkExpected);
            y += 30;

            dtpExpected = new DateTimePicker
            {
                Location = new Point(20, y),
                Width = 400,
                Format = DateTimePickerFormat.Short,
                Enabled = false,
                MinDate = DateTime.Today.AddDays(1)
            };
            _body.Controls.Add(dtpExpected);
            y += dtpExpected.Height + 6;
            Hint("Optional — leave unchecked if there's no fixed return date");

            y += 10;
            _body.AutoScrollMinSize = new Size(0, y);
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

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            if (cmbEquipment.SelectedValue == null)
            {
                ToastHelper.Show(this, "Please select an equipment item.", ToastType.Warning);
                return;
            }

            int? locationId = cmbLocation.SelectedValue is int lid ? lid : (int?)null;

            // The actual rule: equipment is required, and AT LEAST ONE of
            // employee / location must be set — not both, not neither.
            if (_selectedEmployeeId == null && locationId == null)
            {
                ToastHelper.Show(this,
                    "Please choose an employee, a location, or both — at least one is required.",
                    ToastType.Warning);
                return;
            }

            Result = new Allocation
            {
                EquipmentId = (int)cmbEquipment.SelectedValue,
                EmployeeId = _selectedEmployeeId,
                LocationId = locationId,
                Status = "pending",
                AllocationDate = DateTime.UtcNow,
                AllocatedByUserId = AuthService.CurrentUserId,
                ExpectedReturnDate = chkExpected.Checked
                    ? DateOnly.FromDateTime(dtpExpected.Value) : null
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

    