using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Inventory_Management_System.Helpers
{
    public enum ToastType { Success, Info, Warning, Error }

    public static class ToastHelper
    {
        private const int MaxContentWidth = 380; // text wraps once it would exceed this

        public static void Show(Form hostForm, string message, ToastType type = ToastType.Success, int durationMs = 3000)
        {
            if (hostForm == null || hostForm.IsDisposed) return;

            var (bgColor, icon) = type switch
            {
                ToastType.Success => (Color.FromArgb(46, 125, 50), "✓"),
                ToastType.Info => (Color.FromArgb(33, 150, 243), "ℹ"),
                ToastType.Warning => (Color.FromArgb(245, 158, 11), "⚠"),
                ToastType.Error => (Color.FromArgb(239, 68, 68), "✕"),
                _ => (Color.FromArgb(46, 125, 50), "✓")
            };

            string fullText = $"{icon}  {message}";
            var font = new Font("Segoe UI", 9.5f, FontStyle.Bold);

            // Measure with word-wrap BEFORE creating any controls, so we know
            // the exact size the toast needs to be — this is what actually
            // fixes the cutoff (previously nothing ever wrapped or resized).
            Size measured = TextRenderer.MeasureText(
                fullText, font, new Size(MaxContentWidth, int.MaxValue), TextFormatFlags.WordBreak);

            int toastWidth = Math.Max(180, Math.Min(MaxContentWidth, measured.Width) + 36);
            int toastHeight = Math.Max(48, measured.Height + 26);

            var toast = new Panel
            {
                BackColor = bgColor,
                Size = new Size(toastWidth, toastHeight),
                Visible = true
            };

            var lbl = new Label
            {
                Text = fullText,
                Font = font,
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft,
                Size = new Size(toastWidth - 36, toastHeight - 16),
                Location = new Point(18, 8)
            };
            toast.Controls.Add(lbl);

            toast.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using var path = RoundedRect(new Rectangle(0, 0, toast.Width - 1, toast.Height - 1), 10);
                toast.Region = new Region(path);
                using var pen = new Pen(Color.FromArgb(60, 0, 0, 0), 1f);
                g.DrawPath(pen, path);
            };

            hostForm.Controls.Add(toast);
            toast.BringToFront();

            PositionToast(hostForm, toast);

            var opacity = 0;
            var fadeInTimer = new System.Windows.Forms.Timer { Interval = 15 };
            fadeInTimer.Tick += (s, e) =>
            {
                opacity += 25;
                if (opacity >= 255)
                {
                    fadeInTimer.Stop();
                    fadeInTimer.Dispose();

                    var holdTimer = new System.Windows.Forms.Timer { Interval = durationMs };
                    holdTimer.Tick += (s2, e2) =>
                    {
                        holdTimer.Stop();
                        holdTimer.Dispose();
                        FadeOutAndRemove(hostForm, toast);
                    };
                    holdTimer.Start();
                }
            };
            fadeInTimer.Start();
        }

        private static void FadeOutAndRemove(Form hostForm, Panel toast)
        {
            int step = 0;
            var timer = new System.Windows.Forms.Timer { Interval = 15 };
            var startY = toast.Top;
            timer.Tick += (s, e) =>
            {
                step++;
                toast.Top = startY - step * 2;
                if (step >= 15)
                {
                    timer.Stop();
                    timer.Dispose();
                    if (!hostForm.IsDisposed)
                    {
                        hostForm.Controls.Remove(toast);
                        toast.Dispose();
                    }
                }
            };
            timer.Start();
        }

        private static void PositionToast(Form hostForm, Panel toast)
        {
            const int margin = 24;
            const int spacing = 10;

            int stackOffset = 0;
            foreach (Control c in hostForm.Controls)
            {
                if (c is Panel p && p != toast && p.Tag as string == "toast")
                    stackOffset += p.Height + spacing;
            }

            toast.Tag = "toast";
            toast.Location = new Point(
                hostForm.ClientSize.Width - toast.Width - margin,
                hostForm.ClientSize.Height - toast.Height - margin - stackOffset);
            toast.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        }

        private static GraphicsPath RoundedRect(Rectangle r, int radius)
        {
            var path = new GraphicsPath();
            int d = radius * 2;
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}