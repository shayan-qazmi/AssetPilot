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
        private readonly AuditService      _audit             = new();
        private List<Allocation> _allReturns = new();

        public ReturnsForm()
        {
            InitializeComponent();
            ApplyTheme();
            LoadReturns();
        }

        private void ApplyTheme()
        {
            ThemeHelper.ApplyFormBackground(this);
            ThemeHelper.StyleToolbarPanel(panelTop);
            ThemeHelper.StyleButton(btnAdd);                                        // green — positive action
            ThemeHelper.StyleButton(btnEdit, Color.FromArgb(245, 158, 11));         // amber — modify
            ThemeHelper.StyleDangerButton(btnDelete);                               // red — destructive
            ThemeHelper.StyleButton(btnRefresh, Color.FromArgb(33, 150, 243));      // blue — matches "Under Repair" blue elsewhere
            ThemeHelper.StyleSecondaryButton(btnExport);                            // grey — neutral/export
            ThemeHelper.StyleTextBox(txtSearch);
            ThemeHelper.StyleDataGridView(dgvReturns);
            panelBottom.BackColor = Color.FromArgb(249, 250, 251);
            lblRowCount.ForeColor = ThemeHelper.TextSecondary;
            lblRowCount.Font      = ThemeHelper.FontSmall;
        }

        private void LoadReturns(string filter = "")
        {
            var result = _allocationService.GetAll();
            if (!result.Success)
            {
                MessageBox.Show(result.ErrorMessage, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            _allReturns = result.Data?.Where(a => a.Status == "returned").ToList() ?? new();
            RefreshGrid(filter);
        }

        private void RefreshGrid(string filter = "")
        {
            dgvReturns.Rows.Clear();

            var items = string.IsNullOrWhiteSpace(filter)
                ? _allReturns
                : _allReturns.Where(a =>
                    FuzzySearchHelper.IsFuzzyMatch(a.Equipment?.EquipmentCode, filter) ||
                    FuzzySearchHelper.IsFuzzyMatch(a.Equipment?.Name, filter) ||
                    FuzzySearchHelper.IsFuzzyMatch(a.Employee?.FullName, filter)
                )
                .OrderByDescending(a => Math.Max(
                    FuzzySearchHelper.FuzzyScore(a.Equipment?.EquipmentCode, filter),
                    Math.Max(
                        FuzzySearchHelper.FuzzyScore(a.Equipment?.Name, filter),
                        FuzzySearchHelper.FuzzyScore(a.Employee?.FullName, filter)
                    )
                ))
                .ToList();

            foreach (var item in items)
            {
                // Calculate duration
                var returnDate   = item.ActualReturnDate.HasValue
                    ? item.ActualReturnDate.Value.ToDateTime(TimeOnly.MinValue)
                    : DateTime.Today;
                int durationDays = (returnDate - item.AllocationDate).Days;

                int rowIdx = dgvReturns.Rows.Add(
                    item.Id,
                    item.Equipment?.EquipmentCode ?? "—",
                    item.Equipment?.Name          ?? "—",
                    item.Employee?.FullName        ?? "—",
                    item.ActualReturnDate?.ToString("yyyy-MM-dd") ?? "—",
                    item.Equipment?.ConditionGrade ?? "—"
                );

                // Color long durations amber
                if (durationDays > 30)
                    dgvReturns.Rows[rowIdx].Cells["ReturnDate"].Style.ForeColor = ThemeHelper.Warning;
            }

            lblRowCount.Text = $"Showing {dgvReturns.Rows.Count} of {_allReturns.Count} returns";
        }

        private Allocation? GetSelectedReturn()
        {
            if (dgvReturns.SelectedRows.Count == 0) return null;
            if (!int.TryParse(dgvReturns.SelectedRows[0].Cells["ReturnID"].Value?.ToString(), out int id)) return null;
            return _allReturns.FirstOrDefault(a => a.Id == id);
        }

        // ── Add (Process New Return) ──────────────────────────────────────
        private void BtnAdd_Click(object? sender, EventArgs e)
        {
            // Load active approved allocations
            var allResult = _allocationService.GetAll();
            if (!allResult.Success)
            {
                MessageBox.Show(allResult.ErrorMessage, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var activeAllocations = allResult.Data?
                .Where(a => a.Status == "approved")
                .ToList() ?? new();

            if (activeAllocations.Count == 0)
            {
                MessageBox.Show("There are no active approved allocations to return.", "No Active Allocations",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var frm = new ProcessReturnForm(activeAllocations, isEdit: false);
            if (frm.ShowDialog() != DialogResult.OK) return;

            try
            {
                using var db = new InventoryDbContext();

                var alloc = db.Allocations
                    .Include(a => a.Equipment)
                    .FirstOrDefault(a => a.Id == frm.AllocationId);

                if (alloc == null)
                {
                    MessageBox.Show("Allocation not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Mark as returned
                alloc.Status           = "returned";
                alloc.ActualReturnDate = frm.ReturnDate;

                // Update equipment status and condition
                if (alloc.Equipment != null)
                {
                    alloc.Equipment.CurrentStatus  = "in_store";
                    alloc.Equipment.ConditionGrade = frm.ConditionGrade;
                }

                db.SaveChanges();

                _audit.Log(alloc.EquipmentId, AuditService.ActionAllocationReturned,
                    "allocated", "in_store");

                LoadReturns();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to process return: " + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ── Edit (Modify Existing Return) ─────────────────────────────────
        private void BtnEdit_Click(object? sender, EventArgs e)
        {
            var ret = GetSelectedReturn();
            if (ret == null)
            {
                MessageBox.Show("Please select a return to edit.", "No Selection",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using var frm = new ProcessReturnForm(new List<Allocation> { ret }, isEdit: true, existing: ret);
            if (frm.ShowDialog() != DialogResult.OK) return;

            try
            {
                using var db = new InventoryDbContext();

                var alloc = db.Allocations
                    .Include(a => a.Equipment)
                    .FirstOrDefault(a => a.Id == ret.Id);

                if (alloc == null)
                {
                    MessageBox.Show("Return record not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                alloc.ActualReturnDate = frm.ReturnDate;

                if (alloc.Equipment != null)
                    alloc.Equipment.ConditionGrade = frm.ConditionGrade;

                db.SaveChanges();
                LoadReturns();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to update return: " + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ── Delete (Undo Return) ────────────────────────────────────────────
        // A "return" is not its own row — it's an Allocation with Status =
        // "returned". Deleting it here means undoing that return: the
        // allocation goes back to "approved" (still checked out) and the
        // equipment's status reverts to "allocated". This preserves history
        // instead of permanently destroying the allocation record.
        private void BtnDelete_Click(object? sender, EventArgs e)
        {
            var ret = GetSelectedReturn();
            if (ret == null)
            {
                MessageBox.Show("Please select a return to delete.", "Delete Return",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var confirm = MessageBox.Show(
                $"This will undo the return for '{ret.Equipment?.EquipmentCode} — {ret.Equipment?.Name}', " +
                "reverting it back to an active allocation. Continue?",
                "Undo Return", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes) return;

            try
            {
                using var db = new InventoryDbContext();

                var alloc = db.Allocations
                    .Include(a => a.Equipment)
                    .FirstOrDefault(a => a.Id == ret.Id);

                if (alloc == null)
                {
                    MessageBox.Show("Return record not found.", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                string oldValue = $"{alloc.Equipment?.EquipmentCode} - returned";

                alloc.Status           = "approved";
                alloc.ActualReturnDate = null;

                if (alloc.Equipment != null)
                    alloc.Equipment.CurrentStatus = "allocated";

                db.SaveChanges();

                _audit.Log(alloc.EquipmentId, AuditService.ActionAllocationReturned,
                    oldValue, $"{alloc.Equipment?.EquipmentCode} - approved (return undone)");

                LoadReturns();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to undo return: " + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            txtSearch.Text = string.Empty;
            LoadReturns();
        }

        private void txtSearch_TextChanged(object sender, EventArgs e)
            => RefreshGrid(txtSearch.Text.Trim());

        private void btnExport_Click(object sender, EventArgs e)
        {
            try
            {
                using var sfd = new SaveFileDialog
                {
                    Filter   = "CSV files|*.csv",
                    FileName = $"Returns_{DateTime.Now:yyyyMMdd_HHmm}.csv"
                };
                if (sfd.ShowDialog() != DialogResult.OK) return;

                var sb = new StringBuilder();
                sb.AppendLine("Return ID,Equipment Code,Equipment Name,Returned By,Return Date,Condition");
                foreach (DataGridViewRow row in dgvReturns.Rows)
                {
                    string E(object? v) => $"\"{v?.ToString()?.Replace("\"", "\"\"") ?? ""}\"";
                    sb.AppendLine(string.Join(",",
                        E(row.Cells["ReturnID"].Value), E(row.Cells["EquipmentCode"].Value),
                        E(row.Cells["EquipmentName"].Value), E(row.Cells["Employee"].Value),
                        E(row.Cells["ReturnDate"].Value), E(row.Cells["Condition"].Value)));
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

    // ── Process Return Dialog ─────────────────────────────────────────────
    public class ProcessReturnForm : Form
    {
        public int      AllocationId   { get; private set; }
        public DateOnly ReturnDate     { get; private set; }
        public string   ConditionGrade { get; private set; } = "Good";

        private readonly List<Allocation> _allocations;
        private readonly bool             _isEdit;

        private ComboBox       cmbAllocation  = null!;
        private DateTimePicker dtpReturnDate  = null!;
        private ComboBox       cmbCondition   = null!;
        private Button         btnSave        = null!;
        private Button         btnCancel      = null!;

        private static readonly string[] Conditions = { "New", "Good", "Fair", "Poor", "Unserviceable" };

        public ProcessReturnForm(List<Allocation> allocations, bool isEdit, Allocation? existing = null)
        {
            _allocations = allocations;
            _isEdit      = isEdit;
            BuildUI(existing);
        }

        private void BuildUI(Allocation? existing)
        {
            Text            = _isEdit ? "Edit Return" : "Process Return";
            Size            = new Size(460, 310);
            StartPosition   = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox     = false;
            ThemeHelper.ApplyFormBackground(this);

            int y = 20;
            void AddRow(string label, Control ctrl)
            {
                var lbl = new Label { Text = label, Location = new Point(20, y), AutoSize = true };
                ThemeHelper.StyleLabel(lbl, bold: true);
                Controls.Add(lbl);
                y += 22;
                ctrl.Location = new Point(20, y);
                ctrl.Width    = 400;
                Controls.Add(ctrl);
                y += 42;
            }

            // Allocation dropdown
            cmbAllocation = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
            cmbAllocation.DisplayMember = "AllocationDisplay";
            cmbAllocation.ValueMember   = "Id";
            // Build display-friendly list
            var displayList = _allocations.Select(a => new
            {
                a.Id,
                AllocationDisplay = $"{a.Equipment?.EquipmentCode} — {a.Equipment?.Name} → {a.Employee?.FullName}"
            }).ToList<object>();
            cmbAllocation.DataSource    = displayList;
            ThemeHelper.StyleComboBox(cmbAllocation);

            if (_isEdit)
                cmbAllocation.Enabled = false;   // cannot re-pick the allocation when editing

            AddRow(_isEdit ? "Allocation:" : "Select Allocation (approved only):", cmbAllocation);

            // Return date picker
            dtpReturnDate = new DateTimePicker
            {
                Format  = DateTimePickerFormat.Short,
                MaxDate = DateTime.Today,
                Value   = existing?.ActualReturnDate.HasValue == true
                    ? existing.ActualReturnDate.Value.ToDateTime(TimeOnly.MinValue)
                    : DateTime.Today
            };
            AddRow("Return Date:", dtpReturnDate);

            // Condition dropdown
            cmbCondition = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
            cmbCondition.Items.AddRange(Conditions);
            cmbCondition.SelectedItem = existing?.Equipment?.ConditionGrade ?? "Good";
            if (cmbCondition.SelectedIndex < 0) cmbCondition.SelectedIndex = 1; // default Good
            ThemeHelper.StyleComboBox(cmbCondition);
            AddRow("Equipment Condition on Return:", cmbCondition);

            // Buttons
            btnSave = new Button { Text = _isEdit ? "Save" : "Process Return", Location = new Point(20, y), Size = new Size(190, 36) };
            ThemeHelper.StyleButton(btnSave);
            btnSave.Click += BtnSave_Click;
            Controls.Add(btnSave);

            btnCancel = new Button { Text = "Cancel", Location = new Point(222, y), Size = new Size(198, 36) }; 
            ThemeHelper.StyleSecondaryButton(btnCancel);
            btnCancel.Click += (s, e) => DialogResult = DialogResult.Cancel;
            Controls.Add(btnCancel);

            ClientSize   = new Size(440, y + 70);
            AcceptButton = btnSave;
            CancelButton = btnCancel;
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            if (cmbAllocation.SelectedValue == null)
            {
                MessageBox.Show("Please select an allocation.", "Validation",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (cmbCondition.SelectedItem == null)
            {
                MessageBox.Show("Please select a condition grade.", "Validation",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            AllocationId   = (int)cmbAllocation.SelectedValue;
            ReturnDate     = DateOnly.FromDateTime(dtpReturnDate.Value);
            ConditionGrade = cmbCondition.SelectedItem.ToString()!;

            DialogResult = DialogResult.OK;
        }
    }
}