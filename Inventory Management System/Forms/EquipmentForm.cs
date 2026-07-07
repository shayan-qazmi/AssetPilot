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

namespace Inventory_Management_System.Forms
{
    public partial class EquipmentForm : Form
    {
        private readonly EquipmentService _equipmentService = new();
        private List<Equipment> _allEquipment = new();

        public EquipmentForm()
        {
            InitializeComponent();
            ApplyTheme();
            LoadEquipment();
        }

        private void ApplyTheme()
        {
            ThemeHelper.ApplyFormBackground(this);
            ThemeHelper.StyleToolbarPanel(panelTop);
            ThemeHelper.StyleButton(btnAdd);
            ThemeHelper.StyleButton(btnRefresh);
            ThemeHelper.StyleButton(btnShowQR);
            ThemeHelper.StyleButton(btnImportExcel);
            ThemeHelper.StyleButton(btnSelectAll);
            ThemeHelper.StyleButton(btnGenerateQRSheet);
            ThemeHelper.StyleDangerButton(btnDelete);
            ThemeHelper.StyleSecondaryButton(btnEdit);
            ThemeHelper.StyleSecondaryButton(btnExport);
            ThemeHelper.StyleTextBox(txtSearch);
            ThemeHelper.StyleDataGridView(dgvEquipment);
            panelBottom.BackColor = Color.FromArgb(249, 250, 251);
            lblRowCount.ForeColor = ThemeHelper.TextSecondary;
            lblRowCount.Font      = ThemeHelper.FontSmall;
        }

        private void LoadEquipment(string searchTerm = "")
        {
            ServiceResult<List<Equipment>> result;

            if (string.IsNullOrWhiteSpace(searchTerm))
                result = _equipmentService.GetAll();
            else
                result = _equipmentService.Search(searchTerm);

            if (!result.Success)
            {
                MessageBox.Show(result.ErrorMessage, "Data Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            _allEquipment = result.Data ?? new List<Equipment>();
            RefreshGrid(_allEquipment);
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

                int rowIdx = dgvEquipment.Rows.Add(
                    item.EquipmentCode,
                    item.QrCode,
                    procType,
                    item.Name,
                    item.Make,
                    item.Model,
                    item.SerialNumber,
                    item.Category ?? "",
                    item.CurrentStatus,
                    item.ConditionGrade ?? "",
                    locationName,
                    item.PurchaseDate?.ToString("yyyy-MM-dd") ?? ""
                );

                // Color-code status cell
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

            lblRowCount.Text = $"Showing {dgvEquipment.Rows.Count} records";
        }

        private Equipment? GetSelectedEquipment()
        {
            if (dgvEquipment.SelectedRows.Count == 0) return null;
            var code = dgvEquipment.SelectedRows[0].Cells["Code"].Value?.ToString();
            return _allEquipment.FirstOrDefault(e => e.EquipmentCode == code);
        }

        private List<Equipment> GetSelectedEquipmentList()
        {
            var selected = new List<Equipment>();
            foreach (DataGridViewRow row in dgvEquipment.SelectedRows)
            {
                var code = row.Cells["Code"].Value?.ToString();
                var eq   = _allEquipment.FirstOrDefault(e => e.EquipmentCode == code);
                if (eq != null) selected.Add(eq);
            }
            return selected;
        }

        private void dgvEquipment_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var eq = GetSelectedEquipment();
            if (eq == null) return;
            using var popup = new EquipmentDetailPopup(eq);
            popup.ShowDialog();
        }

        // ── Button handlers ───────────────────────────────────────────────

        private void btnAdd_Click(object sender, EventArgs e)
        {
            using var form = new AddEditEquipmentForm();
            if (form.ShowDialog() == DialogResult.OK)
                LoadEquipment();
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
                LoadEquipment();
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            var eq = GetSelectedEquipment();
            if (eq == null)
            {
                MessageBox.Show("Please select an equipment row to delete.",
                    "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (MessageBox.Show(
                    $"Are you sure you want to delete '{eq.Name}' ({eq.EquipmentCode})?",
                    "Confirm Delete",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning) != DialogResult.Yes)
                return;

            var result = _equipmentService.Delete(eq.Id);
            if (result.Success)
                LoadEquipment();
            else
                MessageBox.Show(result.ErrorMessage, "Delete Failed",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            txtSearch.Text = string.Empty;
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

        private void btnSelectAll_Click(object sender, EventArgs e)
        {
            dgvEquipment.SelectAll();
        }

        private void btnGenerateQRSheet_Click(object sender, EventArgs e)
        {
            var selected = GetSelectedEquipmentList();
            if (selected.Count == 0)
            {
                MessageBox.Show("Please select at least one equipment row.\n\nTip: Use Select All to select everything.",
                    "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Pre-generate all QR bitmaps
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
                catch { /* skip items that fail */ }
            }

            if (qrItems.Count == 0)
            {
                MessageBox.Show("None of the selected items have a valid QR code.",
                    "No QR Codes", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Layout constants (in hundredths of an inch — PrintDocument units)
            // A4 page: 827 x 1169 (hundredths of inch)
            const int pageW       = 827;
            const int pageH       = 1169;
            const int marginL     = 40;
            const int marginT     = 40;
            const int cellW       = 180;  // each QR cell width
            const int cellH       = 200;  // each QR cell height
            const int cols        = 4;    // 4 per row
            const int qrSize      = 120;  // QR image size inside cell

            int currentIndex = 0;

            using var pd = new PrintDocument();
            pd.DefaultPageSettings.PaperSize = new PaperSize("A4", pageW, pageH);
            pd.DefaultPageSettings.Margins   = new Margins(marginL, marginL, marginT, marginT);

            pd.PrintPage += (ps, pe) =>
            {
                var g = pe.Graphics!;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                // Load settings for company name
                string companyName = "Q-Inventory";
                try
                {
                    using var db = new InventoryDbContext();
                    companyName = db.AppSettings.FirstOrDefault()?.CompanyName ?? "Q-Inventory";
                }
                catch { }

                using var codeFont  = new Font("Segoe UI", 7f, FontStyle.Bold);
                using var nameFont  = new Font("Segoe UI", 6f, FontStyle.Regular);
                using var coFont    = new Font("Segoe UI", 5.5f, FontStyle.Regular);

                int col = 0;
                int row = 0;
                int usableW = pageW - marginL * 2;
                int startX  = marginL;
                int startY  = marginT;

                while (currentIndex < qrItems.Count)
                {
                    int x = startX + col * (cellW + 10);
                    int y = startY + row * (cellH + 10);

                    // Stop if we've run off the page
                    if (y + cellH > pageH - marginT)
                    {
                        pe.HasMorePages = true;
                        return;
                    }

                    var (eq, bmp) = qrItems[currentIndex];

                    // Cell border
                    g.DrawRectangle(Pens.LightGray, x, y, cellW, cellH);

                    // QR image centered in cell
                    int qrX = x + (cellW - qrSize) / 2;
                    int qrY = y + 8;
                    g.DrawImage(bmp, new Rectangle(qrX, qrY, qrSize, qrSize));

                    // Company name
                    g.DrawString(companyName, coFont, Brushes.DimGray,
                        new RectangleF(x + 4, qrY + qrSize + 4, cellW - 8, 16));

                    // Equipment code
                    g.DrawString(eq.EquipmentCode, codeFont, Brushes.Black,
                        new RectangleF(x + 4, qrY + qrSize + 18, cellW - 8, 18));

                    // Equipment name (truncate if too long)
                    string name = eq.Name.Length > 22 ? eq.Name[..22] + "…" : eq.Name;
                    g.DrawString(name, nameFont, Brushes.DimGray,
                        new RectangleF(x + 4, qrY + qrSize + 34, cellW - 8, 16));

                    // Status
                    g.DrawString($"Status: {eq.CurrentStatus}", nameFont, Brushes.DimGray,
                        new RectangleF(x + 4, qrY + qrSize + 48, cellW - 8, 16));

                    currentIndex++;
                    col++;
                    if (col >= cols) { col = 0; row++; }
                }

                pe.HasMorePages = false;
            };

            pd.EndPrint += (ps, pe) =>
            {
                foreach (var (_, bmp) in qrItems)
                    bmp.Dispose();
            };

            using var ppd = new PrintPreviewDialog
            {
                Document      = pd,
                Width         = 900,
                Height        = 700,
                StartPosition = FormStartPosition.CenterParent,
                Text          = $"QR Sheet Preview — {qrItems.Count} items"
            };
            ppd.ShowDialog();
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

            var pic = new PictureBox
            {
                Image     = qrBitmap,
                SizeMode  = PictureBoxSizeMode.Zoom,
                Dock      = DockStyle.None,
                Size      = new Size(300, 300),
                Location  = new Point(38, 20),
                BackColor = Color.White
            };

            var lblCode = new Label
            {
                Text      = $"{eq.EquipmentCode}  •  {eq.Name}",
                Font      = ThemeHelper.FontBold,
                ForeColor = ThemeHelper.TextPrimary,
                AutoSize  = false,
                Width     = 340,
                Height    = 24,
                Location  = new Point(18, 330),
                TextAlign = ContentAlignment.MiddleCenter
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
                    using var db      = new InventoryDbContext();
                    var setting       = db.AppSettings.FirstOrDefault();
                    string companyName = setting?.CompanyName ?? "Q-Inventory";
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

                    pd.PrintPage += (ps, pe) =>
                    {
                        var g      = pe.Graphics!;
                        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                        var bounds = pe.PageBounds;
                        int qrSize = Math.Min(bounds.Height - 20, 80);

                        g.DrawImage(qrBitmap,
                            new Rectangle(bounds.X + 5,
                                bounds.Y + (bounds.Height - qrSize) / 2,
                                qrSize, qrSize));

                        int textX = bounds.X + qrSize + 12;
                        int textW = bounds.Width - qrSize - 20;

                        using var boldFont  = new Font("Segoe UI", 8f, FontStyle.Bold);
                        using var smallFont = new Font("Segoe UI", 6.5f);

                        g.DrawString(companyName,     boldFont,  Brushes.Black,
                            new RectangleF(textX, bounds.Y + 6,  textW, 18));
                        g.DrawString(eq.EquipmentCode, boldFont, Brushes.Black,
                            new RectangleF(textX, bounds.Y + 22, textW, 18));
                        g.DrawString($"Status: {eq.CurrentStatus}", smallFont, Brushes.DimGray,
                            new RectangleF(textX, bounds.Y + 38, textW, 16));
                        g.DrawString(DateTime.Now.ToString("yyyy-MM-dd"), smallFont, Brushes.DimGray,
                            new RectangleF(textX, bounds.Y + 52, textW, 16));

                        pe.HasMorePages = false;
                    };

                    using var ppd = new PrintPreviewDialog
                    {
                        Document      = pd,
                        Width         = 600,
                        Height        = 400,
                        StartPosition = FormStartPosition.CenterParent,
                        Text          = "Barcode Label Preview"
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
            try
            {
                items = ExcelHelper.ImportEquipment(ofd.FileName);
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
                MessageBox.Show(
                    "No valid rows found in the Excel file.\n\nMake sure row 1 is a header and data starts from row 2.",
                    "Nothing to Import", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var result = _equipmentService.AddRange(items);
            if (result.Success)
            {
                MessageBox.Show($"✅ {result.Data} of {items.Count} records imported successfully.",
                    "Import Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadEquipment();
            }
            else
            {
                MessageBox.Show($"Import completed with errors:\n\n{result.ErrorMessage}",
                    "Import Partial", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                LoadEquipment();
            }
        }

        private void txtSearch_TextChanged(object sender, EventArgs e)
        {
            LoadEquipment(txtSearch.Text.Trim());
        }
    }
}