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
using System.Drawing.Drawing2D;

namespace Inventory_Management_System.Forms
{
    public partial class DecommissionForm : Form, IThemableForm
    {
        private readonly EquipmentService _equipmentService = new();
        private readonly AuditService     _audit            = new();
        private List<Equipment>           _allItems         = new();
        private int _currentPage = 1;
        private int _pageSize = 25;
        private int _totalPages = 1;
        private Label lblPageIndicator = null!;
        private Button btnPrevPage = null!;
        private Button btnNextPage = null!;
        private ComboBox cmbPageSize = null!;
        private Button btnRestoreDecomm = null!;


        public DecommissionForm()
        {
            InitializeComponent();
            BuildPaginationControls();
            BuildRestoreButton();
            flowToolbar.SizeChanged += (s, e) => panelTop.Height = flowToolbar.Height;
            ApplyTheme();
            LoadDecommissioned();

            this.Shown += (s, e) =>
            {
                dgvDecommission.ClearSelection();
                dgvDecommission.CurrentCell = null;
                BeginInvoke(new Action(RepositionPagination));
            };
            dgvDecommission.CellDoubleClick += DgvDecommission_CellDoubleClick;
        }

        private void DgvDecommission_CellDoubleClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var row = dgvDecommission.Rows[e.RowIndex];

            using var popup = new DecommissionDetailPopup(
                row.Cells["EquipmentCode"].Value?.ToString() ?? "—",
                row.Cells["EquipmentName"].Value?.ToString() ?? "—",
                row.Cells["Make"].Value?.ToString() ?? "—",
                row.Cells["Model"].Value?.ToString() ?? "—",
                row.Cells["SerialNumber"].Value?.ToString() ?? "—",
                row.Cells["Reason"].Value?.ToString() ?? "—",
                row.Cells["DecommDate"].Value?.ToString() ?? "—");
            popup.ShowDialog(this);
        }


        private void BuildPaginationControls()
        {
            cmbPageSize = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Size = new Size(75, 26) };
            cmbPageSize.Items.AddRange(new object[] { "25", "50", "100", "250", "500", "1000" });
            cmbPageSize.SelectedItem = "25";
            cmbPageSize.SelectedIndexChanged += (s, e) =>
            {
                if (int.TryParse(cmbPageSize.SelectedItem?.ToString(), out int size))
                { _pageSize = size; _currentPage = 1; RefreshGrid(txtSearch.Text.Trim()); }
            };

            btnPrevPage = new Button { Text = "◀ Prev", Size = new Size(90, 26), Font = ThemeHelper.FontSmall };
            btnPrevPage.Click += (s, e) => { if (_currentPage > 1) { _currentPage--; RefreshGrid(txtSearch.Text.Trim()); } };

            lblPageIndicator = new Label { AutoSize = true, TextAlign = ContentAlignment.MiddleCenter };

            btnNextPage = new Button { Text = "Next ▶", Size = new Size(90, 26), Font = ThemeHelper.FontSmall };
            btnNextPage.Click += (s, e) => { if (_currentPage < _totalPages) { _currentPage++; RefreshGrid(txtSearch.Text.Trim()); } };

            lblPageIndicator.ForeColor = ThemeHelper.TextSecondary;
            lblPageIndicator.Font = ThemeHelper.FontSmall;

            panelBottom.Controls.Add(cmbPageSize);
            panelBottom.Controls.Add(btnPrevPage);
            panelBottom.Controls.Add(lblPageIndicator);
            panelBottom.Controls.Add(btnNextPage);

            // Guarantee these sit above anything else already in panelBottom
            // (e.g. lblRowCount) — this was the main reason they could be
            // invisible even though they were correctly positioned.
            cmbPageSize.BringToFront();
            btnPrevPage.BringToFront();
            lblPageIndicator.BringToFront();
            btnNextPage.BringToFront();

            panelBottom.Resize += (s, e) => RepositionPagination();
            RepositionPagination();
        }

        // ── Restore button — sends a decommissioned item straight back to In Store,
        // after a plain Yes/No confirmation. Replaces the old Edit-dialog flow. ──
        private void BuildRestoreButton()
        {
            btnRestoreDecomm = new Button { Text = "↩ Restore", Size = new Size(130, 32) };
            ThemeHelper.StyleButton(btnRestoreDecomm, Color.FromArgb(0, 150, 136));
            btnRestoreDecomm.Click += BtnRestoreDecomm_Click;

            flowToolbar.Controls.Add(btnRestoreDecomm);
            flowToolbar.Controls.SetChildIndex(btnRestoreDecomm, flowToolbar.Controls.IndexOf(btnRefresh));
        }

        private void BtnRestoreDecomm_Click(object? sender, EventArgs e)
        {
            if (dgvDecommission.SelectedRows.Count == 0)
            {
                ToastHelper.Show(this, "Please select an item to restore.", ToastType.Info);
                return;
            }

            string code = dgvDecommission.SelectedRows[0].Cells["EquipmentCode"].Value?.ToString() ?? "";
            var eq = _allItems.FirstOrDefault(x => x.EquipmentCode == code);
            if (eq == null)
            {
                ToastHelper.Show(this, "Could not find the selected equipment.", ToastType.Error);
                return;
            }

            if (MessageBox.Show(
                $"Are you sure you want to set '{eq.Name}' ({eq.EquipmentCode}) back to In Store?",
                "Confirm Restore", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            try
            {
                using var db = new InventoryDbContext();
                var entity = db.Equipment.FirstOrDefault(x => x.Id == eq.Id);
                if (entity == null) { ToastHelper.Show(this, "Equipment not found.", ToastType.Error); return; }

                entity.CurrentStatus = "in_store";
                db.SaveChanges();

                _audit.Log(entity.Id, AuditService.ActionDecommissionUndone,
       "Status: Decommissioned", "Status: In Store | Source Screen: Decommission Screen");

                ToastHelper.Show(this, $"'{entity.Name}' restored to In Store.", ToastType.Success);
                LoadDecommissioned();
            }
            catch (Exception ex)
            {
                ToastHelper.Show(this, "Restore failed: " + ex.Message, ToastType.Error);
            }
        }

        private void RepositionPagination()
        {
            int rightMargin = 12, gap = 8, ctrlY = 3;
            int x = panelBottom.Width - rightMargin;
            x -= btnNextPage.Width; btnNextPage.Location = new Point(x, ctrlY); x -= gap;
            x -= lblPageIndicator.PreferredWidth; lblPageIndicator.Location = new Point(x, ctrlY + 3); x -= gap;
            x -= btnPrevPage.Width; btnPrevPage.Location = new Point(x, ctrlY); x -= gap;
            x -= cmbPageSize.Width; cmbPageSize.Location = new Point(x, ctrlY);
        }



        public void ApplyTheme()
        {
            ThemeHelper.ApplyFormBackground(this);
            ThemeHelper.StyleToolbarPanel(panelTop);
            ThemeHelper.StyleDangerButton(btnDecommission);
            ThemeHelper.StyleButton(btnRefresh, Color.FromArgb(33, 150, 243));
            ThemeHelper.StyleButton(btnRestoreDecomm, Color.FromArgb(0, 150, 136));
            ThemeHelper.StyleSecondaryButton(btnExport);
            ThemeHelper.StyleComboBox(cmbPageSize);
            ThemeHelper.StyleSecondaryButton(btnPrevPage);
            ThemeHelper.StyleSecondaryButton(btnNextPage);
            ApplyRoundedSearchStyle(txtSearch);
            ThemeHelper.StyleDataGridView(dgvDecommission);
            panelBottom.BackColor = ThemeHelper.IsDarkMode ? Color.FromArgb(22, 24, 29) : Color.FromArgb(249, 250, 251);
            lblRowCount.ForeColor = ThemeHelper.TextSecondary;
            lblRowCount.Font = ThemeHelper.FontSmall;

            // Explicit dark-mode override — StyleSecondaryButton/StyleComboBox
            // default to a light control face; force these three to match
            // the dark panelBottom instead of standing out as light boxes.
            if (ThemeHelper.IsDarkMode)
            {
                Color darkFace = Color.FromArgb(36, 39, 46);
                Color darkBorder = Color.FromArgb(70, 74, 82);

                cmbPageSize.BackColor = darkFace;
                cmbPageSize.ForeColor = ThemeHelper.TextPrimary;
                cmbPageSize.FlatStyle = FlatStyle.Flat;

                foreach (var btn in new[] { btnPrevPage, btnNextPage })
                {
                    btn.BackColor = darkFace;
                    btn.ForeColor = ThemeHelper.TextPrimary;
                    btn.FlatStyle = FlatStyle.Flat;
                    btn.FlatAppearance.BorderColor = darkBorder;
                    btn.FlatAppearance.BorderSize = 1;
                    btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(50, 54, 62);
                }
            }

            lblPageIndicator.ForeColor = ThemeHelper.TextSecondary;
            lblPageIndicator.Font = ThemeHelper.FontSmall;
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_CLIENTEDGE = 0x200;

        private static GraphicsPath RoundedRectPath(RectangleF r, float radius)
        {
            var path = new GraphicsPath();
            float d = radius * 2f;
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        private void ApplyRoundedSearchStyle(TextBox txt)
        {
            var parent = txt.Parent;
            if (parent == null) return;

            var originalSize = txt.Size;
            var originalMargin = txt.Margin;
            int idx = parent.Controls.GetChildIndex(txt);
            parent.Controls.Remove(txt);

            var wrapper = new Panel
            {
                Size = originalSize,
                Margin = originalMargin,
                BackColor = Color.White
            };

            Color inputBorder = Color.FromArgb(209, 213, 219);
            Color focusBorder = Color.FromArgb(0x52, 0xA2, 0x3F);
            Color currentBorder = inputBorder;

            void RoundInput(object? s, EventArgs e)
            {
                if (wrapper.Width <= 0 || wrapper.Height <= 0) return;
                using var path = RoundedRectPath(new RectangleF(0, 0, wrapper.Width, wrapper.Height), 8f);
                wrapper.Region = new Region(path);
            }
            wrapper.Resize += RoundInput;

            wrapper.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var path = RoundedRectPath(new RectangleF(0.5f, 0.5f, wrapper.Width - 1, wrapper.Height - 1), 8f);
                using var pen = new Pen(currentBorder, 1f);
                e.Graphics.DrawPath(pen, path);
            };

            var searchIcon = new Label
            {
                Text = "🔍",
                Font = new Font("Segoe UI Emoji", 8.5f),
                ForeColor = ThemeHelper.TextSecondary,
                AutoSize = false,
                Size = new Size(24, wrapper.Height),
                Location = new Point(6, 0),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };

            txt.BorderStyle = BorderStyle.None;
            int exStyle = GetWindowLong(txt.Handle, GWL_EXSTYLE);
            SetWindowLong(txt.Handle, GWL_EXSTYLE, exStyle & ~WS_EX_CLIENTEDGE);
            txt.BackColor = Color.White;
            txt.Location = new Point(30, (wrapper.Height - txt.Height) / 2);
            txt.Width = wrapper.Width - 38;

            wrapper.Controls.Add(searchIcon);
            wrapper.Controls.Add(txt);
            parent.Controls.Add(wrapper);
            parent.Controls.SetChildIndex(wrapper, idx);

            txt.Enter += (s, e) => { currentBorder = focusBorder; wrapper.Invalidate(); };
            txt.Leave += (s, e) => { currentBorder = inputBorder; wrapper.Invalidate(); };

            RoundInput(null, EventArgs.Empty);
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

            var matched = string.IsNullOrWhiteSpace(filter)
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

            _totalPages = Math.Max(1, (int)Math.Ceiling(matched.Count / (double)_pageSize));
            if (_currentPage > _totalPages) _currentPage = _totalPages;

            var pageItems = matched
                .Skip((_currentPage - 1) * _pageSize)
                .Take(_pageSize)
                .ToList();

            foreach (var item in pageItems)
            {
                string reason = "—";
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
                        reason = log.NewValue ?? "—";
                        decommOn = log.CreatedAt?.ToString("yyyy-MM-dd") ?? "—";
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "DecommissionForm.RefreshGrid (Log lookup)");
                }

                int rowIdx = dgvDecommission.Rows.Add(
                    item.EquipmentCode, item.Name, item.Make ?? "—", item.Model ?? "—",
                    item.SerialNumber, reason, decommOn);

                foreach (DataGridViewCell cell in dgvDecommission.Rows[rowIdx].Cells)
                    cell.Style.ForeColor = Color.FromArgb(183, 28, 28);
            }

            lblRowCount.Text = $"Showing {pageItems.Count} of {matched.Count} decommissioned items";
            lblPageIndicator.Text = $"Page {_currentPage} of {_totalPages}";
            btnPrevPage.Enabled = _currentPage > 1;
            btnNextPage.Enabled = _currentPage < _totalPages;
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
                ToastHelper.Show(this,
                    "No equipment available to decommission. All equipment has already been decommissioned.",
                    ToastType.Info);
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

                string activeAllocInfo = activeAllocations.Count > 0
     ? string.Join(", ", activeAllocations.Select(a => $"Alloc #{a.Id} closed"))
     : "No active allocation";

                _audit.Log(eq.Id, AuditService.ActionDecommissionApproved,
                    $"Status: {FormatStatusForLog(oldStatus)} | {activeAllocInfo}",
                    $"Status: Decommissioned | Reason: {frm.Reason} | Source Screen: Decommission Screen");

                LoadDecommissioned();
                ToastHelper.Show(this, $"'{eq.Name}' has been decommissioned.", ToastType.Success);
            }
            catch (Exception ex)
            {
                ToastHelper.Show(this, "Decommission failed: " + ex.Message, ToastType.Error);
            }
        }

        private static string FormatStatusForLog(string s) => s switch
        {
            "in_store" => "In Store",
            "allocated" => "Allocated",
            "under_repair" => "Under Repair",
            _ => s
        };

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
        private ComboBox  cmbCategoryFilter  = null!;
        private TextBox   txtEquipmentSearch = null!;
        private ComboBox  cmbEquipment = null!;
        private ComboBox  cmbReason    = null!;
        private TextBox   txtNotes     = null!;
        private Button    btnSave      = null!;
        private Button    btnCancel    = null!;

        private readonly List<Equipment> _equipment;
        private EntitySuggestHelper<Equipment>? _equipmentSuggest;
        private bool _suppressEquipmentSync = false;

        // Public properties to expose selected equipment and decommission reason
        public int EquipmentId { get; private set; }
        public string Reason { get; private set; } = string.Empty;

        private static readonly Color DangerAccent = Color.FromArgb(211, 47, 47);
        private static Color CardBorder => ThemeHelper.IsDarkMode ? Color.FromArgb(55, 58, 65) : Color.FromArgb(224, 224, 224);
        private static readonly Color MutedText    = Color.FromArgb(110, 110, 120);

        public DecommissionDialog(List<Equipment> equipment)
        {
            _equipment = equipment;
            BuildUI();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _equipmentSuggest?.Dispose();
            }
            base.Dispose(disposing);
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

            const int formWidth = 500;
            const int margin = 28;
            int contentWidth = formWidth - margin * 2;

            // ── Header Banner ──
            var pnlHeader = new Panel { Location = new Point(0, 0), Size = new Size(formWidth, 10), BackColor = DangerAccent };
            Controls.Add(pnlHeader);

            var lblTitle = new Label
            {
                Text      = "🗑  Decommission Equipment",
                Font      = new Font("Segoe UI", 14.5f, FontStyle.Bold),
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
            var pnlFooter = new Panel { Size = new Size(formWidth, footerHeight), BackColor = ThemeHelper.IsDarkMode ? Color.FromArgb(22, 24, 29) : Color.FromArgb(249, 250, 251) };
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

            cmbCategoryFilter = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
            var categoryNames = new CategoryService().GetAll().Data?.Select(c => c.Name) ?? Enumerable.Empty<string>();
            var equipmentCategories = _equipment.Where(e => !string.IsNullOrWhiteSpace(e.Category)).Select(e => e.Category!).Distinct();
            var allCategoryOptions = new List<string> { "All Categories" };
            allCategoryOptions.AddRange(categoryNames.Union(equipmentCategories, StringComparer.OrdinalIgnoreCase).OrderBy(c => c, StringComparer.OrdinalIgnoreCase));
            cmbCategoryFilter.DataSource = allCategoryOptions;
            ThemeHelper.StyleComboBox(cmbCategoryFilter);
            FieldLabel("Filter by Category");
            PlaceControl(cmbCategoryFilter);
            Hint("Narrows the equipment list below — optional");
            cmbCategoryFilter.SelectedIndexChanged += (s, e) => RefreshEquipmentDropdown();

            txtEquipmentSearch = new TextBox { PlaceholderText = "Search by code, name, or serial..." };
            ThemeHelper.StyleTextBox(txtEquipmentSearch);
            FieldLabel("Find Equipment", required: true);
            PlaceControl(txtEquipmentSearch);
            Hint("Type to search, then pick a suggestion");

            cmbEquipment = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
            cmbEquipment.DisplayMember = "DisplayText";
            cmbEquipment.ValueMember   = "Id";
            ThemeHelper.StyleComboBox(cmbEquipment);
            FieldLabel("Select Equipment to Decommission", required: true);
            PlaceControl(cmbEquipment);
            Hint("Choose the item you want to mark as decommissioned");

            cmbEquipment.SelectedIndexChanged += (s, e) =>
            {
                if (_suppressEquipmentSync) return;
                if (cmbEquipment.SelectedValue == null) return;
                int selectedId = (int)cmbEquipment.SelectedValue;
                if (selectedId == 0) return;

                var eq = _equipment.FirstOrDefault(x => x.Id == selectedId);
                if (eq == null) return;
                
                _suppressEquipmentSync = true;
                txtEquipmentSearch.Text = $"{eq.EquipmentCode} — {eq.Name}";
                txtEquipmentSearch.SelectionStart = txtEquipmentSearch.Text.Length;
                _suppressEquipmentSync = false;
            };

            _equipmentSuggest = new EntitySuggestHelper<Equipment>(
                textBox: txtEquipmentSearch,
                hostForm: this,
                onSelected: eq =>
                {
                    cmbEquipment.SelectedValue = eq.Id;
                    txtEquipmentSearch.Text = $"{eq.EquipmentCode} — {eq.Name}";
                    txtEquipmentSearch.SelectionStart = txtEquipmentSearch.Text.Length;
                },
                scoreFn: (eq, term) => Math.Max(
                    FuzzySearchHelper.FuzzyScoreCode(eq.EquipmentCode, term),
                    Math.Max(
                        FuzzySearchHelper.FuzzyScoreCode(eq.QrCode, term),
                        Math.Max(
                            FuzzySearchHelper.FuzzyScore(eq.Name, term),
                            FuzzySearchHelper.FuzzyScoreCode(eq.SerialNumber, term)))),
                line1Selector: eq => $"{eq.EquipmentCode} — {eq.Name}",
                line2Selector: eq => string.IsNullOrWhiteSpace(eq.SerialNumber) ? (eq.Category ?? "—") : $"S/N: {eq.SerialNumber}");

            RefreshEquipmentDropdown();
            // Ensure no equipment is pre-selected
            cmbEquipment.SelectedIndex = 0;
            txtEquipmentSearch.Text = string.Empty;
            // Default category filter to All Categories
            cmbCategoryFilter.SelectedIndex = 0;

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

        private void RefreshEquipmentDropdown()
        {
            string selectedCategory = cmbCategoryFilter?.SelectedItem?.ToString() ?? "All Categories";

            var filtered = selectedCategory == "All Categories"
                ? _equipment
                : _equipment.Where(e => string.Equals(e.Category, selectedCategory, StringComparison.OrdinalIgnoreCase)).ToList();

            var equipmentOptions = new List<DecommissionEquipmentOption>
            {
                new DecommissionEquipmentOption { Id = 0, DisplayText = "-- Select equipment --" }
            };
            equipmentOptions.AddRange(filtered.Select(e => new DecommissionEquipmentOption
            {
                Id          = e.Id,
                DisplayText = string.IsNullOrWhiteSpace(e.EquipmentCode) ? e.Name : $"{e.EquipmentCode} — {e.Name}"
            }));

            _suppressEquipmentSync = true;
            cmbEquipment.DataSource = equipmentOptions;
            _suppressEquipmentSync = false;

            _equipmentSuggest?.LoadItems(filtered);
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            if (cmbEquipment.SelectedValue == null || (int)cmbEquipment.SelectedValue == 0)
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
    public enum DecommissionEditAction { None, Restore, Delete }

    // ── Restore a decommissioned item to In Store, or delete it permanently ──
    public class DecommissionEditDialog : Form
    {
        public DecommissionEditAction SelectedAction { get; private set; } = DecommissionEditAction.None;

        private static readonly Color EditAccent = Color.FromArgb(245, 158, 11);
        private static Color CardBorder => ThemeHelper.IsDarkMode ? Color.FromArgb(55, 58, 65) : Color.FromArgb(224, 224, 224);

        public DecommissionEditDialog(string equipmentCode, string equipmentName)
        {
            BuildUI(equipmentCode, equipmentName);
        }

        private void BuildUI(string equipmentCode, string equipmentName)
        {
            Text = "Edit Decommissioned Item";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            Font = new Font("Segoe UI", 9F);
            ThemeHelper.ApplyFormBackground(this);

            const int formWidth = 420;
            const int margin = 24;
            int contentWidth = formWidth - margin * 2;

            var pnlHeader = new Panel { Location = new Point(0, 0), Size = new Size(formWidth, 10), BackColor = EditAccent };
            Controls.Add(pnlHeader);

            var lblTitle = new Label
            {
                Text = "✏  Edit Decommissioned Item",
                Font = new Font("Segoe UI", 13.5f, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(margin, 16)
            };
            pnlHeader.Controls.Add(lblTitle);

            var subtitleFont = new Font("Segoe UI", 9f, FontStyle.Regular);
            string subtitleText = $"{equipmentCode} — {equipmentName}";
            var subtitleSize = TextRenderer.MeasureText(
                subtitleText, subtitleFont, new Size(contentWidth, int.MaxValue), TextFormatFlags.WordBreak);
            var lblSubtitle = new Label
            {
                Text = subtitleText,
                Font = subtitleFont,
                ForeColor = Color.FromArgb(255, 245, 230),
                AutoSize = false,
                Size = new Size(contentWidth, subtitleSize.Height + 4),
                Location = new Point(margin, lblTitle.Bottom + 6)
            };
            pnlHeader.Controls.Add(lblSubtitle);
            pnlHeader.Height = lblSubtitle.Bottom + 16;

            var body = new Panel { Location = new Point(0, pnlHeader.Height), Size = new Size(formWidth, 10) };
            Controls.Add(body);

            int y = 20;

            body.Controls.Add(new Label
            {
                Text = "Choose an action for this item:",
                Location = new Point(margin, y),
                AutoSize = true,
                Font = ThemeHelper.FontBold,
                ForeColor = ThemeHelper.TextPrimary
            });
            y += 32;

            var btnRestore = new Button
            {
                Text = "↩  Restore to In Store",
                Location = new Point(margin, y),
                Size = new Size(contentWidth, 42)
            };
            ThemeHelper.StyleButton(btnRestore, Color.FromArgb(0x52, 0xA2, 0x3F));
            btnRestore.Click += (s, e) => { SelectedAction = DecommissionEditAction.Restore; DialogResult = DialogResult.OK; };
            body.Controls.Add(btnRestore);
            y += 52;

            var btnDelete = new Button
            {
                Text = "🗑  Delete Permanently",
                Location = new Point(margin, y),
                Size = new Size(contentWidth, 42)
            };
            ThemeHelper.StyleDangerButton(btnDelete);
            btnDelete.Click += (s, e) => { SelectedAction = DecommissionEditAction.Delete; DialogResult = DialogResult.OK; };
            body.Controls.Add(btnDelete);
            y += 60;

            body.Size = new Size(formWidth, y);

            const int footerHeight = 60;
            var pnlFooter = new Panel { Size = new Size(formWidth, footerHeight), BackColor = ThemeHelper.IsDarkMode ? Color.FromArgb(22, 24, 29) : Color.FromArgb(249, 250, 251) };
            pnlFooter.Controls.Add(new Panel { Location = new Point(0, 0), Size = new Size(formWidth, 1), BackColor = CardBorder });

            var btnCancel = new Button { Text = "Cancel", Size = new Size(130, 38) };
            ThemeHelper.StyleSecondaryButton(btnCancel);
            btnCancel.Click += (s, e) => { SelectedAction = DecommissionEditAction.None; DialogResult = DialogResult.Cancel; };
            btnCancel.Location = new Point((formWidth - btnCancel.Width) / 2, 11);
            pnlFooter.Controls.Add(btnCancel);
            Controls.Add(pnlFooter);

            int totalHeight = pnlHeader.Height + body.Height + footerHeight;
            ClientSize = new Size(formWidth, totalHeight);
            pnlFooter.Location = new Point(0, ClientSize.Height - footerHeight);

            CancelButton = btnCancel;
        }
    }
}