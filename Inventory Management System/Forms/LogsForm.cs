// Forms/LogsForm.cs
using Inventory_Management_System.Helpers;
using Inventory_Management_System.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Inventory_Management_System.Forms
{
    public partial class LogsForm : Form
    {
        private List<EquipmentLog> _allLogs = new();

        public LogsForm()
        {
            InitializeComponent();
            ApplyTheme();
            LoadLogs();
        }

        private void ApplyTheme()
        {
            ThemeHelper.ApplyFormBackground(this);
            ThemeHelper.StyleToolbarPanel(panelTop);
            ThemeHelper.StyleButton(btnRefresh);
            ThemeHelper.StyleSecondaryButton(btnExport);
            ThemeHelper.StyleTextBox(txtSearch);
            ThemeHelper.StyleDataGridView(dgvLogs);

            panelBottom.BackColor = Color.FromArgb(249, 250, 251);
            lblRowCount.ForeColor = ThemeHelper.TextSecondary;
            lblRowCount.Font      = ThemeHelper.FontSmall;

            // Make action column slightly wider since text is long
            if (dgvLogs.Columns["Action"] != null) dgvLogs.Columns["Action"]!.FillWeight = 22;
            if (dgvLogs.Columns["OldValue"] != null) dgvLogs.Columns["OldValue"]!.FillWeight = 18;
            if (dgvLogs.Columns["NewValue"] != null) dgvLogs.Columns["NewValue"]!.FillWeight = 18;
        }

        private void LoadLogs(string filter = "")
        {
            try
            {
                using var db = new InventoryDbContext();
                _allLogs = db.EquipmentLogs
                    .Include(l => l.Equipment)
                    .Include(l => l.ChangedByUser)
                    .OrderByDescending(l => l.CreatedAt)
                    .ToList();
            }
            catch
            {
                _allLogs = new List<EquipmentLog>();
            }

            RefreshGrid(filter);
        }

        private void RefreshGrid(string filter = "")
        {
            dgvLogs.Rows.Clear();

            var filtered = string.IsNullOrWhiteSpace(filter)
                ? _allLogs
                : _allLogs.Where(l =>
                    FuzzySearchHelper.IsFuzzyMatch(l.Equipment?.EquipmentCode, filter) ||
                    FuzzySearchHelper.IsFuzzyMatch(l.ActionType, filter) ||
                    FuzzySearchHelper.IsFuzzyMatch(l.ChangedByUser?.FullName, filter) ||
                    FuzzySearchHelper.IsFuzzyMatch(l.OldValue, filter) ||
                    FuzzySearchHelper.IsFuzzyMatch(l.NewValue, filter)
                )
                .OrderByDescending(l => Math.Max(
                    FuzzySearchHelper.FuzzyScore(l.Equipment?.EquipmentCode, filter),
                    Math.Max(
                        FuzzySearchHelper.FuzzyScore(l.ActionType, filter),
                        Math.Max(
                            FuzzySearchHelper.FuzzyScore(l.ChangedByUser?.FullName, filter),
                            Math.Max(
                                FuzzySearchHelper.FuzzyScore(l.OldValue, filter),
                                FuzzySearchHelper.FuzzyScore(l.NewValue, filter)
                            )
                        )
                    )
                ))
                .ToList();

            foreach (var item in filtered)
            {
                int rowIdx = dgvLogs.Rows.Add(
                    item.Id,
                    item.Equipment?.EquipmentCode ?? "—",
                    FormatAction(item.ActionType),
                    item.ChangedByUser?.FullName ?? "System",
                    item.OldValue ?? "",
                    item.NewValue ?? "",
                    item.CreatedAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? ""
                );

                // Color-code rows by action type
                var row = dgvLogs.Rows[rowIdx];
                var actionCell = row.Cells["Action"];

                switch (item.ActionType)
                {
                    case "equipment_created":
                        actionCell.Style.BackColor = Color.FromArgb(200, 230, 201); // light green
                        actionCell.Style.ForeColor = Color.FromArgb(27, 94, 32);
                        break;
                    case "allocation_approved":
                    case "allocation_requested":
                        actionCell.Style.BackColor = Color.FromArgb(187, 222, 251); // light blue
                        actionCell.Style.ForeColor = Color.FromArgb(13, 71, 161);
                        break;
                    case "allocation_returned":
                        actionCell.Style.BackColor = Color.FromArgb(255, 249, 196); // light yellow
                        actionCell.Style.ForeColor = Color.FromArgb(130, 100, 0);
                        break;
                    case "allocation_rejected":
                    case "decommission_requested":
                    case "decommission_approved":
                        actionCell.Style.BackColor = Color.FromArgb(255, 205, 210); // light red
                        actionCell.Style.ForeColor = Color.FromArgb(183, 28, 28);
                        break;
                    case "transferred":
                        actionCell.Style.BackColor = Color.FromArgb(225, 190, 231); // light purple
                        actionCell.Style.ForeColor = Color.FromArgb(74, 20, 140);
                        break;
                    case "location_changed":
                        actionCell.Style.BackColor = Color.FromArgb(255, 224, 178); // light orange
                        actionCell.Style.ForeColor = Color.FromArgb(130, 60, 0);
                        break;
                }
                actionCell.Style.Font = ThemeHelper.FontBold;
            }

            lblRowCount.Text = $"Showing {dgvLogs.Rows.Count} of {_allLogs.Count} records";
        }

        // Converts raw DB enum to a clean readable label
        private static string FormatAction(string? raw) => raw switch
        {
            "equipment_created"       => "Equipment Created",
            "location_changed"        => "Location Changed",
            "allocation_requested"    => "Allocation Requested",
            "allocation_approved"     => "Allocation Approved",
            "allocation_rejected"     => "Allocation Rejected",
            "allocation_returned"     => "Returned to Store",
            "decommission_requested"  => "Decommission Requested",
            "decommission_approved"   => "Decommission Approved",
            "decommission_rejected"   => "Decommission Rejected",
            "transferred"             => "Transferred",
            _                         => raw ?? "Unknown"
        };

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            txtSearch.Text = string.Empty;
            LoadLogs();
        }

        private void txtSearch_TextChanged(object sender, EventArgs e)
        {
            RefreshGrid(txtSearch.Text.Trim());
        }

        private void btnExport_Click(object sender, EventArgs e)
        {
            try
            {
                using var dlg = new SaveFileDialog
                {
                    Filter   = "CSV files (*.csv)|*.csv",
                    FileName = $"AuditLogs_{DateTime.Now:yyyyMMdd_HHmm}.csv"
                };

                if (dlg.ShowDialog() != DialogResult.OK) return;

                var sb = new StringBuilder();
                sb.AppendLine("Log ID,Equipment Code,Action,Changed By,Previous Value,New Value,Timestamp");

                foreach (DataGridViewRow row in dgvLogs.Rows)
                {
                    string Escape(object? v) => $"\"{(v?.ToString() ?? "").Replace("\"", "\"\"")}\"";
                    sb.AppendLine(string.Join(",",
                        Escape(row.Cells["ID"].Value),
                        Escape(row.Cells["Equipment"].Value),
                        Escape(row.Cells["Action"].Value),
                        Escape(row.Cells["User"].Value),
                        Escape(row.Cells["OldValue"].Value),
                        Escape(row.Cells["NewValue"].Value),
                        Escape(row.Cells["Date"].Value)));
                }

                System.IO.File.WriteAllText(dlg.FileName, sb.ToString());
                MessageBox.Show($"Logs exported successfully to:\n{dlg.FileName}",
                    "Export Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Export failed: " + ex.Message,
                    "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}