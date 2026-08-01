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
            _allItems = result.Data?.Where(e =>
                string.Equals(e.CurrentStatus, "decommissioned", StringComparison.OrdinalIgnoreCase)).ToList() ?? new(); RefreshGrid(filter);
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
            // Show all non-decommissioned equipment that can be decommissioned
            var result = _equipmentService.GetAll();
            if (!result.Success) return;

            var available = result.Data?
                .Where(eq => !string.Equals(eq.CurrentStatus, "decommissioned", StringComparison.OrdinalIgnoreCase))
                .ToList() ?? new();

            if (available.Count == 0)
            {
                MessageBox.Show("No equipment available to decommission.\nAll equipment has already been decommissioned.",
                    "None Available", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var frm = new DecommissionDialog(available);
            if (frm.ShowDialog() != DialogResult.OK) return;

            try
            {
                using var db = new InventoryDbContext();
                var eq = db.Equipment
                    .Include(x => x.Allocations)
                    .FirstOrDefault(x => x.Id == frm.EquipmentId);
                if (eq == null) return;

                string oldStatus = eq.CurrentStatus ?? "unknown";

                // Close any active allocations for this equipment
                var activeAllocations = eq.Allocations
                    .Where(a => a.Status == "approved" || a.Status == "pending")
                    .ToList();
                foreach (var alloc in activeAllocations)
                    alloc.Status = "returned";

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
            dgvDecommission.ClearSelection();
            dgvDecommission.CurrentCell = null;
        }

        private void txtSearch_TextChanged(object sender, EventArgs e)
        {
            RefreshGrid(txtSearch.Text.Trim());
            dgvDecommission.ClearSelection();
            dgvDecommission.CurrentCell = null;
        }

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

    public class DecommissionEquipmentOption
    {
        public int Id { get; set; }
        public string DisplayText { get; set; } = "";
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

        private static readonly Color DangerAccent = Color.FromArgb(211, 47, 47);
        private static readonly Color CardBorder   = Color.FromArgb(224, 224, 224);
        private static readonly Color MutedText    = Color.FromArgb(110, 110, 120);

        public DecommissionDialog(List<Equipment> equipment)
        {
            _equipment = equipment;
            BuildUI();
        }

        private void BuildUI()
        {
            Text            = "Decommission Equipment";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox     = false;
            MinimizeBox     = false;
            StartPosition   = FormStartPosition.CenterParent;
            AutoScroll      = false;
            Font            = new Font("Segoe UI", 9F);
            ThemeHelper.ApplyFormBackground(this);

            const int formWidth = 460;
            const int margin = 28;
            int contentWidth = formWidth - margin * 2;

            // ── Header Banner ──
            var pnlHeader = new Panel { Location = new Point(0, 0), Size = new Size(formWidth, 10), BackColor = DangerAccent };
            Controls.Add(pnlHeader);

            var lblTitle = new Label
            {
                Text      = "🗑  Decommission Equipment",
                Font      = new Font("Segoe UI", 15.5f, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize  = true,
                Location  = new Point(margin, 18)
            };
            pnlHeader.Controls.Add(lblTitle);

            var subtitleFont = new Font("Segoe UI", 9.5f, FontStyle.Regular);
            string subtitleText = "Mark equipment as decommissioned and log the reason";
            var subtitleSize = TextRenderer.MeasureText(
                subtitleText, subtitleFont, new Size(contentWidth, int.MaxValue), TextFormatFlags.WordBreak);
            var lblSubtitle = new Label
            {
                Text      = subtitleText,
                Font      = subtitleFont,
                ForeColor = Color.FromArgb(255, 235, 238),
                AutoSize  = false,
                Size      = new Size(contentWidth, subtitleSize.Height + 4),
                Location  = new Point(margin, lblTitle.Bottom + 8)
            };
            pnlHeader.Controls.Add(lblSubtitle);
            pnlHeader.Height = lblSubtitle.Bottom + 20;

            // ── Body Panel ──
            var body = new Panel { Location = new Point(0, pnlHeader.Height), Size = new Size(formWidth, 10), AutoScroll = false };
            Controls.Add(body);

            // ── Footer Panel ──
            const int footerHeight = 72;
            var pnlFooter = new Panel { Size = new Size(formWidth, footerHeight), BackColor = Color.FromArgb(249, 250, 251) };
            pnlFooter.Controls.Add(new Panel { Location = new Point(0, 0), Size = new Size(formWidth, 1), BackColor = CardBorder });

            btnCancel = new Button { Text = "Cancel", Size = new Size(140, 42) };
            ThemeHelper.StyleSecondaryButton(btnCancel);
            btnCancel.Click += (s, e) => DialogResult = DialogResult.Cancel;

            btnSave = new Button { Text = "🗑  Decommission", Size = new Size(170, 42) };
            ThemeHelper.StyleDangerButton(btnSave);
            btnSave.Click += BtnSave_Click;

            int totalButtonsWidth = btnCancel.Width + 12 + btnSave.Width;
            int buttonsStartX = (formWidth - totalButtonsWidth) / 2;
            btnCancel.Location = new Point(buttonsStartX, 15);
            btnSave.Location   = new Point(buttonsStartX + btnCancel.Width + 12, 15);

            pnlFooter.Controls.Add(btnCancel);
            pnlFooter.Controls.Add(btnSave);
            Controls.Add(pnlFooter);

            int y = 24;

            void SectionHeader(string title)
            {
                body.Controls.Add(new Panel { BackColor = DangerAccent, Size = new Size(4, 22), Location = new Point(margin, y) });
                body.Controls.Add(new Label
                {
                    Text      = title,
                    Font      = new Font("Segoe UI", 12f, FontStyle.Bold),
                    ForeColor = ThemeHelper.TextPrimary,
                    AutoSize  = true,
                    Location  = new Point(margin + 12, y - 1)
                });
                y += 34;
            }

            void FieldLabel(string label, bool required = false)
            {
                body.Controls.Add(new Label
                {
                    Text      = required ? $"{label}  *" : label,
                    Location  = new Point(margin, y),
                    AutoSize  = true,
                    Font      = ThemeHelper.FontBold,
                    ForeColor = ThemeHelper.TextPrimary
                });
                y += 20;
            }

            void PlaceControl(Control ctrl, int height = 30)
            {
                ctrl.Location = new Point(margin, y);
                ctrl.Width    = contentWidth;
                if (ctrl.Height < height) ctrl.Height = height;
                body.Controls.Add(ctrl);
                y += ctrl.Height + 4;
            }

            void Hint(string text)
            {
                var font = new Font("Segoe UI", 7.8f, FontStyle.Italic);
                var sz   = TextRenderer.MeasureText(text, font, new Size(contentWidth, int.MaxValue), TextFormatFlags.WordBreak);
                body.Controls.Add(new Label
                {
                    Text      = text,
                    Font      = font,
                    ForeColor = MutedText,
                    AutoSize  = false,
                    Size      = new Size(contentWidth, sz.Height + 4),
                    Location  = new Point(margin, y)
                });
                y += sz.Height + 4 + 10;
            }

            SectionHeader("🗑  Decommission Details");

            var equipmentOptions = _equipment.Select(e => new DecommissionEquipmentOption
            {
                Id          = e.Id,
                DisplayText = string.IsNullOrWhiteSpace(e.EquipmentCode) ? e.Name : $"{e.EquipmentCode} — {e.Name}"
            }).ToList();

            cmbEquipment = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
            cmbEquipment.DisplayMember = "DisplayText";
            cmbEquipment.ValueMember   = "Id";
            cmbEquipment.DataSource    = equipmentOptions;
            ThemeHelper.StyleComboBox(cmbEquipment);
            FieldLabel("Select Equipment to Decommission", required: true);
            PlaceControl(cmbEquipment);
            Hint("Choose the item you want to mark as decommissioned");

            cmbReason = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
            cmbReason.Items.AddRange(new[] { "Damaged", "Obsolete", "Beyond Repair", "End of Life", "Lost", "Other" });
            cmbReason.SelectedIndex = 0;
            ThemeHelper.StyleComboBox(cmbReason);
            FieldLabel("Reason", required: true);
            PlaceControl(cmbReason);
            Hint("Select the primary reason for decommissioning");

            txtNotes = new TextBox { Multiline = true, Height = 60, ScrollBars = ScrollBars.Vertical };
            ThemeHelper.StyleTextBox(txtNotes);
            txtNotes.PlaceholderText = "Additional notes (optional)...";
            FieldLabel("Notes");
            PlaceControl(txtNotes, height: 60);
            Hint("Add any relevant contextual details or audit references");

            body.Size = new Size(formWidth, y);
            int totalHeight = pnlHeader.Height + body.Height + footerHeight;
            ClientSize = new Size(formWidth, totalHeight);
            pnlFooter.Location = new Point(0, ClientSize.Height - footerHeight);

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

            var selectedOption = cmbEquipment.SelectedItem as DecommissionEquipmentOption;
            string itemName = selectedOption?.DisplayText ?? "this equipment";

            if (MessageBox.Show(
                $"Are you sure you want to decommission '{itemName}'?\n\nThis action will update its status to decommissioned.",
                "Confirm Decommission", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                return;

            EquipmentId  = (int)cmbEquipment.SelectedValue;
            Reason       = $"{cmbReason.SelectedItem}" + (string.IsNullOrWhiteSpace(txtNotes.Text) ? "" : $" — {txtNotes.Text.Trim()}");
            DialogResult = DialogResult.OK;
        }
    }
}