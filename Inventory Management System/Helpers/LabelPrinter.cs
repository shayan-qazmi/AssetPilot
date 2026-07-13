using System;
using System.Drawing;
using System.Drawing.Printing;
using Inventory_Management_System.Models;
using Inventory_Management_System.Helpers;

namespace Inventory_Management_System.Helpers
{
    /// <summary>
    /// Handles creation of a PrintDocument for barcode label printing.
    /// Consumes AppSetting values for printer name and label dimensions.
    /// </summary>
    public class LabelPrinter
    {
        private readonly string _printerName;
        private readonly int _labelWidthMm;
        private readonly int _labelHeightMm;

        public LabelPrinter(AppSetting settings)
        {
            _printerName   = settings.PrinterName ?? string.Empty;
            _labelWidthMm  = settings.LabelWidthMm ?? 62; // default size if not set
            _labelHeightMm = settings.LabelHeightMm ?? 29;
        }

        /// <summary>
        /// Prints the supplied QR bitmap with an optional caption.
        /// </summary>
        public void Print(Bitmap qrBitmap, string? caption = null)
        {
            var doc = BuildDocument(qrBitmap, caption);
            doc.Print();
        }

        /// <summary>
        /// Shows a preview dialog before printing.
        /// </summary>
        public void ShowPreview(Bitmap qrBitmap, string? caption = null)
        {
            var doc = BuildDocument(qrBitmap, caption);
            using var preview = new PrintPreviewDialog
            {
                Document = doc,
                Width = 600,
                Height = 400,
                StartPosition = FormStartPosition.CenterParent,
                Text = "Barcode Label Preview"
            };
            preview.ShowDialog();
        }

        private PrintDocument BuildDocument(Bitmap qrBitmap, string? caption)
        {
            var doc = new PrintDocument();
            if (!string.IsNullOrWhiteSpace(_printerName))
                doc.PrinterSettings.PrinterName = _printerName;

            // Convert mm to hundredths of an inch (PrintDocument units).
            int wHundredths = (int)Math.Round(_labelWidthMm * 3.937);
            int hHundredths = (int)Math.Round(_labelHeightMm * 3.937);
            doc.DefaultPageSettings.PaperSize = new PaperSize("Label", wHundredths, hHundredths);
            doc.DefaultPageSettings.Margins = new Margins(5, 5, 5, 5);

            doc.PrintPage += (s, e) =>
            {
                if (e.Graphics is not Graphics g) return;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                // Center QR on the label.
                int qrSize = Math.Min(e.PageBounds.Height - 20, 80);
                int qrX = (e.PageBounds.Width - qrSize) / 2;
                int qrY = 5;
                g.DrawImage(qrBitmap, qrX, qrY, qrSize, qrSize);

                if (!string.IsNullOrWhiteSpace(caption))
                {
                    using var font = new Font("Segoe UI", 7f, FontStyle.Bold);
                    var rect = new RectangleF(5, qrY + qrSize + 2, e.PageBounds.Width - 10, 20);
                    g.DrawString(caption, font, Brushes.Black, rect, new StringFormat { Alignment = StringAlignment.Center });
                }
                e.HasMorePages = false;
            };
            return doc;
        }
    }
}
