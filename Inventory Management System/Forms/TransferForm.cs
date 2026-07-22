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
        private List<Allocation> _allTransfers = new();

        private int _currentPage = 1;
        private int _pageSize = 25;
        private int _totalPages = 1;
        private int _totalMatchingCount = 0;

        public TransferForm()
        {
            InitializeComponent();
            ApplyTheme();
            LoadTransfers();
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

        // ── Load with full includes ───────────────────────────────────────
        private void LoadTransfers()
        {
            try
            {
                using var db = new InventoryDbContext();
                _allTransfers = db.Allocations
                    .Where(a => a.PreviousAllocationId != null)
                    .Include(a => a.Equipment)
                    .Include(a => a.Employee)
                    .Include(a => a.Location)
                    .Include(a => a.PreviousAllocation)
                        .ThenInclude(p => p!.Employee)
                    .Include(a => a.PreviousAllocation)
                        .ThenInclude(p => p!.Location)
                    .OrderByDescending(a => a.AllocationDate)
                    .ToList();

                RefreshGrid();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading transfers: " + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RefreshGrid(string filter = "")
        {
            dgvTransfer.Rows.Clear();

            var matched = string.IsNullOrWhiteSpace(filter)
                ? _allTransfers
                : _allTransfers.Where(a =>
                    FuzzySearchHelper.IsFuzzyMatch(a.Equipment?.EquipmentCode, filter) ||
                    FuzzySearchHelper.IsFuzzyMatch(a.Equipment?.QrCode, filter) ||
                    FuzzySearchHelper.IsFuzzyMatch(a.Equipment?.Name, filter) ||
                    FuzzySearchHelper.IsFuzzyMatch(a.Employee?.FullName, filter) ||
                    FuzzySearchHelper.IsFuzzyMatch(a.Location?.LocationName, filter) ||
                    FuzzySearchHelper.IsFuzzyMatch(a.PreviousAllocation?.Employee?.FullName, filter) ||
                    FuzzySearchHelper.IsFuzzyMatch(a.AllocationDate.ToString("yyyy-MM-dd"), filter))
                .OrderByDescending(a => Math.Max(
                    FuzzySearchHelper.FuzzyScore(a.Equipment?.EquipmentCode, filter),
                    Math.Max(
                        FuzzySearchHelper.FuzzyScore(a.Equipment?.Name, filter),
                        Math.Max(
                            FuzzySearchHelper.FuzzyScore(a.Employee?.FullName, filter),
                            FuzzySearchHelper.FuzzyScore(a.PreviousAllocation?.Employee?.FullName, filter)))))
                .ToList();

            _totalMatchingCount = matched.Count;
            _totalPages = Math.Max(1, (int)Math.Ceiling(_totalMatchingCount / (double)_pageSize));
            if (_currentPage > _totalPages) _currentPage = _totalPages;

            var pageItems = matched
                .Skip((_currentPage - 1) * _pageSize)
                .Take(_pageSize)
                .ToList();

            foreach (var item in pageItems)
            {
                dgvTransfer.Rows.Add(
                    item.Id,
                    string.IsNullOrWhiteSpace(item.Equipment?.QrCode) ? "—" : item.Equipment!.QrCode,
                    item.Equipment?.EquipmentCode ?? "—",
                    item.Equipment?.Name ?? "—",
                    item.PreviousAllocation?.Employee?.FullName ?? "—",
                    item.Employee?.FullName ?? "—",
                    item.PreviousAllocation?.Location?.LocationName ?? "—",
                    item.Location?.LocationName ?? "—",
                    item.AllocationDate.ToString("yyyy-MM-dd")
                );
            }

            lblRowCount.Text = $"Showing {pageItems.Count} of {_totalMatchingCount} transfers";
            lblPageIndicator.Text = $"Page {_currentPage} of {Math.Max(1, _totalPages)}";
            btnPrevPage.Enabled = _currentPage > 1;
            btnNextPage.Enabled = _currentPage < _totalPages;
        }

        private void btnTransfer_Click(object sender, EventArgs e)
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
                    MessageBox.Show("No allocated equipment available to transfer.",
                        "No Equipment", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                using var frm = new TransferDialog(
                    activeAllocations,
                    _employeeService.GetAll().Data ?? new(),
                    _locationService.GetAll().Data ?? new());

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

                LoadTransfers();
                MessageBox.Show("Equipment transferred successfully.", "Transfer Complete",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Transfer failed: " + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            txtSearch.Text = string.Empty;
            _currentPage = 1;
            LoadTransfers();
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
                    FileName = $"Transfers_{DateTime.Now:yyyyMMdd_HHmm}.csv"
                };
                if (sfd.ShowDialog() != DialogResult.OK) return;

                var sb = new StringBuilder();
                sb.AppendLine("Transfer ID,QR Code,Equipment Code,Equipment Name,From Employee,To Employee,From Location,To Location,Transfer Date");
                foreach (DataGridViewRow row in dgvTransfer.Rows)
                {
                    string E(object? v) => $"\"{v?.ToString()?.Replace("\"", "\"\"") ?? ""}\"";
                    sb.AppendLine(string.Join(",",
                        E(row.Cells["TransferID"].Value), E(row.Cells["QRCode"].Value),
                        E(row.Cells["EquipmentCode"].Value), E(row.Cells["EquipmentName"].Value),
                        E(row.Cells["FromEmployee"].Value), E(row.Cells["ToEmployee"].Value),
                        E(row.Cells["FromLocation"].Value), E(row.Cells["ToLocation"].Value),
                        E(row.Cells["TransferDate"].Value)));
                }
                System.IO.File.WriteAllText(sfd.FileName, sb.ToString());
                MessageBox.Show("Exported successfully.", "Export", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Export failed: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    // ── Transfer Dialog — unchanged ───────────────────────────────────────
    public class TransferDialog : Form
    {
        public int OldAllocationId { get; private set; }
        public int NewEmployeeId { get; private set; }
        public int NewLocationId { get; private set; }

        private ComboBox cmbAllocation = null!;
        private ComboBox cmbEmployee = null!;
        private ComboBox cmbLocation = null!;
        private Button btnSave = null!;
        private Button btnCancel = null!;

        private readonly List<Allocation> _allocations;
        private readonly List<Employee> _employees;
        private readonly List<Location> _locations;

        public TransferDialog(List<Allocation> allocations, List<Employee> employees, List<Location> locations)
        {
            _allocations = allocations;
            _employees = employees;
            _locations = locations;
            BuildUI();
        }

        private void BuildUI()
        {
            Text = "New Transfer";
            Size = new Size(420, 320);
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

            cmbAllocation = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
            cmbAllocation.DisplayMember = "DisplayText";
            cmbAllocation.ValueMember = "Id";
            cmbAllocation.DataSource = _allocations.Select(a => new
            {
                a.Id,
                DisplayText = $"{a.Equipment?.EquipmentCode} → {a.Employee?.FullName}"
            }).ToList();
            ThemeHelper.StyleComboBox(cmbAllocation);
            AddRow("Equipment to transfer:", cmbAllocation);

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

            ClientSize = new Size(400, y + 70);
            AcceptButton = btnSave;
            CancelButton = btnCancel;
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            if (cmbAllocation.SelectedValue == null || cmbEmployee.SelectedValue == null || cmbLocation.SelectedValue == null)
            {
                MessageBox.Show("Please fill all fields.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            OldAllocationId = (int)cmbAllocation.SelectedValue;
            NewEmployeeId = (int)cmbEmployee.SelectedValue;
            NewLocationId = (int)cmbLocation.SelectedValue;
            DialogResult = DialogResult.OK;
        }
    }
}