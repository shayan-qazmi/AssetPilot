// Forms/DashboardForm.cs
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Linq;
using System.Windows.Forms;
using Inventory_Management_System.Forms;
using Inventory_Management_System.Helpers;
using Inventory_Management_System.Models;
using Inventory_Management_System.Services;

namespace Inventory_Management_System
{
    public partial class DashboardForm : Form
    {
        private Form?   activeForm      = null;
        private Button? activeNavButton = null;

        // Chart data — populated in LoadStats, consumed by paint handlers
        private int _cntInStore      = 0;
        private int _cntAllocated    = 0;
        private int _cntUnderRepair  = 0;
        private int _cntDecommission = 0;
        private int _cntEmployees    = 0;
        private List<(string Month, int Count)> _monthlyData = new();

        // Chart panels — created once, live inside panelCharts
        private Panel? _panelDonut    = null;
        private Panel? _panelBar      = null;
        private Panel? _panelActivity = null;
        private Panel? _panelCharts   = null;

        private Button[] AdminOnlyButtons => new[]
        {
            btnDecommission,
            btnSettings
        };

        public DashboardForm()
        {
            InitializeComponent();
            ApplyTheme();
            ApplyRolePermissions();
            LoadStats();

            this.Resize += (s, e) => { if (panelStats.Visible) LayoutCharts(); };
        }

        // ── Theme ─────────────────────────────────────────────────────────
        private void ApplyTheme()
        {
            BackColor = ThemeHelper.Sidebar;

            panelTop.BackColor    = ThemeHelper.Header;
            lblAppBrand.Text      = "IMS";
            lblAppBrand.Dock      = DockStyle.Top;
            lblAppBrand.Height    = 150;
            lblAppBrand.TextAlign = ContentAlignment.MiddleCenter;
            lblTitle.AutoSize     = false;
            lblTitle.Dock         = DockStyle.Fill;
            lblTitle.TextAlign    = ContentAlignment.MiddleCenter;
            lblTitle.Text         = "Q-Inventory";
            lblTitle.Name         = "lblTitle";
            lblTitle.Font         = new Font(lblTitle.Font.FontFamily, 16F, FontStyle.Bold);
            lblTitle.ForeColor    = Color.FromArgb(255, 255, 224);
            lblTitle.Visible      = true;
            this.Text             = "Q-Inventory";

            panelMenu.BackColor = ThemeHelper.Sidebar;

            try
            {
                var logoPath = System.IO.Path.Combine(Application.StartupPath, "Images", "logo.png");
                if (System.IO.File.Exists(logoPath))
                {
                    lblAppBrand.Visible = false;
                    var picLogo = panelMenu.Controls.OfType<PictureBox>()
                                           .FirstOrDefault(p => p.Name == "picLogoSide");
                    if (picLogo == null)
                    {
                        picLogo = new PictureBox
                        {
                            Name      = "picLogoSide",
                            SizeMode  = PictureBoxSizeMode.Zoom,
                            Height    = 120,
                            Dock      = DockStyle.Top,
                            BackColor = ThemeHelper.Sidebar
                        };
                        panelMenu.Controls.Add(picLogo);
                    }
                    picLogo.Image = Image.FromFile(logoPath);
                    panelMenu.Controls.SetChildIndex(picLogo, panelMenu.Controls.Count - 1);
                }
                else
                {
                    lblAppBrand.Text    = "IMS";
                    lblAppBrand.Visible = true;
                }
            }
            catch
            {
                lblAppBrand.Text    = "IMS";
                lblAppBrand.Visible = true;
            }

            ThemeHelper.StyleSidebarButton(btnDashboard,   active: true);
            ThemeHelper.StyleSidebarButton(btnEquipment);
            ThemeHelper.StyleSidebarButton(btnEmployees);
            ThemeHelper.StyleSidebarButton(btnLocations);
            ThemeHelper.StyleSidebarButton(btnAllocation);
            ThemeHelper.StyleSidebarButton(btnReturns);
            ThemeHelper.StyleSidebarButton(btnTransfer);
            ThemeHelper.StyleSidebarButton(btnDecommission);
            ThemeHelper.StyleSidebarButton(btnLogs);
            ThemeHelper.StyleSidebarButton(btnQRScanner);
            ThemeHelper.StyleSidebarButton(btnReports);
            ThemeHelper.StyleSidebarButton(btnSettings);

            btnLogout.UseVisualStyleBackColor = false;
            btnLogout.FlatStyle = FlatStyle.Flat;
            btnLogout.FlatAppearance.BorderSize         = 0;
            btnLogout.BackColor = Color.Firebrick;
            btnLogout.ForeColor = Color.White;
            btnLogout.FlatAppearance.MouseOverBackColor = Color.DarkRed;
            btnLogout.FlatAppearance.MouseDownBackColor = Color.Maroon;
            btnLogout.TextAlign = ContentAlignment.MiddleLeft;
            btnLogout.Padding   = new Padding(12, 0, 0, 0);

            panelDesktop.BackColor = Color.FromArgb(240, 244, 240);
            panelStats.BackColor   = Color.FromArgb(240, 244, 240);

            // Who's logged in
            var lblWhoAmI = panelMenu.Controls.OfType<Label>().FirstOrDefault(l => l.Name == "lblWhoAmI");
            if (lblWhoAmI == null)
            {
                lblWhoAmI = new Label
                {
                    Name      = "lblWhoAmI",
                    Dock      = DockStyle.Bottom,
                    Height    = 40,
                    TextAlign = ContentAlignment.MiddleCenter,
                    ForeColor = ThemeHelper.SidebarText,
                    BackColor = Color.Transparent,
                    Font      = ThemeHelper.FontSmall
                };
                panelMenu.Controls.Add(lblWhoAmI);
                panelMenu.Controls.SetChildIndex(lblWhoAmI, 0);
            }
            lblWhoAmI.Text = $"{AuthService.CurrentFullName}  •  {AuthService.CurrentRole}";
        }

        private void ApplyRolePermissions()
        {
            bool isAdmin = AuthService.IsAdmin;
            foreach (var btn in AdminOnlyButtons)
                btn.Visible = isAdmin;
        }

        private bool EnsureAdmin()
        {
            if (AuthService.IsAdmin) return true;
            MessageBox.Show("This section is restricted to Administrators only.",
                "Access Denied", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }

        // ── Stats + Charts ────────────────────────────────────────────────
        private void LoadStats()
        {
            try
            {
                using var db = new InventoryDbContext();

                int totalEquipment   = db.Equipment.Count();
                _cntAllocated        = db.Equipment.Count(e => e.CurrentStatus == "allocated");
                _cntInStore          = db.Equipment.Count(e => e.CurrentStatus == "in_store");
                _cntUnderRepair      = db.Equipment.Count(e => e.CurrentStatus == "under_repair");
                _cntDecommission     = db.Equipment.Count(e => e.CurrentStatus == "decommissioned");
                _cntEmployees        = db.Employees.Count(e => e.IsActive);

                // Monthly allocation counts for the last 6 months
                var sixMonthsAgo = DateTime.Now.AddMonths(-5);
                _monthlyData = db.Allocations
                    .Where(a => a.AllocationDate >= sixMonthsAgo)
                    .AsEnumerable()
                    .GroupBy(a => new { a.AllocationDate.Year, a.AllocationDate.Month })
                    .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
                    .Select(g => (
                        Month: new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMM"),
                        Count: g.Count()
                    ))
                    .ToList();

                // Pad with zeros if fewer than 6 months of data
                if (_monthlyData.Count == 0)
                {
                    for (int i = 5; i >= 0; i--)
                        _monthlyData.Add((DateTime.Now.AddMonths(-i).ToString("MMM"), 0));
                }

                // Stat cards
                panelStats.Controls.Clear();

                var cardDefs = new[]
                {
                    (title: "Total Equipment", value: totalEquipment.ToString(),    color: Color.FromArgb(25,  118, 210), icon: "🖥️"),
                    (title: "Equipment Allocated", value: _cntAllocated.ToString(),    color: Color.FromArgb(245, 124, 0),   icon: "📋"),
                    (title: "Equipment In Stock", value: _cntInStore.ToString(),      color: Color.FromArgb(56,  142, 60),  icon: "✅"),
                    (title: "Total Employees",  value: _cntEmployees.ToString(),    color: Color.FromArgb(123, 31,  162), icon: "👤"),
                };

                int gap       = 16;
                int padL      = 16;
                int cardCount = cardDefs.Length;
                int totalW    = panelStats.Width - padL * 2 - gap * (cardCount - 1);
                int cardW     = Math.Max(160, totalW / cardCount);
                int cardH     = 250;
                int x         = padL;

                foreach (var def in cardDefs)
                {
                    var card      = ThemeHelper.CreateStatCard(def.title, def.value, def.color, def.icon);
                    card.Width    = cardW;
                    card.Height   = cardH;
                    card.Location = new Point(x, 12);

                    var lblIcon = card.Controls.OfType<Label>().FirstOrDefault(l => l.Name == "lblIcon");
                    if (lblIcon != null) lblIcon.Location = new Point(cardW - 90, 20);

                    var lblTitle = card.Controls.OfType<Label>().FirstOrDefault(l => l.Name == "lblTitle");
                    if (lblTitle != null) lblTitle.Width = cardW - 32;

                    panelStats.Controls.Add(card);
                    x += cardW + gap;
                }

                panelStats.Height = cardH + 24;

                BuildChartArea();
            }
            catch
            {
                // DB not available
            }
        }

        // ── Chart area construction ───────────────────────────────────────
        private void BuildChartArea()
        {
            // Remove old chart panel if present
            if (_panelCharts != null)
            {
                panelDesktop.Controls.Remove(_panelCharts);
                _panelCharts.Dispose();
                _panelCharts = null;
            }

            _panelCharts = new Panel
            {
                Name      = "panelCharts",
                BackColor = Color.FromArgb(240, 244, 240),
                Dock      = DockStyle.Fill,
                Padding   = new Padding(16, 12, 16, 12),
                AutoScroll = true
            };

            // Insert BEHIND panelStats (panelStats is Dock.Top, panelContent is Dock.Fill)
            // We add panelCharts to panelDesktop and it sits under panelStats naturally
            // when panelContent is hidden.
            // Add charts to panelContent itself — it's always Fill-docked
            // so we host charts inside it directly when no child form is open
            panelContent.Controls.Clear();
            panelContent.Controls.Add(_panelCharts);
            _panelCharts.Dock = DockStyle.Fill; 

            // ── Donut chart card ─────────────────────────────────────────
            _panelDonut = MakeChartCard("Equipment Status");
            _panelDonut.Paint += PaintDonutChart;

            // ── Bar chart card ───────────────────────────────────────────
            _panelBar = MakeChartCard("Monthly Allocations (Last 6 Months)");
            _panelBar.Paint += PaintBarChart;

            // ── Activity summary card ────────────────────────────────────
            _panelActivity = MakeChartCard("Quick Summary");
            _panelActivity.Paint += PaintActivityCard;

            _panelCharts.Controls.Add(_panelDonut);
            _panelCharts.Controls.Add(_panelBar);
            _panelCharts.Controls.Add(_panelActivity);

            LayoutCharts();
        }

        private static Panel MakeChartCard(string title)
        {
            var card = new Panel
            {
                BackColor = Color.White,
                Cursor    = Cursors.Default
            };

            // Card shadow effect via Paint
            card.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;

                // Rounded rect clip + background
                using var path = RoundedRect(new Rectangle(0, 0, card.Width - 1, card.Height - 1), 14);
                card.Region = new Region(path);

                // Title bar strip
                using var titleBrush = new SolidBrush(Color.FromArgb(248, 250, 248));
                g.FillRectangle(titleBrush, 0, 0, card.Width, 44);

                // Title text
                using var titleFont = new Font("Segoe UI", 10f, FontStyle.Bold, GraphicsUnit.Point);
                using var titleFg   = new SolidBrush(Color.FromArgb(27, 46, 26));
                // Tag stores the title
                string lbl = card.Tag?.ToString() ?? string.Empty;
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
                g.DrawString(lbl, titleFont, titleFg, new PointF(16, 12));

                // Divider line
                using var divPen = new Pen(Color.FromArgb(229, 231, 235), 1f);
                g.DrawLine(divPen, 0, 44, card.Width, 44);
            };

            card.Tag = title;
            return card;
        }

        private void LayoutCharts()
        {
            if (_panelCharts == null || _panelDonut == null || _panelBar == null || _panelActivity == null)
                return;

            int pad  = 16;
            int gap  = 16;
            int w    = _panelCharts.ClientSize.Width - pad * 2;
            int y    = pad;

            // Row 1: donut (38%) | bar (62%)
            int donutW = (int)(w * 0.38) - gap / 2;
            int barW   = w - donutW - gap;
            int row1H  = 340;

            _panelDonut.SetBounds(pad, y, donutW, row1H);
            _panelBar.SetBounds(pad + donutW + gap, y, barW, row1H);

            y += row1H + gap;

            // Row 2: activity summary — tall enough for icon + number + label + sub
            _panelActivity.SetBounds(pad, y, w, 200);

            _panelDonut.Invalidate();
            _panelBar.Invalidate();
            _panelActivity.Invalidate();
        }

        // ── Donut Chart ───────────────────────────────────────────────────
        private void PaintDonutChart(object? sender, PaintEventArgs e)
        {
            if (sender is not Panel p) return;
            var g = e.Graphics;
            g.SmoothingMode      = SmoothingMode.AntiAlias;
            g.TextRenderingHint  = TextRenderingHint.ClearTypeGridFit;

            int total = _cntInStore + _cntAllocated + _cntUnderRepair + _cntDecommission;
            if (total == 0) total = 1; // avoid divide-by-zero

            var segments = new[]
            {
                (Label: "Available",      Count: _cntInStore,      Color: Color.FromArgb(56,  142, 60)),
                (Label: "Allocated",      Count: _cntAllocated,    Color: Color.FromArgb(245, 124, 0)),
                (Label: "Under Repair",   Count: _cntUnderRepair,  Color: Color.FromArgb(33,  150, 243)),
                (Label: "Decommissioned", Count: _cntDecommission, Color: Color.FromArgb(239, 68,  68)),
            };

            int   contentTop = 52;
            int   size       = Math.Min(p.Width - 32, p.Height - contentTop - 80);
            size = Math.Max(size, 80);
            int   cx         = p.Width / 2;
            int   cy         = contentTop + size / 2 + 8;
            var   outerRect  = new RectangleF(cx - size / 2f, cy - size / 2f, size, size);
            float inner      = size * 0.54f;
            var   innerRect  = new RectangleF(cx - inner / 2f, cy - inner / 2f, inner, inner);

            float startAngle = -90f;
            foreach (var seg in segments)
            {
                float sweep = (float)seg.Count / total * 360f;
                if (sweep < 0.1f) { startAngle += sweep; continue; }

                using var brush = new SolidBrush(seg.Color);
                g.FillPie(brush, outerRect.X, outerRect.Y, outerRect.Width, outerRect.Height, startAngle, sweep);
                startAngle += sweep;
            }

            // Donut hole — white fill
            using var holeBrush = new SolidBrush(Color.White);
            g.FillEllipse(holeBrush, innerRect);

            // Centre text: total
            using var numFont = new Font("Segoe UI", 18f, FontStyle.Bold, GraphicsUnit.Point);
            using var subFont = new Font("Segoe UI", 8f,  FontStyle.Regular, GraphicsUnit.Point);
            using var fg      = new SolidBrush(Color.FromArgb(17, 24, 39));
            using var fgSub   = new SolidBrush(Color.FromArgb(107, 114, 128));

            var totalStr = total.ToString();
            var numSz    = g.MeasureString(totalStr, numFont);
            g.DrawString(totalStr, numFont, fg,    new PointF(cx - numSz.Width / 2f, cy - numSz.Height / 2f - 6));
            var subSz = g.MeasureString("Total", subFont);
            g.DrawString("Total", subFont, fgSub, new PointF(cx - subSz.Width / 2f, cy + numSz.Height / 2f - 8));

            // Legend below donut
            int legendY = (int)(outerRect.Bottom + 14);
            int legendX = 16;
            using var legendFont = new Font("Segoe UI", 8.5f, FontStyle.Regular, GraphicsUnit.Point);

            foreach (var seg in segments)
            {
                if (legendX + 130 > p.Width) { legendX = 16; legendY += 22; }
                using var dotBrush = new SolidBrush(seg.Color);
                g.FillEllipse(dotBrush, legendX, legendY + 4, 10, 10);
                using var legFg = new SolidBrush(Color.FromArgb(55, 65, 81));
                g.DrawString($"{seg.Label} ({seg.Count})", legendFont, legFg, legendX + 16, legendY);
                legendX += 140;
            }
        }

        // ── Bar Chart ─────────────────────────────────────────────────────
        private void PaintBarChart(object? sender, PaintEventArgs e)
        {
            if (sender is not Panel p) return;
            var g = e.Graphics;
            g.SmoothingMode     = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            if (_monthlyData.Count == 0) return;

            int    contentTop = 54;
            int    padL       = 52;
            int    padR       = 20;
            int    padB       = 40;
            int    chartW     = p.Width  - padL - padR;
            int    chartH     = p.Height - contentTop - padB - 8;
            int    chartX     = padL;
            int    chartY     = contentTop;

            int    maxVal     = Math.Max(1, _monthlyData.Max(m => m.Count));
            // Round up to a nice grid max
            int    gridMax    = ((maxVal / 5) + 1) * 5;

            // Grid lines + Y labels
            using var gridPen  = new Pen(Color.FromArgb(229, 231, 235), 1f);
            using var axisFont = new Font("Segoe UI", 7.5f, FontStyle.Regular, GraphicsUnit.Point);
            using var axisFg   = new SolidBrush(Color.FromArgb(156, 163, 175));

            int gridLines = 4;
            for (int i = 0; i <= gridLines; i++)
            {
                int yVal  = gridMax * i / gridLines;
                int yPos  = chartY + chartH - (int)((float)yVal / gridMax * chartH);
                g.DrawLine(gridPen, chartX, yPos, chartX + chartW, yPos);
                var label = yVal.ToString();
                var sz    = g.MeasureString(label, axisFont);
                g.DrawString(label, axisFont, axisFg, chartX - sz.Width - 4, yPos - sz.Height / 2f);
            }

            // Bars
            int   barCount = _monthlyData.Count;
            float slotW    = (float)chartW / barCount;
            float barW     = slotW * 0.55f;

            var barGradTop = Color.FromArgb(76,  175, 80);
            var barGradBot = Color.FromArgb(27,  94,  32);

            using var labelFont = new Font("Segoe UI", 8f, FontStyle.Regular, GraphicsUnit.Point);
            using var labelFg   = new SolidBrush(Color.FromArgb(55, 65, 81));
            using var valFg     = new SolidBrush(Color.FromArgb(46, 125, 50));
            using var valFont   = new Font("Segoe UI", 7.5f, FontStyle.Bold, GraphicsUnit.Point);

            for (int i = 0; i < barCount; i++)
            {
                var  m      = _monthlyData[i];
                float bx    = chartX + i * slotW + (slotW - barW) / 2f;
                float bh    = m.Count == 0 ? 0 : Math.Max(4, (float)m.Count / gridMax * chartH);
                float by    = chartY + chartH - bh;

                if (bh > 0)
                {
                    var barRect = new RectangleF(bx, by, barW, bh);
                    using var grad = new LinearGradientBrush(
                        new PointF(bx, by), new PointF(bx, by + bh),
                        barGradTop, barGradBot);
                    // Rounded top corners
                    using var barPath = RoundedRectF(barRect, 5f);
                    g.FillPath(grad, barPath);

                    // Value label above bar
                    var valStr = m.Count.ToString();
                    var valSz  = g.MeasureString(valStr, valFont);
                    g.DrawString(valStr, valFont, valFg, bx + barW / 2f - valSz.Width / 2f, by - valSz.Height - 2);
                }

                // Month label below
                var mSz = g.MeasureString(m.Month, labelFont);
                g.DrawString(m.Month, labelFont, labelFg,
                    bx + barW / 2f - mSz.Width / 2f,
                    chartY + chartH + 6f);
            }

            // X axis line
            using var axisLine = new Pen(Color.FromArgb(209, 213, 219), 1.5f);
            g.DrawLine(axisLine, chartX, chartY + chartH, chartX + chartW, chartY + chartH);
        }

        // ── Activity Summary Card ─────────────────────────────────────────
        private void PaintActivityCard(object? sender, PaintEventArgs e)
        {
            if (sender is not Panel p) return;
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            int total = _cntInStore + _cntAllocated + _cntUnderRepair + _cntDecommission;
            float allocPct = total == 0 ? 0 : (float)_cntAllocated / total * 100f;
            float availPct = total == 0 ? 0 : (float)_cntInStore / total * 100f;

            var items = new[]
            {
                (Icon: "✅", Label: "Available",      Value: _cntInStore.ToString(),      Sub: $"{availPct:0}% of fleet",    Clr: Color.FromArgb(56,  142, 60)),
                (Icon: "📋", Label: "Allocated",      Value: _cntAllocated.ToString(),    Sub: $"{allocPct:0}% utilisation", Clr: Color.FromArgb(245, 124, 0)),
                (Icon: "🔧", Label: "Under Repair",   Value: _cntUnderRepair.ToString(),  Sub: "Pending return",             Clr: Color.FromArgb(33,  150, 243)),
                (Icon: "🚫", Label: "Decommissioned", Value: _cntDecommission.ToString(), Sub: "Retired assets",             Clr: Color.FromArgb(239, 68,  68)),
                (Icon: "👤", Label: "Active Staff",   Value: _cntEmployees.ToString(),    Sub: "Employees on record",        Clr: Color.FromArgb(123, 31,  162)),
            };

            int contentTop = 54;
            int itemW = (p.Width - 32) / items.Length;
            int startX = 16;

            using var bigFont = new Font("Segoe UI", 26f, FontStyle.Bold, GraphicsUnit.Point);
            using var lblFont = new Font("Segoe UI", 9f, FontStyle.Bold, GraphicsUnit.Point);
            using var subFont = new Font("Segoe UI", 8f, FontStyle.Regular, GraphicsUnit.Point);
            using var iconFont = new Font("Segoe UI Emoji", 16f, FontStyle.Regular, GraphicsUnit.Point);

            for (int i = 0; i < items.Length; i++)
            {
                var item = items[i];
                int ix = startX + i * itemW;
                int iy = contentTop + 10;

                // Accent bar
                using var accentBrush = new SolidBrush(item.Clr);
                g.FillRectangle(accentBrush, ix, iy, 4, 100);

                // Icon
                g.DrawString(item.Icon, iconFont, new SolidBrush(item.Clr), ix + 16, iy + 2);

                // Big number — below icon, no overlap
                using var numFg = new SolidBrush(Color.FromArgb(17, 24, 39));
                g.DrawString(item.Value, bigFont, numFg, ix + 16, iy + 30);

                // Label — sized from actual font measure so it never overlaps
                var numH = g.MeasureString(item.Value, bigFont).Height;
                using var lblFg = new SolidBrush(Color.FromArgb(55, 65, 81));
                g.DrawString(item.Label, lblFont, lblFg, ix + 16, iy + 32 + (int)numH);

                // Sub
                var lblH = g.MeasureString(item.Label, lblFont).Height;
                using var subFg = new SolidBrush(Color.FromArgb(156, 163, 175));
                g.DrawString(item.Sub, subFont, subFg, ix + 16, iy + 34 + (int)numH + (int)lblH);

                // Divider
                if (i < items.Length - 1)
                {
                    using var divPen = new Pen(Color.FromArgb(229, 231, 235), 1f);
                    g.DrawLine(divPen, ix + itemW - 1, iy + 4, ix + itemW - 1, iy + 110);
                }
            }
        }
        // ── Helpers ───────────────────────────────────────────────────────
        private static GraphicsPath RoundedRect(Rectangle r, int radius)
        {
            var path = new GraphicsPath();
            int d = radius * 2;
            path.AddArc(r.X,          r.Y,           d, d, 180, 90);
            path.AddArc(r.Right - d,  r.Y,           d, d, 270, 90);
            path.AddArc(r.Right - d,  r.Bottom - d,  d, d, 0,   90);
            path.AddArc(r.X,          r.Bottom - d,  d, d, 90,  90);
            path.CloseFigure();
            return path;
        }

        private static GraphicsPath RoundedRectF(RectangleF r, float radius)
        {
            var path = new GraphicsPath();
            float d = radius * 2f;
            path.AddArc(r.X,          r.Y,           d, d, 180, 90);
            path.AddArc(r.Right - d,  r.Y,           d, d, 270, 90);
            path.AddArc(r.Right - d,  r.Bottom - d,  d, d, 0,   90);
            path.AddArc(r.X,          r.Bottom - d,  d, d, 90,  90);
            path.CloseFigure();
            return path;
        }

        // ── Navigation ────────────────────────────────────────────────────
        private void SetActiveButton(Button btn)
        {
            if (activeNavButton != null)
                ThemeHelper.StyleSidebarButton(activeNavButton, active: false);
            ThemeHelper.StyleSidebarButton(btn, active: true);
            activeNavButton = btn;
        }

        private void OpenChildForm(Form childForm, Button navBtn)
        {
            activeForm?.Close();
            activeForm = null;

            SetActiveButton(navBtn);

            activeForm                = childForm;
            childForm.TopLevel        = false;
            childForm.FormBorderStyle = FormBorderStyle.None;
            childForm.Dock            = DockStyle.Fill;

            panelContent.Controls.Clear();
            panelContent.Controls.Add(childForm);
            childForm.BringToFront();
            childForm.Show();

            panelStats.Visible   = false;
            if (_panelCharts != null) _panelCharts.Visible = false;
        }

        private void ShowDashboard()
        {
            activeForm?.Close();
            activeForm = null;

            panelStats.Visible = true;
            SetActiveButton(btnDashboard);
            LoadStats(); // rebuilds charts inside panelContent each time
        }

        private void btnDashboard_Click(object sender, EventArgs e)   => ShowDashboard();
        private void btnEquipment_Click(object sender, EventArgs e)   => OpenChildForm(new EquipmentForm(),    btnEquipment);
        private void btnEmployees_Click(object sender, EventArgs e)   => OpenChildForm(new EmployeesForm(),    btnEmployees);
        private void btnLocations_Click(object sender, EventArgs e)   => OpenChildForm(new LocationsForm(),    btnLocations);
        private void btnAllocation_Click(object sender, EventArgs e)  => OpenChildForm(new AllocationForm(),   btnAllocation);
        private void btnReturns_Click(object sender, EventArgs e)     => OpenChildForm(new ReturnsForm(),      btnReturns);
        private void btnTransfer_Click(object sender, EventArgs e)    => OpenChildForm(new TransferForm(),     btnTransfer);

        private void btnDecommission_Click(object sender, EventArgs e)
        {
            if (!EnsureAdmin()) return;
            OpenChildForm(new DecommissionForm(), btnDecommission);
        }

        private void btnLogs_Click(object sender, EventArgs e)        => OpenChildForm(new LogsForm(),         btnLogs);
        private void btnQRScanner_Click(object sender, EventArgs e)   => OpenChildForm(new QRScannerForm(),    btnQRScanner);
        private void btnReports_Click(object sender, EventArgs e)     => OpenChildForm(new ReportsForm(),      btnReports);

        private void btnSettings_Click(object sender, EventArgs e)
        {
            if (!EnsureAdmin()) return;
            OpenChildForm(new SettingsForm(), btnSettings);
        }

        private void btnLogout_Click(object sender, EventArgs e)
        {
            var result = MessageBox.Show(
                "Are you sure you want to log out?",
                "Confirm Logout",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                activeForm?.Close();
                activeForm = null;
                AuthService.ClearCurrentUser();
                this.Hide();

                using var login = new LoginForm();
                if (login.ShowDialog() == DialogResult.OK)
                {
                    panelContent.Controls.Clear();
                    panelStats.Visible  = true;
                    activeNavButton     = null;
                    LoadStats();
                    ApplyTheme();
                    ApplyRolePermissions();
                    this.Show();
                }
                else
                {
                    Application.Exit();
                }
            }
        }
    }
}