using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Inventory_Management_System.Helpers
{
    public class RoundedPanel : Panel
    {
        public int CornerRadius { get; set; } = 8;
        public Color BorderColor { get; set; } = Color.FromArgb(209, 213, 219); // Default gray border
        public int BorderWidth { get; set; } = 1;

        public RoundedPanel()
        {
            DoubleBuffered = true;
            BackColor = Color.FromArgb(238, 241, 245); // default #EEF1F5 background
            Padding = new Padding(1);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            // Do not call base.OnPaint to prevent default rectangle background fill
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            // Define bounds
            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            if (rect.Width <= 0 || rect.Height <= 0) return;

            using (var path = GetRoundRectPath(rect, CornerRadius))
            {
                // Draw background
                using (var brush = new SolidBrush(BackColor))
                {
                    e.Graphics.FillPath(brush, path);
                }

                // Draw border — softened: forced low alpha regardless of incoming color
                // so it always reads as a subtle, modern hairline rather than a hard edge.
                if (BorderWidth > 0)
                {
                    var softColor = Color.FromArgb(90, BorderColor.R, BorderColor.G, BorderColor.B);
                    using (var pen = new Pen(softColor, BorderWidth))
                    {
                        e.Graphics.DrawPath(pen, path);
                    }
                }
            }
        }

        private GraphicsPath GetRoundRectPath(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            float diameter = radius * 2f;
            // Prevent diameter from being larger than rect dimension
            if (diameter > rect.Width) diameter = rect.Width;
            if (diameter > rect.Height) diameter = rect.Height;

            path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
            path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
            path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}