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

        private List<AllocationRow> _allSourceRows = new();
        private List<AllocationRow> _allRows = new();

        private ComboBox cmbEquipmentFilter = null!;
        private string _activeStatusFilter = "Available";
        private List<AllocationRow> _lastRenderedRows = new();

        private EntitySuggestHelper<Allocation>? _suggest;

        private int _currentPage = 1;
        private int _pageSize = 25;
        private int _totalPages = 1;
        private int _totalMatchingCount = 0;

        // ── Toolbar wrap/resize fix — mirrors EquipmentForm.cs's fix so the
        // toolbar row wraps to a second line properly instead of hiding
        // behind the grid when the window is resized narrow. ──────────────
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

            public Allocation? AllocationRef { get; set; }
            public int? TransactionId { get; set; }
        }

        public AllocationForm()
        {
            InitializeComponent();
            ApplyTheme();
            SetupEquipmentFilterDropdown();
            LoadAllocations();
            SetupSuggestions();

            dgvAllocation.SelectionChanged += (s, e) => UpdateActionButtonsForSelection();

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
                DropDownWidth = 220
            };

            cmbEquipmentFilter.Items.Add("Equipment: All");
            cmbEquipmentFilter.Items.Add("Equipment: Allocated");
            cmbEquipmentFilter.Items.Add("Equipment: Pending");
            cmbEquipmentFilter.SelectedIndex = 0;

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
                    "Equipment: Allocated" => "Allocated",
                    "Equipment: Pending" => "Pending",
                    _ => "Available"
                };
                _currentPage = 1;
                RefreshGrid(txtSearch.Text.Trim());
            };

            var flow = panelTop.Controls.OfType<FlowLayoutPanel>().First();
            flow.Controls.Add(cmbEquipmentFilter);
            flow.Controls.SetChildIndex(cmbEquipmentFilter, flow.Controls.IndexOf(btnRefresh) + 1);
        }

        // "Available" equipment rows — now checks Allocations/Transactions
        // directly instead of trusting Equipment.CurrentStatus, since that
        // field can go stale (e.g. an equipment with an approved Allocation
        // but CurrentStatus still showing "in_store"). This mirrors the same
        // fix applied to AddAllocationForm's eligibility filter below, so
        // both views agree on what's actually available.
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
                    onSelected: a => SelectAllocationRow(a.Id),
                    scoreFn: (a, term) => Math.Max(
                        FuzzySearchHelper.FuzzyScoreCode(a.Equipment?.EquipmentCode, term),
                        Math.Max(
                            FuzzySearchHelper.FuzzyScore(a.Equipment?.Name, term),
                            Math.Max(
                                FuzzySearchHelper.FuzzyScore(a.Employee?.FullName, term),
                                Math.Max(
                                    FuzzySearchHelper.FuzzyScore(a.Location?.LocationName, term),
                                    FuzzySearchHelper.FuzzyScore(a.Status, term))))),
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
                        EquipmentCode = t.Equipment?.EquipmentCode ?? "—",
                        EquipmentName = t.Equipment?.Name ?? "—",
                        SerialNumber = t.Equipment?.SerialNumber ?? "—",
                        Make = t.Equipment?.Make ?? "—",
                        Model = t.Equipment?.Model ?? "—",
                        Employee = string.IsNullOrWhiteSpace(t.IssuedTo) ? "—" : t.IssuedTo,
                        Location = string.IsNullOrWhiteSpace(t.Department) ? "—" : t.Department,
                        SortDate = t.TransactionDate,
                        DateAllocated = t.TransactionDate.ToString("yyyy-MM-dd"),
                        ExpectedReturn = t.ExpectedReturn?.ToString("yyyy-MM-dd") ?? "—",
                        Status = t.Status ?? "—",
                        AllocationRef = null,
                        TransactionId = t.TransactionId
                    })
                    .ToList();

                var allocationRows = _suggestSourceAllocations.Select(a => new AllocationRow
                {
                    Id = a.Id,
                    Source = "Allocation",
                    EquipmentCode = a.Equipment?.EquipmentCode ?? "—",
                    EquipmentName = a.Equipment?.Name ?? "—",
                    SerialNumber = a.Equipment?.SerialNumber ?? "—",
                    Make = a.Equipment?.Make ?? "—",
                    Model = a.Equipment?.Model ?? "—",
                    Employee = a.Employee?.FullName ?? "—",
                    Location = a.Location?.LocationName ?? "—",
                    SortDate = a.AllocationDate,
                    DateAllocated = a.AllocationDate.ToString("yyyy-MM-dd"),
                    ExpectedReturn = a.ExpectedReturnDate?.ToString("yyyy-MM-dd") ?? "—",
                    Status = a.Status,
                    AllocationRef = a,
                    TransactionId = null
                }).ToList();

                _allSourceRows = allocationRows
                    .Concat(checkInOutRows)
                    .OrderByDescending(r => r.SortDate)
                    .ToList();

                if (string.IsNullOrWhiteSpace(filter))
                {
                    _totalMatchingCount = _allSourceRows.Count;
                    _totalPages = Math.Max(1, (int)Math.Ceiling(_totalMatchingCount / (double)_pageSize));
                    if (_currentPage > _totalPages) _currentPage = _totalPages;

                    _allRows = _allSourceRows
                        .Skip((_currentPage - 1) * _pageSize)
                        .Take(_pageSize)
                        .ToList();
                }
                else
                {
                    _allRows = _allSourceRows;
                }

                _suggest?.LoadItems(_suggestSourceAllocations);
                RefreshGrid(filter);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading allocations: " + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RefreshGrid(string filter = "")
        {
            dgvAllocation.Rows.Clear();

            bool isAvailableView = _activeStatusFilter == "Available";
            dgvAllocation.Columns["AllocationID"].Visible = !isAvailableView;
            dgvAllocation.Columns["Status"].Visible = !isAvailableView;
            dgvAllocation.Columns["Source"].Visible = !isAvailableView;
            dgvAllocation.Columns["SerialNumber"].Visible = isAvailableView;
            dgvAllocation.Columns["Make"].Visible = isAvailableView;
            dgvAllocation.Columns["Model"].Visible = isAvailableView;

            List<AllocationRow> items;
            int totalMatching;

            if (_activeStatusFilter == "Available")
            {
                var available = GetAvailableEquipmentRows();
                items = string.IsNullOrWhiteSpace(filter)
                    ? available
                    : available.Where(r =>
                        FuzzySearchHelper.IsFuzzyMatch(r.EquipmentCode, filter) ||
                        FuzzySearchHelper.IsFuzzyMatch(r.EquipmentName, filter) ||
                        FuzzySearchHelper.IsFuzzyMatch(r.SerialNumber, filter))
                      .ToList();

                totalMatching = items.Count;
                _totalPages = 1;
                _currentPage = 1;
            }
            else
            {
                if (string.IsNullOrWhiteSpace(filter))
                {
                    items = _allRows;
                    totalMatching = _totalMatchingCount;
                    _totalPages = Math.Max(1, (int)Math.Ceiling(totalMatching / (double)_pageSize));
                }
                else
                {
                    var filtered = _allRows.Where(r =>
                        FuzzySearchHelper.IsFuzzyMatch(r.EquipmentCode, filter) ||
                        FuzzySearchHelper.IsFuzzyMatch(r.EquipmentName, filter) ||
                        FuzzySearchHelper.IsFuzzyMatch(r.Employee, filter) ||
                        FuzzySearchHelper.IsFuzzyMatch(r.Location, filter) ||
                        FuzzySearchHelper.IsFuzzyMatch(r.Status, filter) ||
                        FuzzySearchHelper.IsFuzzyMatch(r.Source, filter))
                    .OrderByDescending(r => Math.Max(
                        FuzzySearchHelper.FuzzyScore(r.EquipmentCode, filter),
                        Math.Max(
                            FuzzySearchHelper.FuzzyScore(r.EquipmentName, filter),
                            Math.Max(
                                FuzzySearchHelper.FuzzyScore(r.Employee, filter),
                                Math.Max(
                                    FuzzySearchHelper.FuzzyScore(r.Location, filter),
                                    Math.Max(
                                        FuzzySearchHelper.FuzzyScore(r.Status, filter),
                                        FuzzySearchHelper.FuzzyScore(r.Source, filter)))))))
                    .ToList();

                    totalMatching = filtered.Count;
                    _totalPages = Math.Max(1, (int)Math.Ceiling(totalMatching / (double)_pageSize));
                    if (_currentPage > _totalPages) _currentPage = _totalPages;

                    items = filtered
                        .Skip((_currentPage - 1) * _pageSize)
                        .Take(_pageSize)
                        .ToList();
                }

                if (_activeStatusFilter == "Allocated")
                    items = items.Where(r => string.Equals(r.Status, "approved", StringComparison.OrdinalIgnoreCase)).ToList();
                else if (_activeStatusFilter == "Pending")
                    items = items.Where(r => string.Equals(r.Status, "pending", StringComparison.OrdinalIgnoreCase)).ToList();

                totalMatching = items.Count;
            }

            _lastRenderedRows = items;

            foreach (var item in items)
            {
                int rowIdx = dgvAllocation.Rows.Add(
                    item.Id,
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
                    item.Source
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

            // Prevent the grid's automatic first-row selection from being picked up
            // as a real user selection — Allocate should only preselect equipment
            // when the user actually clicked a row.
            dgvAllocation.ClearSelection();
            dgvAllocation.CurrentCell = null;

            lblRowCount.Text = $"Showing {items.Count} of {totalMatching} records";
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

            btnApprove.Enabled = !isCheckInOutSelected;
            btnReturn.Enabled = !isCheckInOutSelected;
            btnDelete.Enabled = !isCheckInOutSelected;
        }

        // Pre-selects the equipment matching the currently-selected grid row
        // (if any) inside the New Allocation popup — e.g. selecting an
        // "Available" row and clicking Allocate now opens the dialog with
        // that equipment already chosen.
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
                    null, $"Allocated to Employee ID {frm.Result.EmployeeId}");
                LoadAllocations();
            }
            else
                MessageBox.Show(result.ErrorMessage, "Allocation Failed",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void btnApprove_Click(object sender, EventArgs e)
        {
            var alloc = GetSelectedAllocation();
            if (alloc == null) { MessageBox.Show("Select an allocation first.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
            if (alloc.Status != "pending") { MessageBox.Show("Only pending allocations can be approved.", "Invalid", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

            try
            {
                using var db = new InventoryDbContext();

                bool isCheckedOut = db.Transactions.Any(t =>
                    t.EquipmentId == alloc.EquipmentId &&
                    (t.Status == "Pending" || t.Status == "Overdue"));

                if (isCheckedOut)
                {
                    MessageBox.Show("This equipment is currently checked out via Check In/Out and cannot be approved until it is checked back in.",
                        "Conflict", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
                MessageBox.Show("Approve failed: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnReturn_Click(object sender, EventArgs e)
        {
            var alloc = GetSelectedAllocation();
            if (alloc == null) { MessageBox.Show("Select an allocation first.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
            if (alloc.Status == "returned") { MessageBox.Show("This allocation is already returned.", "Invalid", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

            if (MessageBox.Show($"Return '{alloc.Equipment?.Name}' from '{alloc.Employee?.FullName}'?",
                "Confirm Return", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

            var result = _allocationService.Deallocate(alloc.Id);
            if (result.Success)
            {
                _audit.Log(alloc.EquipmentId, AuditService.ActionAllocationReturned, "allocated", "in_store");
                LoadAllocations();
            }
            else
                MessageBox.Show(result.ErrorMessage, "Return Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            var alloc = GetSelectedAllocation();
            if (alloc == null) { MessageBox.Show("Select an allocation first.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
            if (alloc.Status == "approved") { MessageBox.Show("Cannot delete an approved allocation. Return it first.", "Not Allowed", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

            if (MessageBox.Show("Delete this allocation record?", "Confirm Delete",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;

            try
            {
                using var db = new InventoryDbContext();
                var a = db.Allocations.FirstOrDefault(x => x.Id == alloc.Id);
                if (a != null) { db.Allocations.Remove(a); db.SaveChanges(); }
                LoadAllocations();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Delete failed: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            LoadAllocations(txtSearch.Text.Trim());
        }

        private void cmbPageSize_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (int.TryParse(cmbPageSize.SelectedItem?.ToString(), out int size))
            {
                _pageSize = size;
                _currentPage = 1;
                if (string.IsNullOrWhiteSpace(txtSearch.Text))
                    LoadAllocations();
                else
                    RefreshGrid(txtSearch.Text.Trim());
            }
        }

        private void btnPrevPage_Click(object sender, EventArgs e)
        {
            if (_currentPage <= 1) return;
            _currentPage--;
            if (string.IsNullOrWhiteSpace(txtSearch.Text))
                LoadAllocations();
            else
                RefreshGrid(txtSearch.Text.Trim());
        }

        private void btnNextPage_Click(object sender, EventArgs e)
        {
            if (_currentPage >= _totalPages) return;
            _currentPage++;
            if (string.IsNullOrWhiteSpace(txtSearch.Text))
                LoadAllocations();
            else
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
                sb.AppendLine("ID,Equipment Code,Equipment Name,Serial Number,Make,Model,Employee,Location,Date Allocated,Expected Return,Status,Source");
                foreach (DataGridViewRow row in dgvAllocation.Rows)
                {
                    string E(object? v) => $"\"{v?.ToString()?.Replace("\"", "\"\"") ?? ""}\"";
                    sb.AppendLine(string.Join(",",
                        E(row.Cells["AllocationID"].Value), E(row.Cells["EquipmentCode"].Value),
                        E(row.Cells["EquipmentName"].Value), E(row.Cells["SerialNumber"].Value),
                        E(row.Cells["Make"].Value), E(row.Cells["Model"].Value),
                        E(row.Cells["Employee"].Value), E(row.Cells["Location"].Value),
                        E(row.Cells["DateAllocated"].Value), E(row.Cells["ExpectedReturn"].Value),
                        E(row.Cells["Status"].Value), E(row.Cells["Source"].Value)));
                }
                System.IO.File.WriteAllText(sfd.FileName, sb.ToString());
                MessageBox.Show("Exported successfully.", "Export", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Export failed: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _suggest?.Dispose();
            base.OnFormClosed(e);
        }
    }

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

        public AddAllocationForm(List<Equipment> equipment, List<Employee> employees, List<Location> locations,
            string? preselectEquipmentCode = null)
        {
            _preselectEquipmentCode = preselectEquipmentCode;

            using (var db = new InventoryDbContext())
            {
                // Equipment currently mid-Check-Out (Pending/Overdue Transaction)
                // must be excluded regardless of CurrentStatus.
                var checkedOutEquipmentIds = new HashSet<int>(
                    db.Transactions
                        .Where(t => t.Status == "Pending" || t.Status == "Overdue")
                        .Select(t => t.EquipmentId));

                // Fixed: previously this only excluded equipment with an
                // *approved* allocation when CurrentStatus == "allocated" —
                // if CurrentStatus was stale (e.g. still "in_store" despite
                // having an approved Allocation), the equipment slipped
                // through and later failed at save time. Now checks for any
                // pending OR approved Allocation directly, independent of
                // CurrentStatus.
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
            Size = new Size(420, 460);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            ThemeHelper.ApplyFormBackground(this);
            AutoScroll = true;

            int y = 20;
            void AddRow(string label, Control ctrl)
            {
                var lbl = new Label { Text = label, Location = new Point(20, y), AutoSize = true };
                ThemeHelper.StyleLabel(lbl, bold: true);
                Controls.Add(lbl);
                y += 22;
                ctrl.Location = new Point(20, y);
                ctrl.Width = 360;
                Controls.Add(ctrl);
                y += 38;
            }

            // ── Category filter ──────────────────────────────────────────
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
            AddRow("Filter by Category:", cmbCategoryFilter);

            // ── Equipment search-by-code/name/serial ────────────────────────
            txtEquipmentSearch = new TextBox { PlaceholderText = "Search by equipment code, name, or serial…" };
            ThemeHelper.StyleTextBox(txtEquipmentSearch);
            AddRow("Find Equipment:", txtEquipmentSearch);

            // ── Equipment — browsable dropdown, filtered by category ────────
            cmbEquipment = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
            cmbEquipment.DisplayMember = "DisplayText";
            cmbEquipment.ValueMember = "Id";
            ThemeHelper.StyleComboBox(cmbEquipment);
            AddRow("Equipment (available only):", cmbEquipment);
            RefreshEquipmentDropdown();
            cmbCategoryFilter.SelectedIndexChanged += (s, e) => RefreshEquipmentDropdown();

            _equipmentSuggest = new EntitySuggestHelper<Equipment>(
                textBox: txtEquipmentSearch,
                hostForm: this,
                onSelected: eq =>
                {
                    // Ensure the picked equipment is present in the currently
                    // filtered dropdown regardless of the active category filter.
                    if (cmbCategoryFilter.SelectedItem?.ToString() != "All Categories")
                        cmbCategoryFilter.SelectedItem = "All Categories"; // triggers RefreshEquipmentDropdown
                    cmbEquipment.SelectedValue = eq.Id;
                },
                scoreFn: (eq, term) => Math.Max(
                    FuzzySearchHelper.FuzzyScoreCode(eq.EquipmentCode, term),
                    Math.Max(
                        FuzzySearchHelper.FuzzyScore(eq.Name, term),
                        FuzzySearchHelper.FuzzyScoreCode(eq.SerialNumber, term))),
                line1Selector: eq => $"{eq.EquipmentCode} — {eq.Name}",
                line2Selector: eq => string.IsNullOrWhiteSpace(eq.SerialNumber)
                    ? (eq.Category ?? "—") : $"S/N: {eq.SerialNumber}");
            _equipmentSuggest.LoadItems(_equipment);

            // ── Employee search-by-code ────────────────────────────────────
            txtEmployeeSearch = new TextBox { PlaceholderText = "Search by employee code or name…" };
            ThemeHelper.StyleTextBox(txtEmployeeSearch);
            AddRow("Find Employee:", txtEmployeeSearch);

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

            cmbLocation = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
            cmbLocation.DisplayMember = "LocationName";
            cmbLocation.ValueMember = "Id";
            cmbLocation.DataSource = _locations;
            ThemeHelper.StyleComboBox(cmbLocation);
            AddRow("Location:", cmbLocation);

            chkExpected = new CheckBox
            {
                Text = "Set expected return date",
                Location = new Point(20, y),
                AutoSize = true,
                Font = ThemeHelper.FontDefault,
                ForeColor = ThemeHelper.TextPrimary
            };
            chkExpected.CheckedChanged += (s, e) => dtpExpected.Enabled = chkExpected.Checked;
            Controls.Add(chkExpected);
            y += 28;

            dtpExpected = new DateTimePicker
            {
                Location = new Point(20, y),
                Width = 360,
                Format = DateTimePickerFormat.Short,
                Enabled = false,
                MinDate = DateTime.Today.AddDays(1)
            };
            Controls.Add(dtpExpected);
            y += 44;

            btnSave = new Button { Text = "Allocate", Location = new Point(20, y), Size = new Size(170, 36) };
            ThemeHelper.StyleButton(btnSave);
            btnSave.Click += BtnSave_Click;
            Controls.Add(btnSave);

            btnCancel = new Button { Text = "Cancel", Location = new Point(202, y), Size = new Size(178, 36) };
            ThemeHelper.StyleSecondaryButton(btnCancel);
            btnCancel.Click += (s, e) => DialogResult = DialogResult.Cancel;
            Controls.Add(btnCancel);

            ClientSize = new Size(400, y + 70);
            // Intentionally no AcceptButton: with one set, pressing Enter to pick
            // a suggestion from EntitySuggestHelper's popup also fires
            // btnSave.Click for the same keystroke. Allocate must only run from
            // an explicit button click.
            CancelButton = btnCancel;
        }

        private void RefreshEquipmentDropdown()
        {
            string selectedCategory = cmbCategoryFilter?.SelectedItem?.ToString() ?? "All Categories";

            var filtered = selectedCategory == "All Categories"
                ? _equipment
                : _equipment.Where(e => string.Equals(e.Category, selectedCategory, StringComparison.OrdinalIgnoreCase)).ToList();

            cmbEquipment.DataSource = filtered.Select(e => new
            {
                e.Id,
                DisplayText = $"{e.EquipmentCode} — {e.Name}" +
                              (string.IsNullOrWhiteSpace(e.SerialNumber) ? "" : $" (S/N: {e.SerialNumber})")
            }).ToList();
            cmbEquipment.SelectedIndex = -1; // don't auto-pick the first equipment

            _equipmentSuggest?.LoadItems(filtered);

            // Apply row-selection preselect (once) — if the equipment the
            // user had selected in the background grid before clicking
            // Allocate is present in the currently eligible list, pick it.
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
            if (cmbEquipment.SelectedValue == null || _selectedEmployeeId == null || cmbLocation.SelectedValue == null)
            {
                MessageBox.Show("Please select an equipment and an employee, and choose a location.",
                    "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            Result = new Allocation
            {
                EquipmentId = (int)cmbEquipment.SelectedValue,
                EmployeeId = _selectedEmployeeId.Value,
                LocationId = (int)cmbLocation.SelectedValue,
                Status = "pending",
                AllocationDate = DateTime.UtcNow,
                AllocatedByUserId = 1,
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
}