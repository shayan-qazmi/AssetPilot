// Forms/ReturnsForm.cs
using Inventory_Management_System.Helpers;
using Inventory_Management_System.Models;
using Inventory_Management_System.Services;
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
            ThemeHelper.StyleButton(btnRefresh);
            ThemeHelper.StyleSecondaryButton(btnExport);
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
                    item.AllocationDate.ToString("yyyy-MM-dd"),
                    item.ActualReturnDate?.ToString("yyyy-MM-dd") ?? "—",
                    durationDays
                );

                // Color long durations amber
                if (durationDays > 30)
                    dgvReturns.Rows[rowIdx].Cells["Duration"].Style.ForeColor = ThemeHelper.Warning;
            }

            lblRowCount.Text = $"Showing {dgvReturns.Rows.Count} of {_allReturns.Count} returns";
        }

        private void BtnAdd_Click(object? sender, EventArgs e)
        {
            MessageBox.Show("Add return functionality.", "Add Return", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnEdit_Click(object? sender, EventArgs e)
        {
            if (dgvReturns.SelectedRows.Count == 0)
            {
                MessageBox.Show("Please select a return to edit.", "Edit Return", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            MessageBox.Show("Edit return functionality.", "Edit Return", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnDelete_Click(object? sender, EventArgs e)
        {
            if (dgvReturns.SelectedRows.Count == 0)
            {
                MessageBox.Show("Please select a return to delete.", "Delete Return", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            var confirm = MessageBox.Show("Are you sure you want to delete this return?", "Delete Return", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirm == DialogResult.Yes)
            {
                LoadReturns();
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
                sb.AppendLine("Return ID,Equipment Code,Equipment Name,Returned By,Allocated On,Return Date,Duration (days)");
                foreach (DataGridViewRow row in dgvReturns.Rows)
                {
                    string E(object? v) => $"\"{v?.ToString()?.Replace("\"", "\"\"") ?? ""}\"";
                    sb.AppendLine(string.Join(",",
                        E(row.Cells["ReturnID"].Value), E(row.Cells["EquipmentCode"].Value),
                        E(row.Cells["EquipmentName"].Value), E(row.Cells["Employee"].Value),
                        E(row.Cells["AllocatedOn"].Value), E(row.Cells["ReturnDate"].Value),
                        E(row.Cells["Duration"].Value)));
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
}