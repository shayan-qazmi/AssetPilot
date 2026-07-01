// Helpers/ThemeHelper.cs
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

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
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(45, 90, 45);
            btn.FlatAppearance.MouseDownBackColor = Color.FromArgb(55, 100, 55);
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
                // Pill shape
                ApplyPillRegion(btn);
                btn.Resize += SidebarBtn_Resize;

                // Left accent bar drawn via Paint
                btn.Paint += SidebarBtn_PaintAccent;
            }
            else
            {
                btn.Region = null;
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

        private static void SidebarBtn_Resize(object? sender, EventArgs e)
        {
            if (sender is Button btn) ApplyPillRegion(btn);
        }

        private static void ApplyPillRegion(Button btn)
        {
            if (btn.Width <= 0 || btn.Height <= 0) return;

            // Inset pill — 6px from left, 4px top/bottom, flush right
            int padL = 6, padT = 4;
            var rect = new Rectangle(padL, padT,
                btn.Width - padL - 1,
                btn.Height - padT * 2);

            var path = new GraphicsPath();
            int radius = 10;
            path.AddArc(rect.X,              rect.Y,              radius, radius, 180, 90);
            path.AddArc(rect.Right - radius, rect.Y,              radius, radius, 270, 90);
            path.AddArc(rect.Right - radius, rect.Bottom - radius, radius, radius, 0,   90);
            path.AddArc(rect.X,              rect.Bottom - radius, radius, radius, 90,  90);
            path.CloseFigure();
            btn.Region = new Region(path);
        }

        private static void SidebarBtn_MouseEnter(object? sender, EventArgs e)
        {
            if (sender is Button btn)
            {
                bool isActive = btn.Tag is bool a && a;
                if (isActive)
                {
                    // active button retains its active background
                    btn.BackColor = Color.FromArgb(45, 90, 45);
                }
                else
                {
                    // subtle hover highlight for inactive button
                    btn.BackColor = ControlPaint.Light(Sidebar, 0.12f);
                    // show pill shape on hover
                    ApplyPillRegion(btn);
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
                    // revert to default inactive styling
                    btn.BackColor = Sidebar;
                    btn.Region    = null;
                }
                else
                {
                    // ensure active button retains its pill region and background
                    btn.BackColor = Color.FromArgb(45, 90, 45);
                    ApplyPillRegion(btn);
                }
            }
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
        public static Panel CreateStatCard(string title, string value, Color cardColor, string icon = "📦")
        {
            var card = new Panel
            {
                BackColor = cardColor,
                Cursor    = Cursors.Hand
            };

            var lblValue = new Label
            {
                Name      = "lblValue",
                Text      = value,
                Font      = new Font("Segoe UI", 22f, FontStyle.Bold, GraphicsUnit.Point),
                ForeColor = Color.White,
                AutoSize  = false,
                Size      = new Size(190, 80),
                Location  = new Point(12, 12),
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Color.Transparent
            };

            var lblIcon = new Label
            {
                Name      = "lblIcon",
                Text      = icon,
                Font      = new Font("Segoe UI Emoji", 15f, FontStyle.Regular, GraphicsUnit.Point),
                ForeColor = Color.FromArgb(160, 255, 255, 255),
                AutoSize  = false,
                Size      = new Size(40, 40),
                Location  = new Point(999, 8),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };

            var lblTitle = new Label
            {
                Name      = "lblTitle",
                Text      = title,
                Font      = new Font("Segoe UI", 9f, FontStyle.Regular, GraphicsUnit.Point),
                ForeColor = Color.FromArgb(220, 255, 255, 255),
                AutoSize  = false,
                Height    = 22,
                Location  = new Point(12, 56),
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Color.Transparent
            };

            card.Controls.Add(lblValue);
            card.Controls.Add(lblIcon);
            card.Controls.Add(lblTitle);

            card.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;

                using var path = new GraphicsPath();
                int cr   = 12;
                var rect = new Rectangle(0, 0, card.Width - 1, card.Height - 1);
                path.AddArc(rect.X,          rect.Y,          cr, cr, 180, 90);
                path.AddArc(rect.Right - cr, rect.Y,          cr, cr, 270, 90);
                path.AddArc(rect.Right - cr, rect.Bottom - cr, cr, cr, 0,   90);
                path.AddArc(rect.X,          rect.Bottom - cr, cr, cr, 90,  90);
                path.CloseFigure();
                card.Region = new Region(path);

                // Pin title to bottom, icon to top-right
                lblTitle.Location = new Point(12, card.Height - 28);
                lblTitle.Width    = card.Width - 24;
                lblIcon.Location  = new Point(card.Width - 44, 2);
                lblIcon.Size      = new Size(40, 36);
            };

            return card;
        }
    }
}