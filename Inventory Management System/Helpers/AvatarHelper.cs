// Helpers/AvatarHelper.cs
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;

namespace Inventory_Management_System.Helpers
{
    // Shared avatar rendering + loading logic used by AddEditUserForm (upload/preview)
    // and UserDetailPopup (read-only display). Keeping this in one place means the
    // circular clipping and default "blank profile" silhouette only need to be
    // implemented once.
    internal static class AvatarHelper
    {
        private static readonly Color PlaceholderBg = Color.FromArgb(224, 224, 224);
        private static readonly Color PlaceholderFg = Color.FromArgb(158, 158, 168);
        private static readonly Color AvatarBorder = Color.FromArgb(210, 210, 210);

        /// Draws a circular avatar inside `bounds`. If `image` is null, draws the
        /// default gray "blank profile" silhouette instead of leaving it empty.
        public static void DrawAvatar(Graphics g, Rectangle bounds, Image? image)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;

            using var clipPath = new GraphicsPath();
            clipPath.AddEllipse(bounds);

            var oldClip = g.Clip;
            g.SetClip(clipPath, CombineMode.Replace);

            if (image != null)
            {
                g.DrawImage(image, bounds);
            }
            else
            {
                using var bg = new SolidBrush(PlaceholderBg);
                g.FillRectangle(bg, bounds);

                using var fg = new SolidBrush(PlaceholderFg);

                // Head
                int headD = (int)(bounds.Width * 0.36);
                var headRect = new Rectangle(
                    bounds.X + (bounds.Width - headD) / 2,
                    bounds.Y + (int)(bounds.Height * 0.18),
                    headD, headD);
                g.FillEllipse(fg, headRect);

                // Shoulders
                int bodyW = (int)(bounds.Width * 0.66);
                int bodyH = (int)(bounds.Height * 0.46);
                var bodyRect = new Rectangle(
                    bounds.X + (bounds.Width - bodyW) / 2,
                    bounds.Bottom - bodyH + (int)(bounds.Height * 0.10),
                    bodyW, bodyH);
                using var bodyPath = new GraphicsPath();
                bodyPath.AddEllipse(bodyRect);
                g.FillPath(fg, bodyPath);
            }

            g.SetClip(oldClip, CombineMode.Replace);

            using var borderPen = new Pen(AvatarBorder, 1.5f);
            g.DrawEllipse(borderPen, bounds.X, bounds.Y, bounds.Width - 1, bounds.Height - 1);
        }

        /// Attempts to load a user's picture from the shared network folder.
        /// Never throws — returns null (caller falls back to the default
        /// placeholder silhouette) if the share is unreachable or the file
        /// is missing, same tolerant behavior as equipment pictures.
        public static Image? TryLoadUserImage(string? storedPicturePath)
        {
            if (string.IsNullOrWhiteSpace(storedPicturePath)) return null;

            try
            {
                NetworkShareHelper.EnsureConnected();
                string fullPath = NetworkPaths.GetUserPictureFullPath(storedPicturePath);
                if (!File.Exists(fullPath)) return null;

                using var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read);
                return Image.FromStream(stream);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "AvatarHelper.TryLoadUserImage");
                return null;
            }
        }
    }
}