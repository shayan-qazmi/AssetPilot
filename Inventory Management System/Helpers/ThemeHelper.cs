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
        public static readonly Font FontCardNum  = new Font("Segoe UI", 32f,   FontStyle.Bold,    GraphicsUnit.Point);
        public static readonly Font FontCardLbl  = new Font("Segoe UI", 10f,   FontStyle.Regular, GraphicsUnit.Point);
        public static readonly Font FontCardInfo = new Font("Segoe UI", 8.5f,  FontStyle.Regular, GraphicsUnit.Point);
        public static readonly Font FontSidebar  = new Font("Segoe UI", 9.5f,  FontStyle.Regular, GraphicsUnit.Point);
        public static readonly Font FontSidebarB = new Font("Segoe UI", 9.5f,  FontStyle.Bold,    GraphicsUnit.Point);

        // ── StyleButton ───────────────────────────────────────────────────
        public static void StyleButton(Button btn, Color? color = null)
        {
            var baseColor = color ?? Accent;
            btn.BackColor   = baseColor;
            btn.ForeColor   = Color.White;
            btn.FlatStyle   = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize         = 0;
            btn.FlatAppearance.MouseOverBackColor = baseColor == Accent ? Header : ControlPaint.Light(baseColor, 0.15f);
            btn.FlatAppearance.MouseDownBackColor = ControlPaint.Dark(baseColor, 0.1f);
            btn.Font        = FontBold;
            btn.Cursor      = Cursors.Hand;
            btn.UseVisualStyleBackColor = false;
        }

        public static void StyleDangerButton(Button btn) => StyleButton(btn, Danger);

        public static void StyleSecondaryButton(Button btn)
        {
            btn.BackColor   = Color.FromArgb(243, 244, 246);
            btn.ForeColor   = TextPrimary;
            btn.FlatStyle   = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize  = 1;
            btn.FlatAppearance.BorderColor = InputBorder;
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(229, 231, 235);
            btn.Font        = FontDefault;
            btn.Cursor      = Cursors.Hand;
            btn.UseVisualStyleBackColor = false;
        }

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
            btn.Padding   = new Padding(16, 0, 0, 0);
            btn.Cursor    = Cursors.Hand;
            btn.UseVisualStyleBackColor = false;

            btn.MouseEnter -= SidebarBtn_MouseEnter;
            btn.MouseLeave -= SidebarBtn_MouseLeave;
            btn.MouseEnter += SidebarBtn_MouseEnter;
            btn.MouseLeave += SidebarBtn_MouseLeave;

            if (active)
                ApplyRoundedRegion(btn);
            else
                btn.Region = null;
        }

        private static void ApplyRoundedRegion(Button btn)
        {
            if (btn.Width <= 0 || btn.Height <= 0) return;
            var path = new GraphicsPath();
            int radius = 10;
            var rect = new Rectangle(0, 0, btn.Width, btn.Height);
            path.AddArc(rect.X, rect.Y, radius, radius, 180, 90);
            path.AddArc(rect.Right - radius, rect.Y, radius, radius, 270, 90);
            path.AddArc(rect.Right - radius, rect.Bottom - radius, radius, radius, 0, 90);
            path.AddArc(rect.X, rect.Bottom - radius, radius, radius, 90, 90);
            path.CloseFigure();
            btn.Region = new Region(path);
        }

        private static void SidebarBtn_MouseEnter(object? sender, EventArgs e)
        {
            if (sender is Button btn)
            {
                btn.FlatAppearance.BorderSize = 0;
                btn.BackColor = Color.FromArgb(45, 90, 45);
                ApplyRoundedRegion(btn);
            }
        }

        private static void SidebarBtn_MouseLeave(object? sender, EventArgs e)
        {
            if (sender is Button btn)
            {
                bool active = btn.Tag is bool a && a;
                if (!active)
                {
                    btn.BackColor = Sidebar;
                    btn.Region    = null;
                }
            }
        }

        public static void StyleDataGridView(DataGridView dgv)
        {
            dgv.BackgroundColor          = ContentBg;
            dgv.BorderStyle              = BorderStyle.None;
            dgv.CellBorderStyle          = DataGridViewCellBorderStyle.SingleHorizontal;
            dgv.GridColor                = Color.FromArgb(234, 236, 240);
            dgv.RowHeadersVisible        = false;
            dgv.ColumnHeadersHeight      = 40;
            dgv.RowTemplate.Height       = 36;
            dgv.Font                     = FontDefault;
            dgv.DefaultCellStyle.Font    = FontDefault;
            dgv.DefaultCellStyle.ForeColor = TextPrimary;
            dgv.DefaultCellStyle.BackColor = ContentBg;
            dgv.DefaultCellStyle.SelectionBackColor = Color.FromArgb(224, 231, 255);
            dgv.DefaultCellStyle.SelectionForeColor = TextPrimary;
            dgv.DefaultCellStyle.Padding = new Padding(4, 0, 4, 0);
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
        {
            pnl.BackColor = bg ?? ContentBg;
        }

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
        // No "More Info" footer. Cards fill the full row equally.
        public static Panel CreateStatCard(string title, string value, Color cardColor, string icon = "📦")
        {
            var card = new Panel
            {
                // Width set dynamically in DashboardForm.LoadStats()
                Height    = 140,
                BackColor = cardColor,
                Cursor    = Cursors.Hand
            };

            // ── Big number top-left ──
            var lblValue = new Label
            {
                Text      = value,
                Font      = FontCardNum,
                ForeColor = Color.White,
                AutoSize  = false,
                Width     = 200,
                Height    = 70,
                Location  = new Point(16, 18),
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Color.Transparent
            };

            // ── Icon top-right ──
            var lblIcon = new Label
            {
                Text      = icon,
                Font      = new Font("Segoe UI Emoji", 28f, FontStyle.Regular, GraphicsUnit.Point),
                ForeColor = Color.FromArgb(160, 255, 255, 255),
                AutoSize  = false,
                Width     = 64,
                Height    = 64,
                // X is set after card width is known — see DashboardForm
                Location  = new Point(999, 10),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent,
                Name      = "lblIcon"
            };

            // ── Title below number ──
            var lblTitle = new Label
            {
                Text      = title,
                Font      = new Font("Segoe UI", 11f, FontStyle.Regular, GraphicsUnit.Point),
                ForeColor = Color.FromArgb(220, 255, 255, 255),
                AutoSize  = false,
                Height    = 26,
                Location  = new Point(16, 95),
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Color.Transparent,
                Name      = "lblTitle"
            };

            card.Controls.Add(lblValue);
            card.Controls.Add(lblIcon);
            card.Controls.Add(lblTitle);

            // Rounded corners
            card.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var path = new GraphicsPath();
                int r = 12;
                var rect = new Rectangle(0, 0, card.Width - 1, card.Height - 1);
                path.AddArc(rect.X, rect.Y, r, r, 180, 90);
                path.AddArc(rect.Right - r, rect.Y, r, r, 270, 90);
                path.AddArc(rect.Right - r, rect.Bottom - r, r, r, 0, 90);
                path.AddArc(rect.X, rect.Bottom - r, r, r, 90, 90);
                path.CloseFigure();
                card.Region = new Region(path);
            };

            return card;
        }
    }
}