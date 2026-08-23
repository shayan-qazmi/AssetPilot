// Helpers/ThemeHelper.cs
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Inventory_Management_System.Helpers
{
    public static class ThemeHelper
    {
        // ── Dark Mode State + Persistence ─────────────────────────────────
        private static readonly string SettingsPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "theme.json");

        public static bool IsDarkMode { get; private set; } = false;

        /// <summary>Call once at app startup to restore the user's last theme choice.</summary>
        public static void LoadTheme()
        {
            try
            {
                if (File.Exists(SettingsPath))
                {
                    string json = File.ReadAllText(SettingsPath);
                    var dict = JsonSerializer.Deserialize<Dictionary<string, bool>>(json);
                    if (dict != null && dict.TryGetValue("IsDarkMode", out bool mode))
                        IsDarkMode = mode;
                }
            }
            catch { /* defaults to light */ }
        }

        /// <summary>Flip the theme and persist. Caller is responsible for refreshing UI.</summary>
        public static void ToggleTheme()
        {
            IsDarkMode = !IsDarkMode;
            SaveTheme();
        }

        private static void SaveTheme()
        {
            try
            {
                var dict = new Dictionary<string, bool> { { "IsDarkMode", IsDarkMode } };
                File.WriteAllText(SettingsPath, JsonSerializer.Serialize(dict));
            }
            catch { /* non-critical */ }
        }

        // ── Static (Light-only) Color Palette ─────────────────────────────
        // Sidebar / accent colors never change between themes — the dark sidebar
        // is already very dark green, which works for both modes.
        public static Color Sidebar => IsDarkMode ? Color.FromArgb(14, 16, 19) : Color.FromArgb(26, 46, 26);

        public static readonly Color Accent = Color.FromArgb(56, 142, 60);
        public static readonly Color Success = Color.FromArgb(76, 175, 80);
        public static readonly Color Warning = Color.FromArgb(245, 158, 11);
        public static readonly Color Danger = Color.FromArgb(239, 68, 68);
        public static Color SidebarText => IsDarkMode ? Color.FromArgb(205, 210, 218) : Color.FromArgb(168, 197, 168);
        public static Color GridAltRow => IsDarkMode ? Color.FromArgb(22, 24, 29) : Color.FromArgb(241, 248, 233);
        public static readonly Color GridHeader    = Color.FromArgb(46, 125, 50);
        public static readonly Color Header        = Color.FromArgb(46, 125, 50);
        public static readonly Color InputBorder   = Color.FromArgb(209, 213, 219);
        public static readonly Color CardBorder    = Color.FromArgb(229, 231, 235);
        public static bool UseGlobalAccentIndicator = false;

        // ── Theme-aware Color Properties ──────────────────────────────────
        // Forms / grids
        public static Color ContentBg     => IsDarkMode ? Color.FromArgb(28, 30, 36) : Color.White;
        public static Color CardBg        => IsDarkMode ? Color.FromArgb(36, 39, 46) : Color.White;
        public static Color TextPrimary   => IsDarkMode ? Color.FromArgb(220, 224, 232) : Color.FromArgb(17, 24, 39);
        public static Color TextSecondary => IsDarkMode ? Color.FromArgb(140, 148, 160) : Color.FromArgb(107, 114, 128);

        // Dashboard-specific surfaces
        /// <summary>Overall dashboard desktop/stats area background.</summary>
        public static Color DashboardBg    => IsDarkMode ? Color.FromArgb(18, 20, 24)  : Color.FromArgb(237, 241, 237);
        /// <summary>Chart card body background.</summary>
        public static Color ChartCardBg   => IsDarkMode ? Color.FromArgb(28, 30, 36)  : Color.White;
        /// <summary>Chart card title-band (top 44px strip) background.</summary>
        public static Color ChartCardHdrBg=> IsDarkMode ? Color.FromArgb(36, 39, 46)  : Color.FromArgb(248, 250, 248);
        /// <summary>Bar/track background inside charts and category bars.</summary>
        public static Color ChartTrackBg  => IsDarkMode ? Color.FromArgb(36, 39, 46)  : Color.FromArgb(240, 244, 240);
        /// <summary>Thin divider line inside chart cards.</summary>
        public static Color ChartDivider  => IsDarkMode ? Color.FromArgb(55, 58, 65)  : Color.FromArgb(229, 231, 235);
        /// <summary>Chart card title text.</summary>
        public static Color ChartTitleText=> IsDarkMode ? Color.FromArgb(220, 224, 232): Color.FromArgb(27, 46, 26);
        /// <summary>Body text inside charts (labels, axes, legends).</summary>
        public static Color ChartBodyText => IsDarkMode ? Color.FromArgb(200, 204, 212): Color.FromArgb(55, 65, 81);
        /// <summary>Muted secondary text inside charts.</summary>
        public static Color ChartSecText  => IsDarkMode ? Color.FromArgb(140, 148, 160): Color.FromArgb(107, 114, 128);
        /// <summary>Activities list host panel background.</summary>
        public static Color ChartHostBg   => IsDarkMode ? Color.FromArgb(28, 30, 36)  : Color.White;
        /// <summary>Top header bar background.</summary>
        public static Color TopBarBg => IsDarkMode ? Color.FromArgb(45, 48, 55) : Color.FromArgb(0x99, 0xC2, 0x21);
        /// <summary>Activity-row icon pill background.</summary>
        public static Color ActivityIconBg=> IsDarkMode ? Color.FromArgb(36, 39, 46)  : Color.FromArgb(243, 244, 246);
        /// <summary>Donut chart centre hole fill.</summary>
        public static Color DonutHoleBg   => IsDarkMode ? Color.FromArgb(28, 30, 36)  : Color.White;
        /// <summary>Donut centre large number text.</summary>
        public static Color DonutNumText  => IsDarkMode ? Color.FromArgb(220, 224, 232): Color.FromArgb(17, 24, 39);
        /// <summary>Donut centre "Total" sub-label text.</summary>
        public static Color DonutSubText  => IsDarkMode ? Color.FromArgb(140, 148, 160): Color.FromArgb(107, 114, 128);
        /// <summary>Popup/dropdown background.</summary>
        public static Color PopupBg       => IsDarkMode ? Color.FromArgb(36, 39, 46)  : Color.White;
        /// <summary>Popup/dropdown border.</summary>
        public static Color PopupBorder   => IsDarkMode ? Color.FromArgb(55, 58, 65)  : Color.FromArgb(220, 220, 220);
        /// <summary>Popup hover-row highlight.</summary>
        public static Color PopupHoverBg  => IsDarkMode ? Color.FromArgb(50, 54, 62)  : Color.FromArgb(243, 244, 246);
        /// <summary>Search box fill.</summary>
        public static Color SearchBoxBg   => IsDarkMode ? Color.FromArgb(36, 39, 46)  : Color.White;
        /// <summary>Search text colour.</summary>
        public static Color SearchFgColor => IsDarkMode ? Color.FromArgb(200, 204, 212): Color.FromArgb(55, 65, 81);
        /// <summary>Dashboard title label foreground.</summary>
        public static Color TitleForeColor=> IsDarkMode ? Color.FromArgb(220, 224, 232): Color.FromArgb(33, 33, 33);
        /// <summary>Period-selector label / dropdown text background.</summary>
        public static Color PeriodDropBg  => IsDarkMode ? Color.FromArgb(36, 39, 46)  : Color.White;

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
            btn.BackColor = IsDarkMode ? Color.FromArgb(55, 58, 65) : Color.FromArgb(243, 244, 246);
            btn.ForeColor = TextPrimary;
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize         = 1;
            btn.FlatAppearance.BorderColor        = InputBorder;
            btn.FlatAppearance.MouseOverBackColor = IsDarkMode ? Color.FromArgb(65, 68, 76) : Color.FromArgb(229, 231, 235);
            btn.Font      = FontDefault;
            btn.Cursor    = Cursors.Hand;
            btn.UseVisualStyleBackColor = false;
        }

        // ── Sidebar button ─────────────────────────────────────────────────
        public static void StyleSidebarButton(Button btn, bool active = false)
        {
            btn.Tag = active;
            btn.BackColor = active ? Color.FromArgb(45, 90, 45) : GetSidebarGradientColorAt(btn);
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

        /// <summary>Sidebar buttons sit on panelMenu's vertical gradient. Sample the gradient
        /// at this button's own vertical position so an idle button's flat BackColor blends
        /// seamlessly into the background — otherwise every inactive button reads as its own
        /// highlighted box (flat top-of-gradient color) against the darker gradient behind it.</summary>
        private static Color GetSidebarGradientColorAt(Control btn)
        {
            var parent = btn.Parent;
            if (parent == null || parent.Height <= 0) return Sidebar;

            float t = Math.Clamp((btn.Top + btn.Height / 2f) / parent.Height, 0f, 1f);
            Color top = Sidebar;
            Color bottom = ControlPaint.Dark(Sidebar, 0.12f);
            return Color.FromArgb(
                (int)(top.R + (bottom.R - top.R) * t),
                (int)(top.G + (bottom.G - top.G) * t),
                (int)(top.B + (bottom.B - top.B) * t));
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
            barPath.AddArc(6, barY,            r, r, 180, 90);
            barPath.AddArc(6, barY,            r, r, 270, 90);
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

            int padL  = elevated ? 4 : 6;
            int padT  = elevated ? 2 : 4;
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
                    FadeButtonColor(btn, btn.BackColor, GetSidebarGradientColorAt(btn), onComplete: () =>
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
            Color baseBg = IsDarkMode ? Color.FromArgb(20, 22, 27) : Color.White;
            Color altBg = IsDarkMode ? Color.FromArgb(20, 22, 27) : GridAltRow; // same as base in dark mode — no banding
            Color fg = IsDarkMode ? Color.FromArgb(222, 226, 233) : TextPrimary;

            dgv.BackgroundColor = baseBg;
            dgv.BorderStyle = BorderStyle.None;
            dgv.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            dgv.GridColor = IsDarkMode ? Color.FromArgb(45, 48, 55) : Color.FromArgb(234, 236, 240);
            dgv.RowHeadersVisible = false;
            dgv.ColumnHeadersHeight = 40;
            dgv.RowTemplate.Height = 36;
            dgv.Font = FontDefault;
            dgv.EnableHeadersVisualStyles = false;

            dgv.DefaultCellStyle.Font = FontDefault;
            dgv.DefaultCellStyle.ForeColor = fg;
            dgv.DefaultCellStyle.BackColor = baseBg;
            dgv.DefaultCellStyle.SelectionBackColor = IsDarkMode ? Color.FromArgb(40, 85, 50) : Color.FromArgb(224, 231, 255);
            dgv.DefaultCellStyle.SelectionForeColor = IsDarkMode ? Color.White : TextPrimary;
            dgv.DefaultCellStyle.Padding = new Padding(4, 0, 4, 0);

            dgv.AlternatingRowsDefaultCellStyle.BackColor = altBg;
            dgv.AlternatingRowsDefaultCellStyle.Font = FontDefault;
            dgv.AlternatingRowsDefaultCellStyle.ForeColor = fg;
            dgv.AlternatingRowsDefaultCellStyle.SelectionBackColor = dgv.DefaultCellStyle.SelectionBackColor;
            dgv.AlternatingRowsDefaultCellStyle.SelectionForeColor = dgv.DefaultCellStyle.SelectionForeColor;

            dgv.ColumnHeadersDefaultCellStyle.BackColor = GridHeader; // green, both modes
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgv.ColumnHeadersDefaultCellStyle.Font = FontBold;
            dgv.ColumnHeadersDefaultCellStyle.Padding = new Padding(4, 0, 4, 0);
            dgv.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.True;
            dgv.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;

            dgv.CellMouseEnter -= Dgv_CellMouseEnter;
            dgv.CellMouseLeave -= Dgv_CellMouseLeave;
            dgv.CellFormatting -= Dgv_CellFormatting;
            dgv.CellMouseEnter += Dgv_CellMouseEnter;
            dgv.CellMouseLeave += Dgv_CellMouseLeave;
            dgv.CellFormatting += Dgv_CellFormatting;
        }

        // Safety net: guarantees readable text on every cell in dark mode even if a
        // form's own code (e.g. colored Status badges) sets styles independently.
        // Skips any cell that already has a custom (non-row) background — badges
        // keep their own colors untouched.
        private static void Dgv_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (!IsDarkMode || sender is not DataGridView dgv) return;
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;

            var cellStyle = dgv.Rows[e.RowIndex].Cells[e.ColumnIndex].Style;

            // If the cell already has an explicit custom ForeColor (e.g. Status
            // badges colored green/orange/red in ReportsForm), leave it alone —
            // only fall back to the safe gray when nothing custom was set.
            if (!cellStyle.ForeColor.IsEmpty)
                return;

            Color rowBg = dgv.Rows[e.RowIndex].DefaultCellStyle.BackColor;
            Color effectiveBg = cellStyle.BackColor.IsEmpty ? rowBg : cellStyle.BackColor;

            bool isPlainRowBg = effectiveBg.IsEmpty
                || effectiveBg == dgv.DefaultCellStyle.BackColor
                || effectiveBg == dgv.AlternatingRowsDefaultCellStyle.BackColor;

            if (isPlainRowBg)
                e.CellStyle.ForeColor = Color.FromArgb(222, 226, 233);
        }

        private static void Dgv_CellMouseEnter(object? sender, DataGridViewCellEventArgs e)
        {
            if (sender is not DataGridView dgv || e.RowIndex < 0) return;
            var row = dgv.Rows[e.RowIndex];
            if (row.Selected) return;
            row.DefaultCellStyle.BackColor = IsDarkMode ? Color.FromArgb(36, 40, 47) : Color.FromArgb(243, 244, 246);
        }

        private static void Dgv_CellMouseLeave(object? sender, DataGridViewCellEventArgs e)
        {
            if (sender is not DataGridView dgv || e.RowIndex < 0) return;
            var row = dgv.Rows[e.RowIndex];
            if (row.Selected) return;
            row.DefaultCellStyle.BackColor = IsDarkMode
                ? Color.FromArgb(20, 22, 27)
                : (e.RowIndex % 2 == 1 ? GridAltRow : ContentBg);
        }

        public static void StyleTextBox(TextBox txt)
        {
            txt.Font        = FontDefault;
            txt.BackColor   = IsDarkMode ? Color.FromArgb(45, 48, 55) : Color.White;
            txt.ForeColor   = TextPrimary;
            txt.BorderStyle = BorderStyle.FixedSingle;
        }

        public static void StyleComboBox(ComboBox cmb)
        {
            cmb.Font      = FontDefault;
            cmb.BackColor = IsDarkMode ? Color.FromArgb(45, 48, 55) : Color.White;
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
            pnl.BackColor = IsDarkMode ? Color.FromArgb(36, 39, 46) : Color.FromArgb(249, 250, 251);
            pnl.Padding   = new Padding(12, 0, 12, 0);
        }

        // ── CreateStatCard ─────────────────────────────────────────────────
        // trendValues: optional recent-history points (e.g. last 7 days) — when
        // provided, an animated sparkline draws in under the value and a delta
        // badge appears. Pass null (or omit) to get the original card unchanged.
        public static Panel CreateStatCard(string title, string value, Color cardColor, string icon = "📦",
     float[]? trendValues = null, string? deltaText = null, int startDelayMs = 0)
        {
            bool dark = IsDarkMode;
            Color cardBg = dark ? Color.FromArgb(30, 33, 39) : cardColor;
            Color valueFg = dark ? cardColor : Color.White;
            Color titleFg = dark ? Color.FromArgb(cardColor.R, cardColor.G, cardColor.B) : Color.FromArgb(220, 255, 255, 255);
            Color titleFgMuted = dark ? Color.FromArgb(190, titleFg) : titleFg;

            var card = new Panel { BackColor = cardBg, Cursor = Cursors.Hand };

            Action<Control>? wireHover = null;
            wireHover = (c) =>
            {
                c.MouseEnter += (s, e) => { card.BackColor = ControlPaint.Light(cardBg, dark ? 0.25f : 0.1f); };
                c.MouseLeave += (s, e) => {
                    var mousePos = card.PointToClient(Cursor.Position);
                    if (!card.ClientRectangle.Contains(mousePos))
                        card.BackColor = cardBg;
                };
            };

            bool hasTrend = trendValues != null && trendValues.Length >= 2;

            bool isNumeric = int.TryParse(value, out int targetNum);
            var lblValue = new Label
            {
                Name = "lblValue",
                Text = isNumeric ? "0" : value,
                Font = new Font("Segoe UI", 22f, FontStyle.Bold, GraphicsUnit.Point),
                ForeColor = valueFg,
                AutoSize = false,
                Size = new Size(190, 48),      // was 40 — 22pt Bold needs more line height than that, so the tops/bottoms of digits were being clipped
                Location = new Point(12, 8),   // nudged up 4px to keep it centered against the taller box
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Color.Transparent
            };

            var lblTitle = new Label
            {
                Name = "lblTitle",
                Text = title,
                Font = new Font("Segoe UI", 9f, FontStyle.Regular, GraphicsUnit.Point),
                ForeColor = titleFgMuted,
                AutoSize = false,
                Height = 22,
                Location = new Point(12, 56),
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Color.Transparent
            };

            card.Controls.Add(lblValue);
            card.Controls.Add(lblTitle);

            wireHover(card);
            wireHover(lblValue);
            wireHover(lblTitle);

            float sparkProgress = 0f;
            System.Windows.Forms.Timer? sparkTimer = null;

            if (hasTrend)
            {
                sparkTimer = new System.Windows.Forms.Timer { Interval = 15 };
                sparkTimer.Tick += (s, e) =>
                {
                    sparkProgress = Math.Min(1f, sparkProgress + 0.04f);
                    card.Invalidate();
                    if (sparkProgress >= 1f) { sparkTimer.Stop(); sparkTimer.Dispose(); }
                };
                card.HandleCreated += (s, e) => sparkTimer.Start();
                card.Disposed += (s, e) => { sparkTimer?.Stop(); sparkTimer?.Dispose(); };
            }

            card.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;

                using var path = new GraphicsPath();
                int cr = 12;
                var rect = new Rectangle(0, 0, card.Width - 1, card.Height - 1);
                path.AddArc(rect.X, rect.Y, cr, cr, 180, 90);
                path.AddArc(rect.Right - cr, rect.Y, cr, cr, 270, 90);
                path.AddArc(rect.Right - cr, rect.Bottom - cr, cr, cr, 0, 90);
                path.AddArc(rect.X, rect.Bottom - cr, cr, cr, 90, 90);
                path.CloseFigure();
                card.Region = new Region(path);

                if (dark)
                {
                    // subtle colored border + top accent strip so the card reads by its accent color
                    using var borderPen = new Pen(Color.FromArgb(90, cardColor), 1.25f);
                    g.DrawPath(borderPen, path);
                    using var accentBrush = new SolidBrush(cardColor);
                    using var accentClip = new GraphicsPath();
                    accentClip.AddArc(rect.X, rect.Y, cr, cr, 180, 90);
                    accentClip.AddArc(rect.Right - cr, rect.Y, cr, cr, 270, 90);
                    accentClip.AddLine(rect.Right, cr, rect.Right, 3);
                    accentClip.AddLine(rect.Right, 3, rect.X, 3);
                    accentClip.CloseFigure();
                    g.FillPath(accentBrush, accentClip);
                }

                using var emojiFont = new Font("Segoe UI Emoji", 20f, FontStyle.Regular, GraphicsUnit.Point);
                using var emojiSf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                g.DrawString(icon, emojiFont, Brushes.White, new RectangleF(card.Width - 54, 4, 50, 50), emojiSf);

                if (hasTrend)
                {
                    int sparkX = 12, sparkY = 56, sparkW = card.Width - 24, sparkH = 26;
                    float min = trendValues!.Min(), max = trendValues.Max();
                    if (max - min < 0.001f) max = min + 1f;

                    var points = new PointF[trendValues.Length];
                    for (int i = 0; i < trendValues.Length; i++)
                    {
                        float nx = sparkX + (sparkW * i / (float)(trendValues.Length - 1));
                        float ny = sparkY + sparkH - ((trendValues[i] - min) / (max - min) * sparkH);
                        points[i] = new PointF(nx, ny);
                    }

                    int visibleCount = Math.Max(2, (int)(points.Length * sparkProgress) + 1);
                    visibleCount = Math.Min(visibleCount, points.Length);
                    if (visibleCount >= 2)
                    {
                        Color sparkColor = dark ? Color.FromArgb(210, cardColor) : Color.FromArgb(210, 255, 255, 255);
                        using var sparkPen = new Pen(sparkColor, 2f) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };
                        g.DrawLines(sparkPen, points[..visibleCount]);

                        var last = points[visibleCount - 1];
                        using var dotBrush = new SolidBrush(dark ? cardColor : Color.White);
                        g.FillEllipse(dotBrush, last.X - 3, last.Y - 3, 6, 6);
                    }

                    if (!string.IsNullOrWhiteSpace(deltaText) && sparkProgress >= 1f)
                    {
                        using var deltaFont = new Font("Segoe UI", 8f, FontStyle.Bold, GraphicsUnit.Point);
                        using var deltaFg = new SolidBrush(dark ? Color.FromArgb(220, cardColor) : Color.FromArgb(230, 255, 255, 255));
                        g.DrawString(deltaText, deltaFont, deltaFg, sparkX, sparkY + sparkH + 2);
                    }
                }

                int stripH = 26;
                int stripY = card.Height - stripH;

                using var footerBg = new SolidBrush(dark ? Color.FromArgb(18, Color.White) : Color.FromArgb(25, Color.Black));
                using var topBorder = new Pen(dark ? Color.FromArgb(35, Color.White) : Color.FromArgb(30, Color.White), 1f);
                g.FillRectangle(footerBg, 0, stripY, card.Width, stripH);
                g.DrawLine(topBorder, 0, stripY, card.Width, stripY);

                using var footerFont = new Font("Segoe UI", 8.25f, FontStyle.Regular, GraphicsUnit.Point);
                using var footerFg = new SolidBrush(dark ? Color.FromArgb(190, 195, 205) : Color.FromArgb(190, 255, 255, 255));
                string footerText = "More info";
                var fSize = g.MeasureString(footerText, footerFont);
                float totalW = fSize.Width + 14;
                float sx = (card.Width - totalW) / 2f;
                float sy = stripY + (stripH - fSize.Height) / 2f;
                g.DrawString(footerText, footerFont, footerFg, sx, sy);
                g.DrawString("⟳", footerFont, footerFg, sx + fSize.Width + 2, sy - 1);

                // AFTER
                lblTitle.Location = new Point(12, card.Height - 28 - stripH);
                lblTitle.Width = card.Width - 24;

                // Keep the value label in sync with the card's actual width too — it was
                // hardcoded to 190px, which clipped on narrower cards now that 7 share the row.
                lblValue.Width = Math.Max(60, card.Width - 24);
            };
            if (isNumeric)
            {
                lblValue.Text = "0"; // holds at 0 until the card's own slide/bounce entrance has landed

                int steps = 20;
                int step = 0;
                var countTimer = new System.Windows.Forms.Timer { Interval = 20 };
                countTimer.Tick += (s, e) =>
                {
                    step++;
                    float t = Math.Min(1f, step / (float)steps);
                    float eased = 1f - MathF.Pow(1f - t, 3f);
                    lblValue.Text = (int)(targetNum * eased) + (t >= 1f ? "" : "");
                    if (t >= 1f)
                    {
                        lblValue.Text = targetNum.ToString();
                        countTimer.Stop();
                        countTimer.Dispose();
                    }
                };

                System.Windows.Forms.Timer? startDelayTimer = null;
                card.HandleCreated += (s, e) =>
                {
                    if (startDelayMs > 0)
                    {
                        startDelayTimer = new System.Windows.Forms.Timer { Interval = startDelayMs };
                        startDelayTimer.Tick += (s2, e2) =>
                        {
                            startDelayTimer!.Stop();
                            startDelayTimer.Dispose();
                            countTimer.Start();
                        };
                        startDelayTimer.Start();
                    }
                    else
                    {
                        countTimer.Start();
                    }
                };
                card.Disposed += (s, e) =>
                {
                    startDelayTimer?.Stop(); startDelayTimer?.Dispose();
                    countTimer.Stop(); countTimer.Dispose();
                };
            }
            return card;
        }
    }
}