// Forms/SettingsForm.cs
using Inventory_Management_System.Helpers;
using Inventory_Management_System.Models;
using QRCoder;
using System;
using System.Drawing;
using System.Drawing.Printing;
using System.Linq;
using System.Windows.Forms;

namespace Inventory_Management_System.Forms
{
    public partial class SettingsForm : Form
    {
        private AppSetting? _current;

        public SettingsForm()
        {
            InitializeComponent();
            ApplyTheme();
            LoadSettings();
        }

        private void ApplyTheme()
        {
            ThemeHelper.ApplyFormBackground(this);

            ThemeHelper.StyleLabel(lblSectionTitle,  bold: true, color: ThemeHelper.Accent);
            ThemeHelper.StyleLabel(lblBarcodeSection, bold: true, color: ThemeHelper.Accent);

            ThemeHelper.StyleLabel(lblCompany,     bold: true);
            ThemeHelper.StyleLabel(lblTheme,       bold: true);
            ThemeHelper.StyleLabel(lblServer,      bold: true);
            ThemeHelper.StyleLabel(lblDatabase,    bold: true);
            ThemeHelper.StyleLabel(lblPrinterName, bold: true);
            ThemeHelper.StyleLabel(lblLabelWidth,  bold: true);
            ThemeHelper.StyleLabel(lblLabelHeight, bold: true);

            ThemeHelper.StyleTextBox(txtCompany);
            ThemeHelper.StyleTextBox(txtServer);
            ThemeHelper.StyleTextBox(txtDatabase);
            ThemeHelper.StyleTextBox(txtPrinterName);
            ThemeHelper.StyleTextBox(txtLabelWidth);
            ThemeHelper.StyleTextBox(txtLabelHeight);
            ThemeHelper.StyleComboBox(cmbTheme);

            ThemeHelper.StyleButton(btnSave);
            ThemeHelper.StyleSecondaryButton(btnReset);
            ThemeHelper.StyleButton(btnTestPrint);
        }

        private void LoadSettings()
        {
            try
            {
                using var db = new InventoryDbContext();
                _current = db.AppSettings.FirstOrDefault();

                if (_current == null)
                {
                    _current = new AppSetting
                    {
                        CompanyName   = "Q-Inventory",
                        Theme         = "Light",
                        DbServer      = "localhost",
                        DbName        = "inventory_management",
                        PrinterName   = "",
                        LabelWidthMm  = 62,
                        LabelHeightMm = 29
                    };
                    db.AppSettings.Add(_current);
                    db.SaveChanges();
                }

                txtCompany.Text    = _current.CompanyName;
                txtServer.Text     = _current.DbServer;
                txtDatabase.Text   = _current.DbName;
                txtPrinterName.Text = _current.PrinterName ?? "";
                txtLabelWidth.Text  = (_current.LabelWidthMm ?? 62).ToString();
                txtLabelHeight.Text = (_current.LabelHeightMm ?? 29).ToString();

                cmbTheme.SelectedItem = _current.Theme;
                if (cmbTheme.SelectedItem == null)
                    cmbTheme.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Could not load settings from the database.\n\n" + ex.Message,
                    "Settings Unavailable", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtCompany.Text))
            {
                MessageBox.Show("Company name cannot be empty.", "Validation",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!int.TryParse(txtLabelWidth.Text, out int lw) || lw <= 0 ||
                !int.TryParse(txtLabelHeight.Text, out int lh) || lh <= 0)
            {
                MessageBox.Show("Label width and height must be positive numbers.", "Validation",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                using var db = new InventoryDbContext();
                var entity = db.AppSettings.FirstOrDefault();

                if (entity == null)
                {
                    entity = new AppSetting();
                    db.AppSettings.Add(entity);
                }

                entity.CompanyName   = txtCompany.Text.Trim();
                entity.Theme         = cmbTheme.SelectedItem?.ToString() ?? "Light";
                entity.DbServer      = txtServer.Text.Trim();
                entity.DbName        = txtDatabase.Text.Trim();
                entity.PrinterName   = txtPrinterName.Text.Trim();
                entity.LabelWidthMm  = lw;
                entity.LabelHeightMm = lh;
                entity.UpdatedAt     = DateTime.Now;

                db.SaveChanges();
                _current = entity;

                MessageBox.Show("Settings saved successfully.", "Saved",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to save settings: " + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnReset_Click(object sender, EventArgs e)
        {
            if (_current == null) { LoadSettings(); return; }

            txtCompany.Text     = _current.CompanyName;
            txtServer.Text      = _current.DbServer;
            txtDatabase.Text    = _current.DbName;
            txtPrinterName.Text = _current.PrinterName ?? "";
            txtLabelWidth.Text  = (_current.LabelWidthMm ?? 62).ToString();
            txtLabelHeight.Text = (_current.LabelHeightMm ?? 29).ToString();
            cmbTheme.SelectedItem = _current.Theme;
        }

        private void btnTestPrint_Click(object sender, EventArgs e)
        {
            if (!int.TryParse(txtLabelWidth.Text, out int lw) || lw <= 0)  lw = 62;
            if (!int.TryParse(txtLabelHeight.Text, out int lh) || lh <= 0) lh = 29;

            string printerName = txtPrinterName.Text.Trim();
            string companyName = txtCompany.Text.Trim().Length > 0
                ? txtCompany.Text.Trim() : "Q-Inventory";

            try
            {
                // Generate a sample QR code for the test label
                var generator = new QRCodeGenerator();
                var data      = generator.CreateQrCode("TEST-LABEL-001", QRCodeGenerator.ECCLevel.Q);
                var qr        = new QRCode(data);
                using var qrBitmap = qr.GetGraphic(4);

                var pd = new PrintDocument();

                if (!string.IsNullOrWhiteSpace(printerName))
                    pd.PrinterSettings.PrinterName = printerName;

                // Convert mm to hundredths of an inch (PrintDocument unit)
                int wHundredths = (int)(lw * 3.937);
                int hHundredths = (int)(lh * 3.937);
                pd.DefaultPageSettings.PaperSize =
                    new PaperSize("Label", wHundredths, hHundredths);
                pd.DefaultPageSettings.Margins =
                    new Margins(10, 10, 10, 10);

                pd.PrintPage += (s, ev) =>
                {
                    var g = ev.Graphics!;
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                    var bounds = ev.PageBounds;
                    int qrSize = Math.Min(bounds.Height - 20, 80);

                    // QR code on the left
                    g.DrawImage(qrBitmap,
                        new Rectangle(bounds.X + 5, bounds.Y + (bounds.Height - qrSize) / 2,
                            qrSize, qrSize));

                    // Text on the right
                    int textX = bounds.X + qrSize + 12;
                    int textW = bounds.Width - qrSize - 20;

                    using var boldFont  = new Font("Segoe UI", 8f, FontStyle.Bold);
                    using var smallFont = new Font("Segoe UI", 6.5f);

                    g.DrawString(companyName,    boldFont,  Brushes.Black,
                        new RectangleF(textX, bounds.Y + 6,  textW, 18));
                    g.DrawString("TEST-LABEL-001", boldFont, Brushes.Black,
                        new RectangleF(textX, bounds.Y + 22, textW, 18));
                    g.DrawString("Status: In Store",  smallFont, Brushes.DimGray,
                        new RectangleF(textX, bounds.Y + 38, textW, 16));
                    g.DrawString(DateTime.Now.ToString("yyyy-MM-dd"), smallFont, Brushes.DimGray,
                        new RectangleF(textX, bounds.Y + 52, textW, 16));

                    ev.HasMorePages = false;
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
                MessageBox.Show("Test print failed: " + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}