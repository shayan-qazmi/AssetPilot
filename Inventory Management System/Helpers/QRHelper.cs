// Helpers/QRHelper.cs
using QRCoder;
using System.Drawing;

namespace Inventory_Management_System.Helpers
{
    public static class QRHelper
    {
        public static Bitmap GenerateQRCode(string content, int pixelsPerModule = 20)
        {
            var generator = new QRCodeGenerator();
            var data      = generator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);
            var qr        = new QRCode(data);
            return qr.GetGraphic(pixelsPerModule);
        }
        // Wrapper method used by label printer workflow
        public static Bitmap GenerateQrBitmap(string data, int pixelsPerModule = 4)
        {
            // Reuse the existing GenerateQRCode implementation
            return GenerateQRCode(data, pixelsPerModule);
        }
    }
}