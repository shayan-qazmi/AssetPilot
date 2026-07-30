// Forms/DecommissionForm.cs
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
    public partial class DecommissionForm : Form
    {
        private readonly EquipmentService _equipmentService = new();
        private readonly AuditService     _audit            = new();
        private List<Equipment>           _allItems         = new();

        public DecommissionForm()
        {
            InitializeComponent();
            flowToolbar.SizeChanged += (s, e) => panelTop.Height = flowToolbar.Height;
            ApplyTheme();
            LoadDecommissioned();

            this.Shown += (s, e) =>
            {
                dgvDecommission.ClearSelection();
                dgvDecommission.CurrentCell = null;
            };
        }

        private void ApplyTheme()
        {
            ThemeHelper.ApplyFormBackground(this);
            ThemeHelper.StyleToolbarPanel(panelTop);
            ThemeHelper.StyleDangerButton(btnDecommission);
            ThemeHelper.StyleButton(btnRefresh, Color.FromArgb(33, 150, 243));
            ThemeHelper.StyleSecondaryButton(btnExport);
            ThemeHelper.StyleTextBox(txtSearch);
            ThemeHelper.StyleDataGridView(dgvDecommission);
            panelBottom.BackColor = Color.FromArgb(249, 250, 251);
            lblRowCount.ForeColor = ThemeHelper.TextSecondary;
            lblRowCount.Font      = ThemeHelper.FontSmall;
        }

        private void LoadDecommissioned(string filter = "")
        {
            var result = _equipmentService.GetAll();
            if (!result.Success)
            {
                ToastHelper.Show(this, result.ErrorMessage, ToastType.Error);
                return;
            }
            _allItems = result.Data?.Where(e => e.CurrentStatus == "decommissioned").ToList() ?? new();
            RefreshGrid(filter);
        }

        private void RefreshGrid(string filter = "")
        {
            dgvDecommission.Rows.Clear();

            var items = string.IsNullOrWhiteSpace(filter)
                ? _allItems
                : _allItems.Where(e =>
                    FuzzySearchHelper.IsFuzzyMatch(e.EquipmentCode, filter) ||
                    FuzzySearchHelper.IsFuzzyMatch(e.Name, filter) ||
                    FuzzySearchHelper.IsFuzzyMatch(e.Make, filter) ||
                    FuzzySearchHelper.IsFuzzyMatch(e.Model, filter) ||
                    FuzzySearchHelper.IsFuzzyMatch(e.SerialNumber, filter)
                )
                .OrderByDescending(e => Math.Max(
                    FuzzySearchHelper.FuzzyScore(e.EquipmentCode, filter),
                    Math.Max(
                        FuzzySearchHelper.FuzzyScore(e.Name, filter),
                        Math.Max(
                            FuzzySearchHelper.FuzzyScore(e.Make, filter),
                            Math.Max(
                                FuzzySearchHelper.FuzzyScore(e.Model, filter),
                                FuzzySearchHelper.FuzzyScore(e.SerialNumber, filter)
                            )
                        )
                    )
                ))
                .ToList();

            foreach (var item in items)
            {
                // Get last log entry for this equipment to show reason/date
                string reason   = "—";
                string decommOn = "—";
                try
                {
                    using var db = new InventoryDbContext();
                    var log = db.EquipmentLogs
                        .Where(l => l.EquipmentId == item.Id &&
                               (l.ActionType == "decommission_approved" || l.ActionType == "decommission_requested"))
                        .OrderByDescending(l => l.CreatedAt)
                        .FirstOrDefault();
                    if (log != null)
                    {
                        reason   = log.NewValue ?? "—";
                        decommOn = log.CreatedAt?.ToString("yyyy-MM-dd") ?? "—";
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "DecommissionForm.LoadData (Log lookup)");
                }

                int rowIdx = dgvDecommission.Rows.Add(
                    item.EquipmentCode,
                    item.Name,
                    item.Make ?? "—",
                    item.Model ?? "—",
                    item.SerialNumber,
                    reason,
                    decommOn
                );

                // Red tint on entire row
                foreach (DataGridViewCell cell in dgvDecommission.Rows[rowIdx].Cells)
                    cell.Style.ForeColor = Color.FromArgb(183, 28, 28);
            }

            lblRowCount.Text = $"Showing {dgvDecommission.Rows.Count} decommissioned items";
        }

        private void btnDecommission_Click(object sender, EventArgs e)
        {
            // Show available (in_store) equipment to decommission
            var result = _equipmentService.GetAll();
            if (!result.Success) return;

            var available = result.Data?
                .Where(eq => eq.CurrentStatus == "in_store" || eq.CurrentStatus == "under_repair")
                .ToList() ?? new();

            if (available.Count == 0)
            {
                MessageBox.Show("No equipment available to decommission.\nOnly in_store or under_repair items can be decommissioned.",
                    "None Available", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var frm = new DecommissionDialog(available);
            if (frm.ShowDialog() != DialogResult.OK) return;

            try
            {
                using var db = new InventoryDbContext();
                var eq = db.Equipment.FirstOrDefault(e => e.Id == frm.EquipmentId);
                if (eq == null) return;

                string oldStatus = eq.CurrentStatus ?? "unknown";
                eq.CurrentStatus = "decommissioned";
                db.SaveChanges();

                _audit.Log(eq.Id, AuditService.ActionDecommissionApproved,
                    oldStatus, frm.Reason);

                LoadDecommissioned();
                ToastHelper.Show(this, $"'{eq.Name}' has been decommissioned.", ToastType.Success);

            }
            catch (Exception ex)
            {
               ToastHelper.Show(this, "Decommission failed: " + ex.Message, ToastType.Error);

            }
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            txtSearch.Text = string.Empty;
            LoadDecommissioned();
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
                    FileName = $"Decommissioned_{DateTime.Now:yyyyMMdd_HHmm}.csv"
                };
                if (sfd.ShowDialog() != DialogResult.OK) return;

                var sb = new StringBuilder();
                sb.AppendLine("Equipment Code,Name,Make,Model,Serial Number,Reason,Decommission Date");
                foreach (DataGridViewRow row in dgvDecommission.Rows)
                {
                    string E(object? v) => $"\"{v?.ToString()?.Replace("\"", "\"\"") ?? ""}\"";
                    sb.AppendLine(string.Join(",",
                        E(row.Cells["EquipmentCode"].Value), E(row.Cells["EquipmentName"].Value),
                        E(row.Cells["Make"].Value), E(row.Cells["Model"].Value),
                        E(row.Cells["SerialNumber"].Value), E(row.Cells["Reason"].Value),
                        E(row.Cells["DecommDate"].Value)));
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

    // ── Decommission Dialog ───────────────────────────────────────────────
    public class DecommissionDialog : Form
    {
        public int    EquipmentId { get; private set; }
        public string Reason      { get; private set; } = "";

        private ComboBox  cmbEquipment = null!;
        private ComboBox  cmbReason    = null!;
        private TextBox   txtNotes     = null!;
        private Button    btnSave      = null!;
        private Button    btnCancel    = null!;

        private readonly List<Equipment> _equipment;

        public DecommissionDialog(List<Equipment> equipment)
        {
            _equipment = equipment;
            BuildUI();
        }

        private void BuildUI()
        {
            Text            = "Decommission Equipment";
            Size            = new Size(420, 340);
            StartPosition   = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox     = false;
            ThemeHelper.ApplyFormBackground(this);
            this.AutoScroll = true;

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
                y += 40;
            }

            cmbEquipment = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
            cmbEquipment.DisplayMember = "Name";
            cmbEquipment.ValueMember   = "Id";
            cmbEquipment.DataSource    = _equipment;
            ThemeHelper.StyleComboBox(cmbEquipment);
            AddRow("Select equipment to decommission:", cmbEquipment);

            cmbReason = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
            cmbReason.Items.AddRange(new[] { "Damaged", "Obsolete", "Beyond Repair", "End of Life", "Lost", "Other" });
            cmbReason.SelectedIndex = 0;
            ThemeHelper.StyleComboBox(cmbReason);
            AddRow("Reason:", cmbReason);

            txtNotes = new TextBox { Multiline = true, Height = 60, ScrollBars = ScrollBars.Vertical };
            ThemeHelper.StyleTextBox(txtNotes);
            txtNotes.PlaceholderText = "Additional notes (optional)...";
            AddRow("Notes:", txtNotes);
            y += 28; // extra space for multiline

            btnSave = new Button { Text = "Decommission", Location = new Point(20, y), Size = new Size(170, 36) };
            ThemeHelper.StyleDangerButton(btnSave);
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
            if (cmbEquipment.SelectedValue == null)
            {
                ToastHelper.Show(this, "Please select equipment.", ToastType.Warning);
                return;
            }

            if (MessageBox.Show(
                $"Are you sure you want to decommission '{(cmbEquipment.SelectedItem as Equipment)?.Name}'?\n\nThis cannot be undone.",
                "Confirm Decommission", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                return;

            EquipmentId = (int)cmbEquipment.SelectedValue;
            Reason      = $"{cmbReason.SelectedItem}" + (string.IsNullOrWhiteSpace(txtNotes.Text) ? "" : $" — {txtNotes.Text.Trim()}");
            DialogResult = DialogResult.OK;
        }
    }
}