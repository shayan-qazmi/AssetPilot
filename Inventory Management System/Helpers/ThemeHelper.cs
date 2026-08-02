// Helpers/ThemeHelper.cs
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Collections.Generic;

namespace Inventory_Management_System.Helpers
{
    public static class ThemeHelper
    {
        // ── Color Palette ──────────────────────────────────────────────────
        public static readonly Color Sidebar       = Color.FromArgb(26, 46, 26);
        public static readonly Color Accent        = Color.FromArgb(56, 142, 60);
        public static readonly Color ContentBg     = Color.White;
        public static readonly Color Success       = Color.FromArgb(76, 175, 80);
        public static readonly Color Warning       = Color.FromArgb(245, 158, 11);
        public static readonly Color Danger        = Color.FromArgb(239, 68, 68);
        public static readonly Color SidebarText   = Color.FromArgb(168, 197, 168);
        public static readonly Color GridAltRow    = Color.FromArgb(241, 248, 233);
        public static readonly Color GridHeader    = Color.FromArgb(46, 125, 50);
        public static readonly Color Header        = Color.FromArgb(46, 125, 50);
        public static readonly Color TextPrimary   = Color.FromArgb(17, 24, 39);
        public static readonly Color TextSecondary = Color.FromArgb(107, 114, 128);
        public static readonly Color InputBorder   = Color.FromArgb(209, 213, 219);
        public static readonly Color CardBg        = Color.White;
        public static readonly Color CardBorder    = Color.FromArgb(229, 231, 235);
        public static bool UseGlobalAccentIndicator = false;
        // ── Fonts ─────────────────────────────────────────────────────────
        public static readonly Font FontDefault  = new Font("Segoe UI", 9.5f,  FontStyle.Regular, GraphicsUnit.Point);
        public static readonly Font FontBold     = new Font("Segoe UI", 9.5f,  FontStyle.Bold,    GraphicsUnit.Point);
        public static readonly Font FontSmall    = new Font("Segoe UI", 8.5f,  FontStyle.Regular, GraphicsUnit.Point);
        public static readonly Font FontLarge    = new Font("Segoe UI", 14f,   FontStyle.Bold,    GraphicsUnit.Point);
        public static readonly Font FontTitle    = new Font("Segoe UI", 16f,   FontStyle.Bold,    GraphicsUnit.Point);
        public static readonly Font FontCardNum  = new Font("Segoe UI", 10f,   FontStyle.Bold,    GraphicsUnit.Point);
        public static readonly Font FontCardLbl  = new Font("Segoe UI", 10f,   FontStyle.Regular, GraphicsUnit.Point);
        public static readonly Font FontCardInfo = new Font("Segoe UI", 8.5f,  FontStyle.Regular, GraphicsUnit.Point);
        public static readonly Font FontSidebar  = new Font("Segoe UI", 9.5f,  FontStyle.Regular, GraphicsUnit.Point);
        public static readonly Font FontSidebarB = new Font("Segoe UI", 9.5f,  FontStyle.Bold,    GraphicsUnit.Point);

        // ── StyleButton ───────────────────────────────────────────────────
        public static void StyleButton(Button btn, Color? color = null)
        {
            var baseColor = color ?? Accent;
            btn.BackColor = baseColor;
            btn.ForeColor = Color.White;
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize         = 0;
            btn.FlatAppearance.MouseOverBackColor = baseColor == Accent ? Header : ControlPaint.Light(baseColor, 0.15f);
            btn.FlatAppearance.MouseDownBackColor = ControlPaint.Dark(baseColor, 0.1f);
            btn.Font      = FontBold;
            btn.Cursor    = Cursors.Hand;
            btn.UseVisualStyleBackColor = false;
        }

        public static void StyleDangerButton(Button btn) => StyleButton(btn, Danger);

        public static void StyleSecondaryButton(Button btn)
        {
            btn.BackColor = Color.FromArgb(243, 244, 246);
            btn.ForeColor = TextPrimary;
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize         = 1;
            btn.FlatAppearance.BorderColor        = InputBorder;
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(229, 231, 235);
            btn.Font      = FontDefault;
            btn.Cursor    = Cursors.Hand;
            btn.UseVisualStyleBackColor = false;
        }

        // ── Sidebar button ─────────────────────────────────────────────────
        public static void StyleSidebarButton(Button btn, bool active = false)
        {
            btn.Tag       = active;
            btn.BackColor = active ? Color.FromArgb(45, 90, 45) : Sidebar;
            btn.ForeColor = active ? Color.White : SidebarText;
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize         = 0;
            // Disabled — our own MouseEnter/MouseLeave handlers (FadeButtonColor) already
            // drive the hover visual. Leaving these active created two competing hover
            // systems: WinForms' native FlatAppearance overlay could get stuck "on" when
            // the mouse moved quickly across buttons whose Region had been custom-clipped,
            // since our fade code updates BackColor but never touches this separate overlay.
            btn.FlatAppearance.MouseOverBackColor = btn.BackColor;
            btn.FlatAppearance.MouseDownBackColor = btn.BackColor;      
            btn.Font      = active ? FontSidebarB : FontSidebar;
            btn.TextAlign = ContentAlignment.MiddleLeft;
            // Indent text to make room for the left accent bar
            btn.Padding   = active
                ? new Padding(22, 0, 0, 0)   // extra left pad when active (accent bar = 4px)
                : new Padding(16, 0, 0, 0);
            btn.Cursor    = Cursors.Hand;
            btn.UseVisualStyleBackColor = false;

            // Detach old handlers before re-attaching
            btn.MouseEnter -= SidebarBtn_MouseEnter;
            btn.MouseLeave -= SidebarBtn_MouseLeave;
            btn.Paint      -= SidebarBtn_PaintAccent;
            btn.Resize     -= SidebarBtn_Resize;

            btn.MouseEnter += SidebarBtn_MouseEnter;
            btn.MouseLeave += SidebarBtn_MouseLeave;

            if (active)
            {
                // Pill shape — elevated (slightly bigger, overshoots edge)
                ApplyPillRegion(btn, elevated: true);
                btn.Resize += SidebarBtn_Resize;

                // Left accent bar drawn via Paint
                if (!UseGlobalAccentIndicator)
                    btn.Paint += SidebarBtn_PaintAccent;
            }
            else
            {
                btn.Region = null;
            }
        }

private static void SidebarBtn_Resize(object? sender, EventArgs e)
{
    if (sender is Button btn)
    {
        bool isActive = btn.Tag is bool a && a;
        ApplyPillRegion(btn, elevated: isActive);
    }
}

private static void SidebarBtn_PaintAccent(object? sender, PaintEventArgs e)
        {
            if (sender is not Button btn) return;
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // Left accent bar — bright green pill shape
            int barW = 4;
            int barH = btn.Height - 16;
            int barY = 8;
            using var barBrush = new SolidBrush(Color.FromArgb(144, 238, 144));
            using var barPath  = new GraphicsPath();
            int r = barW;
            barPath.AddArc(6, barY,           r, r, 180, 90);
            barPath.AddArc(6, barY,           r, r, 270, 90);
            barPath.AddArc(6, barY + barH - r, r, r, 0,   90);
            barPath.AddArc(6, barY + barH - r, r, r, 90,  90);
            barPath.CloseFigure();
            g.FillRectangle(barBrush, 6, barY, barW, barH);
        }

      

private static void ApplyPillRegion(Button btn, bool elevated = false)
{
    if (btn.Width <= 0 || btn.Height <= 0) return;

    // Clear previously painted pixels before the region changes —
    // prevents ghost trails when the mouse moves quickly across buttons.
    btn.Parent?.Invalidate(btn.Bounds, true);

    int padL = elevated ? 4 : 6;
    int padT = elevated ? 2 : 4;
    int bleed = elevated ? 4 : 0; // slight overshoot so active item reads as "raised"

    var rect = new Rectangle(padL, padT, btn.Width - padL - 1 + bleed, btn.Height - padT * 2);

    var path = new GraphicsPath();
    int radius = 10;
    path.AddArc(rect.X, rect.Y, radius, radius, 180, 90);
    path.AddArc(rect.Right - radius, rect.Y, radius, radius, 270, 90);
    path.AddArc(rect.Right - radius, rect.Bottom - radius, radius, radius, 0, 90);
    path.AddArc(rect.X, rect.Bottom - radius, radius, radius, 90, 90);
    path.CloseFigure();
    btn.Region = new Region(path);
    btn.Invalidate();
}
private static void SidebarBtn_MouseEnter(object? sender, EventArgs e)
        {
            if (sender is Button btn)
            {
                bool isActive = btn.Tag is bool a && a;
                if (isActive)
                {
                    btn.BackColor = Color.FromArgb(45, 90, 45);
                }
                else
                {
                    ApplyPillRegion(btn);
                    FadeButtonColor(btn, btn.BackColor, ControlPaint.Light(Sidebar, 0.12f));
                }
            }
        }

        private static void SidebarBtn_MouseLeave(object? sender, EventArgs e)
        {
            if (sender is Button btn)
            {
                bool isActive = btn.Tag is bool a && a;
                if (!isActive)
                {
                    FadeButtonColor(btn, btn.BackColor, Sidebar, onComplete: () =>
                    {
                        btn.Parent?.Invalidate(btn.Bounds, true);
                        btn.Region = null;
                        btn.Invalidate();
                    });
                }
                else
                {
                    btn.BackColor = Color.FromArgb(45, 90, 45);
                    ApplyPillRegion(btn, elevated: true);
                }
            }
        }

        private static readonly Dictionary<Button, System.Windows.Forms.Timer> _fadeTimers = new();

        private static void FadeButtonColor(Button btn, Color from, Color to, Action? onComplete = null)
        {
            // Cancel any fade already in progress for this button — otherwise fast
            // mouse movement (Enter then Leave in quick succession) starts two
            // timers racing on the same button, and whichever one's last tick fires
            // wins, freezing the button on an intermediate color until re-hovered.
            if (_fadeTimers.TryGetValue(btn, out var existingTimer))
            {
                existingTimer.Stop();
                existingTimer.Dispose();
                _fadeTimers.Remove(btn);
            }

            const int steps = 8;
            const int intervalMs = 12;
            int step = 0;

            var timer = new System.Windows.Forms.Timer { Interval = intervalMs };
            timer.Tick += (s, e) =>
            {
                step++;
                float t = Math.Min(1f, step / (float)steps);
                btn.BackColor = Color.FromArgb(
                    (int)(from.R + (to.R - from.R) * t),
                    (int)(from.G + (to.G - from.G) * t),
                    (int)(from.B + (to.B - from.B) * t));

                if (t >= 1f)
                {
                    timer.Stop();
                    timer.Dispose();
                    _fadeTimers.Remove(btn);
                    onComplete?.Invoke();
                }
            };
            _fadeTimers[btn] = timer;
            timer.Start();
        }

        // ── DataGridView ───────────────────────────────────────────────────
        public static void StyleDataGridView(DataGridView dgv)
        {
            dgv.BackgroundColor = ContentBg;
            dgv.BorderStyle     = BorderStyle.None;
            dgv.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            dgv.GridColor       = Color.FromArgb(234, 236, 240);
            dgv.RowHeadersVisible   = false;
            dgv.ColumnHeadersHeight = 40;
            dgv.RowTemplate.Height  = 36;
            dgv.Font                = FontDefault;
            dgv.DefaultCellStyle.Font          = FontDefault;
            dgv.DefaultCellStyle.ForeColor     = TextPrimary;
            dgv.DefaultCellStyle.BackColor     = ContentBg;
            dgv.DefaultCellStyle.SelectionBackColor = Color.FromArgb(224, 231, 255);
            dgv.DefaultCellStyle.SelectionForeColor = TextPrimary;
            dgv.DefaultCellStyle.Padding       = new Padding(4, 0, 4, 0);
            dgv.AlternatingRowsDefaultCellStyle.BackColor = GridAltRow;
            dgv.AlternatingRowsDefaultCellStyle.Font      = FontDefault;
            dgv.AlternatingRowsDefaultCellStyle.ForeColor = TextPrimary;
            dgv.ColumnHeadersDefaultCellStyle.BackColor   = GridHeader;
            dgv.ColumnHeadersDefaultCellStyle.ForeColor   = Color.White;
            dgv.ColumnHeadersDefaultCellStyle.Font        = FontBold;
            dgv.ColumnHeadersDefaultCellStyle.Padding     = new Padding(4, 0, 4, 0);
            dgv.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.True;
            dgv.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgv.EnableHeadersVisualStyles = false;
            dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        }

        public static void StyleTextBox(TextBox txt)
        {
            txt.Font        = FontDefault;
            txt.BackColor   = Color.White;
            txt.ForeColor   = TextPrimary;
            txt.BorderStyle = BorderStyle.FixedSingle;
        }

        public static void StyleComboBox(ComboBox cmb)
        {
            cmb.Font      = FontDefault;
            cmb.BackColor = Color.White;
            cmb.ForeColor = TextPrimary;
            cmb.FlatStyle = FlatStyle.Flat;
        }

        public static void StyleLabel(Label lbl, bool bold = false, Color? color = null)
        {
            lbl.Font      = bold ? FontBold : FontDefault;
            lbl.ForeColor = color ?? TextPrimary;
            lbl.BackColor = Color.Transparent;
        }

        public static void StylePanel(Panel pnl, Color? bg = null)
            => pnl.BackColor = bg ?? ContentBg;

        public static void ApplyFormBackground(Form form)
        {
            form.BackColor = ContentBg;
            form.Font      = FontDefault;
        }

        public static void StyleToolbarPanel(Panel pnl)
        {
            pnl.BackColor = Color.FromArgb(249, 250, 251);
            pnl.Padding   = new Padding(12, 0, 12, 0);
        }

        // ── CreateStatCard ─────────────────────────────────────────────────
        public static Panel CreateStatCard(string title, string value, Color cardColor, string icon = "📦", string trendText = "+0% vs last month", bool trendUp = true)
        {
            var card = new Panel
            {
                BackColor = Color.White,
                Cursor    = Cursors.Hand,
                Padding   = new Padding(16)
            };

            Action<Control>? wireHover = null;
            wireHover = (c) =>
            {
                c.MouseEnter += (s, e) => { card.BackColor = Color.FromArgb(250, 250, 250); };
                c.MouseLeave += (s, e) => {
                    var mousePos = card.PointToClient(Cursor.Position);
                    if (!card.ClientRectangle.Contains(mousePos))
                        card.BackColor = Color.White;
                };
            };

            // Icon background panel
            var iconBg = new Panel
            {
                Size = new Size(40, 40),
                Location = new Point(16, 16),
                BackColor = Color.FromArgb(20, cardColor)
            };

            var lblIcon = new Label
            {
                Name      = "lblIcon",
                Text      = icon,
                Font      = new Font("Segoe UI Emoji", 14f, FontStyle.Regular, GraphicsUnit.Point),
                ForeColor = cardColor,
                AutoSize  = false,
                Size      = new Size(40, 40),
                Location  = new Point(0, 0),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };
            iconBg.Controls.Add(lblIcon);

            var lblValue = new Label
            {
                Name      = "lblValue",
                Text      = value,
                Font      = new Font("Segoe UI", 24f, FontStyle.Bold, GraphicsUnit.Point),
                ForeColor = Color.FromArgb(31, 41, 55),
                AutoSize  = false,
                Size      = new Size(160, 40),
                Location  = new Point(16, 64),
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Color.Transparent
            };

            var lblTitle = new Label
            {
                Name      = "lblTitle",
                Text      = title,
                Font      = new Font("Segoe UI", 9.5f, FontStyle.Regular, GraphicsUnit.Point),
                ForeColor = Color.FromArgb(107, 114, 128),
                AutoSize  = true,
                Location  = new Point(14, 110),
                BackColor = Color.Transparent
            };
            
            var lblTrendIcon = new Label
            {
                Name      = "lblTrendIcon",
                Text      = trendUp ? "↑" : "↓",
                Font      = new Font("Segoe UI", 8.5f, FontStyle.Bold, GraphicsUnit.Point),
                ForeColor = trendUp ? Success : Danger,
                AutoSize  = true,
                Location  = new Point(14, 134),
                BackColor = Color.Transparent
            };

            var lblTrendText = new Label
            {
                Name      = "lblTrendText",
                Text      = trendText,
                Font      = new Font("Segoe UI", 8f, FontStyle.Regular, GraphicsUnit.Point),
                ForeColor = Color.FromArgb(156, 163, 175),
                AutoSize  = true,
                Location  = new Point(28, 134),
                BackColor = Color.Transparent
            };

            card.Controls.Add(iconBg);
            card.Controls.Add(lblValue);
            card.Controls.Add(lblTitle);
            card.Controls.Add(lblTrendIcon);
            card.Controls.Add(lblTrendText);

            wireHover(card);
            wireHover(iconBg);
            wireHover(lblIcon);
            wireHover(lblValue);
            wireHover(lblTitle);
            wireHover(lblTrendIcon);
            wireHover(lblTrendText);

            card.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;

                using var path = new GraphicsPath();
                int cr   = 16;
                var rect = new Rectangle(0, 0, card.Width - 1, card.Height - 1);
                path.AddArc(rect.X,          rect.Y,          cr, cr, 180, 90);
                path.AddArc(rect.Right - cr, rect.Y,          cr, cr, 270, 90);
                path.AddArc(rect.Right - cr, rect.Bottom - cr, cr, cr, 0,   90);
                path.AddArc(rect.X,          rect.Bottom - cr, cr, cr, 90,  90);
                path.CloseFigure();
                card.Region = new Region(path);
                
                // Top border accent (optional, matching some modern designs, or just left plain white)
                // The reference uses plain white cards. We will add a soft border.
                using var borderPen = new Pen(Color.FromArgb(235, 237, 240), 1.5f);
                g.DrawPath(borderPen, path);
                
                // Draw a decorative sparkline at the bottom right
                int lineW = 70;
                int lineH = 15;
                int startX = card.Width - lineW - 16;
                int startY = card.Height - lineH - 16;
                
                using var sparkPen = new Pen(Color.FromArgb(60, cardColor), 2f);
                using var sparkPath = new GraphicsPath();
                
                // Generate a pseudo-random looking curve that ends up or down depending on trendUp
                int midX = startX + lineW / 2;
                int midY = startY + lineH / 2;
                
                int yEnd = trendUp ? startY : startY + lineH;
                int yStart = trendUp ? startY + lineH : startY;
                
                Point[] pts = {
                    new Point(startX, yStart),
                    new Point(startX + lineW / 4, yStart + (trendUp ? -lineH/2 : lineH/2)),
                    new Point(startX + lineW / 2, midY + (trendUp ? lineH/4 : -lineH/4)),
                    new Point(startX + 3 * lineW / 4, yEnd + (trendUp ? lineH/2 : -lineH/2)),
                    new Point(startX + lineW, yEnd)
                };
                sparkPath.AddCurve(pts, 0.5f);
                g.DrawPath(sparkPen, sparkPath);
                
                // Fill soft gradient under sparkline
                using var gradPath = new GraphicsPath();
                gradPath.AddCurve(pts, 0.5f);
                gradPath.AddLine(startX + lineW, card.Height);
                gradPath.AddLine(startX, card.Height);
                gradPath.CloseFigure();
                
                using var gradBrush = new LinearGradientBrush(
                    new Rectangle(startX, startY - lineH, lineW, lineH * 2 + 10),
                    Color.FromArgb(30, cardColor),
                    Color.Transparent,
                    LinearGradientMode.Vertical);
                g.FillPath(gradBrush, gradPath);

                // Make the icon background panel rounded
                using var iconPath = new GraphicsPath();
                int ir = 10;
                var irect = new Rectangle(0, 0, iconBg.Width - 1, iconBg.Height - 1);
                iconPath.AddArc(irect.X,          irect.Y,          ir, ir, 180, 90);
                iconPath.AddArc(irect.Right - ir, irect.Y,          ir, ir, 270, 90);
                iconPath.AddArc(irect.Right - ir, irect.Bottom - ir, ir, ir, 0,   90);
                iconPath.AddArc(irect.X,          irect.Bottom - ir, ir, ir, 90,  90);
                iconPath.CloseFigure();
                iconBg.Region = new Region(iconPath);
            };

            return card;
        }
    }
}