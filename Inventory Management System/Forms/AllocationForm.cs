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
        private readonly AllocationService  _allocationService  = new();
        private readonly EquipmentService   _equipmentService   = new();
        private readonly EmployeeService    _employeeService    = new();
        private readonly LocationService    _locationService    = new();
        private readonly AuditService       _audit              = new();

        // Full (unpaged) list of formal Allocation entities — used by the
        // live-suggestion dropdown and by dedup logic. Unchanged in shape
        // from before this change.
        private List<Allocation>            _suggestSourceAllocations = new();

        // Unified (Allocation + Check In/Out) rows, for display only.
        // _allSourceRows = full unfiltered/unpaged unified list.
        // _allRows       = current page (or full filtered set when a search filter is active).
        private List<AllocationRow>         _allSourceRows = new();
        private List<AllocationRow>         _allRows       = new();

        private EntitySuggestHelper<Allocation>? _suggest;

        private int _currentPage = 1;
        private int _pageSize = 25;
        private int _totalPages = 1;
        private int _totalMatchingCount = 0;

        // ── Display-only view-model row. Never persisted. Combines a formal
        // Allocation record OR a Check In/Out-originated Transaction so both
        // can be shown side-by-side in the same grid. ─────────────────────
        private class AllocationRow
        {
            // For Allocation-sourced rows this is the real Allocation.Id.
            // For Check In/Out-sourced rows this is the negative of the
            // Transaction.TransactionId (never collides with a real
            // Allocation Id, which is always positive).
            public int    Id             { get; set; }
            public string Source         { get; set; } = "Allocation"; // "Allocation" | "Check In/Out"
            public string EquipmentCode  { get; set; } = "—";
            public string EquipmentName  { get; set; } = "—";
            public string Employee       { get; set; } = "—";
            public string Location       { get; set; } = "—";
            public DateTime SortDate     { get; set; }
            public string DateAllocated  { get; set; } = "—";
            public string ExpectedReturn { get; set; } = "—";
            public string Status         { get; set; } = "—";

            // Non-null only for Allocation-sourced rows. This is what the
            // Approve/Return/Delete buttons operate on.
            public Allocation? AllocationRef { get; set; }

            // Non-null only for Check In/Out-sourced rows (informational).
            public int? TransactionId { get; set; }
        }

        public AllocationForm()
        {
            InitializeComponent();
            ApplyTheme();
            LoadAllocations();
            SetupSuggestions();

            dgvAllocation.SelectionChanged += (s, e) => UpdateActionButtonsForSelection();
        }

        private void ApplyTheme()
        {
            ThemeHelper.ApplyFormBackground(this);
            ThemeHelper.StyleToolbarPanel(panelTop);
            ThemeHelper.StyleButton(btnAdd);
            ThemeHelper.StyleButton(btnApprove, Color.FromArgb(25, 118, 210));
            ThemeHelper.StyleButton(btnReturn,  Color.FromArgb(245, 124, 0));
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
            lblRowCount.Font      = ThemeHelper.FontSmall;
        }

        // ── Live fuzzy-suggestion dropdown (same UX as QR Scanner / Check-In-Out) ──
        // NOTE: intentionally scoped to formal Allocation records only. Extending
        // this strongly-typed helper to a mixed Allocation/Transaction type would
        // require a larger refactor of EntitySuggestHelper<T> for comparatively
        // little benefit, since the primary search box below already covers both
        // sources. The grid's own search/filter (txtSearch_TextChanged) DOES cover
        // both sources — see RefreshGrid().
        private void SetupSuggestions()
        {
            try
            {
                _suggest = new EntitySuggestHelper<Allocation>(
                    textBox: txtSearch,
                    hostForm: this,
                    onSelected: a => SelectAllocationRow(a.Id),
                    scoreFn: (a, term) => Math.Max(
                        FuzzySearchHelper.FuzzyScore(a.Equipment?.EquipmentCode, term),
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

        // ── Load with full includes so navigation props are populated ─────
        private void LoadAllocations(string filter = "")
        {
            try
            {
                using var db = new InventoryDbContext();

                // 1) Formal Allocation records (unchanged query/behavior).
                var allocQuery = db.Allocations
                    .Include(a => a.Equipment)
                    .Include(a => a.Employee)
                    .Include(a => a.Location)
                    .OrderByDescending(a => a.AllocationDate);

                _suggestSourceAllocations = allocQuery.ToList();

                // Equipment IDs that already have a formal *approved* Allocation —
                // used to avoid double-counting an item that was both formally
                // allocated AND independently checked out via Check In/Out.
                var approvedEquipmentIds = new HashSet<int>(
                    _suggestSourceAllocations
                        .Where(a => a.Status == "approved")
                        .Select(a => a.EquipmentId));

                // 2) Check In/Out-sourced "allocations": currently checked-out
                // items (Transactions table) whose equipment is "allocated" and
                // which don't already have a formal approved Allocation.
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
                        Id             = -t.TransactionId,
                        Source         = "Check In/Out",
                        EquipmentCode  = t.Equipment?.EquipmentCode ?? "—",
                        EquipmentName  = t.Equipment?.Name ?? "—",
                        Employee       = string.IsNullOrWhiteSpace(t.IssuedTo) ? "—" : t.IssuedTo,
                        // Transaction has no Location FK — Department is the
                        // closest available field.
                        Location       = string.IsNullOrWhiteSpace(t.Department) ? "—" : t.Department,
                        SortDate       = t.TransactionDate,
                        DateAllocated  = t.TransactionDate.ToString("yyyy-MM-dd"),
                        ExpectedReturn = t.ExpectedReturn?.ToString("yyyy-MM-dd") ?? "—",
                        Status         = t.Status ?? "—",
                        AllocationRef  = null,
                        TransactionId  = t.TransactionId
                    })
                    .ToList();

                var allocationRows = _suggestSourceAllocations.Select(a => new AllocationRow
                {
                    Id             = a.Id,
                    Source         = "Allocation",
                    EquipmentCode  = a.Equipment?.EquipmentCode ?? "—",
                    EquipmentName  = a.Equipment?.Name ?? "—",
                    Employee       = a.Employee?.FullName ?? "—",
                    Location       = a.Location?.LocationName ?? "—",
                    SortDate       = a.AllocationDate,
                    DateAllocated  = a.AllocationDate.ToString("yyyy-MM-dd"),
                    ExpectedReturn = a.ExpectedReturnDate?.ToString("yyyy-MM-dd") ?? "—",
                    Status         = a.Status,
                    AllocationRef  = a,
                    TransactionId  = null
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

            List<AllocationRow> items;
            int totalMatching;

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
                    FuzzySearchHelper.IsFuzzyMatch(r.Employee,      filter) ||
                    FuzzySearchHelper.IsFuzzyMatch(r.Location,      filter) ||
                    FuzzySearchHelper.IsFuzzyMatch(r.Status,        filter) ||
                    FuzzySearchHelper.IsFuzzyMatch(r.Source,        filter))
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

            foreach (var item in items)
            {
                int rowIdx = dgvAllocation.Rows.Add(
                    item.Id,
                    item.EquipmentCode,
                    item.EquipmentName,
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
                }
                statusCell.Style.Font = ThemeHelper.FontBold;

                // Source badge — subtle color distinction, consistent with the
                // existing status color-coding convention on this grid.
                var sourceCell = dgvAllocation.Rows[rowIdx].Cells["Source"];
                if (item.Source == "Check In/Out")
                {
                    sourceCell.Style.ForeColor = Color.FromArgb(25, 118, 210);
                    sourceCell.Style.Font = ThemeHelper.FontBold;
                }
                else
                {
                    sourceCell.Style.ForeColor = Color.FromArgb(107, 114, 128);
                    sourceCell.Style.Font = ThemeHelper.FontDefault;
                }
            }

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
            return _allRows.FirstOrDefault(r => r.Id == id);
        }

        // Existing action-button logic continues to operate ONLY on real
        // Allocation records — returns null for Check In/Out-sourced rows,
        // exactly like it did before this row could even be selected.
        private Allocation? GetSelectedAllocation()
        {
            return GetSelectedRow()?.AllocationRef;
        }

        // Disables Approve/Return/Delete when a Check In/Out-sourced row is
        // selected, since those actions must only ever apply to formal
        // Allocation records. Check In/Out items must be managed from the
        // Check In/Out screen itself.
        private void UpdateActionButtonsForSelection()
        {
            var row = GetSelectedRow();
            bool isCheckInOutSelected = row != null && row.AllocationRef == null;

            btnApprove.Enabled = !isCheckInOutSelected;
            btnReturn.Enabled  = !isCheckInOutSelected;
            btnDelete.Enabled  = !isCheckInOutSelected;
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            using var frm = new AddAllocationForm(
                _equipmentService.GetAll().Data ?? new(),
                _employeeService.GetAll().Data  ?? new(),
                _locationService.GetAll().Data  ?? new());

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
                var a = db.Allocations
                    .Include(x => x.Equipment)
                    .FirstOrDefault(x => x.Id == alloc.Id);
                if (a == null) return;
                a.Status           = "approved";
                a.ApprovalDate     = DateTime.UtcNow;
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
                    Filter   = "CSV files|*.csv",
                    FileName = $"Allocations_{DateTime.Now:yyyyMMdd_HHmm}.csv"
                };
                if (sfd.ShowDialog() != DialogResult.OK) return;

                var sb = new StringBuilder();
                sb.AppendLine("ID,Equipment Code,Equipment Name,Employee,Location,Date Allocated,Expected Return,Status,Source");
                foreach (DataGridViewRow row in dgvAllocation.Rows)
                {
                    string E(object? v) => $"\"{v?.ToString()?.Replace("\"", "\"\"") ?? ""}\"";
                    sb.AppendLine(string.Join(",",
                        E(row.Cells["AllocationID"].Value),  E(row.Cells["EquipmentCode"].Value),
                        E(row.Cells["EquipmentName"].Value), E(row.Cells["Employee"].Value),
                        E(row.Cells["Location"].Value),      E(row.Cells["DateAllocated"].Value),
                        E(row.Cells["ExpectedReturn"].Value), E(row.Cells["Status"].Value),
                        E(row.Cells["Source"].Value)));
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

    // ── Add Allocation Dialog — unchanged ─────────────────────────────────
    public class AddAllocationForm : Form
    {
        public Allocation? Result { get; private set; }

        private readonly List<Equipment> _equipment;
        private readonly List<Employee>  _employees;
        private readonly List<Location>  _locations;

        private ComboBox       cmbEquipment = null!;
        private ComboBox       cmbEmployee  = null!;
        private ComboBox       cmbLocation  = null!;
        private DateTimePicker dtpExpected  = null!;
        private CheckBox       chkExpected  = null!;
        private Button         btnSave      = null!;
        private Button         btnCancel    = null!;

        public AddAllocationForm(List<Equipment> equipment, List<Employee> employees, List<Location> locations)
        {
            using (var db = new InventoryDbContext())
            {
                _equipment = equipment.Where(e => e.CurrentStatus == "in_store" ||
                                                 (e.CurrentStatus == "allocated" &&
                                                  !db.Allocations.Any(a => a.EquipmentId == e.Id && a.Status == "approved"))).ToList();
            }
            _employees = employees.Where(e => e.IsActive).ToList();
            _locations = locations;
            BuildUI();
        }

        private void BuildUI()
        {
            Text            = "New Allocation";
            Size            = new Size(420, 380);
            StartPosition   = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox     = false;
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
                ctrl.Width    = 360;
                Controls.Add(ctrl);
                y += 38;
            }

            cmbEquipment = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
            cmbEquipment.DisplayMember = "Name";
            cmbEquipment.ValueMember   = "Id";
            cmbEquipment.DataSource    = _equipment;
            ThemeHelper.StyleComboBox(cmbEquipment);
            AddRow("Equipment (available only):", cmbEquipment);

            cmbEmployee = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
            cmbEmployee.DisplayMember = "FullName";
            cmbEmployee.ValueMember   = "Id";
            cmbEmployee.DataSource    = _employees;
            ThemeHelper.StyleComboBox(cmbEmployee);
            AddRow("Employee:", cmbEmployee);

            cmbLocation = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
            cmbLocation.DisplayMember = "LocationName";
            cmbLocation.ValueMember   = "Id";
            cmbLocation.DataSource    = _locations;
            ThemeHelper.StyleComboBox(cmbLocation);
            AddRow("Location:", cmbLocation);

            chkExpected = new CheckBox
            {
                Text      = "Set expected return date",
                Location  = new Point(20, y),
                AutoSize  = true,
                Font      = ThemeHelper.FontDefault,
                ForeColor = ThemeHelper.TextPrimary
            };
            chkExpected.CheckedChanged += (s, e) => dtpExpected.Enabled = chkExpected.Checked;
            Controls.Add(chkExpected);
            y += 28;

            dtpExpected = new DateTimePicker
            {
                Location = new Point(20, y), Width = 360,
                Format   = DateTimePickerFormat.Short,
                Enabled  = false,
                MinDate  = DateTime.Today.AddDays(1)
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

            ClientSize   = new Size(400, y + 70);
            AcceptButton = btnSave;
            CancelButton = btnCancel;
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            if (cmbEquipment.SelectedValue == null || cmbEmployee.SelectedValue == null || cmbLocation.SelectedValue == null)
            {
                MessageBox.Show("Please select equipment, employee, and location.", "Validation",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            Result = new Allocation
            {
                EquipmentId        = (int)cmbEquipment.SelectedValue,
                EmployeeId         = (int)cmbEmployee.SelectedValue,
                LocationId         = (int)cmbLocation.SelectedValue,
                Status             = "pending",
                AllocationDate     = DateTime.UtcNow,
                AllocatedByUserId  = 1,
                ExpectedReturnDate = chkExpected.Checked
                    ? DateOnly.FromDateTime(dtpExpected.Value) : null
            };
            DialogResult = DialogResult.OK;
        }
    }
}