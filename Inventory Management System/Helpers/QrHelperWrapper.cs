using System.Drawing;
using Inventory_Management_System.Helpers;

namespace Inventory_Management_System.Helpers
{
    /// <summary>
    /// Preferred QR helper name used by new label‑printing workflow.
    /// Delegates to the existing QRHelper implementation.
    /// </summary>
    public static class QrHelper
    {
        /// <summary>
        /// Generates a QR code bitmap for the supplied data.
        /// </summary>
        /// <param name="data">String to encode.</param>
        /// <param name="pixelsPerModule">Size scaling factor (default 4).</param>
        public static Bitmap GenerateQrBitmap(string data, int pixelsPerModule = 4)
        {
            // Reuse the existing QRHelper which already produces a Bitmap.
            return QRHelper.GenerateQRCode(data, pixelsPerModule);
        }
    }
}
