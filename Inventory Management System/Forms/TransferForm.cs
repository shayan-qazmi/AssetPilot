// Forms/TransferForm.cs
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
    public partial class TransferForm : Form
    {
        private readonly AllocationService _allocationService = new();
        private readonly EquipmentService _equipmentService = new();
        private readonly EmployeeService _employeeService = new();
        private readonly LocationService _locationService = new();
        private readonly AuditService _audit = new();
        private List<Allocation> _allAllocations = new(); // currently allocated equipment

        private int _currentPage = 1;
        private int _pageSize = 25;
        private int _totalPages = 1;
        private int _totalMatchingCount = 0;

        public TransferForm()
        {
            InitializeComponent();
            flowToolbar.SizeChanged += (s, e) => panelTop.Height = flowToolbar.Height;
            ApplyTheme();
            LoadAllocatedEquipment();

            // Same fix as AllocationForm — Form.Shown fires once, after WinForms'
            // automatic first-row selection, so clearing here reliably wins.
            this.Shown += (s, e) =>
            {
                dgvTransfer.ClearSelection();
                dgvTransfer.CurrentCell = null;
            };
        }

        private void ApplyTheme()
        {
            ThemeHelper.ApplyFormBackground(this);
            ThemeHelper.StyleToolbarPanel(panelTop);
            ThemeHelper.StyleButton(btnTransfer);
            ThemeHelper.StyleButton(btnRefresh, Color.FromArgb(33, 150, 243));
            ThemeHelper.StyleSecondaryButton(btnExport);
            ThemeHelper.StyleTextBox(txtSearch);
            ThemeHelper.StyleComboBox(cmbPageSize);
            ThemeHelper.StyleSecondaryButton(btnPrevPage);
            ThemeHelper.StyleSecondaryButton(btnNextPage);
            ThemeHelper.StyleDataGridView(dgvTransfer);
            panelBottom.BackColor = Color.FromArgb(249, 250, 251);
            lblRowCount.ForeColor = ThemeHelper.TextSecondary;
            lblRowCount.Font = ThemeHelper.FontSmall;
        }

        // ── Load all currently allocated equipment ──────────────────────────
        private void LoadAllocatedEquipment()
        {
            try
            {
                using var db = new InventoryDbContext();
                _allAllocations = db.Allocations
                    .Where(a => a.Status == "approved")
                    .Include(a => a.Equipment)
                    .Include(a => a.Employee)
                    .Include(a => a.Location)
                    .OrderBy(a => a.Equipment!.EquipmentCode)
                    .ToList();

                RefreshGrid();
            }
            catch (Exception ex)
            {
               ToastHelper.Show(this, "Error loading allocated equipment: " + ex.Message, ToastType.Error);

            }
        }

        private void RefreshGrid(string filter = "")
        {
            dgvTransfer.Rows.Clear();

            List<Allocation> matched;
            if (string.IsNullOrWhiteSpace(filter))
            {
                matched = _allAllocations;
            }
            else
            {
                // Scanned/typed codes need an exact substring match — fuzzy matching
                // lets "QR001" incorrectly match QR002, QR005, etc.
                var codeMatches = _allAllocations.Where(a =>
                    (!string.IsNullOrEmpty(a.Equipment?.QrCode) &&
                        a.Equipment.QrCode.Contains(filter, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrEmpty(a.Equipment?.EquipmentCode) &&
                        a.Equipment.EquipmentCode.Contains(filter, StringComparison.OrdinalIgnoreCase)))
                    .ToList();

                matched = codeMatches.Count > 0
                    ? codeMatches
                    : _allAllocations.Where(a =>
                        FuzzySearchHelper.IsFuzzyMatch(a.Equipment?.Name, filter) ||
                        FuzzySearchHelper.IsFuzzyMatch(a.Employee?.FullName, filter) ||
                        FuzzySearchHelper.IsFuzzyMatch(a.Location?.LocationName, filter) ||
                        FuzzySearchHelper.IsFuzzyMatch(a.AllocationDate.ToString("yyyy-MM-dd"), filter))
                        .OrderByDescending(a => Math.Max(
                            FuzzySearchHelper.FuzzyScore(a.Equipment?.Name, filter),
                            FuzzySearchHelper.FuzzyScore(a.Employee?.FullName, filter)))
                        .ToList();
            }

            _totalMatchingCount = matched.Count;
            _totalPages = Math.Max(1, (int)Math.Ceiling(_totalMatchingCount / (double)_pageSize));
            if (_currentPage > _totalPages) _currentPage = _totalPages;

            var pageItems = matched
                .Skip((_currentPage - 1) * _pageSize)
                .Take(_pageSize)
                .ToList();

            foreach (var item in pageItems)
            {
                string status = (item.ExpectedReturnDate.HasValue &&
                                  item.ExpectedReturnDate.Value < DateOnly.FromDateTime(DateTime.Today))
                                  ? "Overdue" : "approved";

                int rowIdx = dgvTransfer.Rows.Add(
                    item.Id,
                    string.IsNullOrWhiteSpace(item.Equipment?.QrCode) ? "—" : item.Equipment!.QrCode,
                    item.Equipment?.EquipmentCode ?? "—",
                    item.Equipment?.Name ?? "—",
                    item.Employee?.FullName ?? "—",
                    item.Location?.LocationName ?? "—",
                    item.AllocationDate.ToString("yyyy-MM-dd"),
                    status
                );

                var statusCell = dgvTransfer.Rows[rowIdx].Cells["Status"];
                if (status == "Overdue")
                {
                    statusCell.Style.BackColor = Color.FromArgb(255, 205, 210);
                    statusCell.Style.ForeColor = Color.FromArgb(183, 28, 28);
                }
                else
                {
                    statusCell.Style.BackColor = Color.FromArgb(200, 230, 201);
                    statusCell.Style.ForeColor = Color.FromArgb(27, 94, 32);
                }
                statusCell.Style.Font = ThemeHelper.FontBold;
            }

            lblRowCount.Text = $"Showing {pageItems.Count} of {_totalMatchingCount} allocated items";
            lblPageIndicator.Text = $"Page {_currentPage} of {Math.Max(1, _totalPages)}";
            btnPrevPage.Enabled = _currentPage > 1;
            btnNextPage.Enabled = _currentPage < _totalPages;
        }   

        // ── Toolbar "New Transfer" — pick from full active list ─────────────
        private void btnTransfer_Click(object sender, EventArgs e)
        {
            OpenTransferDialog(preselectedAllocationId: null);
        }

        // ── Double-click a row — jump straight to that item's transfer ──────
        // FIX: bounds-check both ends of the row index, and resolve the
        // AllocationId strictly from the row that was actually clicked
        // (e.RowIndex), never from SelectedRows[0] or any cached index.
        private void dgvTransfer_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= dgvTransfer.Rows.Count) return;

            var clickedRow = dgvTransfer.Rows[e.RowIndex];
            var idValue = clickedRow.Cells["AllocationId"].Value;
            if (idValue == null || !int.TryParse(idValue.ToString(), out int allocationId)) return;

            OpenTransferDialog(preselectedAllocationId: allocationId);
        }

        // ── Shared dialog + save logic for both entry points ─────────────────
        private void OpenTransferDialog(int? preselectedAllocationId)
        {
            try
            {
                using var db = new InventoryDbContext();
                var activeAllocations = db.Allocations
                    .Where(a => a.Status == "approved")
                    .Include(a => a.Equipment)
                    .Include(a => a.Employee)
                    .ToList();

                if (activeAllocations.Count == 0)
                {
                   ToastHelper.Show(this, "No allocated equipment available to transfer.", ToastType.Info);

                    return;
                }

                using var frm = new TransferDialog(
                    activeAllocations,
                    _employeeService.GetAll().Data ?? new(),
                    _locationService.GetAll().Data ?? new(),
                    preselectedAllocationId);

                if (frm.ShowDialog() != DialogResult.OK) return;

                var oldAlloc = db.Allocations.FirstOrDefault(a => a.Id == frm.OldAllocationId);
                if (oldAlloc == null) return;

                oldAlloc.Status = "returned";
                oldAlloc.ActualReturnDate = DateOnly.FromDateTime(DateTime.Today);

                var newAlloc = new Allocation
                {
                    EquipmentId = oldAlloc.EquipmentId,
                    EmployeeId = frm.NewEmployeeId,
                    LocationId = frm.NewLocationId,
                    Status = "approved",
                    AllocationDate = DateTime.UtcNow,
                    AllocatedByUserId = 1,
                    ApprovedByUserId = 1,
                    ApprovalDate = DateTime.UtcNow,
                    PreviousAllocationId = oldAlloc.Id
                };

                db.Allocations.Add(newAlloc);
                db.SaveChanges();

                _audit.Log(oldAlloc.EquipmentId, AuditService.ActionTransferred,
                    $"Employee {oldAlloc.EmployeeId}", $"Employee {frm.NewEmployeeId}");

                LoadAllocatedEquipment();
             ToastHelper.Show(this, "Equipment transferred successfully.", ToastType.Success);

            }
            catch (Exception ex)
            {
               ToastHelper.Show(this, "Transfer failed: " + ex.Message, ToastType.Error);

            }
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            txtSearch.Text = string.Empty;
            _currentPage = 1;
            LoadAllocatedEquipment();
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
                    FileName = $"AllocatedEquipment_{DateTime.Now:yyyyMMdd_HHmm}.csv"
                };
                if (sfd.ShowDialog() != DialogResult.OK) return;

                var sb = new StringBuilder();
                sb.AppendLine("QR Code,Equipment Code,Equipment Name,Currently With,Current Location,Allocated Since");
                foreach (DataGridViewRow row in dgvTransfer.Rows)
                {
                    string E(object? v) => $"\"{v?.ToString()?.Replace("\"", "\"\"") ?? ""}\"";
                    sb.AppendLine(string.Join(",",
                        E(row.Cells["QRCode"].Value), E(row.Cells["EquipmentCode"].Value),
                        E(row.Cells["EquipmentName"].Value), E(row.Cells["CurrentEmployee"].Value),
                        E(row.Cells["CurrentLocation"].Value), E(row.Cells["AllocatedSince"].Value)));
                }
                System.IO.File.WriteAllText(sfd.FileName, sb.ToString());
                ToastHelper.Show(this, "Exported successfully.", ToastType.Success);
            }
            catch (Exception ex)
            {
                 ToastHelper.Show(this, "Export failed: " + ex.Message, ToastType.Error);
            }
        }
    }

    // ── Transfer Dialog — now supports live fuzzy search + preselection ──
    public class TransferDialog : Form
    {
        public int OldAllocationId { get; private set; }
        public int NewEmployeeId { get; private set; }
        public int NewLocationId { get; private set; }

        private TextBox txtEquipmentSearch = null!;
        private ComboBox cmbAllocation = null!;
        private ComboBox cmbEmployee = null!;
        private ComboBox cmbLocation = null!;
        private Button btnSave = null!;
        private Button btnCancel = null!;

        private readonly List<Allocation> _allocations;
        private readonly List<Employee> _employees;
        private readonly List<Location> _locations;
        private readonly int? _preselectedAllocationId;
        private EntitySuggestHelper<Allocation>? _equipmentSuggest;

        public TransferDialog(List<Allocation> allocations, List<Employee> employees, List<Location> locations,
            int? preselectedAllocationId = null)
        {
            _allocations = allocations;
            _employees = employees;
            _locations = locations;
            _preselectedAllocationId = preselectedAllocationId;
            BuildUI();
        }

        private void BuildUI()
        {
            Text = "New Transfer";
            Size = new Size(420, 390);
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
                y += 40;
            }

            // NEW: fuzzy search box, same pattern as the Allocation dialog
            txtEquipmentSearch = new TextBox { PlaceholderText = "Search by equipment code, name, or serial…" };
            ThemeHelper.StyleTextBox(txtEquipmentSearch);
            AddRow("Find Equipment:", txtEquipmentSearch);

            var allocationItems = new List<dynamic>
{
    new { Id = 0, DisplayText = "-- Select equipment --" }
};
            allocationItems.AddRange(_allocations.Select(a => (dynamic)new
            {
                a.Id,
                DisplayText = $"{a.Equipment?.EquipmentCode} → {a.Employee?.FullName}"
            }));

            cmbAllocation = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
            cmbAllocation.DisplayMember = "DisplayText";
            cmbAllocation.ValueMember = "Id";
            cmbAllocation.DataSource = allocationItems;
            cmbAllocation.SelectedIndexChanged += CmbAllocation_SelectedIndexChanged;
            ThemeHelper.StyleComboBox(cmbAllocation);
            AddRow("Equipment to transfer:", cmbAllocation);

            if (_preselectedAllocationId.HasValue)
            {
                var preAlloc = _allocations.FirstOrDefault(a => a.Id == _preselectedAllocationId.Value);
                if (preAlloc != null)
                {
                    cmbAllocation.SelectedValue = _preselectedAllocationId.Value;
                    txtEquipmentSearch.Text = $"{preAlloc.Equipment?.EquipmentCode} — {preAlloc.Equipment?.Name}";
                }
                txtEquipmentSearch.Enabled = false;
                cmbAllocation.Enabled = false; // double-clicked a specific item — lock it in
            }
            else
            {
               

                _equipmentSuggest = new EntitySuggestHelper<Allocation>(
                    textBox: txtEquipmentSearch,
                    hostForm: this,
                    onSelected: alloc =>
                    {
                        _suppressSync = true;
                        int index = allocationItems.FindIndex(a => a.Id == alloc.Id);
                        if (index >= 0)
                        {
                            cmbAllocation.SelectedIndex = index;
                        }
                        txtEquipmentSearch.Text = $"{alloc.Equipment?.EquipmentCode} — {alloc.Equipment?.Name}";
                        txtEquipmentSearch.SelectionStart = txtEquipmentSearch.Text.Length;
                        _suppressSync = false;
                    },
                    scoreFn: (alloc, term) => Math.Max(
                        FuzzySearchHelper.FuzzyScoreCode(alloc.Equipment?.EquipmentCode, term),
                        Math.Max(
                            FuzzySearchHelper.FuzzyScoreCode(alloc.Equipment?.QrCode, term),
                            Math.Max(
                                FuzzySearchHelper.FuzzyScore(alloc.Equipment?.Name, term),
                                FuzzySearchHelper.FuzzyScoreCode(alloc.Equipment?.SerialNumber, term)))),
                    line1Selector: alloc => $"{alloc.Equipment?.EquipmentCode} — {alloc.Equipment?.Name}",
                    line2Selector: alloc => string.IsNullOrWhiteSpace(alloc.Equipment?.SerialNumber)
                        ? $"With: {alloc.Employee?.FullName ?? "—"}"
                        : $"S/N: {alloc.Equipment.SerialNumber} | With: {alloc.Employee?.FullName ?? "—"}");

                _equipmentSuggest.LoadItems(_allocations);
            }

            cmbEmployee = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
            cmbEmployee.DisplayMember = "FullName";
            cmbEmployee.ValueMember = "Id";
            cmbEmployee.DataSource = _employees.Where(e => e.IsActive).ToList();
            ThemeHelper.StyleComboBox(cmbEmployee);
            AddRow("Transfer to employee:", cmbEmployee);

            cmbLocation = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
            cmbLocation.DisplayMember = "LocationName";
            cmbLocation.ValueMember = "Id";
            cmbLocation.DataSource = _locations;
            ThemeHelper.StyleComboBox(cmbLocation);
            AddRow("New location:", cmbLocation);

            btnSave = new Button { Text = "Transfer", Location = new Point(20, y), Size = new Size(170, 36) };
            ThemeHelper.StyleButton(btnSave, Color.FromArgb(123, 31, 162));
            btnSave.Click += BtnSave_Click;
            Controls.Add(btnSave);

            btnCancel = new Button { Text = "Cancel", Location = new Point(202, y), Size = new Size(178, 36) };
            ThemeHelper.StyleSecondaryButton(btnCancel);
            btnCancel.Click += (s, e) => DialogResult = DialogResult.Cancel;
            Controls.Add(btnCancel);

            var lblNote = new Label
            {
                Text = "ℹ  After transferring, set a new expected return date\nfrom the Allocations screen.",
                Location = new Point(20, y + 46),
                Size = new Size(360, 36),
                Font = ThemeHelper.FontSmall,
                ForeColor = ThemeHelper.TextSecondary,
                BackColor = Color.Transparent
            };
            Controls.Add(lblNote);

            ClientSize = new Size(400, y + 100);
            AcceptButton = btnSave;
            CancelButton = btnCancel;
        }

        private bool _suppressSync = false;

        private void CmbAllocation_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (_suppressSync) return;
            if (cmbAllocation.SelectedValue == null) return;

            int selectedId = (int)cmbAllocation.SelectedValue;
            if (selectedId == 0) return; // placeholder — ignore

            var alloc = _allocations.FirstOrDefault(a => a.Id == selectedId);
            if (alloc == null) return;

            _suppressSync = true;
            txtEquipmentSearch.Text = $"{alloc.Equipment?.EquipmentCode} — {alloc.Equipment?.Name}";
            txtEquipmentSearch.SelectionStart = txtEquipmentSearch.Text.Length;
            _suppressSync = false;
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            if (cmbAllocation.SelectedValue == null || cmbEmployee.SelectedValue == null || cmbLocation.SelectedValue == null)
            {
                ToastHelper.Show(this, "Please fill all fields.", ToastType.Warning);
                 return;
            }
            OldAllocationId = (int)cmbAllocation.SelectedValue;
            NewEmployeeId = (int)cmbEmployee.SelectedValue;
            NewLocationId = (int)cmbLocation.SelectedValue;
            DialogResult = DialogResult.OK;
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _equipmentSuggest?.Dispose();
            base.OnFormClosed(e);
        }
    }
}