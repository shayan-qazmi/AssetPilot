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
    // Double-buffered panel to eliminate flicker/jitter during chart animations.
    internal class DoubleBufferedPanel : Panel
    {
        public DoubleBufferedPanel()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint, true);
            UpdateStyles();
        }
    }

    public partial class DashboardForm : Form
    {
        private Form?   activeForm      = null;
        private Button? activeNavButton = null;

        // ── Live data ─────────────────────────────────────────────────────
        private int _cntInStore      = 0;
        private int _cntAllocated    = 0;
        private int _cntUnderRepair  = 0;
        private int _cntDecommission = 0;
        private int _cntOverdue      = 0;
        private int _cntEmployees    = 0;
        private List<(string Category, int Count)> _categoryBreakdown = new();

        // ── Chart panels ──────────────────────────────────────────────────
        private Panel? _panelCharts  = null;
        private Panel? _panelDonut   = null;
        private Panel? _panelBar     = null;
        private Panel? _panelLine    = null;
        private Panel? _panelTopRow;   
        private Panel? _panelBotRow;

        private Button[] AdminOnlyButtons => new[] { btnDecommission, btnSettings, btnDeletedItems };
        // ── Top bar (yellow, matched to logo) ───────────────────────────────
        private static readonly Color TopBarYellow = Color.FromArgb(0x99, 0xC2, 0x21);

        // ── Title typing animation ───────────────────────────────────────
        private System.Windows.Forms.Timer? _titleTypeTimer;

        // ── Card entrance animations ────────────────────────────────────
        private System.Windows.Forms.Timer? _animTimer;
        private List<CardAnimState>? _cardAnims;
        // ── Sidebar active-indicator (sliding accent bar) ────────────────
        private Panel? _accentIndicator;
        private System.Windows.Forms.Timer? _accentTimer;
        private int _accentStartY;
        private int _accentTargetY;
        private float _accentProgress = 1f;
        private const int AccentBarHeight = 30;
        private float _donutAnimProgress = 1f;
        private const int AnimTickIntervalMs = 15;
        private const int CenterCardIndex = 3; // "Under Repair"
        private float _barAnimProgress = 1f;

        private enum CardAnimKind { Slide, Bounce }

        private class CardAnimState
        {
            public Control Card { get; set; } = null!;
            public Rectangle StartBounds { get; set; }
            public Rectangle TargetBounds { get; set; }
            public float Progress { get; set; }
            public float DelayMs { get; set; }
            public CardAnimKind Kind { get; set; }
        }

        public DashboardForm()
        {
            InitializeComponent();
            ApplyTheme();
            ApplyRolePermissions();
            LoadStats();

            this.Resize += (s, e) => { if (panelStats.Visible) LayoutCharts(); };
            this.Shown  += (s, e) => { if (panelStats.Visible) LoadStats(); };
        }

        // ════════════════════════════════════════════════════════════════
        //  THEME
        // ════════════════════════════════════════════════════════════════
        private void ApplyTheme()
        {
            BackColor = ThemeHelper.Sidebar;

            // Dashboard's own top strip color — intentionally NOT ThemeHelper.Header,
            // since that color is shared by other forms' header strips.
            panelTop.BackColor    = TopBarYellow;
            NotificationHelper.AttachBell(panelTop, this);
            lblAppBrand.Dock      = DockStyle.Top;
            lblAppBrand.Height    = 150;
            lblAppBrand.TextAlign = ContentAlignment.MiddleCenter;
            lblTitle.AutoSize     = false;
            lblTitle.Dock         = DockStyle.Fill;
            lblTitle.TextAlign    = ContentAlignment.MiddleCenter;
            lblTitle.Font         = new Font(lblTitle.Font.FontFamily, 16F, FontStyle.Bold);
            // Dark text for legibility against the new yellow strip (was pale yellow
            // for contrast against the old green strip).
            lblTitle.ForeColor    = Color.FromArgb(33, 33, 33);
            lblTitle.Visible      = true;
            this.Text             = "Q-Inventory";

            AnimateTitleTo("Dashboard");

            panelMenu.BackColor = ThemeHelper.Sidebar;

            panelMenu.Paint += (s, e) =>
            {
                using var grad = new LinearGradientBrush(
                    panelMenu.ClientRectangle,
                    ThemeHelper.Sidebar,
                    ControlPaint.Dark(ThemeHelper.Sidebar, 0.12f),
                    LinearGradientMode.Vertical);
                e.Graphics.FillRectangle(grad, panelMenu.ClientRectangle);
            };
            panelMenu.Invalidate();

            // Logo in sidebar
            try
            {
                var logoPath = System.IO.Path.Combine(Application.StartupPath, "Images", "logo.png");
                if (System.IO.File.Exists(logoPath))
                {
                    lblAppBrand.Visible = false;
                    lblAppBrand.Size = Size.Empty;
                    var pic = panelMenu.Controls.OfType<PictureBox>()
                                       .FirstOrDefault(p => p.Name == "picLogoSide");
                    if (pic == null)
                    {
                     pic = new PictureBox
                    {
                        Name = "picLogoSide",
                        SizeMode = PictureBoxSizeMode.Zoom,
                        Size = new Size(55, 55),
                        Location = new Point((panelMenu.Width - 55) / 2, 10),
                        BackColor = ThemeHelper.Sidebar
                    };
                        panelMenu.Controls.Add(pic);
                    }
                    pic.Image = Image.FromFile(logoPath);
                    panelMenu.Controls.SetChildIndex(pic, panelMenu.Controls.Count - 1);
                }
                else { lblAppBrand.Text = "Q-Inventory"; lblAppBrand.Visible = true; }
            }
            catch { lblAppBrand.Text = "Q-Inventory"; lblAppBrand.Visible = true; }



            // Nav buttons
            ThemeHelper.StyleSidebarButton(btnDashboard,   active: true);
            activeNavButton = btnDashboard;
            ThemeHelper.StyleSidebarButton(btnEquipment);
            ThemeHelper.StyleSidebarButton(btnEmployees);
            ThemeHelper.StyleSidebarButton(btnLocations);
            ThemeHelper.StyleSidebarButton(btnAllocation);
            ThemeHelper.StyleSidebarButton(btnReturns);
            ThemeHelper.StyleSidebarButton(btnTransfer);
            ThemeHelper.StyleSidebarButton(btnDecommission);
            ThemeHelper.StyleSidebarButton(btnDeletedItems);
            ThemeHelper.StyleSidebarButton(btnLogs);
            ThemeHelper.StyleSidebarButton(btnQRScanner);
            ThemeHelper.StyleSidebarButton(btnCheckInOut);
            ThemeHelper.StyleSidebarButton(btnReports);
            ThemeHelper.StyleSidebarButton(btnSettings);
                     // Use one shared, animated accent bar instead of each button
            // drawing its own static one.
            ThemeHelper.UseGlobalAccentIndicator = true;
            EnsureAccentIndicator();
            PositionAccentIndicator(btnDashboard, animate: false);

         

            // Logout — keep red
            btnLogout.UseVisualStyleBackColor              = false;
            btnLogout.FlatStyle                           = FlatStyle.Flat;
            btnLogout.FlatAppearance.BorderSize           = 0;
            btnLogout.BackColor                           = Color.Firebrick;
            btnLogout.ForeColor                           = Color.White;
            btnLogout.FlatAppearance.MouseOverBackColor   = Color.DarkRed;
            btnLogout.FlatAppearance.MouseDownBackColor   = Color.Maroon;
            btnLogout.TextAlign                           = ContentAlignment.MiddleLeft;
            btnLogout.Padding                             = new Padding(12, 0, 0, 0);

            panelDesktop.BackColor = Color.FromArgb(240, 244, 240);
            panelStats.BackColor   = Color.FromArgb(240, 244, 240);

          

            // Who's logged in
            var lbl = panelMenu.Controls.OfType<Label>().FirstOrDefault(l => l.Name == "lblWhoAmI");
            if (lbl == null)
            {
                lbl = new Label
                {
                    Name      = "lblWhoAmI",
                    Dock      = DockStyle.Bottom,
                    Height    = 40,
                    TextAlign = ContentAlignment.MiddleCenter,
                    ForeColor = ThemeHelper.SidebarText,
                    BackColor = Color.Transparent,
                    Font      = ThemeHelper.FontSmall
                };
                panelMenu.Controls.Add(lbl);
                panelMenu.Controls.SetChildIndex(lbl, 0);
            }
            lbl.Text = AuthService.CurrentFullName;
        }

        private void ApplyRolePermissions()
        {
            bool isAdmin = AuthService.IsAdmin;
            foreach (var btn in AdminOnlyButtons) btn.Visible = isAdmin;
        }


        private void EnsureAccentIndicator()
        {
            if (_accentIndicator != null) return;

            _accentIndicator = new Panel
            {
                Name = "accentIndicator",
                Width = 4,
                Height = AccentBarHeight,
                BackColor = Color.FromArgb(144, 238, 144)
            };
            panelMenu.Controls.Add(_accentIndicator);
            _accentIndicator.BringToFront();
        }

        private void PositionAccentIndicator(Button btn, bool animate = true)
        {
            if (_accentIndicator == null) return;

            const int targetX = 6; // matches the left-bar offset used by SidebarBtn_PaintAccent
            int targetY = btn.Top + (btn.Height - AccentBarHeight) / 2;

            if (!animate)
            {
                _accentIndicator.Location = new Point(targetX, targetY);
                _accentIndicator.Visible = true;
                _accentIndicator.BringToFront();
                return;
            }

            _accentTimer?.Stop();
            _accentTimer?.Dispose();

            _accentStartY = _accentIndicator.Top;
            _accentTargetY = targetY;
            _accentProgress = 0f;
            _accentIndicator.Left = targetX;
            _accentIndicator.BringToFront();

            _accentTimer = new System.Windows.Forms.Timer { Interval = 12 };
            _accentTimer.Tick += (s, e) =>
            {
                _accentProgress += 0.15f;
                if (_accentProgress >= 1f)
                {
                    _accentProgress = 1f;
                    _accentIndicator.Top = _accentTargetY;
                    _accentTimer.Stop();
                    _accentTimer.Dispose();
                    _accentTimer = null;
                }
                else
                {
                    float ease = 1f - MathF.Pow(1f - _accentProgress, 3f);
                    _accentIndicator.Top = (int)(_accentStartY + (_accentTargetY - _accentStartY) * ease);
                }
            };
            _accentTimer.Start();
        }


        private bool EnsureAdmin()
        {
            if (AuthService.IsAdmin) return true;
            MessageBox.Show("This section is restricted to Administrators only.",
                "Access Denied", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }

        // ════════════════════════════════════════════════════════════════
        //  TITLE TYPING ANIMATION
        // ════════════════════════════════════════════════════════════════
        private void AnimateTitleTo(string fullText)
        {
            _titleTypeTimer?.Stop();
            _titleTypeTimer?.Dispose();
            _titleTypeTimer = null;

            lblTitle.Text = "";
            int i = 0;
            var timer = new System.Windows.Forms.Timer { Interval = 45 };
            timer.Tick += (s, e) =>
            {
                if (i >= fullText.Length)
                {
                    timer.Stop();
                    timer.Dispose();
                    if (_titleTypeTimer == timer) _titleTypeTimer = null;
                    return;
                }
                i++;
                lblTitle.Text = fullText.Substring(0, i);
            };
            _titleTypeTimer = timer;
            timer.Start();
        }

        // ════════════════════════════════════════════════════════════════
        //  STATS + CHARTS
        // ════════════════════════════════════════════════════════════════
        private void LoadStats()
        {
            try
            {
                _animTimer?.Stop();
                _animTimer?.Dispose();
                _animTimer = null;
                _cardAnims = new List<CardAnimState>();
                _donutAnimProgress = 0f;
                _barAnimProgress = 0f;

                using var db = new InventoryDbContext();

                int totalEquipment = db.Equipment.Count();
                var checkedOutIds = db.Transactions
      .Where(t => t.Status == "Pending" || t.Status == "Overdue")
      .Select(t => t.EquipmentId);

                var activelyAllocatedIds = db.Allocations
                    .Where(a => a.Status == "pending" || a.Status == "approved")
                    .Select(a => a.EquipmentId);

                _cntAllocated = checkedOutIds.Union(activelyAllocatedIds).Distinct().Count();
                _cntInStore        = db.Equipment.Count(e => e.CurrentStatus == "in_store");
                _cntUnderRepair    = db.Equipment.Count(e => e.CurrentStatus == "under_repair");
                _cntDecommission   = db.Equipment.Count(e => e.CurrentStatus == "decommissioned");
                _cntEmployees      = db.Employees.Count(e => e.IsActive);
                int overdueCount = db.Allocations.Count(a =>
                    a.Status == "approved" &&
                    a.ExpectedReturnDate.HasValue &&
                    a.ExpectedReturnDate.Value < DateOnly.FromDateTime(DateTime.Today) &&
                    a.ActualReturnDate == null);
                _cntOverdue = overdueCount;


                // Category breakdown — equipment count per category
                _categoryBreakdown = db.Equipment
                    .Where(e => e.Category != null && e.Category != "")
                    .GroupBy(e => e.Category)
                    .Select(g => new { Category = g.Key, Count = g.Count() })
                    .OrderByDescending(g => g.Count)
                    .ToList()
                    .Select(g => (Category: g.Category!, Count: g.Count))
                    .ToList();

                // ── Stat cards — all 6 in one row ──────────────────────────
                panelStats.Controls.Clear();

                var cardDefs = new[]
                {
                    (title:"Total Equipment",    value:totalEquipment.ToString(),    color:Color.FromArgb(94, 53, 177), icon:"🖥"),
                    (title:"Allocated",          value:_cntAllocated.ToString(),     color:Color.FromArgb(245, 124, 0),   icon:"📋"),
                    (title:"In Stock",           value:_cntInStore.ToString(),       color:Color.FromArgb(56,  142, 60),  icon:"✅"),
                    (title:"Under Repair",       value:_cntUnderRepair.ToString(),   color:Color.FromArgb(33,  150, 243), icon:"🔧"),
(title:"Currently Overdue",  value:overdueCount.ToString(),      color:Color.FromArgb(255, 160,  0),  icon:"⚠️"),
                    (title:"Decommissioned",     value:_cntDecommission.ToString(),  color:Color.FromArgb(239, 68,  68),  icon:"🚫"),
                    (title:"Total Employees",    value:_cntEmployees.ToString(),     color:Color.FromArgb(123, 31,  162), icon:"👤"),
                };

                int padL  = 12;
                int gap   = 10;
                int count = cardDefs.Length;
                int totalW = panelStats.Width - padL * 2 - gap * (count - 1);
                int cardW  = Math.Max(120, totalW / count);
                panelStats.Height = 210;
                int cardH  = 180;
                int x      = padL;
                int cardIndex = 0;

                const float staggerStepMs = 60f;

                foreach (var def in cardDefs)
                {
                    var card      = ThemeHelper.CreateStatCard(def.title, def.value, def.color, def.icon);
                    Rectangle targetBounds = new Rectangle(x, 8, cardW, cardH);

                    CardAnimState animState;

                  if (cardIndex == CenterCardIndex)
                    {
                        // Center card: bounce-in via vertical position only — size stays
                        // fixed at target size the entire time, matching every other card.
                        Rectangle startBounds = new Rectangle(
                            targetBounds.X,
                            targetBounds.Y + targetBounds.Height, // starts one card-height below
                            targetBounds.Width,
                            targetBounds.Height);

                        card.Bounds = startBounds;

                        animState = new CardAnimState
                        {
                            Card         = card,
                            StartBounds  = startBounds,
                            TargetBounds = targetBounds,
                            Progress     = 0f,
                            DelayMs      = 0f,
                            Kind         = CardAnimKind.Bounce
                        };
                    }
                    else if (cardIndex < CenterCardIndex)
                    {
                        // Left group (0,1,2): slide in from off-screen left, converging
                        // toward the center — closest to center starts first.
                        int distanceFromCenter = CenterCardIndex - cardIndex; // 1,2,3
                        float delay = (distanceFromCenter - 1) * staggerStepMs;

                        Rectangle startBounds = new Rectangle(
                            -targetBounds.Width, targetBounds.Y, targetBounds.Width, targetBounds.Height);

                        card.Bounds = startBounds;

                        animState = new CardAnimState
                        {
                            Card         = card,
                            StartBounds  = startBounds,
                            TargetBounds = targetBounds,
                            Progress     = 0f,
                            DelayMs      = delay,
                            Kind         = CardAnimKind.Slide
                        };
                    }
                    else
                    {
                        // Right group (4,5,6): slide in from off-screen right, converging
                        // toward the center — closest to center starts first.
                        int distanceFromCenter = cardIndex - CenterCardIndex; // 1,2,3
                        float delay = (distanceFromCenter - 1) * staggerStepMs;

                        Rectangle startBounds = new Rectangle(
                            panelStats.Width, targetBounds.Y, targetBounds.Width, targetBounds.Height);

                        card.Bounds = startBounds;

                        animState = new CardAnimState
                        {
                            Card         = card,
                            StartBounds  = startBounds,
                            TargetBounds = targetBounds,
                            Progress     = 0f,
                            DelayMs      = delay,
                            Kind         = CardAnimKind.Slide
                        };
                    }

                    _cardAnims.Add(animState);

                    EventHandler? clickHandler = def.title switch
                    {
                        "Total Equipment"  => btnEquipment_Click,
                        "Allocated"        => btnAllocation_Click,
                        "In Stock"         => btnEquipment_Click,
                        "Under Repair"     => btnEquipment_Click,
                        "Currently Overdue" => btnAllocation_Click,
                        "Decommissioned"   => AuthService.IsAdmin ? btnDecommission_Click : null,
                        "Total Employees"  => btnEmployees_Click,
                        _                  => null
                    };

                    if (clickHandler != null)
                    {
                        AttachCardClick(card, clickHandler);
                    }

                    panelStats.Controls.Add(card);
                    x += cardW + gap;
                    cardIndex++;
                }

                BuildChartArea();

                _animTimer = new System.Windows.Forms.Timer { Interval = AnimTickIntervalMs };
                _animTimer.Tick += AnimTimer_Tick;
                _animTimer.Start();
            }
            catch { /* DB unavailable */ }
        }

        private void AnimTimer_Tick(object? sender, EventArgs e)
        {
            bool allDone = true;

            if (_cardAnims != null)
            {
                foreach (var anim in _cardAnims)
                {
                    if (anim.DelayMs > 0)
                    {
                        anim.DelayMs -= AnimTickIntervalMs;
                        allDone = false;
                        continue;
                    }

                    if (anim.Progress < 1f)
                    {
                        anim.Progress += 0.08f;
                        if (anim.Progress >= 1f) anim.Progress = 1f;
                        else allDone = false;

                        float t = anim.Progress;

                        if (anim.Kind == CardAnimKind.Bounce)
                        {
                            // Position-only bounce (drop + overshoot). Width/Height stay locked
                            // to TargetBounds throughout — no scale change, matching the sliding cards.
                            float c1 = 1.0f;
                            float c3 = c1 + 1;
                            float tMinus1 = t - 1;
                            float easeBack = 1 + c3 * tMinus1 * tMinus1 * tMinus1 + c1 * tMinus1 * tMinus1;

                            int cy = (int)(anim.StartBounds.Y + (anim.TargetBounds.Y - anim.StartBounds.Y) * easeBack);
                            int cx = anim.TargetBounds.X;

                            anim.Card.Bounds = new Rectangle(cx, cy, anim.TargetBounds.Width, anim.TargetBounds.Height);
                        }
                        else // Slide — position-only ease-out, no scale/size change, no overshoot
                        {
                            float ease = 1f - MathF.Pow(1f - t, 3f);

                            int cx = (int)(anim.StartBounds.X + (anim.TargetBounds.X - anim.StartBounds.X) * ease);
                            int cy = anim.TargetBounds.Y;

                            anim.Card.Bounds = new Rectangle(cx, cy, anim.TargetBounds.Width, anim.TargetBounds.Height);
                        }
                    }
                }
            }
            if (_donutAnimProgress < 1f)
            {
                float raw = Math.Min(1f, _donutAnimProgress + 0.0025f);
                _donutAnimProgress = raw >= 1f ? 1f : 1f - MathF.Pow(1f - raw, 3f);
                if (_donutAnimProgress > 0.999f) _donutAnimProgress = 1f;
                _panelDonut?.Invalidate();
                allDone = false;
            }

            if (_barAnimProgress < 1f)
            {
                float raw = Math.Min(1f, _barAnimProgress + 0.0025f);
                _barAnimProgress = raw >= 1f ? 1f : 1f - MathF.Pow(1f - raw, 3f);
                if (_barAnimProgress > 0.999f) _barAnimProgress = 1f;
                _panelBar?.Invalidate();
                _panelLine?.Invalidate();
                allDone = false;
            }

            if (allDone)
            {
                _animTimer?.Stop();
                _animTimer?.Dispose();
                _animTimer = null;
                
                if (_cardAnims != null)
                {
                    foreach (var anim in _cardAnims)
                    {
                        anim.Card.Bounds = anim.TargetBounds;
                    }
                }
            }
        }

        // ════════════════════════════════════════════════════════════════
        //  CHART AREA
        // ════════════════════════════════════════════════════════════════
        private void BuildChartArea()
        {
            // Tear down old charts
            if (_panelCharts != null)
            {
                panelContent.Controls.Remove(_panelCharts);
                _panelCharts.Dispose();
                _panelCharts = null;
            }

            _panelCharts = new DoubleBufferedPanel
            {
                Name       = "panelCharts",
                BackColor  = Color.FromArgb(240, 244, 240),
                Dock       = DockStyle.Fill,
                Padding    = new Padding(14, 12, 14, 12),
                AutoScroll = false
            };

            panelContent.Controls.Clear();
            panelContent.Controls.Add(_panelCharts);

            // ── Top row: Donut (left) + Bar (right) ──────────────────────
            _panelTopRow = new Panel
            {
                BackColor = Color.Transparent,
                Dock      = DockStyle.None
            };

            _panelDonut = MakeChartCard("Equipment Status");
            _panelDonut.Paint += PaintDonutChart;

            _panelBar = MakeChartCard("Asset Status Breakdown");
            _panelBar.Paint += PaintBarChart;

            _panelTopRow.Controls.Add(_panelDonut);
            _panelTopRow.Controls.Add(_panelBar);

            // ── Bottom row: Line chart full-width ─────────────────────────
            _panelBotRow = new Panel
            {
                BackColor = Color.Transparent,
                Dock      = DockStyle.None
            };

            _panelLine = MakeChartCard("Equipment by Category");
            _panelLine.Paint += PaintCategoryBreakdownChart;

            _panelBotRow.Controls.Add(_panelLine);

            _panelCharts.Controls.Add(_panelBotRow);
            _panelCharts.Controls.Add(_panelTopRow);

            LayoutCharts();
        }

        private void LayoutCharts()
        {
            if (_panelCharts == null) return;

            int pad  = 14;
            int gap  = 14;
            int w    = _panelCharts.ClientSize.Width  - pad * 2;
            int h    = _panelCharts.ClientSize.Height - pad * 2;

            // Top row: 40% donut | 60% bar
            int topH   = (int)(h * 0.56);
            int donutW = (int)(w * 0.38) - gap / 2;
            int barW   = w - donutW - gap;

            if (_panelTopRow != null)
            {
                _panelTopRow.SetBounds(pad, pad, w, topH);

                _panelDonut?.SetBounds(0, 0, donutW, topH);
                _panelBar?.SetBounds(donutW + gap, 0, barW, topH);
            }

            // Bottom row: full-width line chart
            int botY = pad + topH + gap;
            int botH = h - topH - gap;
            if (botH < 160) botH = 160;

            if (_panelBotRow != null)
            {
                _panelBotRow.SetBounds(pad, botY, w, botH);
                _panelLine?.SetBounds(0, 0, w, botH);
            }

            _panelDonut?.Invalidate();
            _panelBar?.Invalidate();
            _panelLine?.Invalidate();
        }

        // ════════════════════════════════════════════════════════════════
        //  CHART CARD FACTORY
        // ════════════════════════════════════════════════════════════════
        private static Panel MakeChartCard(string title)
        {
            var card = new DoubleBufferedPanel { BackColor = Color.White };

            card.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode     = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

                // Rounded clip
                using var path = RoundedRect(new Rectangle(0, 0, card.Width - 1, card.Height - 1), 14);
                card.Region = new Region(path);

                // Header strip
                using var hBrush = new SolidBrush(Color.FromArgb(248, 250, 248));
                g.FillRectangle(hBrush, 0, 0, card.Width, 44);

                // Title
                using var tFont = new Font("Segoe UI", 10f, FontStyle.Bold, GraphicsUnit.Point);
                using var tFg   = new SolidBrush(Color.FromArgb(27, 46, 26));
                g.DrawString(card.Tag?.ToString() ?? "", tFont, tFg, new PointF(16, 12));

                // Divider
                using var div = new Pen(Color.FromArgb(229, 231, 235), 1f);
                g.DrawLine(div, 0, 44, card.Width, 44);
            };

            card.Tag = title;
            return card;
        }

        // ════════════════════════════════════════════════════════════════
        //  DONUT CHART
        // ════════════════════════════════════════════════════════════════
        private void PaintDonutChart(object? sender, PaintEventArgs e)
        {
            if (sender is not Panel p) return;
            var g = e.Graphics;
            g.SmoothingMode     = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            int total = _cntInStore + _cntAllocated + _cntUnderRepair + _cntDecommission + _cntOverdue;
            if (total == 0) total = 1;

            var segs = new[]
            {
                (Label:"Available",      Count:_cntInStore,      Color:Color.FromArgb(56,  142, 60)),
                (Label:"Allocated",      Count:_cntAllocated,    Color:Color.FromArgb(245, 124, 0)),
                (Label:"Under Repair",   Count:_cntUnderRepair,  Color:Color.FromArgb(33,  150, 243)),
                (Label:"Decommissioned", Count:_cntDecommission, Color:Color.FromArgb(239, 68,  68)),
                (Label:"Overdue",        Count:_cntOverdue,      Color:Color.FromArgb(255, 160, 0)),
            };

            int   top   = 54;
            int   avail = Math.Min(p.Width - 32, p.Height - top - 90);
            int   size  = Math.Max(80, avail);
            int   cx    = p.Width / 2;
            int   cy    = top + size / 2 + 8;
            var   outerR = new RectangleF(cx - size / 2f, cy - size / 2f, size, size);
            float inner  = size * 0.54f;
            var   innerR = new RectangleF(cx - inner / 2f, cy - inner / 2f, inner, inner);

            // Fixed: advance `start` by the animated sweep (animSweep), not the full
            // final sweep — otherwise each segment grows from its own final anchor
            // angle independently, producing visible gaps mid-animation instead of
            // one continuous expanding ring.
            float start = -90f;
            foreach (var seg in segs)
            {
                float sweep = (float)seg.Count / total * 360f;
                float animSweep = sweep * _donutAnimProgress;
                if (animSweep < 0.1f) continue;
                using var br = new SolidBrush(seg.Color);
                g.FillPie(br, outerR.X, outerR.Y, outerR.Width, outerR.Height, start, animSweep);
                start += animSweep;
            }

            using var hole = new SolidBrush(Color.White);
            g.FillEllipse(hole, innerR);

            // Centre label
            using var nf  = new Font("Segoe UI", 18f, FontStyle.Bold, GraphicsUnit.Point);
            using var sf  = new Font("Segoe UI", 8f,  FontStyle.Regular, GraphicsUnit.Point);
            using var fg  = new SolidBrush(Color.FromArgb(17, 24, 39));
            using var fg2 = new SolidBrush(Color.FromArgb(107, 114, 128));
            var ns = g.MeasureString(total.ToString(), nf);
            g.DrawString(total.ToString(), nf, fg, new PointF(cx - ns.Width / 2f, cy - ns.Height / 2f - 6));
            var ss = g.MeasureString("Total", sf);
            g.DrawString("Total", sf, fg2, new PointF(cx - ss.Width / 2f, cy + ns.Height / 2f - 8));

            // Legend
            int ly = (int)(outerR.Bottom + 14), lx = 12;
            using var lf = new Font("Segoe UI", 8.5f, FontStyle.Regular, GraphicsUnit.Point);
            foreach (var seg in segs)
            {
                if (lx + 130 > p.Width) { lx = 12; ly += 22; }
                using var db2 = new SolidBrush(seg.Color);
                g.FillEllipse(db2, lx, ly + 4, 10, 10);
                using var lc = new SolidBrush(Color.FromArgb(55, 65, 81));
                g.DrawString($"{seg.Label} ({seg.Count})", lf, lc, lx + 16, ly);
                lx += 140;
            }
        }

        // ════════════════════════════════════════════════════════════════
        //  BAR CHART
        // ════════════════════════════════════════════════════════════════
        // ════════════════════════════════════════════════════════════════
        //  BAR CHART — now vertical, matches "Asset Status Breakdown"
        // ════════════════════════════════════════════════════════════════
        // ════════════════════════════════════════════════════════════════
        //  BAR CHART — now vertical, matches "Asset Status Breakdown"
        // ════════════════════════════════════════════════════════════════
        private void PaintBarChart(object? sender, PaintEventArgs e)
        {
            if (sender is not Panel p) return;
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            var bars = new[]
            {
                (Label:"In Stock",        Count:_cntInStore,      Color:Color.FromArgb(56,  142, 60)),
                (Label:"Allocated",       Count:_cntAllocated,    Color:Color.FromArgb(245, 124, 0)),
                (Label:"Under Repair",    Count:_cntUnderRepair,  Color:Color.FromArgb(33,  150, 243)),
                (Label:"Decommissioned",  Count:_cntDecommission, Color:Color.FromArgb(239, 68,  68)),
                (Label:"Overdue",         Count:_cntOverdue,      Color:Color.FromArgb(255, 160, 0)),
            };

            int total = Math.Max(1, bars.Sum(b => b.Count));
            int maxVal = Math.Max(1, bars.Max(b => b.Count));
            int barCount = bars.Length;

            using var labelFont = new Font("Segoe UI", 8f, FontStyle.Regular, GraphicsUnit.Point);
            using var valFont = new Font("Segoe UI", 8.5f, FontStyle.Bold, GraphicsUnit.Point);
            using var labelFg = new SolidBrush(Color.FromArgb(55, 65, 81));
            using var bgBar = new SolidBrush(Color.FromArgb(240, 244, 240));

            // Reserve space: header (44, drawn by card) + top padding for value
            // labels above bars + bottom padding for (possibly 2-line) category
            // labels below bars, so nothing ever gets clipped regardless of panel size.
            int top = 54;
            int padSide = 16;
            int valueGap = 20; // room for "count (pct%)" above each bar
            float labelLineH = g.MeasureString("Ag", labelFont).Height;
            int labelGap = (int)(labelLineH * 2 + 8); // up to 2 wrapped lines below

            int chartX = padSide;
            int chartW = Math.Max(10, p.Width - padSide * 2);
            int chartTopY = top + valueGap;
            int chartH = Math.Max(10, p.Height - chartTopY - labelGap);

            float gapRatio = 0.35f;
            float colWidth = chartW / (float)barCount;
            float barWidth = colWidth * (1f - gapRatio);
            if (barWidth > 56f) barWidth = 56f; // don't let bars get absurdly fat on wide panels

            // Local helper: draw text centered under a bar, wrapping to a 2nd
            // line if it doesn't fit the column width instead of cutting off.
            void DrawWrappedLabel(string text, float centerX, float startY, float maxWidth)
            {
                var fullSize = g.MeasureString(text, labelFont);
                if (fullSize.Width <= maxWidth)
                {
                    g.DrawString(text, labelFont, labelFg, centerX - fullSize.Width / 2f, startY);
                    return;
                }

                // Split on the nearest space to the middle so both lines are
                // roughly balanced and nothing is truncated.
                var words = text.Split(' ');
                if (words.Length < 2)
                {
                    g.DrawString(text, labelFont, labelFg, centerX - fullSize.Width / 2f, startY);
                    return;
                }

                int mid = words.Length / 2;
                string line1 = string.Join(" ", words.Take(mid));
                string line2 = string.Join(" ", words.Skip(mid));

                var l1Size = g.MeasureString(line1, labelFont);
                var l2Size = g.MeasureString(line2, labelFont);
                g.DrawString(line1, labelFont, labelFg, centerX - l1Size.Width / 2f, startY);
                g.DrawString(line2, labelFont, labelFg, centerX - l2Size.Width / 2f, startY + labelLineH);
            }

            for (int i = 0; i < barCount; i++)
            {
                var b = bars[i];
                float colCenterX = chartX + colWidth * i + colWidth / 2f;
                float barX = colCenterX - barWidth / 2f;

                // Background track (full height column)
                using var trackPath = RoundedRectF(new RectangleF(barX, chartTopY, barWidth, chartH), 5f);
                g.FillPath(bgBar, trackPath);

                // Filled bar, growing upward from the bottom — animated from 0 to full height
                float targetFillH = maxVal == 0 ? 0 : Math.Max(b.Count > 0 ? barWidth * 0.4f : 0, (float)b.Count / maxVal * chartH);
                float fillH = targetFillH * _barAnimProgress;

                if (fillH > 0)
                {
                    float barY = chartTopY + (chartH - fillH);
                    using var grad = new LinearGradientBrush(
                        new PointF(barX, chartTopY + chartH), new PointF(barX, barY),
                        ControlPaint.Light(b.Color, 0.3f), b.Color);
                    using var fillPath = RoundedRectF(new RectangleF(barX, barY, barWidth, fillH), 5f);
                    g.FillPath(grad, fillPath);

                    // Value + pct, centered above the bar
                    float pct = (float)b.Count / total * 100f;
                    string valStr = $"{b.Count} ({pct:0}%)";
                    var vSz = g.MeasureString(valStr, valFont);
                    using var vFg = new SolidBrush(b.Color);
                    float vY = Math.Max(top, barY - vSz.Height - 2);
                    g.DrawString(valStr, valFont, vFg, colCenterX - vSz.Width / 2f, vY);
                }
                else
                {
                    // Zero-value bars: still show "0 (0%)" above the empty track
                    string valStr = "0 (0%)";
                    var vSz = g.MeasureString(valStr, valFont);
                    using var vFg = new SolidBrush(Color.FromArgb(156, 163, 175));
                    g.DrawString(valStr, valFont, vFg, colCenterX - vSz.Width / 2f, chartTopY - vSz.Height - 2);
                }

                // Category label below the column, wrapped if needed
                DrawWrappedLabel(b.Label, colCenterX, chartTopY + chartH + 6, colWidth - 6);
            }
        }

        // ════════════════════════════════════════════════════════════════
        //  CATEGORY BREAKDOWN CHART — Equipment count by Category
        // ════════════════════════════════════════════════════════════════
        private void PaintCategoryBreakdownChart(object? sender, PaintEventArgs e)
        {
            if (sender is not Panel p) return;
            var g = e.Graphics;
            g.SmoothingMode     = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            // ── Empty state ──────────────────────────────────────────────
            if (_categoryBreakdown.Count == 0)
            {
                using var emptyFont = new Font("Segoe UI", 10f, FontStyle.Regular, GraphicsUnit.Point);
                using var emptyFg   = new SolidBrush(Color.FromArgb(156, 163, 175));
                var msg  = "No category data available";
                var msgSz = g.MeasureString(msg, emptyFont);
                g.DrawString(msg, emptyFont, emptyFg,
                    (p.Width  - msgSz.Width)  / 2f,
                    (p.Height - msgSz.Height) / 2f);
                return;
            }

            // ── Palette — cycles if more categories than colours ─────────
            var palette = new[]
            {
                Color.FromArgb(94,  53,  177),
                Color.FromArgb(245, 124,   0),
                Color.FromArgb(56,  142,  60),
                Color.FromArgb(33,  150, 243),
                Color.FromArgb(239,  68,  68),
                Color.FromArgb(123,  31, 162),
            };

            // ── Layout — identical constants to PaintBarChart ────────────
            int   top    = 54, padL = 160, padR = 120, padB = 20;
            int   chartW = p.Width  - padL - padR;
            int   chartH = p.Height - top  - padB - 8;
            int   chartX = padL, chartY = top;

            int   barCount = _categoryBreakdown.Count;
            int   total    = Math.Max(1, _categoryBreakdown.Sum(c => c.Count));
            int   maxVal   = Math.Max(1, _categoryBreakdown.Max(c => c.Count));

            using var labelFont = new Font("Segoe UI", 9f,   FontStyle.Regular, GraphicsUnit.Point);
            using var valFont   = new Font("Segoe UI", 8.5f, FontStyle.Bold,    GraphicsUnit.Point);
            using var labelFg   = new SolidBrush(Color.FromArgb(55,  65, 81));
            using var bgBar     = new SolidBrush(Color.FromArgb(240, 244, 240));

            // Cap bar height so the chart stays balanced with many or few categories
            float rowH = (float)chartH / barCount;
            float barH = Math.Clamp(rowH * 0.50f, 14f, 32f);

            for (int i = 0; i < barCount; i++)
            {
                var   cat   = _categoryBreakdown[i];
                Color color = palette[i % palette.Length];
                // Slide-up entrance: each row starts offset below its final position
                // and eases upward to 0 offset, using the same _barAnimProgress curve
                // driving the other charts.
                float rowDelay = i * 0.08f; // stagger start slightly per row
                float rowProgress = Math.Clamp((_barAnimProgress - rowDelay) / (1f - rowDelay), 0f, 1f);
                float verticalOffset = (1f - rowProgress) * 24f;
                float by = chartY + i * rowH + (rowH - barH) / 2f + verticalOffset;

                // Background track
                using var trackPath = RoundedRectF(new RectangleF(chartX, by, chartW, barH), 5f);
                g.FillPath(bgBar, trackPath);

                // Filled bar (gradient, identical style to PaintBarChart) — full width immediately,
                // the row itself slides up into place instead (see verticalOffset below).
                float fillW = maxVal == 0 ? 0 : Math.Max(barH, (float)cat.Count / maxVal * chartW);

                if (fillW > 0)
                {
                    using var grad = new LinearGradientBrush(
                        new PointF(chartX, by), new PointF(chartX + fillW, by),
                        ControlPaint.Light(color, 0.3f), color);
                    using var fillPath = RoundedRectF(new RectangleF(chartX, by, fillW, barH), 5f);
                    g.FillPath(grad, fillPath);
                }

                // Category label (left of track)
                var lSz = g.MeasureString(cat.Category, labelFont);
                g.DrawString(cat.Category, labelFont, labelFg,
                    chartX - lSz.Width - 10, by + barH / 2f - lSz.Height / 2f);

                // Count + percentage (right of bar, coloured like the bar)
                float pct     = (float)cat.Count / total * 100f;
                string valStr = $"{cat.Count}  ({pct:0}%)";
                var vSz = g.MeasureString(valStr, valFont);
                using var vFg = new SolidBrush(color);
                g.DrawString(valStr, valFont, vFg,
                    chartX + fillW + 8, by + barH / 2f - vSz.Height / 2f);
            }
        }

        // ════════════════════════════════════════════════════════════════
        //  SIDEBAR POLISH
        // ════════════════════════════════════════════════════════════════
        private void ApplySidebarRoundedCorners()
        {
            if (panelMenu.Width <= 0 || panelMenu.Height <= 0) return;
            int r = 22;   // increased from 16
            int d = r * 2;
            var rect = new Rectangle(0, 0, panelMenu.Width, panelMenu.Height);
            var path = new GraphicsPath();
            // Left side square (flush with window edge), right side rounded
            path.AddLine(rect.X, rect.Y, rect.Right - d, rect.Y);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddLine(rect.Right, rect.Y + r, rect.Right, rect.Bottom - r);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddLine(rect.Right - r, rect.Bottom, rect.X, rect.Bottom);
            path.AddLine(rect.X, rect.Bottom, rect.X, rect.Y);
            path.CloseFigure();
            panelMenu.Region = new Region(path);
        }

        private void AddSidebarDivider(Button afterButton)
        {
            var divider = new Panel
            {
                Height = 1,
                Dock = DockStyle.Top,
                BackColor = Color.FromArgb(40, 255, 255, 255) // soft translucent line on dark green
            };
            int index = panelMenu.Controls.IndexOf(afterButton);
            panelMenu.Controls.Add(divider);
            panelMenu.Controls.SetChildIndex(divider, index); // sits directly above it in dock order
        }

        private void PaintSidebarShadow(object? sender, PaintEventArgs e)
        {
            using var br = new LinearGradientBrush(
                new Rectangle(0, 0, 10, panelDesktop.Height),
                Color.FromArgb(40, 0, 0, 0), Color.FromArgb(0, 0, 0, 0),
                LinearGradientMode.Horizontal);
            e.Graphics.FillRectangle(br, 0, 0, 10, panelDesktop.Height);
        }

        // ════════════════════════════════════════════════════════════════
        //  HELPERS
        // ════════════════════════════════════════════════════════════════
        private static GraphicsPath RoundedRect(Rectangle r, int radius)
        {
            var path = new GraphicsPath();
            int d = radius * 2;
            path.AddArc(r.X,         r.Y,          d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y,          d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0,   90);
            path.AddArc(r.X,         r.Bottom - d, d, d, 90,  90);
            path.CloseFigure();
            return path;
        }

        private static GraphicsPath RoundedRectF(RectangleF r, float radius)
        {
            var path = new GraphicsPath();
            float d = radius * 2f;
            path.AddArc(r.X,         r.Y,          d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y,          d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0,   90);
            path.AddArc(r.X,         r.Bottom - d, d, d, 90,  90);
            path.CloseFigure();
            return path;
        }

        private static void AttachCardClick(Control card, EventHandler handler)
        {
            card.Cursor = Cursors.Hand;
            card.Click += handler;
            foreach (Control child in card.Controls)
            {
                AttachCardClick(child, handler);
            }
        }

        // ════════════════════════════════════════════════════════════════
        //  NAVIGATION
        // ════════════════════════════════════════════════════════════════
        private void SetActiveButton(Button btn)
        {
            if (activeNavButton != null && activeNavButton != btn)
            {
                ThemeHelper.StyleSidebarButton(activeNavButton, active: false);
                activeNavButton.Invalidate();
                activeNavButton.Update();
            }
            ThemeHelper.StyleSidebarButton(btn, active: true);
            btn.Invalidate();
            btn.Update();
            activeNavButton = btn;


            PositionAccentIndicator(btn);
        }


        private void OpenChildForm(Form childForm, Button navBtn)
        {
            _animTimer?.Stop();
            _animTimer?.Dispose();
            _animTimer = null;

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

            panelStats.Visible = false;
        }

        private void ShowDashboard()
        {
            activeForm?.Close();
            activeForm = null;
            panelStats.Visible = true;
            SetActiveButton(btnDashboard);
            AnimateTitleTo("Dashboard");
            LoadStats();
        }

        private void btnDashboard_Click(object? sender, EventArgs e) => ShowDashboard();

        private void btnEquipment_Click(object? sender, EventArgs e)
        {
            AnimateTitleTo("Equipment");
            OpenChildForm(new EquipmentForm(), btnEquipment);
        }

        private void btnEmployees_Click(object? sender, EventArgs e)
        {
            AnimateTitleTo("Employees");
            OpenChildForm(new EmployeesForm(), btnEmployees);
        }

        private void btnLocations_Click(object? sender, EventArgs e)
        {
            AnimateTitleTo("Locations");
            OpenChildForm(new LocationsForm(), btnLocations);
        }

        private void btnAllocation_Click(object? sender, EventArgs e)
        {
            AnimateTitleTo("Allocation");
            OpenChildForm(new AllocationForm(), btnAllocation);
        }

        private void btnReturns_Click(object? sender, EventArgs e)
        {
            AnimateTitleTo("Returns");
            OpenChildForm(new ReturnsForm(), btnReturns);
        }

        private void btnTransfer_Click(object? sender, EventArgs e)
        {
            AnimateTitleTo("Transfer");
            OpenChildForm(new TransferForm(), btnTransfer);
        }

        private void btnDecommission_Click(object? sender, EventArgs e)
        {
            if (!EnsureAdmin()) return;
            AnimateTitleTo("Decommission");
            OpenChildForm(new DecommissionForm(), btnDecommission);
        }

        private void btnDeletedItems_Click(object? sender, EventArgs e)
        {
            if (!EnsureAdmin()) return;
            AnimateTitleTo("Deleted Items");
            OpenChildForm(new DeletedItemsForm(), btnDeletedItems);
        }

        private void btnLogs_Click(object? sender, EventArgs e)
        {
            AnimateTitleTo("Logs");
            OpenChildForm(new LogsForm(), btnLogs);
        }

        private void btnQRScanner_Click(object? sender, EventArgs e)
        {
            AnimateTitleTo("QR Scanner");
            OpenChildForm(new QRScannerForm(), btnQRScanner);
        }

        private void btnCheckInOut_Click(object? sender, EventArgs e)
        {
            AnimateTitleTo("Check In/Out");
            OpenChildForm(new CheckInOutForm(), btnCheckInOut);
        }

        private void btnReports_Click(object? sender, EventArgs e)
        {
            AnimateTitleTo("Reports");
            OpenChildForm(new ReportsForm(), btnReports);
        }

        private void btnSettings_Click(object? sender, EventArgs e)
        {
            if (!EnsureAdmin()) return;
            AnimateTitleTo("Settings");
            OpenChildForm(new SettingsForm(), btnSettings);
        }

        private void btnLogout_Click(object? sender, EventArgs e)
        {
            var result = MessageBox.Show(
                "Are you sure you want to log out?",
                "Confirm Logout",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                _animTimer?.Stop();
                _animTimer?.Dispose();
                _animTimer = null;

                _titleTypeTimer?.Stop();
                _titleTypeTimer?.Dispose();
                _titleTypeTimer = null;

                activeForm?.Close();
                activeForm = null;
                AuthService.ClearCurrentUser();
                this.Hide();

                using var login = new LoginForm();
                if (login.ShowDialog() == DialogResult.OK)
                {
                    panelContent.Controls.Clear();
                    panelStats.Visible = true;
                    activeNavButton    = null;
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
