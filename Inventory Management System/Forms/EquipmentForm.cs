// Forms/EquipmentForm.cs
using Inventory_Management_System.Helpers;
using Inventory_Management_System.Models;
using Inventory_Management_System.Services;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.Linq;
using System.Windows.Forms;
using Color = System.Drawing.Color;
using Size = System.Drawing.Size;


namespace Inventory_Management_System.Forms
{
    public partial class EquipmentForm : Form
    {
        private readonly EquipmentService _equipmentService = new();
        private List<Equipment> _allEquipment = new();

        private int _currentPage = 1;
        private int _pageSize = 25;
        private int _totalPages = 1;

        // ── Programmatic Dropdown Selection Mode (replacing Select button) ──
        private ComboBox cmbSelectMode  = null!;

        // ── Category filter dropdown ───────────────────────────────────────
        private ComboBox cmbCategory    = null!;
        private string   _activeCategory = ""; // empty = All

        public EquipmentForm()
        {
            InitializeComponent();
            ApplyTheme();
            SetupToolbarLayout();
            LoadCategoryFilter();
            LoadEquipment();

            flowToolbar.Layout += FlowToolbar_Layout;

            // Same fix as AllocationForm/TransferForm — Form.Shown fires once,
            // after WinForms' automatic first-row selection, so clearing here
            // reliably wins instead of racing it.
            this.Shown += (s, e) =>
            {
                dgvEquipment.ClearSelection();
                dgvEquipment.CurrentCell = null;
            };
        }

        private bool _updatingToolbarHeight = false;

        private void FlowToolbar_Layout(object? sender, LayoutEventArgs e)
        {
            if (_updatingToolbarHeight) return;

            int needed = flowToolbar.GetPreferredSize(new Size(panelTop.ClientSize.Width, 0)).Height;
            if (panelTop.Height != needed)
            {
                _updatingToolbarHeight = true;
                panelTop.Height = needed;
                _updatingToolbarHeight = false;
            }
        }


        // ── Toolbar layout ─────────────────────────────────────────────────
        // Restores the original designer dimensions for the buttons so text is
        // not truncated, and programmatically replaces the "Select All" button 
        // with a beautifully styled ComboBox (dropdown) in the exact same position.
        private void SetupToolbarLayout()
        {
            // Hide the original btnSelectAll button to replace it with ComboBox
            btnSelectAll.Visible = false;

            // Setup ComboBox at btnSelectAll's scaled location and size
            cmbSelectMode = new ComboBox
            {
                Name = "cmbSelectMode",
                DropDownStyle = ComboBoxStyle.DropDown,
                Text = "Select",
                Size = btnSelectAll.Size,
                Margin = new Padding(4, 4, 0, 0),
                Cursor = Cursors.Hand,
                FlatStyle = FlatStyle.Flat
            };
            
            // Prevent typing in the DropDown
            cmbSelectMode.KeyPress += (s, e) => e.Handled = true;

            cmbSelectMode.Items.Add("Select ✔");
            cmbSelectMode.Items.Add("Select All");

            // Style dropdown to match the green header/accent buttons
            cmbSelectMode.BackColor = ThemeHelper.Accent;
            cmbSelectMode.ForeColor = Color.White;
            cmbSelectMode.Font = ThemeHelper.FontBold;

            // Owner Draw to style the drop-down items cleanly with modern colors
            cmbSelectMode.DrawMode = DrawMode.OwnerDrawFixed;
            cmbSelectMode.DrawItem += (s, e) =>
            {
                if (e.Index < 0) return;
                bool isSelected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;

                using (var brush = new SolidBrush(isSelected ? ThemeHelper.Header : ThemeHelper.Accent))
                {
                    e.Graphics.FillRectangle(brush, e.Bounds);
                }

                string text = cmbSelectMode.Items[e.Index]?.ToString() ?? "";
                using (var brush = new SolidBrush(Color.White))
                {
                    var sf = new StringFormat
                    {
                        LineAlignment = StringAlignment.Center,
                        Alignment = StringAlignment.Near
                    };
                    e.Graphics.DrawString(text, cmbSelectMode.Font, brush, e.Bounds, sf);
                }
                e.DrawFocusRectangle();
            };

            cmbSelectMode.SelectedIndexChanged += cmbSelectMode_SelectedIndexChanged;
            // Insert cmbSelectMode into flowToolbar right after btnImportExcel
            var flow = panelTop.Controls.OfType<FlowLayoutPanel>().First();
            flow.Controls.Add(cmbSelectMode);
            flow.Controls.SetChildIndex(cmbSelectMode, flow.Controls.IndexOf(btnImportExcel) + 1);

            // ── Category filter dropdown (between Select and QR Sheet) ──────
            cmbCategory = new ComboBox
            {
                Name          = "cmbCategory",
                DropDownStyle = ComboBoxStyle.DropDownList,
                Size          = new Size(115, 32),
                Margin        = new Padding(4, 4, 0, 0),
                Cursor        = Cursors.Hand,
                FlatStyle     = FlatStyle.Flat,
                BackColor     = ThemeHelper.Accent,
                ForeColor     = Color.White,
                Font          = ThemeHelper.FontBold,
                DrawMode      = DrawMode.OwnerDrawFixed,
            };

            // Owner-draw to match the Select dropdown style
            cmbCategory.DrawItem += (s, e) =>
            {
                if (e.Index < 0) return;
                bool isHover = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
                using (var brush = new SolidBrush(isHover ? ThemeHelper.Header : ThemeHelper.Accent))
                    e.Graphics.FillRectangle(brush, e.Bounds);
                string text = cmbCategory.Items[e.Index]?.ToString() ?? "";
                using (var brush = new SolidBrush(Color.White))
                {
                    var sf = new StringFormat { LineAlignment = StringAlignment.Center, Alignment = StringAlignment.Near };
                    e.Graphics.DrawString(text, cmbCategory.Font, brush, e.Bounds, sf);
                }
                e.DrawFocusRectangle();
            };

            cmbCategory.SelectedIndexChanged += (s, e) =>
            {
                var selected = cmbCategory.SelectedItem?.ToString() ?? "";
                _activeCategory = selected == "All Categories" ? "" : selected;
                ApplyFilters();
            };
               
            // Insert cmbCategory right after cmbSelectMode in the flow
            flow.Controls.Add(cmbCategory);
            flow.Controls.SetChildIndex(cmbCategory, flow.Controls.IndexOf(cmbSelectMode) + 1);

            // Dynamically append Checkbox Column for manual selection (visibly hidden by default)
            if (!dgvEquipment.Columns.Contains("ColSelect"))
            {
                var chkCol = new DataGridViewCheckBoxColumn
                {
                    Name = "ColSelect",
                    HeaderText = "",
                    Width = 40,
                    ReadOnly = false,
                    Visible = false,
                    AutoSizeMode = DataGridViewAutoSizeColumnMode.None
                };
                dgvEquipment.Columns.Add(chkCol);

                // Configure grid to allow editing the Checkbox column while others are read-only
                dgvEquipment.ReadOnly = false;
                foreach (DataGridViewColumn col in dgvEquipment.Columns)
                {
                    if (col.Name == "ColSelect")
                        col.ReadOnly = false;
                    else
                        col.ReadOnly = true;
                }

                // Immediately commit edits when clicking the checkbox cell so value updates instantly
                dgvEquipment.CurrentCellDirtyStateChanged += (s, ev) =>
                {
                    if (dgvEquipment.CurrentCell is DataGridViewCheckBoxCell)
                        dgvEquipment.CommitEdit(DataGridViewDataErrorContexts.Commit);
                };
            }
        }

        // ── Theme ──────────────────────────────────────────────────────────

        private void ApplyTheme()
        {
            ThemeHelper.ApplyFormBackground(this);
            ThemeHelper.StyleToolbarPanel(panelTop);
            ThemeHelper.StyleButton(btnAdd);
            ThemeHelper.StyleButton(btnRefresh, Color.FromArgb(33, 150, 243));
            ThemeHelper.StyleButton(btnShowQR);
            ThemeHelper.StyleButton(btnImportExcel);
            ThemeHelper.StyleButton(btnSelectAll);
            ThemeHelper.StyleButton(btnGenerateQRSheet);
            ThemeHelper.StyleDangerButton(btnDelete);
            ThemeHelper.StyleButton(btnEdit, Color.FromArgb(245, 158, 11));
            ThemeHelper.StyleSecondaryButton(btnExport);
            ThemeHelper.StyleTextBox(txtSearch);
            ThemeHelper.StyleComboBox(cmbPageSize);
            ThemeHelper.StyleSecondaryButton(btnPrevPage);
            ThemeHelper.StyleSecondaryButton(btnNextPage);
            ThemeHelper.StyleDataGridView(dgvEquipment);
            panelBottom.BackColor = Color.FromArgb(249, 250, 251);
            lblRowCount.ForeColor = ThemeHelper.TextSecondary;
            lblRowCount.Font      = ThemeHelper.FontSmall;
        }

        // ── Data ───────────────────────────────────────────────────────────

        private void LoadEquipment(string searchTerm = "")
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                var result = _equipmentService.GetPage(_currentPage, _pageSize);
                if (!result.Success)
                {
                    MessageBox.Show(result.ErrorMessage, "Data Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                _totalPages = Math.Max(1, result.Data!.TotalPages);
                if (_currentPage > _totalPages) _currentPage = _totalPages;

                _allEquipment = result.Data.Items;
            }
            else
            {
                var result = _equipmentService.Search(searchTerm);
                if (!result.Success)
                {
                    MessageBox.Show(result.ErrorMessage, "Data Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                var fullMatches = result.Data ?? new List<Equipment>();
                _totalPages = Math.Max(1, (int)Math.Ceiling(fullMatches.Count / (double)_pageSize));
                if (_currentPage > _totalPages) _currentPage = _totalPages;

                _allEquipment = fullMatches
                    .Skip((_currentPage - 1) * _pageSize)
                    .Take(_pageSize)
                    .ToList();
            }

            UpdatePaginationControls();
            ApplyFilters();
        }

        private void UpdatePaginationControls()
        {
            lblPageIndicator.Text = $"Page {_currentPage} of {Math.Max(1, _totalPages)}";
            btnPrevPage.Enabled = _currentPage > 1;
            btnNextPage.Enabled = _currentPage < _totalPages;
        }

        // ── Populate category dropdown from distinct DB values ─────────────
        private void LoadCategoryFilter()
        {
            try
            {
                using var db = new InventoryDbContext();
                var dbCategories = db.Equipment
                    .Where(e => e.Category != null && e.Category != "")
                    .Select(e => e.Category!)
                    .Distinct()
                    .ToList();

                // Union: every category from Settings → Categories table, PLUS
                // any legacy category text already on equipment records (even
                // if it's not in the categories table), so nothing that's
                // actually in use ever disappears.
                var categoryNames = new CategoryService().GetAll().Data?
                    .Select(c => c.Name).ToList() ?? new List<string>();

                var allCategories = categoryNames
                    .Union(dbCategories, StringComparer.OrdinalIgnoreCase)
                    .OrderBy(c => c, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                cmbCategory.Items.Clear();
                cmbCategory.Items.Add("All Categories");
                foreach (var cat in allCategories)
                    cmbCategory.Items.Add(cat);

                cmbCategory.SelectedIndex = 0;
            }
            catch
            {
                cmbCategory.Items.Clear();
                cmbCategory.Items.Add("All Categories");
                cmbCategory.SelectedIndex = 0;
            }
        }

        // ── Apply both search-text and category filter to _allEquipment ────
        private void ApplyFilters()
        {
            var filtered = _allEquipment.AsEnumerable();

            if (!string.IsNullOrEmpty(_activeCategory))
                filtered = filtered.Where(e =>
                    string.Equals(e.Category, _activeCategory, StringComparison.OrdinalIgnoreCase));

            RefreshGrid(filtered.ToList());
        }

        private void RefreshGrid(List<Equipment> items)
        {
            dgvEquipment.Rows.Clear();

            foreach (var item in items)
            {
                string locationName = item.Allocations
                    .FirstOrDefault(a => a.Status != null &&
                                        a.Status.Equals("approved", StringComparison.OrdinalIgnoreCase))
                    ?.Location?.LocationName ?? "";

                string procType = item.ProcurementType?.TypeName ?? "";

                string formattedStatus = item.CurrentStatus switch
                {
                    "in_store"       => "In Store",
                    "allocated"      => "Allocated",
                    "decommissioned" => "Decommissioned",
                    "under_repair"   => "Under Repair",
                    _                => item.CurrentStatus
                };

                int rowIdx = dgvEquipment.Rows.Add(
                    item.EquipmentCode,
                    item.QrCode,
                    procType,
                    item.Name,
                    item.Make,
                    item.Model,
                    item.SerialNumber,
                    item.Category ?? "",
                    formattedStatus,
                    item.ConditionGrade ?? "",
                    locationName,
                    item.PurchaseDate?.ToString("yyyy-MM-dd") ?? ""
                );

                var statusCell = dgvEquipment.Rows[rowIdx].Cells["Status"];
                switch (item.CurrentStatus)
                {
                    case "in_store":
                        statusCell.Style.BackColor = ThemeHelper.Success;
                        break;
                    case "allocated":
                        statusCell.Style.BackColor = ThemeHelper.Warning;
                        break;
                    case "under_repair":
                        statusCell.Style.BackColor = Color.FromArgb(255, 112, 67);
                        break;
                    case "decommissioned":
                        statusCell.Style.BackColor = ThemeHelper.Danger;
                        break;
                    default:
                        statusCell.Style.BackColor = ThemeHelper.GridAltRow;
                        break;
                }
                statusCell.Style.ForeColor = Color.White;
                statusCell.Style.Font      = ThemeHelper.FontBold;
            }

            // Maintain automatic full selection when refreshed in Select All mode
            if (cmbSelectMode != null && cmbSelectMode.SelectedItem?.ToString() == "Select All")
            {
                dgvEquipment.MultiSelect = true;
                dgvEquipment.SelectAll();
            }

            lblRowCount.Text = $"Showing {dgvEquipment.Rows.Count} records";
        }

        // ── Selection helpers ──────────────────────────────────────────────

        private Equipment? GetSelectedEquipment()
        {
            if (dgvEquipment.SelectedRows.Count == 0) return null;
            var code = dgvEquipment.SelectedRows[0].Cells["Code"].Value?.ToString();
            return _allEquipment.FirstOrDefault(e => e.EquipmentCode == code);
        }

        private List<Equipment> GetSelectedEquipmentList()
        {
            var selected = new List<Equipment>();

            // In manual check mode, gather only items that are checked
            if (cmbSelectMode != null && cmbSelectMode.SelectedItem?.ToString() == "Select ✔")
            {
                foreach (DataGridViewRow row in dgvEquipment.Rows)
                {
                    var cellValue = row.Cells["ColSelect"].Value;
                    if (cellValue != null && cellValue is bool b && b)
                    {
                        var code = row.Cells["Code"].Value?.ToString();
                        var eq   = _allEquipment.FirstOrDefault(e => e.EquipmentCode == code);
                        if (eq != null) selected.Add(eq);
                    }
                }
            }
            else
            {
                // In all other modes, use normal Grid Selection
                foreach (DataGridViewRow row in dgvEquipment.SelectedRows)
                {
                    var code = row.Cells["Code"].Value?.ToString();
                    var eq   = _allEquipment.FirstOrDefault(e => e.EquipmentCode == code);
                    if (eq != null) selected.Add(eq);
                }
            }

            return selected;
        }

        // ── Grid events ────────────────────────────────────────────────────

        private void dgvEquipment_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var eq = GetSelectedEquipment();
            if (eq == null) return;
            using var popup = new EquipmentDetailPopup(eq);
            popup.ShowDialog();
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            using var form = new AddEditEquipmentForm();
            if (form.ShowDialog() == DialogResult.OK)
            {
                LoadEquipment();
                LoadCategoryFilter();
            }
        }

        private void btnEdit_Click(object sender, EventArgs e)
        {
            var eq = GetSelectedEquipment();
            if (eq == null)
            {
                MessageBox.Show("Please select an equipment row to edit.",
                    "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            using var form = new AddEditEquipmentForm(eq);
            if (form.ShowDialog() == DialogResult.OK)
            {
                LoadEquipment();
                LoadCategoryFilter();
            }
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            var selectedList = GetSelectedEquipmentList();

            if (selectedList.Count == 0)
            {
                MessageBox.Show("Please select at least one equipment row to delete.",
                    "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string confirmMessage = selectedList.Count == 1
                ? $"Are you sure you want to delete '{selectedList[0].Name}' ({selectedList[0].EquipmentCode})?"
                : $"Are you sure you want to delete these {selectedList.Count} items?\n\n" +
                  string.Join("\n", selectedList.Select(eq => $"• {eq.Name} ({eq.EquipmentCode})"));

            if (MessageBox.Show(confirmMessage, "Confirm Delete",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                return;

            var failures = new List<string>();
            int successCount = 0;

            foreach (var eq in selectedList)
            {
                var result = _equipmentService.Delete(eq.Id);
                if (result.Success)
                    successCount++;
                else
                    failures.Add($"{eq.EquipmentCode}: {result.ErrorMessage}");
            }

            LoadEquipment();

            if (failures.Count == 0)
            {
                MessageBox.Show($"{successCount} item(s) deleted successfully.", "Delete Complete",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show(
                    $"{successCount} of {selectedList.Count} items deleted.\n\nFailures:\n" +
                    string.Join("\n", failures),
                    "Delete Complete with Errors", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            txtSearch.Text = string.Empty;
            _currentPage = 1;

            // Reset Select mode back to normal browsing — Refresh doubles as
            // the way to exit checkbox/select-all mode since there's no
            // dedicated "None" option in the dropdown itself.
            if (cmbSelectMode != null)
            {
                cmbSelectMode.SelectedIndex = -1;
                cmbSelectMode.Text = "Select";
            }

            if (dgvEquipment.Columns.Contains("ColSelect"))
                dgvEquipment.Columns["ColSelect"].Visible = false;

            dgvEquipment.MultiSelect = false;
            dgvEquipment.ClearSelection();

            LoadEquipment();
        }

        private void btnExport_Click(object sender, EventArgs e)
        {
            if (_allEquipment.Count == 0)
            {
                MessageBox.Show("No data to export.", "Export",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var sfd = new SaveFileDialog
            {
                Filter   = "Excel Files|*.xlsx",
                Title    = "Export Equipment",
                FileName = $"Equipment_{DateTime.Now:yyyyMMdd_HHmm}.xlsx"
            };

            if (sfd.ShowDialog() != DialogResult.OK) return;

            try
            {
                ExcelHelper.ExportEquipment(_allEquipment, sfd.FileName);
                MessageBox.Show("Equipment exported successfully.", "Export",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Export failed: {ex.Message}", "Export Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Original btnSelectAll click event (kept to avoid potential Designer reference exceptions)
        private void btnSelectAll_Click(object sender, EventArgs e)
        {
            dgvEquipment.MultiSelect = true;
            dgvEquipment.SelectAll();
        }

        // ── Dropdown Selection Mode Handler ────────────────────────────────
        private void cmbSelectMode_SelectedIndexChanged(object? sender, EventArgs e)
        {
            string selectedMode = cmbSelectMode.SelectedItem?.ToString() ?? "";

            if (selectedMode == "Select ✔")
            {
                // Enter checkbox mode
                dgvEquipment.MultiSelect = false;
                dgvEquipment.ClearSelection();

                if (dgvEquipment.Columns.Contains("ColSelect"))
                {
                    dgvEquipment.Columns["ColSelect"].Visible = true;
                    dgvEquipment.Columns["ColSelect"].DisplayIndex = 0;
                }
            }
            else if (selectedMode == "Select All")
            {
                // Select all rows immediately
                if (dgvEquipment.Columns.Contains("ColSelect"))
                {
                    dgvEquipment.Columns["ColSelect"].Visible = false;
                }

                dgvEquipment.MultiSelect = true;
                dgvEquipment.SelectAll();
            }
            else
            {
                // Neutral default state ("Select ✔/ All ")
                if (dgvEquipment.Columns.Contains("ColSelect"))
                {
                    dgvEquipment.Columns["ColSelect"].Visible = false;
                }

                dgvEquipment.MultiSelect = false;
                dgvEquipment.ClearSelection();
            }
        }

        private void btnGenerateQRSheet_Click(object sender, EventArgs e)
        {
            var selected = GetSelectedEquipmentList();
            if (selected.Count == 0)
            {
                MessageBox.Show("Please select at least one equipment item.",
                    "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var qrItems = new List<(Equipment eq, Bitmap bmp)>();
            foreach (var eq in selected)
            {
                string qrContent = eq.QrCode ?? eq.EquipmentCode;
                if (string.IsNullOrWhiteSpace(qrContent)) continue;
                try
                {
                    var bmp = QRHelper.GenerateQRCode(qrContent, 6);
                    qrItems.Add((eq, bmp));
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "EquipmentForm.btnPrintSelectedQR_Click (GenerateQRCode)");
                }
            }

            if (qrItems.Count == 0)
            {
                MessageBox.Show("None of the selected items have a valid QR code.",
                    "No QR Codes", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            const int pageW = 827;
            const int pageH = 1169;
            const int marginL = 40;
            const int marginT = 40;
            const int cellW = 180;
            const int cellH = 200;
            const int cols = 4;
            const int qrSize = 120;

            int currentIndex = 0;

            using var pd = new PrintDocument();
            pd.DefaultPageSettings.PaperSize = new PaperSize("A4", pageW, pageH);
            pd.DefaultPageSettings.Margins = new Margins(marginL, marginL, marginT, marginT);

            pd.BeginPrint += (ps, pe) =>
            {
                currentIndex = 0;
            };

            pd.PrintPage += (ps, pe) =>
            {
                var g = pe.Graphics!;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                string companyName = "Q-Inventory";
                try
                {
                    using var db = new InventoryDbContext();
                    companyName = db.AppSettings.FirstOrDefault()?.CompanyName ?? "Q-Inventory";
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "EquipmentForm.PrintPage (AppSettings query)");
                }

                using var codeFont = new Font("Segoe UI", 7f, FontStyle.Bold);
                using var nameFont = new Font("Segoe UI", 6f, FontStyle.Regular);
                using var coFont = new Font("Segoe UI", 5.5f, FontStyle.Regular);

                int col = 0;
                int row = 0;
                int startX = marginL;
                int startY = marginT;

                while (currentIndex < qrItems.Count)
                {
                    int x = startX + col * (cellW + 10);
                    int y = startY + row * (cellH + 10);

                    if (y + cellH > pageH - marginT)
                    {
                        pe.HasMorePages = true;
                        return;
                    }

                    var (eq, bmp) = qrItems[currentIndex];

                    g.DrawRectangle(Pens.LightGray, x, y, cellW, cellH);

                    int qrX = x + (cellW - qrSize) / 2;
                    int qrY = y + 8;
                    g.DrawImage(bmp, new Rectangle(qrX, qrY, qrSize, qrSize));

                    g.DrawString(companyName, coFont, Brushes.DimGray,
                        new RectangleF(x + 4, qrY + qrSize + 4, cellW - 8, 16));
                    g.DrawString(eq.EquipmentCode, codeFont, Brushes.Black,
                        new RectangleF(x + 4, qrY + qrSize + 18, cellW - 8, 18));

                    string name = eq.Name.Length > 22 ? eq.Name[..22] + "…" : eq.Name;
                    g.DrawString(name, nameFont, Brushes.DimGray,
                        new RectangleF(x + 4, qrY + qrSize + 34, cellW - 8, 16));
                    g.DrawString($"Status: {eq.CurrentStatus}", nameFont, Brushes.DimGray,
                        new RectangleF(x + 4, qrY + qrSize + 48, cellW - 8, 16));

                    currentIndex++;
                    col++;
                    if (col >= cols) { col = 0; row++; }
                }

                pe.HasMorePages = false;
            };

            try
            {
                using var ppd = new PrintPreviewDialog
                {
                    Document = pd,
                    Width = 900,
                    Height = 700,
                    StartPosition = FormStartPosition.CenterParent,
                    Text = $"QR Sheet Preview — {qrItems.Count} items"
                };
                ppd.ShowDialog();
            }
            catch (System.Drawing.Printing.InvalidPrinterException)
            {
                MessageBox.Show(
                    "No printer is installed on this machine, so a print preview can't be shown.\n\n" +
                    "Install a printer (a virtual PDF printer works fine for testing) and try again.",
                    "No Printer Found", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally
            {
                foreach (var (_, bmp) in qrItems)
                    bmp.Dispose();
            }
        }

        private void btnShowQR_Click(object sender, EventArgs e)
        {
            var eq = GetSelectedEquipment();
            if (eq == null)
            {
                MessageBox.Show("Please select an equipment row first.",
                    "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string qrContent = eq.QrCode ?? eq.EquipmentCode;
            if (string.IsNullOrWhiteSpace(qrContent))
            {
                MessageBox.Show("This equipment has no QR code assigned.",
                    "No QR Code", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            Bitmap? qrBitmap = null;
            try
            {
                qrBitmap = QRHelper.GenerateQRCode(qrContent, 10);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not generate QR code: {ex.Message}\n\nMake sure the QRCoder NuGet package is installed.",
                    "QR Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Fetch company name once, used for the details block below the QR.
            string companyName = "Q-Inventory";
            try
            {
                using var db0 = new InventoryDbContext();
                companyName = db0.AppSettings.FirstOrDefault()?.CompanyName ?? "Q-Inventory";
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "EquipmentForm.ShowSingleQRPreview (AppSettings query)");
            }

            var frm = new Form
            {
                Text            = $"QR Code — {eq.EquipmentCode}",
                Size            = new Size(380, 460),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox     = false,
                MinimizeBox     = false,
                StartPosition   = FormStartPosition.CenterParent,
                BackColor       = Color.White
            };

            // ── QR image (slightly smaller to leave room for details below) ────
            var pic = new PictureBox
            {
                Image     = qrBitmap,
                SizeMode  = PictureBoxSizeMode.Zoom,
                Size      = new Size(240, 240),
                Location  = new Point((380 - 240) / 2, 18),
                BackColor = Color.White
            };

            var lblCode = new Label
            {
                Text      = $"{eq.EquipmentCode}  •  {eq.Name}",
                Font      = ThemeHelper.FontBold,
                ForeColor = ThemeHelper.TextPrimary,
                AutoSize  = false,
                Width     = 344,
                Height    = 22,
                Location  = new Point(18, 264),
                TextAlign = ContentAlignment.MiddleCenter
            };

            // ── Additional equipment details (moved here from the print popup) ─
            using var smallFont = new Font("Segoe UI", 8f, FontStyle.Regular);
            var lblDetails = new Label
            {
                Text      = $"Company: {companyName}\n" +
                            $"Status: {eq.CurrentStatus}\n" +
                            $"Category: {(string.IsNullOrWhiteSpace(eq.Category) ? "—" : eq.Category)}\n" +
                            $"Purchase Date: {eq.PurchaseDate?.ToString("yyyy-MM-dd") ?? "—"}",
                Font      = smallFont,
                ForeColor = Color.DimGray,
                AutoSize  = false,
                Width     = 344,
                Height    = 66,
                Location  = new Point(18, 288),
                TextAlign = ContentAlignment.TopCenter
            };

            var btnPrint = new Button
            {
                Text      = "🖨  Print QR",
                Size      = new Size(160, 38),
                Location  = new Point(38, 368),
                BackColor = ThemeHelper.Accent,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font      = ThemeHelper.FontBold,
                Cursor    = Cursors.Hand
            };
            btnPrint.FlatAppearance.BorderSize = 0;
            btnPrint.Click += (s, ev) =>
            {
                try
                {
                    using var db       = new InventoryDbContext();
                    var setting        = db.AppSettings.FirstOrDefault();
                    string printerName = setting?.PrinterName ?? "";
                    int lw = setting?.LabelWidthMm  ?? 62;
                    int lh = setting?.LabelHeightMm ?? 29;

                    using var pd = new PrintDocument();
                    if (!string.IsNullOrWhiteSpace(printerName))
                        pd.PrinterSettings.PrinterName = printerName;

                    int wHundredths = (int)(lw * 3.937);
                    int hHundredths = (int)(lh * 3.937);
                    pd.DefaultPageSettings.PaperSize =
                        new PaperSize("Label", wHundredths, hHundredths);
                    pd.DefaultPageSettings.Margins = new Margins(10, 10, 10, 10);

                    // Print ONLY the QR code — no text, no equipment details.
                    pd.PrintPage += (ps, pe) =>
                    {
                        var g      = pe.Graphics!;
                        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                        var bounds = pe.PageBounds;
                        int qrSz   = Math.Min(bounds.Width - 10, bounds.Height - 10);

                        g.DrawImage(qrBitmap,
                            new Rectangle(
                                bounds.X + (bounds.Width  - qrSz) / 2,
                                bounds.Y + (bounds.Height - qrSz) / 2,
                                qrSz, qrSz));

                        pe.HasMorePages = false;
                    };

                    using var ppd = new PrintPreviewDialog
                    {
                        Document      = pd,
                        Width         = 600,
                        Height        = 400,
                        StartPosition = FormStartPosition.CenterParent,
                        Text          = "QR Code Print Preview"
                    };
                    ppd.ShowDialog();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Printing failed: " + ex.Message, "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            var btnClose = new Button
            {
                Text      = "Close",
                Size      = new Size(120, 38),
                Location  = new Point(210, 368),
                FlatStyle = FlatStyle.Flat,
                Font      = ThemeHelper.FontDefault,
                Cursor    = Cursors.Hand
            };
            btnClose.Click += (s, ev) => frm.Close();

            frm.Controls.Add(pic);
            frm.Controls.Add(lblCode);
            frm.Controls.Add(lblDetails);
            frm.Controls.Add(btnPrint);
            frm.Controls.Add(btnClose);
            frm.FormClosed += (s, ev) => qrBitmap?.Dispose();
            frm.ShowDialog();
        }

        private void btnImportExcel_Click(object sender, EventArgs e)
        {
            using var ofd = new OpenFileDialog
            {
                Filter = "Excel Files|*.xlsx;*.xls",
                Title  = "Select Excel file to import"
            };

            if (ofd.ShowDialog() != DialogResult.OK) return;

            List<Equipment> items;
            List<string>    parseErrors;
            try
            {
                (items, parseErrors) = ExcelHelper.ImportEquipment(ofd.FileName, _equipmentService);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Could not read file: {ex.Message}\n\nMake sure ClosedXML NuGet package is installed.",
                    "Import Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (items.Count == 0)
            {
                string detail = parseErrors.Count > 0
                    ? "\n\nRow-level issues:\n" + string.Join("\n", parseErrors.Take(10))
                    : "\n\nMake sure row 1 has header columns 'name' and 'serial_number', and data starts from row 2.";
                MessageBox.Show("No valid rows found in the Excel file." + detail,
                    "Nothing to Import", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var result = _equipmentService.AddRange(items);

            // Combine service-level error text with any Excel parse warnings
            var allErrors = new List<string>();
            if (!string.IsNullOrWhiteSpace(result.ErrorMessage))
                allErrors.Add(result.ErrorMessage);
            allErrors.AddRange(parseErrors);

            if (allErrors.Count == 0)
            {
                MessageBox.Show($"✅ {result.Data} of {items.Count} records imported successfully.",
                    "Import Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show(
                    $"{result.Data} of {items.Count} records imported.\n\nDetails:\n" +
                    string.Join("\n", allErrors.Take(20)),
                    "Import Complete", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            LoadEquipment();
        }

        private void txtSearch_TextChanged(object sender, EventArgs e)
        {
            _currentPage = 1;
            LoadEquipment(txtSearch.Text.Trim());
        }

        private void cmbPageSize_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (int.TryParse(cmbPageSize.SelectedItem?.ToString(), out int size))
            {
                _pageSize = size;
                _currentPage = 1;
                LoadEquipment(txtSearch.Text.Trim());
            }
        }

        private void btnPrevPage_Click(object sender, EventArgs e)
        {
            if (_currentPage > 1)
            {
                _currentPage--;
                LoadEquipment(txtSearch.Text.Trim());
            }
        }
  

        private void btnNextPage_Click(object sender, EventArgs e)
        {
            if (_currentPage < _totalPages)
            {
                _currentPage++;
                LoadEquipment(txtSearch.Text.Trim());
            }
        }
    }
}