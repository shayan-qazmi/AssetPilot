// Forms/ReturnsForm.cs
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
    public partial class ReturnsForm : Form
    {
        private readonly AllocationService _allocationService = new();
        private readonly AuditService _audit = new();
        private List<Allocation> _allReturns = new();
        private EntitySuggestHelper<Allocation>? _suggest;

        private int _currentPage = 1;
        private int _pageSize = 25;
        private int _totalPages = 1;
        private int _totalMatchingCount = 0;

        public ReturnsForm()
        {
            InitializeComponent();
            ApplyTheme();
            LoadReturns();
            SetupSuggestions();
        }

        private void ApplyTheme()
        {
            ThemeHelper.ApplyFormBackground(this);
            ThemeHelper.StyleToolbarPanel(panelTop);
            ThemeHelper.StyleButton(btnAdd);
            ThemeHelper.StyleButton(btnEdit, Color.FromArgb(245, 158, 11));
            ThemeHelper.StyleDangerButton(btnDelete);
            ThemeHelper.StyleButton(btnRefresh, Color.FromArgb(33, 150, 243));
            ThemeHelper.StyleSecondaryButton(btnExport);
            ThemeHelper.StyleTextBox(txtSearch);
            ThemeHelper.StyleComboBox(cmbPageSize);
            ThemeHelper.StyleSecondaryButton(btnPrevPage);
            ThemeHelper.StyleSecondaryButton(btnNextPage);
            ThemeHelper.StyleDataGridView(dgvReturns);
            panelBottom.BackColor = Color.FromArgb(249, 250, 251);
            lblRowCount.ForeColor = ThemeHelper.TextSecondary;
            lblRowCount.Font = ThemeHelper.FontSmall;
        }

        // ── Live fuzzy-suggestion dropdown (same UX as QR Scanner / Check-In-Out) ──
        private void SetupSuggestions()
        {
            try
            {
                _suggest = new EntitySuggestHelper<Allocation>(
                    textBox: txtSearch,
                    hostForm: this,
                    onSelected: a => SelectReturnRow(a.Id),
                    scoreFn: (a, term) => Math.Max(
                        FuzzySearchHelper.FuzzyScoreCode(a.Equipment?.EquipmentCode, term),
                        Math.Max(
                            FuzzySearchHelper.FuzzyScoreCode(a.Equipment?.QrCode, term),
                            Math.Max(
                                FuzzySearchHelper.FuzzyScore(a.Equipment?.Name, term),
                                Math.Max(
                                    FuzzySearchHelper.FuzzyScore(a.Employee?.FullName, term),
                                    FuzzySearchHelper.FuzzyScore(a.ActualReturnDate?.ToString("yyyy-MM-dd"), term))))),
                    line1Selector: a => $"{a.Equipment?.EquipmentCode} — {a.Equipment?.Name}",
                    line2Selector: a => $"{a.Employee?.FullName ?? "—"} • {a.ActualReturnDate?.ToString("yyyy-MM-dd") ?? "—"}");
                _suggest.LoadItems(_allReturns);
            }
            catch { /* non-critical UI enhancement, fail silently */ }
        }

        private void SelectReturnRow(int id)
        {
            foreach (DataGridViewRow row in dgvReturns.Rows)
            {
                if (row.Cells["ReturnID"].Value != null &&
                    Convert.ToInt32(row.Cells["ReturnID"].Value) == id)
                {
                    dgvReturns.ClearSelection();
                    row.Selected = true;
                    dgvReturns.CurrentCell = row.Cells[0];
                    dgvReturns.FirstDisplayedScrollingRowIndex = row.Index;
                    break;
                }
            }
        }

        // ── Display-only view-model row combining Allocation returns + Check In/Out completions
        private class ReturnRow
        {
            public int Id { get; set; }
            public string Source { get; set; } = "Allocation"; // "Allocation" | "Check In/Out"
            public string QrCode { get; set; } = "—";
            public string EquipmentCode { get; set; } = "—";
            public string EquipmentName { get; set; } = "—";
            public string Employee { get; set; } = "—";
            public DateTime SortDate { get; set; }
            public string ReturnDate { get; set; } = "—";
            public string Condition { get; set; } = "—";
            public int DurationDays { get; set; }
            public Allocation? AllocationRef { get; set; } // non-null only for Allocation-sourced rows
        }

        private List<ReturnRow> _allReturnRows = new();

        private void LoadReturns()
        {
            try
            {
                using var db = new InventoryDbContext();

                _allReturns = db.Allocations
                    .Where(a => a.Status == "returned")
                    .Include(a => a.Equipment)
                    .Include(a => a.Employee)
                    .Include(a => a.Location)
                    .OrderByDescending(a => a.ActualReturnDate)
                    .ToList();

                var allocRows = _allReturns.Select(item =>
                {
                    var returnDate = item.ActualReturnDate.HasValue
                        ? item.ActualReturnDate.Value.ToDateTime(TimeOnly.MinValue)
                        : DateTime.Today;
                    return new ReturnRow
                    {
                        Id = item.Id,
                        Source = "Allocation",
                        QrCode = string.IsNullOrWhiteSpace(item.Equipment?.QrCode) ? "—" : item.Equipment!.QrCode,
                        EquipmentCode = item.Equipment?.EquipmentCode ?? "—",
                        EquipmentName = item.Equipment?.Name ?? "—",
                        Employee = item.Employee?.FullName ?? "—",
                        SortDate = returnDate,
                        ReturnDate = item.ActualReturnDate?.ToString("yyyy-MM-dd") ?? "—",
                        Condition = item.Equipment?.ConditionGrade ?? "—",
                        DurationDays = (returnDate - item.AllocationDate).Days,
                        AllocationRef = item
                    };
                }).ToList();

                var completedCheckIns = db.Transactions
                    .Include(t => t.Equipment)
                    .Where(t => t.Status == "Completed" && t.ActualReturn.HasValue)
                    .ToList();

                var checkInRows = completedCheckIns.Select(t =>
                {
                    var returnDate = t.ActualReturn!.Value.ToDateTime(TimeOnly.MinValue);
                    return new ReturnRow
                    {
                        Id = -t.TransactionId,
                        Source = "Check In/Out",
                        QrCode = string.IsNullOrWhiteSpace(t.Equipment?.QrCode) ? "—" : t.Equipment!.QrCode,
                        EquipmentCode = t.Equipment?.EquipmentCode ?? "—",
                        EquipmentName = t.Equipment?.Name ?? "—",
                        Employee = string.IsNullOrWhiteSpace(t.IssuedTo) ? "—" : t.IssuedTo,
                        SortDate = returnDate,
                        ReturnDate = t.ActualReturn?.ToString("yyyy-MM-dd") ?? "—",
                        Condition = t.ConditionAfter ?? "—",
                        DurationDays = (returnDate - t.TransactionDate).Days,
                        AllocationRef = null
                    };
                }).ToList();

                _allReturnRows = allocRows.Concat(checkInRows)
                    .OrderByDescending(r => r.SortDate)
                    .ToList();

                _suggest?.LoadItems(_allReturns);   // suggestion dropdown stays Allocation-only, unchanged
                RefreshGrid();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading returns: " + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RefreshGrid(string filter = "")
        {
            dgvReturns.Rows.Clear();

            var matched = string.IsNullOrWhiteSpace(filter)
                ? _allReturnRows
                : _allReturnRows.Where(r =>
                    FuzzySearchHelper.IsFuzzyMatch(r.EquipmentCode, filter) ||
                    FuzzySearchHelper.IsFuzzyMatch(r.QrCode, filter) ||
                    FuzzySearchHelper.IsFuzzyMatch(r.EquipmentName, filter) ||
                    FuzzySearchHelper.IsFuzzyMatch(r.Employee, filter) ||
                    FuzzySearchHelper.IsFuzzyMatch(r.ReturnDate, filter) ||
                    FuzzySearchHelper.IsFuzzyMatch(r.Source, filter))
                .OrderByDescending(r => Math.Max(
                    FuzzySearchHelper.FuzzyScore(r.EquipmentCode, filter),
                    Math.Max(
                        FuzzySearchHelper.FuzzyScore(r.EquipmentName, filter),
                        FuzzySearchHelper.FuzzyScore(r.Employee, filter))))
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
                int rowIdx = dgvReturns.Rows.Add(
                    item.Id,
                    item.QrCode,
                    item.EquipmentCode,
                    item.EquipmentName,
                    item.Employee,
                    item.ReturnDate,
                    item.Condition,
                    item.Source
                );

                if (item.DurationDays > 30)
                    dgvReturns.Rows[rowIdx].Cells["ReturnDate"].Style.ForeColor = ThemeHelper.Warning;

                var sourceCell = dgvReturns.Rows[rowIdx].Cells["Source"];
                sourceCell.Style.ForeColor = item.Source == "Check In/Out"
                    ? Color.FromArgb(25, 118, 210)
                    : Color.FromArgb(107, 114, 128);
                sourceCell.Style.Font = ThemeHelper.FontBold;
            }

            lblRowCount.Text = $"Showing {pageItems.Count} of {_totalMatchingCount} returns";
            lblPageIndicator.Text = $"Page {_currentPage} of {Math.Max(1, _totalPages)}";
            btnPrevPage.Enabled = _currentPage > 1;
            btnNextPage.Enabled = _currentPage < _totalPages;
        }

        private Allocation? GetSelectedReturn()
        {
            if (dgvReturns.SelectedRows.Count == 0) return null;
            if (!int.TryParse(dgvReturns.SelectedRows[0].Cells["ReturnID"].Value?.ToString(), out int id)) return null;
            if (id < 0) return null; // Check In/Out-sourced row — not editable/deletable here
            return _allReturns.FirstOrDefault(a => a.Id == id);
        }

        private void BtnAdd_Click(object? sender, EventArgs e)
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
                    MessageBox.Show("There are no active approved allocations to return.",
                        "No Active Allocations", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                using var frm = new ProcessReturnForm(activeAllocations, isEdit: false);
                if (frm.ShowDialog() != DialogResult.OK) return;

                var alloc = db.Allocations
                    .Include(a => a.Equipment)
                    .FirstOrDefault(a => a.Id == frm.AllocationId);

                if (alloc == null) { MessageBox.Show("Allocation not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); return; }

                alloc.Status = "returned";
                alloc.ActualReturnDate = frm.ReturnDate;
                if (alloc.Equipment != null)
                {
                    alloc.Equipment.CurrentStatus = "in_store";
                    alloc.Equipment.ConditionGrade = frm.ConditionGrade;
                }
                db.SaveChanges();
                _audit.Log(alloc.EquipmentId, AuditService.ActionAllocationReturned, "allocated", "in_store");
                LoadReturns();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to process return: " + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnEdit_Click(object? sender, EventArgs e)
        {
            var ret = GetSelectedReturn();
            if (ret == null) { MessageBox.Show("Please select a return to edit.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

            using var frm = new ProcessReturnForm(new List<Allocation> { ret }, isEdit: true, existing: ret);
            if (frm.ShowDialog() != DialogResult.OK) return;

            try
            {
                using var db = new InventoryDbContext();
                var alloc = db.Allocations.Include(a => a.Equipment).FirstOrDefault(a => a.Id == ret.Id);
                if (alloc == null) { MessageBox.Show("Return record not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); return; }
                alloc.ActualReturnDate = frm.ReturnDate;
                if (alloc.Equipment != null) alloc.Equipment.ConditionGrade = frm.ConditionGrade;
                db.SaveChanges();
                LoadReturns();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to update return: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnDelete_Click(object? sender, EventArgs e)
        {
            var ret = GetSelectedReturn();
            if (ret == null) { MessageBox.Show("Please select a return to delete.", "Delete Return", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

            if (MessageBox.Show(
                $"This will undo the return for '{ret.Equipment?.EquipmentCode} — {ret.Equipment?.Name}', reverting it back to an active allocation. Continue?",
                "Undo Return", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

            try
            {
                using var db = new InventoryDbContext();
                var alloc = db.Allocations.Include(a => a.Equipment).FirstOrDefault(a => a.Id == ret.Id);
                if (alloc == null) { MessageBox.Show("Return record not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); return; }
                string oldValue = $"{alloc.Equipment?.EquipmentCode} - returned";
                alloc.Status = "approved";
                alloc.ActualReturnDate = null;
                if (alloc.Equipment != null) alloc.Equipment.CurrentStatus = "allocated";
                db.SaveChanges();
                _audit.Log(alloc.EquipmentId, AuditService.ActionAllocationReturned,
                    oldValue, $"{alloc.Equipment?.EquipmentCode} - approved (return undone)");
                LoadReturns();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to undo return: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            txtSearch.Text = string.Empty;
            _currentPage = 1;
            LoadReturns();
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
                    FileName = $"Returns_{DateTime.Now:yyyyMMdd_HHmm}.csv"
                };
                if (sfd.ShowDialog() != DialogResult.OK) return;

                var sb = new StringBuilder();
                sb.AppendLine("Return ID,QR Code,Equipment Code,Equipment Name,Returned By,Return Date,Condition");
                foreach (DataGridViewRow row in dgvReturns.Rows)
                {
                    string E(object? v) => $"\"{v?.ToString()?.Replace("\"", "\"\"") ?? ""}\"";
                    sb.AppendLine(string.Join(",",
                        E(row.Cells["ReturnID"].Value), E(row.Cells["QRCode"].Value),
                        E(row.Cells["EquipmentCode"].Value), E(row.Cells["EquipmentName"].Value),
                        E(row.Cells["Employee"].Value), E(row.Cells["ReturnDate"].Value),
                        E(row.Cells["Condition"].Value)));
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

    // ── Process Return Dialog — unchanged ─────────────────────────────────
    public class ProcessReturnForm : Form
    {
        public int AllocationId { get; private set; }
        public DateOnly ReturnDate { get; private set; }
        public string ConditionGrade { get; private set; } = "Good";

        private readonly List<Allocation> _allocations;
        private readonly bool _isEdit;

        private ComboBox cmbAllocation = null!;
        private DateTimePicker dtpReturnDate = null!;
        private ComboBox cmbCondition = null!;
        private Button btnSave = null!;
        private Button btnCancel = null!;

        private static readonly string[] Conditions = { "New", "Good", "Fair", "Poor", "Unserviceable" };

        public ProcessReturnForm(List<Allocation> allocations, bool isEdit, Allocation? existing = null)
        {
            _allocations = allocations;
            _isEdit = isEdit;
            BuildUI(existing);
        }

        private void BuildUI(Allocation? existing)
        {
            Text = _isEdit ? "Edit Return" : "Process Return";
            Size = new Size(460, 310);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            ThemeHelper.ApplyFormBackground(this);

            int y = 20;
            void AddRow(string label, Control ctrl)
            {
                var lbl = new Label { Text = label, Location = new Point(20, y), AutoSize = true };
                ThemeHelper.StyleLabel(lbl, bold: true);
                Controls.Add(lbl);
                y += 22;
                ctrl.Location = new Point(20, y);
                ctrl.Width = 400;
                Controls.Add(ctrl);
                y += 42;
            }

            cmbAllocation = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
            cmbAllocation.DisplayMember = "AllocationDisplay";
            cmbAllocation.ValueMember = "Id";
            cmbAllocation.DataSource = _allocations.Select(a => new
            {
                a.Id,
                AllocationDisplay = $"{a.Equipment?.EquipmentCode} — {a.Equipment?.Name} → {a.Employee?.FullName}"
            }).ToList<object>();
            ThemeHelper.StyleComboBox(cmbAllocation);
            if (_isEdit) cmbAllocation.Enabled = false;
            AddRow(_isEdit ? "Allocation:" : "Select Allocation (approved only):", cmbAllocation);

            dtpReturnDate = new DateTimePicker
            {
                Format = DateTimePickerFormat.Short,
                MaxDate = DateTime.Today,
                Value = existing?.ActualReturnDate.HasValue == true
                    ? existing.ActualReturnDate.Value.ToDateTime(TimeOnly.MinValue)
                    : DateTime.Today
            };
            AddRow("Return Date:", dtpReturnDate);

            cmbCondition = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
            cmbCondition.Items.AddRange(Conditions);
            cmbCondition.SelectedItem = existing?.Equipment?.ConditionGrade ?? "Good";
            if (cmbCondition.SelectedIndex < 0) cmbCondition.SelectedIndex = 1;
            ThemeHelper.StyleComboBox(cmbCondition);
            AddRow("Equipment Condition on Return:", cmbCondition);

            btnSave = new Button { Text = _isEdit ? "Save" : "Process Return", Location = new Point(20, y), Size = new Size(190, 36) };
            ThemeHelper.StyleButton(btnSave);
            btnSave.Click += BtnSave_Click;
            Controls.Add(btnSave);

            btnCancel = new Button { Text = "Cancel", Location = new Point(222, y), Size = new Size(198, 36) };
            ThemeHelper.StyleSecondaryButton(btnCancel);
            btnCancel.Click += (s, e) => DialogResult = DialogResult.Cancel;
            Controls.Add(btnCancel);

            ClientSize = new Size(440, y + 70);
            AcceptButton = btnSave;
            CancelButton = btnCancel;
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            if (cmbAllocation.SelectedValue == null) { MessageBox.Show("Please select an allocation.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            if (cmbCondition.SelectedItem == null) { MessageBox.Show("Please select a condition grade.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            AllocationId = (int)cmbAllocation.SelectedValue;
            ReturnDate = DateOnly.FromDateTime(dtpReturnDate.Value);
            ConditionGrade = cmbCondition.SelectedItem.ToString()!;
            DialogResult = DialogResult.OK;
        }
    }
}