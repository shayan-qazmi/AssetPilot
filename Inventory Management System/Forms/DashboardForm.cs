// Forms/DashboardForm.cs
using Inventory_Management_System.Forms;
using Inventory_Management_System.Helpers;
using Inventory_Management_System.Models;
using Inventory_Management_System.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

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

    internal class NoActivatePopup : Form
    {
        protected override bool ShowWithoutActivation => true;
        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                cp.ExStyle |= 0x08000000; // WS_EX_NOACTIVATE
                return cp;
            }
        }
    }

    // BorderStyle.None alone doesn't always stop Windows painting its own
    // border on a TextBox — stripping WS_BORDER / WS_EX_CLIENTEDGE at the
    // window-style level (not just intercepting the paint message) is what
    // actually removes it reliably.
    internal class NoBorderTextBox : TextBox
    {
        private const int WS_BORDER = 0x00800000;
        private const int WS_EX_CLIENTEDGE = 0x00000200;
        private const int WS_EX_STATICEDGE = 0x00020000;

        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                cp.Style &= ~WS_BORDER;
                cp.ExStyle &= ~WS_EX_CLIENTEDGE;
                cp.ExStyle &= ~WS_EX_STATICEDGE;
                return cp;
            }
        }

        protected override void WndProc(ref Message m)
        {
            const int WM_NCPAINT = 0x0085;
            if (m.Msg == WM_NCPAINT) return;
            base.WndProc(ref m);
        }
    }

    // Standard Label wraps text onto a second line whenever there's enough
    // vertical room — AutoEllipsis only kicks in when there's NO room to
    // wrap. That's why "long" rows were wrapping and overlapping the line
    // below instead of truncating. This forces true single-line + ellipsis
    // rendering no matter how tall the label is.
    internal class SingleLineLabel : Label
    {
        protected override void OnPaint(PaintEventArgs e)
        {
            var flags = TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding;
            flags |= TextAlign switch
            {
                ContentAlignment.MiddleLeft => TextFormatFlags.VerticalCenter | TextFormatFlags.Left,
                ContentAlignment.MiddleCenter => TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter,
                ContentAlignment.MiddleRight => TextFormatFlags.VerticalCenter | TextFormatFlags.Right,
                _ => TextFormatFlags.Top | TextFormatFlags.Left
            };
            TextRenderer.DrawText(e.Graphics, Text, Font, ClientRectangle, ForeColor, flags);
        }
    }

    internal class SharpPictureBox : PictureBox
    {
        public SharpPictureBox()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);
        }

        protected override void OnPaint(PaintEventArgs pe)
        {
            pe.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            pe.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            pe.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            base.OnPaint(pe);
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
        private List<Panel> _statCards = new();
        private List<(Panel Row, Action OnSelect)> _searchResultRows = new();
        private int _searchSelectedIndex = -1;

        // ── Chart panels ──────────────────────────────────────────────────
        private Panel? _panelCharts  = null;
        private Panel? _panelDonut   = null;
        private Panel? _panelBar     = null;
        private Panel? _panelLine    = null;
        private Panel? _panelActivities = null;
        private Panel? _pnlSidebarHeader;
        private Panel? _pnlSearchBox;
        private TextBox? _txtSearch;
        private Panel? _pnlUserProfile;
        private Form? _profilePopup;
        private string _profileFullName = "";
        private string _profileUsername = "";
        private string _profileEmail = "";
        private Label? _lblProfileChevron;
        private float _chevronRotation = 0f; // 0 = pointing down (closed), 180 = pointing up (open)
        private System.Windows.Forms.Timer? _chevronAnimTimer;

        private Label? _lblBarPeriod;
        private Form? _barPeriodPopup;

        private Panel? _panelEmployees = null;
        private Panel? _panelLocation = null;
        private Panel? _panelTrend = null;

        private List<(string Name, int Count, string Dept, string Email, string Code)> _topEmployees = new();
        private List<(string Location, string RoomLabel, int InStock, int Allocated, int Repair)> _locationBreakdown = new();

        private List<(RectangleF Bounds, string Name, int Count)> _empHitRows = new();
        private int _empHoverIndex = -1;

        private List<(RectangleF Bounds, string Location, string RoomLabel, int InStock, int Allocated, int Repair)> _locHitCols = new();
        private int _locHoverIndex = -1;
        private Point _locMousePos;
        private Button[] AdminOnlyButtons => new[] { btnDecommission, btnSettings, btnDeletedItems };
        // ── Top bar (yellow, matched to logo) ───────────────────────────────
        private static readonly Color TopBarYellow = Color.FromArgb(0x99, 0xC2, 0x21);        // ── Title typing animation ───────────────────────────────────────
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
        private const float CardAnimStepPerTick = 0.030f; // ~600ms entrance — a bit snappier, still paced to match the charts
        private const int CenterCardIndex = 3; // "Under Repair"

        // Elapsed-time-based chart animation — replaces the old feedback-loop
        // increment (which re-eased an already-eased value each tick, making
        // the actual duration unpredictable). Tune these two constants to
        // make the pie/bars faster or slower — duration in milliseconds.
        // Stopwatch-based timing — immune to WinForms Timer tick drift under
        // CPU load. Only starts once card entrance is fully done (see
        // AnimTimer_Tick), so chart duration is always accurate regardless
        // of how long the card animation took.
        private readonly Stopwatch _chartAnimClock = new();
        // Chart/activities animation duration — deliberately independent of the card
        // entrance speed (CardAnimStepPerTick). They still START at the same instant
        // (both begin in LoadStats), but this controls how long they take to settle.
        // Increase this value to slow them down further; it no longer needs to match
        // how fast the cards finish.
        private const float TopSectionAnimDurationMs = 1800f;
        private const float DonutAnimDurationMs = TopSectionAnimDurationMs;
        private const float BarAnimDurationMs = TopSectionAnimDurationMs;
        private const float ActivitiesAnimDurationMs = TopSectionAnimDurationMs;

        // ── Hover state (donut / bar / category charts) ─────────────────
        private PointF _donutCenter;
        private float _donutOuterRadius, _donutInnerRadius;
        private List<(string Label, int Count, Color Color, float RelStart, float RelSweep)> _donutHitSlices = new();
        private int _donutHoverIndex = -1;
        private Point _donutMousePos;

        private List<(RectangleF Bounds, string Label, int Count, float Pct, Color Color)> _barHitCols = new();
        private int _barHoverIndex = -1;
        private Point _barMousePos;

        private List<(RectangleF Bounds, string Category, int Count, float Pct, Color Color)> _catHitRows = new();
        private int _catHoverIndex = -1;
        private Point _catMousePos;


        
        private List<(RectangleF Bounds, string Month, string DateLabel, float Allocated, float InStock)> _trendHitPoints = new();
        private int _trendHoverIndex = -1;
        private Point _trendMousePos;
        private string _trendPeriod = "This Year";
        private Label? _lblTrendPeriod;
        private Form? _trendPeriodPopup;

        // ── Cached chart fonts — created once, reused across every repaint
        // instead of allocating (and GDI-handle-backed) fonts every single
        // Paint call. These fire at ~66fps during a 2.8s animation, so this
        // matters. ──
        private static readonly Font ChartLabelFont = new("Segoe UI", 8f, FontStyle.Regular, GraphicsUnit.Point);
        private static readonly Font ChartValueFont = new("Segoe UI", 8.5f, FontStyle.Bold, GraphicsUnit.Point);
        private static readonly Font ChartCatLabelFont = new("Segoe UI", 9f, FontStyle.Regular, GraphicsUnit.Point);
        private static readonly Font ChartTitleFont = new("Segoe UI", 10f, FontStyle.Bold, GraphicsUnit.Point);
        private static readonly Font DonutNumberFont = new("Segoe UI", 16f, FontStyle.Bold, GraphicsUnit.Point);
        private static readonly Font DonutSubFont = new("Segoe UI", 7.5f, FontStyle.Regular, GraphicsUnit.Point);
        private static readonly Font DonutLegendFont = new("Segoe UI", 8.25f, FontStyle.Regular, GraphicsUnit.Point);
        private static readonly Font DonutLegendBoldFont = new("Segoe UI", 8.25f, FontStyle.Bold, GraphicsUnit.Point);
        private static readonly Font TooltipTitleFont = new("Segoe UI", 8.5f, FontStyle.Bold);
        private static readonly Font TooltipSubFont = new("Segoe UI", 8f);
        private static readonly Font ChartTitleEmojiFont = new("Segoe UI Emoji", 10f, FontStyle.Regular, GraphicsUnit.Point);
        private const int BellRightOffset = 118; // bell sits left of the profile widget, both hugging the right edge
        private Panel? _pnlThemeToggle;
        private float _toggleThumbProgress = 0f;
        private bool _toggleIconWasDark = false;
        private System.Windows.Forms.Timer? _toggleAnimTimer;
        private float _barAnimProgress = 1f;
        private float _activitiesAnimProgress = 1f;
        private Rectangle _activitiesTargetBounds;
        private bool _activitiesIntroPlayed = false;

        // add a field near the other Dashboard fields
        private bool _titleInitialized = false;


        private enum CardAnimKind { Slide, Bounce }


        private bool _sidebarCollapsed = false;
        private int _sidebarExpandedWidth = 0;
        private const int SidebarCollapsedWidth = 64;
        private const int LogoSize = 90;
        private readonly Dictionary<Button, string> _navButtonFullText = new();
        private System.Windows.Forms.Timer? _sidebarCollapseTimer;
        private Button? _btnSidebarToggle;
        private readonly EquipmentService _searchEquipmentService = new();
        private readonly EmployeeService _searchEmployeeService = new();
        private Form? _searchResultsPopup;
        private System.Windows.Forms.Timer? _searchDebounceTimer;


        private ToolTip? _employeeTooltip;
        private string _barPeriod = "This Year"; 
        private Dictionary<string, int>? _periodOverrideCounts; // null = show live/all-time counts
        private Dictionary<string, float[]> _trendCache = new();
        private Dictionary<string, (string[] Labels, string[] DateLabels, float[] Allocated, float[] InStock)> _trendSeriesCache = new();
        private Dictionary<string, string?> _deltaCache = new();


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
            this.KeyPreview = true;
            // LoadStats() intentionally NOT called here — panelStats hasn't reached its
            // final (likely maximized) size yet at construction time, which is exactly
            // why the cards used to render cut-off until Dashboard was clicked again.

            panelContent.Resize += (s, e) =>
            {
                if (panelStats.Visible)
                {
                    LayoutStatCards();
                    LayoutCharts();
                }
            };

            panelMenu.Resize += (s, e) => PositionSidebarHeader();
            this.Shown += (s, e) =>
            {
                // BeginInvoke queues this for right after any pending maximize-resize
                // message has already been processed, guaranteeing panelTop/panelStats
                // widths are final — both for the stat cards and the top-bar widgets
                // (search box, theme toggle, profile avatar), which could otherwise be
                // positioned using a stale/zero width from before the window resized.
                BeginInvoke(new Action(() =>
                {
                    PositionTopSearchBar();
                    PositionTopProfileWidget();
                    panelTop.Invalidate(true);

                    if (panelStats.Visible) LoadStats();
                }));
            };
        }

            protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == (Keys.Control | Keys.K))
            {
                _txtSearch?.Focus();
                _txtSearch?.SelectAll();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }
        


        //  THEME
        // ════════════════════════════════════════════════════════════════
        private void ApplyTheme()
        {
            BackColor = ThemeHelper.Sidebar;

            // Dashboard's own top strip color — intentionally NOT ThemeHelper.Header,
            // since that color is shared by other forms' header strips.
            panelTop.BackColor = ThemeHelper.TopBarBg;
            panelTop.Paint -= PaintTopBarDiagonal;
            panelTop.Paint += PaintTopBarDiagonal;
            NotificationHelper.AttachBell(panelTop, this, rightOffset: BellRightOffset);
            BuildThemeToggle();
            BuildTopSearchBar();
            BuildTopProfileWidget();
            panelTop.Resize += (s, e) =>
            {
                if (_sidebarCollapseTimer == null) // only when NOT mid-animation — the timer already handles it then
                {
                    PositionTopSearchBar();
                    PositionTopProfileWidget();
                }
            };
            lblAppBrand.Dock      = DockStyle.Top;
            lblAppBrand.Height    = 150;
            lblAppBrand.TextAlign = ContentAlignment.MiddleCenter;
            lblTitle.AutoSize     = false;
            lblTitle.Dock         = DockStyle.Fill;
            lblTitle.TextAlign    = ContentAlignment.MiddleCenter;
            lblTitle.Font         = new Font(lblTitle.Font.FontFamily, 16F, FontStyle.Bold);
            lblTitle.ForeColor = ThemeHelper.TitleForeColor;
            lblTitle.BackColor = Color.Transparent;
            lblTitle.Visible = true;
            this.Text = "Q-Inventory";
            if (!_titleInitialized)
            {
                _titleInitialized = true;
                AnimateTitleTo("🏠 Dashboard"); // only type this out once, at startup
            }

            panelMenu.BackColor = ThemeHelper.Sidebar;

            panelMenu.Paint += (s, e) =>
            {
                var rect = panelMenu.ClientRectangle;
                if (rect.Width <= 0 || rect.Height <= 0) return;

                using var grad = new LinearGradientBrush(
                    rect,
                    ThemeHelper.Sidebar,
                    ControlPaint.Dark(ThemeHelper.Sidebar, 0.12f),
                    LinearGradientMode.Vertical);
                e.Graphics.FillRectangle(grad, rect);
            };
            panelMenu.Invalidate();

            EnsureSidebarHeaderPanel();

            // Hard-kill lblAppBrand and any stray PictureBox left directly on panelMenu
            // from the old (pre-refactor) logo logic — this is the second/bottom logo box.
            lblAppBrand.Visible = false;
            lblAppBrand.Size = Size.Empty;
            lblAppBrand.Dock = DockStyle.None;

            foreach (var strayPic in panelMenu.Controls.OfType<PictureBox>()
                         .Where(p => p.Name == "picLogoSide").ToList())
            {
                panelMenu.Controls.Remove(strayPic);
                strayPic.Dispose();
            }
            foreach (var strayDivider in panelMenu.Controls.OfType<Panel>()
                         .Where(p => p.Name == "pnlLogoDivider").ToList())
            {
                panelMenu.Controls.Remove(strayDivider);
                strayDivider.Dispose();
            }

            const int LogoSize = 55; // match this to whatever size the lower logo actually is (see note below)

            try
            {
                var logoPath = System.IO.Path.Combine(Application.StartupPath, "Images", "logo.png");
                if (System.IO.File.Exists(logoPath))
                {
                    var pic = _pnlSidebarHeader!.Controls.OfType<PictureBox>().FirstOrDefault(p => p.Name == "picLogoSide");
                    if (pic == null)
                    {
                        pic = new SharpPictureBox   // was: new PictureBox
                        {
                            Name = "picLogoSide",
                            SizeMode = PictureBoxSizeMode.Zoom,
                            Size = new Size(LogoSize, LogoSize),
                            BackColor = Color.Transparent
                        };
                        _pnlSidebarHeader.Controls.Add(pic);
                    }
                    pic.Size = new Size(LogoSize, LogoSize);
                    pic.Image = Image.FromFile(logoPath);
                    pic.BringToFront();
                }
            }
            catch { /* logo missing — header stays icon-only, no fallback text */ }

            var logoDivider = _pnlSidebarHeader!.Controls.OfType<Panel>().FirstOrDefault(p => p.Name == "pnlLogoDivider");
            if (logoDivider == null)
            {
                logoDivider = new Panel
                {
                    Name = "pnlLogoDivider",
                    Dock = DockStyle.Bottom,
                    Height = 1,
                    BackColor = Color.FromArgb(60, 255, 255, 255)
                };
                _pnlSidebarHeader.Controls.Add(logoDivider);
            }

            // Restyle all nav buttons for the new theme, but keep whichever screen is
            // ACTUALLY active — ApplyTheme() runs on every theme toggle (see
            // ToggleThemeAnimated), not just at startup, so it must never hard-reset
            // navigation state back to Dashboard.
            var currentActiveButton = activeNavButton ?? btnDashboard;
            var themeNavButtons = new[]
            {
    btnDashboard, btnEquipment, btnEmployees, btnLocations, btnAllocation,
    btnReturns, btnTransfer, btnDecommission, btnDeletedItems, btnLogs,
    btnQRScanner, btnCheckInOut, btnReports, btnSettings
};
            foreach (var navBtn in themeNavButtons)
                ThemeHelper.StyleSidebarButton(navBtn, active: navBtn == currentActiveButton);
            SetSidebarIcon(btnEmployees, "👥");
            activeNavButton = currentActiveButton;

            // Use one shared, animated accent bar instead of each button
            // drawing its own static one.
            ThemeHelper.UseGlobalAccentIndicator = true;
            EnsureAccentIndicator();
            PositionAccentIndicator(currentActiveButton, animate: false);

            // Logout — keep red
            btnLogout.UseVisualStyleBackColor              = false;
            btnLogout.FlatStyle                           = FlatStyle.Flat;
            btnLogout.FlatAppearance.BorderSize           = 0;
            btnLogout.BackColor                           = Color.Firebrick;
            btnLogout.ForeColor                           = Color.White;
            btnLogout.FlatAppearance.MouseOverBackColor   = Color.DarkRed;
            btnLogout.FlatAppearance.MouseDownBackColor   = Color.Maroon;
            // Icon-only, always — keep just the leading glyph, drop the label text.
            int logoutSpaceIdx = btnLogout.Text.IndexOf(' ');
            if (logoutSpaceIdx > 0) btnLogout.Text = btnLogout.Text.Substring(0, logoutSpaceIdx);
            btnLogout.TextAlign = ContentAlignment.MiddleCenter;
            btnLogout.Padding = new Padding(0);

            panelDesktop.BackColor = ThemeHelper.DashboardBg;
            panelStats.BackColor = ThemeHelper.DashboardBg;
            panelContent.BackColor = ThemeHelper.DashboardBg;
            panelContent.BackColor = ThemeHelper.DashboardBg;

            // Who's logged in — avatar + full name + @username, clickable
            // through to Settings > Users.
            BuildUserProfileFooter();
            BuildSidebarToggle();
        }

        private void BuildBarPeriodSelector(Panel host)
        {
         
            var wrap = new Panel
            {
                Size = new Size(110, 24),
                BackColor = ThemeHelper.PeriodDropBg,   // SOLID — covers the title underneath completely
                Cursor = Cursors.Hand
            };
            var lbl = new Label
            {
                Text = _barPeriod + "  ▾",
                Font = new Font("Segoe UI", 8f),
                ForeColor = ThemeHelper.ChartSecText,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleRight,
                BackColor = ThemeHelper.PeriodDropBg,   // also solid, not transparent
                Cursor = Cursors.Hand,
                AutoEllipsis = true
            };
            wrap.Controls.Add(lbl);

            void OpenPeriodPopup(object? s, EventArgs e) => ToggleBarPeriodPopup(lbl);
            wrap.Click += OpenPeriodPopup;
            lbl.Click += OpenPeriodPopup;

            host.Controls.Add(wrap);
            wrap.Location = new Point(host.Width - wrap.Width - 12, 10);
            wrap.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            wrap.BringToFront();
            _lblBarPeriod = lbl;
        }
        private void BuildTrendPeriodSelector(Panel host)
        {
            var wrap = new Panel
            {
                Size = new Size(110, 24),
                BackColor = ThemeHelper.PeriodDropBg,
                Cursor = Cursors.Hand
            };
            var lbl = new Label
            {
                Text = _trendPeriod + "  ▾",
                Font = new Font("Segoe UI", 8f),
                ForeColor = ThemeHelper.ChartSecText,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleRight,
                BackColor = ThemeHelper.PeriodDropBg,
                Cursor = Cursors.Hand,
                AutoEllipsis = true
            };
            wrap.Controls.Add(lbl);

            void OpenPeriodPopup(object? s, EventArgs e) => ToggleTrendPeriodPopup(lbl);
            wrap.Click += OpenPeriodPopup;
            lbl.Click += OpenPeriodPopup;

            host.Controls.Add(wrap);
            wrap.Location = new Point(host.Width - wrap.Width - 12, 10);
            wrap.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            wrap.BringToFront();
            _lblTrendPeriod = lbl;
        }

        private void ToggleTrendPeriodPopup(Label anchor)
        {
            if (_trendPeriodPopup != null && !_trendPeriodPopup.IsDisposed)
            {
                _trendPeriodPopup.Close();
                _trendPeriodPopup = null;
                return;
            }

            var options = new[] { "Today", "This Week", "Last Week", "This Month", "Last Month", "This Year" };
            var popup = new NoActivatePopup
            {
                FormBorderStyle = FormBorderStyle.None,
                StartPosition = FormStartPosition.Manual,
                ShowInTaskbar = false,
                Size = new Size(130, options.Length * 28 + 8),
                BackColor = ThemeHelper.PopupBg
            };
            popup.Paint += (s, e) =>
            {
                using var pen = new Pen(ThemeHelper.PopupBorder, 1);
                e.Graphics.DrawRectangle(pen, 0, 0, popup.Width - 1, popup.Height - 1);
            };

            var screenPoint = anchor.PointToScreen(new Point(0, anchor.Height));
            popup.Location = new Point(screenPoint.X - popup.Width + anchor.Width, screenPoint.Y + 4);

            int y = 4;
            foreach (var opt in options)
            {
                var row = new Label
                {
                    Text = opt,
                    Location = new Point(10, y),
                    Size = new Size(110, 26),
                    Font = ThemeHelper.FontSmall,
                    ForeColor = opt == _trendPeriod ? ThemeHelper.Accent : ThemeHelper.TextPrimary,
                    TextAlign = ContentAlignment.MiddleLeft,
                    Cursor = Cursors.Hand,
                    BackColor = ThemeHelper.PopupBg
                };
                row.MouseEnter += (s, e) => row.BackColor = ThemeHelper.PopupHoverBg;
                row.MouseLeave += (s, e) => row.BackColor = ThemeHelper.PopupBg;
                row.Click += (s, e) =>
                {
                    _trendPeriod = opt;
                    if (_lblTrendPeriod != null) _lblTrendPeriod.Text = _trendPeriod + "  ▾";
                    _panelTrend?.Invalidate();
                    popup.Close();
                };

                popup.Controls.Add(row);
                y += 28;
            }

            popup.Deactivate += (s, e) => popup.Close();
            popup.FormClosed += (s, e) => _trendPeriodPopup = null;
            _trendPeriodPopup = popup;
            popup.Show(this);
        }

        // AFTER
        private void ToggleBarPeriodPopup(Label anchor)
        {
            if (_barPeriodPopup != null && !_barPeriodPopup.IsDisposed)
            {
                _barPeriodPopup.Close();
                _barPeriodPopup = null;
                return;
            }

            var options = new[] { "Today", "This Week", "Last Week", "This Month", "Last Month", "This Year" };
            var popup = new NoActivatePopup
            {
                FormBorderStyle = FormBorderStyle.None,
                StartPosition = FormStartPosition.Manual,
                ShowInTaskbar = false,
                Size = new Size(130, options.Length * 28 + 8),
                BackColor = ThemeHelper.PopupBg
            };
            popup.Paint += (s, e) =>
            {
                using var pen = new Pen(ThemeHelper.PopupBorder, 1);
                e.Graphics.DrawRectangle(pen, 0, 0, popup.Width - 1, popup.Height - 1);
            };

            var screenPoint = anchor.PointToScreen(new Point(0, anchor.Height));
            popup.Location = new Point(screenPoint.X - popup.Width + anchor.Width, screenPoint.Y + 4);

            int y = 4;
            foreach (var opt in options)
            {
              
                var row = new Label
                {
                    Text = opt,
                    Location = new Point(10, y),
                    Size = new Size(110, 26),
                    Font = ThemeHelper.FontSmall,
                    ForeColor = opt == _barPeriod ? ThemeHelper.Accent : ThemeHelper.TextPrimary,
                    TextAlign = ContentAlignment.MiddleLeft,
                    Cursor = Cursors.Hand,
                    BackColor = ThemeHelper.PopupBg
                };
                row.MouseEnter += (s, e) => row.BackColor = ThemeHelper.PopupHoverBg;
                row.MouseLeave += (s, e) => row.BackColor = ThemeHelper.PopupBg;
                row.Click += (s, e) =>
                {
                    _barPeriod = opt;
                    if (_lblBarPeriod != null) _lblBarPeriod.Text = _barPeriod + "  ▾";
                    RecomputeBarChartForPeriod();
                    _panelBar?.Invalidate();
                    popup.Close();
                };
                popup.Controls.Add(row);
                y += 28;
            }

            popup.Deactivate += (s, e) => popup.Close();
            popup.FormClosed += (s, e) => _barPeriodPopup = null;
            _barPeriodPopup = popup;
            popup.Show(this);
        }

        private void LayoutStatCards()
        {
            // Cheap reposition-only pass — no DB call, no animation restart. Safe to call
            // on every collapse-animation tick. Note: if this fires *during* the entrance
            // animation (rare — only if you toggle the sidebar within ~1s of loading the
            // dashboard), the running animation will briefly fight this on the next tick;
            // not worth the extra bookkeeping to guard against for something this edge-case.
            if (_statCards.Count == 0) return;

            int padL = 12, gap = 10;
            int count = _statCards.Count;
            int totalW = panelStats.Width - padL * 2 - gap * (count - 1);
            int cardW = Math.Max(120, totalW / count);
            int x = padL;

            foreach (var card in _statCards)
            {
                card.SetBounds(x, 8, cardW, 180);
                x += cardW + gap;
            }
        }
        // ── Theme Toggle Button ───────────────────────────────────────────
        private void BuildThemeToggle()
        {
            var old = panelTop.Controls.OfType<Panel>().FirstOrDefault(p => p.Name == "pnlThemeToggle");
            old?.Dispose();

            const int d = 32;

            _pnlThemeToggle = new Panel
            {
                Name = "pnlThemeToggle",
                Size = new Size(d, d),
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };

            _pnlThemeToggle.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                var rect = new Rectangle(0, 0, d - 1, d - 1);

                // Mirror-flip: shows the pre-toggle icon in the first half of the
                // animation, the post-toggle icon in the second half, scaled through
                // zero width in between — a glass "flip" rather than a hard swap.
                float t = Math.Clamp(_toggleThumbProgress, 0f, 1f);
                bool showOldIcon = t < 0.5f;
                bool iconIsDark = showOldIcon ? _toggleIconWasDark : ThemeHelper.IsDarkMode;
                string icon = iconIsDark ? "🌙" : "☀";
                using var iconFont = new Font("Segoe UI Emoji", 14f);
                using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                using var iconFg = new SolidBrush(iconIsDark ? Color.FromArgb(235, 235, 245) : Color.FromArgb(255, 250, 235));

                float flipScale = MathF.Cos(t * MathF.PI); // 1 → 0 → -1, mirrors the glyph through its own centerline
                var iconState = g.Save();
                g.TranslateTransform(d / 2f, d / 2f);
                g.ScaleTransform(Math.Max(0.08f, MathF.Abs(flipScale)), 1f);
                g.TranslateTransform(-d / 2f, -d / 2f);
                g.DrawString(icon, iconFont, iconFg, rect, sf);
                g.Restore(iconState);
            };

            _pnlThemeToggle.Click += (s, e) => ToggleThemeAnimated();

            panelTop.Controls.Add(_pnlThemeToggle);
            _pnlThemeToggle.BringToFront();
            PositionThemeToggle();
        }

        private void ToggleThemeAnimated()
        {
            _toggleAnimTimer?.Stop();
            _toggleAnimTimer?.Dispose();

            _toggleIconWasDark = ThemeHelper.IsDarkMode; // capture pre-toggle state so the flip shows old→new
            ThemeHelper.ToggleTheme();

            // _toggleThumbProgress is repurposed here as a fade-out pulse-ring
            // progress (0 = just clicked, 1 = fully faded) for the glassy toggle —
            // it no longer drives a sliding thumb.
            _toggleThumbProgress = 0f;
            _toggleAnimTimer = new System.Windows.Forms.Timer { Interval = 12 };
            _toggleAnimTimer.Tick += (s, e) =>
            {
                _toggleThumbProgress += 0.15f;
                _pnlThemeToggle?.Invalidate();
                if (_toggleThumbProgress >= 1f)
                {
                    _toggleThumbProgress = 1f;
                    _toggleAnimTimer?.Stop();
                    _toggleAnimTimer?.Dispose();
                    _toggleAnimTimer = null;
                }
            };
            _toggleAnimTimer.Start();

            SuspendLayout();
            ApplyTheme();

            // Replay the full dashboard entrance animation on theme switch,
            // same as clicking the Dashboard nav button — not an instant snap.
            if (panelStats.Visible) LoadStats(instant: false);


            // Re-theme whatever child form is currently open. Every form in this
            // codebase follows the same "private void ApplyTheme()" convention,
            // so this refreshes it in place instead of requiring a re-navigate.
            if (activeForm != null)
            {
                var applyThemeMethod = activeForm.GetType().GetMethod(
                    "ApplyTheme", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
                applyThemeMethod?.Invoke(activeForm, null);

                // Some screens color grid rows/status cells at load time rather than
                // in ApplyTheme() itself — re-run their refresh method too, if they have one,
                // so row-level colors (not just chrome) pick up the new theme immediately.
                foreach (var refreshName in new[] { "RefreshGrid", "LoadData", "Refresh" })
                {
                    var refreshMethod = activeForm.GetType().GetMethod(
                        refreshName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                    if (refreshMethod != null && refreshMethod.GetParameters().All(p => p.IsOptional))
                    {
                        try { refreshMethod.Invoke(activeForm, refreshMethod.GetParameters().Select(p => p.DefaultValue).ToArray()); }
                        catch { /* best-effort — some Refresh() overloads aren't safe to call here */ }
                        break;
                    }
                }

                activeForm.Invalidate(true);
            }

            panelTop.Invalidate(true);
            ResumeLayout(true);
        }

        private void PositionThemeToggle()
        {
            if (_pnlThemeToggle == null) return;
            int bellLeftEdge = panelTop.Width - BellRightOffset;
            int toggleW = _pnlThemeToggle.Width;
            int toggleX = bellLeftEdge - 10 - toggleW;
            _pnlThemeToggle.Location = new Point(toggleX, (panelTop.Height - _pnlThemeToggle.Height) / 2);
            _pnlThemeToggle.Invalidate();
        }

        // Resolves a semi-transparent fill blended over an opaque backdrop into a
        // single flat, opaque color — used so the search box panel and the TextBox
        // sitting inside it are painted with the EXACT same solid color. Previously
        // the panel used an alpha-blended fill while the TextBox used a different
        // solid BackColor, so the TextBox always read as a separate visible box.
        private static Color ResolveOpaqueBlend(Color translucent, Color backdrop)
        {
            float a = translucent.A / 255f;
            int r = (int)(translucent.R * a + backdrop.R * (1 - a));
            int g = (int)(translucent.G * a + backdrop.G * (1 - a));
            int b = (int)(translucent.B * a + backdrop.B * (1 - a));
            return Color.FromArgb(r, g, b);
        }

        private void BuildTopSearchBar()
        {
            var existing = panelTop.Controls.OfType<Panel>().FirstOrDefault(p => p.Name == "pnlSearchBox");
            existing?.Dispose();

            const int boxHeight = 36;
            const int boxWidth = 300;
            const int searchRadius = 18; // fully rounded pill — reads as "glass", not a form field

            // One flat, opaque color used identically by the panel's Paint handler
            // AND the TextBox's BackColor — this is what makes the TextBox
            // invisible as a separate element instead of looking like a nested box.
            // Low-alpha overlay so the pill reads as frosted glass sitting on the
            // banner rather than a solid card — the banner color still dominates.
            Color glassFill = ThemeHelper.IsDarkMode
                ? ResolveOpaqueBlend(Color.FromArgb(60, 20, 22, 26), ThemeHelper.TopBarBg)
                : ResolveOpaqueBlend(Color.FromArgb(70, 255, 255, 255), ThemeHelper.TopBarBg);

            const int shadowPad = 10; // extra room below the pill so the drop shadow has
                                      // space to actually render instead of being clipped
                                      // by the panel's own Region (see below)

            _pnlSearchBox = new Panel
            {
                Name = "pnlSearchBox",
                Size = new Size(boxWidth, boxHeight + shadowPad),
                BackColor = Color.Transparent,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };

            // No tight pill-shaped Region anymore — that was clipping the shadow at
            // exactly the pill's edge. The pill still reads as rounded because it's
            // painted that way below; the panel's hit box is just the full rectangle.
            _pnlSearchBox.Region = null;

            _pnlSearchBox.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;

                // Pill is always drawn at the fixed boxHeight, at the top of the panel —
                // the extra shadowPad below is empty space reserved just for the shadow.
                var rect = new Rectangle(0, 0, _pnlSearchBox.Width - 1, boxHeight - 1);

                // Layered shadow, weighted downward — a soft wide pass plus a tighter
                // close pass just underneath the pill.
                using (var farShadowPath = RoundedRect(new Rectangle(rect.X - 2, rect.Y + 6, rect.Width + 4, rect.Height + 6), searchRadius))
                using (var farShadowBrush = new SolidBrush(Color.FromArgb(40, 0, 0, 0)))
                {
                    g.FillPath(farShadowBrush, farShadowPath);
                }
                using (var shadowPath = RoundedRect(new Rectangle(rect.X - 1, rect.Y + 3, rect.Width + 2, rect.Height + 3), searchRadius))
                using (var shadowBrush = new SolidBrush(Color.FromArgb(60, 0, 0, 0)))
                {
                    g.FillPath(shadowBrush, shadowPath);
                }

                using var path = RoundedRect(rect, searchRadius);

                using var fillBrush = new SolidBrush(glassFill);
                g.FillPath(fillBrush, path);

                using var borderPen = new Pen(Color.FromArgb(ThemeHelper.IsDarkMode ? 70 : 55, 255, 255, 255), 1f);
                g.DrawPath(borderPen, path);

                // Gloss highlight removed — it was a gradient painted across the whole
                // pill, and the TextBox sitting on top of it (flat single color, can't
                // reproduce a gradient) created a visible rectangular seam wherever it
                // sat. A flat fill everywhere keeps the pill and the textbox identical
                // with no seam at all.

                // magnifying glass icon
                Color iconColor = ThemeHelper.IsDarkMode ? Color.FromArgb(190, 195, 210) : Color.FromArgb(120, 120, 120);
                using var iconPen = new Pen(iconColor, 1.6f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
                int glassCx = 20, glassCy = boxHeight / 2 - 2;
                int glassR = 6;
                g.DrawEllipse(iconPen, glassCx - glassR, glassCy - glassR, glassR * 2, glassR * 2);
                g.DrawLine(iconPen,
                    glassCx + (int)(glassR * 0.7f), glassCy + (int)(glassR * 0.7f),
                    glassCx + glassR + 4, glassCy + glassR + 4);
            };

            _txtSearch = new NoBorderTextBox
            {
                Name = "txtSearch",
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 9f),
                ForeColor = ThemeHelper.SearchFgColor,
                // Must be IDENTICAL to the panel's own painted fill color — a different
                // shade here (even a "close" one) is exactly what makes the TextBox
                // read as its own separate visible box inside the pill.
                BackColor = glassFill,
                Location = new Point(36, (boxHeight - 18) / 2),
                Size = new Size(boxWidth - 36 - 52, 18),
                PlaceholderText = "Search anything..."
            };

            _pnlSearchBox.Controls.Add(_txtSearch);

            var lblShortcut = new Label
            {
                Text = "Ctrl K",
                Font = new Font("Segoe UI", 7f),
                ForeColor = ThemeHelper.IsDarkMode ? Color.FromArgb(130, 140, 155) : Color.FromArgb(170, 170, 170),
                AutoSize = false,
                Size = new Size(44, boxHeight),
                Location = new Point(boxWidth - 48, 0),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };
            _pnlSearchBox.Controls.Add(lblShortcut);

            panelTop.Controls.Add(_pnlSearchBox);
            _pnlSearchBox.BringToFront();

            PositionTopSearchBar();
           

            _txtSearch.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Down)
                {
                    MoveSearchSelection(1);
                    e.Handled = true; e.SuppressKeyPress = true;
                }
                else if (e.KeyCode == Keys.Up)
                {
                    MoveSearchSelection(-1);
                    e.Handled = true; e.SuppressKeyPress = true;
                }
                else if (e.KeyCode == Keys.Enter)
                {
                    if (_searchSelectedIndex == -1 && _searchResultRows.Count > 0)
                        _searchSelectedIndex = 0;

                    if (_searchSelectedIndex >= 0 && _searchSelectedIndex < _searchResultRows.Count)
                        _searchResultRows[_searchSelectedIndex].OnSelect();

                    e.Handled = true; e.SuppressKeyPress = true;
                }
                else if (e.KeyCode == Keys.Escape)
                {
                    if (_searchResultsPopup != null)
                    {
                        _searchResultsPopup.Close();
                        _txtSearch?.Focus();
                    }
                    e.Handled = true; e.SuppressKeyPress = true;
                }
            };

            _txtSearch.TextChanged += (s, e) =>
            {
                _searchDebounceTimer?.Stop();
                _searchDebounceTimer?.Dispose();
                _searchDebounceTimer = new System.Windows.Forms.Timer { Interval = 250 };
                _searchDebounceTimer.Tick += (s2, e2) =>
                {
                    _searchDebounceTimer.Stop();
                    _searchDebounceTimer.Dispose();
                    _searchDebounceTimer = null;
                    RunGlobalSearch(_txtSearch.Text.Trim());
                };
                _searchDebounceTimer.Start();
            };
        }

     private void MoveSearchSelection(int delta)
{
    if (_searchResultRows.Count == 0) return;
    if (_searchSelectedIndex == -1)
        _searchSelectedIndex = delta > 0 ? 0 : _searchResultRows.Count - 1;
    else
        _searchSelectedIndex = (_searchSelectedIndex + delta + _searchResultRows.Count) % _searchResultRows.Count;

            for (int i = 0; i < _searchResultRows.Count; i++)
                _searchResultRows[i].Row.BackColor = (i == _searchSelectedIndex)
                    ? (ThemeHelper.IsDarkMode ? Color.FromArgb(50, 54, 62) : Color.FromArgb(238, 240, 244))
                    : ThemeHelper.CardBg;

            var host = _searchResultRows[_searchSelectedIndex].Row.Parent as Panel;
    host?.ScrollControlIntoView(_searchResultRows[_searchSelectedIndex].Row);
    // _txtSearch keeps focus the whole time now — Enter/Down/Up all keep working
}

        private void RunGlobalSearch(string term)
        {
            if (_searchResultsPopup != null)
            {
                _searchResultsPopup.Close();
                _txtSearch?.Focus();
            }
            if (string.IsNullOrWhiteSpace(term)) return;

            var equipMatches = (_searchEquipmentService.GetAll().Data ?? new List<Equipment>())
     .Select(eq => new {
         Item = eq,
         Score = Math.Max(
             FuzzySearchHelper.FuzzyScoreCode(eq.EquipmentCode, term),
             Math.Max(
                 FuzzySearchHelper.FuzzyScoreCode(eq.QrCode, term),
                 Math.Max(
                     FuzzySearchHelper.FuzzyScore(eq.Name, term),
                     FuzzySearchHelper.FuzzyScore(eq.Make, term))))
     })
     .Where(x => x.Score > -10000)
     .OrderByDescending(x => x.Score) 
     .Take(5)
     .Select(x => x.Item)
     .ToList();

            var empMatches = (_searchEmployeeService.GetAll().Data ?? new List<Employee>())
                .Select(emp => new {
                    Item = emp,
                    Score = Math.Max(
                    FuzzySearchHelper.FuzzyScore(emp.FullName, term),
                    FuzzySearchHelper.FuzzyScore(emp.Department, term))
                })
                .Where(x => x.Score > -10000)
                .OrderByDescending(x => x.Score)
                .Take(5)
                .Select(x => x.Item)
                .ToList();

            if (equipMatches.Count == 0 && empMatches.Count == 0) return;
            BuildSearchResultsPopup(equipMatches, empMatches);
        }

        private void BuildSearchResultsPopup(List<Equipment> equipItems, List<Employee> empItems)
        {
            _searchResultRows.Clear();
            _searchSelectedIndex = -1;

            int rowCount = equipItems.Count + empItems.Count;
            // Wider than the search box itself — the pill is intentionally narrow,
            // but result rows (code + name + category) need more horizontal room
            // or they get squeezed and read as cut off.
            int popupWidth = Math.Max(_pnlSearchBox!.Width, 380);
            var popup = new NoActivatePopup
            {
                FormBorderStyle = FormBorderStyle.None,
                StartPosition = FormStartPosition.Manual,
                ShowInTaskbar = false,
                Size = new Size(popupWidth, Math.Min(380, 20 + rowCount * 40 + (equipItems.Count > 0 && empItems.Count > 0 ? 24 : 0))),
                BackColor = ThemeHelper.CardBg
            };

            // Right-align the wider popup to the search box's right edge, since
            // the popup can now be wider than the box that anchors it.
            var screenPoint = _pnlSearchBox.PointToScreen(new Point(_pnlSearchBox.Width, _pnlSearchBox.Height));
            popup.Location = new Point(screenPoint.X - popup.Width, screenPoint.Y + 4);

            var listPanel = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = ThemeHelper.CardBg };
            popup.Controls.Add(listPanel);

            int y = 4;

            void AddSectionHeader(string text)
            {
                listPanel.Controls.Add(new Label
                {
                    Text = text,
                    Location = new Point(12, y),
                    AutoSize = true,
                    Font = ThemeHelper.FontSmall,
                    ForeColor = ThemeHelper.TextSecondary
                });
                y += 22;
            }

            void AddRow(string title, string subtitle, Action onClick)
            {
                var row = new Panel { Location = new Point(0, y), Size = new Size(listPanel.Width - 18, 40), BackColor = ThemeHelper.CardBg, Cursor = Cursors.Hand };
                var lblT = new SingleLineLabel
                {
                    Text = title,
                    Font = ThemeHelper.FontBold,
                    ForeColor = ThemeHelper.TextPrimary,
                    TextAlign = ContentAlignment.MiddleLeft,
                    Location = new Point(12, 3),
                    Size = new Size(row.Width - 24, 18),
                    BackColor = Color.Transparent
                };
                var lblS = new SingleLineLabel
                {
                    Text = subtitle,
                    Font = ThemeHelper.FontSmall,
                    ForeColor = ThemeHelper.IsDarkMode ? Color.FromArgb(160, 165, 175) : Color.FromArgb(100, 104, 112),
                    TextAlign = ContentAlignment.MiddleLeft,
                    Location = new Point(12, 21),
                    Size = new Size(row.Width - 24, 18),
                    BackColor = Color.Transparent
                };
             
          
                row.Controls.Add(lblT);
                row.Controls.Add(lblS);

                Color rowDefaultBg = ThemeHelper.CardBg;
                Color rowHoverBg = ThemeHelper.IsDarkMode ? Color.FromArgb(50, 54, 62) : Color.FromArgb(238, 240, 244);

                void Go()
                {
                    popup.Close();
                    if (_txtSearch != null) _txtSearch.Text = "";
                    onClick();
                }

                row.MouseEnter += (s, e) => { row.BackColor = rowHoverBg; lblT.BackColor = Color.Transparent; lblS.BackColor = Color.Transparent; };
                row.MouseLeave += (s, e) => { if (_searchResultRows.Count > 0 && _searchResultRows[_searchSelectedIndex == -1 ? 0 : _searchSelectedIndex].Row != row) row.BackColor = rowDefaultBg; };
                row.Click += (s2, e2) => Go();
                lblT.Click += (s2, e2) => Go();
                lblS.Click += (s2, e2) => Go();

                listPanel.Controls.Add(row);
                _searchResultRows.Add((row, Go));
                y += 40;
            }

            // TODO (needs EquipmentForm.cs / EmployeesForm.cs source): these onClick
            // handlers currently just open the full list — they don't jump to or
            // highlight the specific matched record. Once those forms accept a
            // pre-filter/select parameter, wire it in here.
            if (equipItems.Count > 0)
            {
                AddSectionHeader("EQUIPMENT");
                foreach (var eq in equipItems)
                    AddRow($"{eq.EquipmentCode} — {eq.Name}", eq.Category ?? "",
                        () => { AnimateTitleTo("🖥️ Equipment"); OpenChildForm(new EquipmentForm(eq.EquipmentCode), btnEquipment); });
            }
            if (empItems.Count > 0)
            {
                AddSectionHeader("EMPLOYEES");
                foreach (var emp in empItems)
                    AddRow(emp.FullName, emp.Department ?? "",
                        () => { AnimateTitleTo("Employees"); OpenChildForm(new EmployeesForm(emp.EmployeeCode), btnEmployees); });
            }

            popup.Deactivate += (s, e) => popup.Close();
            popup.FormClosed += (s, e) => { _searchResultsPopup = null; _searchResultRows.Clear(); _searchSelectedIndex = -1; };
            _searchResultsPopup = popup;
            popup.Show(this);
            _txtSearch?.Focus();
        }
        // Strips whatever leading emoji/icon token the button's designer text already
        // carries, then prefixes the given one — prevents icons stacking (e.g. the old
        // "👥 👤 Employees" bug where a designer-set emoji and a code-added one collided).
        private static void SetSidebarIcon(Button btn, string emoji)
        {
            string baseText = btn.Text.TrimStart();
            int firstSpace = baseText.IndexOf(' ');
            if (firstSpace > 0 && baseText.Substring(0, firstSpace).Any(ch => ch > 127))
                baseText = baseText.Substring(firstSpace + 1).TrimStart();
            btn.Text = emoji + " " + baseText;
        }
        private void PositionTopSearchBar()
        {
            if (_pnlSearchBox == null) return;
            // Bell left-edge is (panelTop.Width - BellRightOffset).
            // Theme toggle sits 10px left of the bell.
            // Search bar sits 20px left of the toggle.
            int toggleW = _pnlThemeToggle?.Width ?? 60;
            int bellLeftEdge = panelTop.Width - BellRightOffset;
            int toggleLeft   = bellLeftEdge - 10 - toggleW;
            const int gapFromToggle = 20;
            _pnlSearchBox.Location = new Point(
                toggleLeft - gapFromToggle - _pnlSearchBox.Width,
                (panelTop.Height - _pnlSearchBox.Height) / 2);
            PositionThemeToggle();
        }
        private void BuildSidebarToggle()
        {
            EnsureSidebarHeaderPanel();

            var existing = _pnlSidebarHeader!.Controls.OfType<Button>().FirstOrDefault(b => b.Name == "btnSidebarToggle");
            existing?.Dispose();

            _btnSidebarToggle = new Button
            {
                Name = "btnSidebarToggle",
                Text = "\u25EB", 
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand,
                UseVisualStyleBackColor = false,
                TextAlign = ContentAlignment.MiddleCenter
            };
            _btnSidebarToggle.FlatAppearance.BorderSize = 0;
            _btnSidebarToggle.FlatAppearance.MouseOverBackColor = Color.FromArgb(35, 255, 255, 255);
            _btnSidebarToggle.Click += (s, e) => ToggleSidebar();

            _pnlSidebarHeader.Controls.Add(_btnSidebarToggle);
            _btnSidebarToggle.BringToFront();

            PositionSidebarHeader();
        }

        /// Single source of truth for the logo/toggle header zone — called on
        /// build and on every collapse/expand so the two never get out of sync.
        private void PositionSidebarHeader()
        {
            if (_pnlSidebarHeader == null) return;

            var logoPic = _pnlSidebarHeader.Controls.OfType<PictureBox>().FirstOrDefault(p => p.Name == "picLogoSide");

            if (_sidebarCollapsed)
            {
                if (logoPic != null) logoPic.Visible = false;
                lblAppBrand.Visible = false;

                _btnSidebarToggle!.Text = "\u25EB"; // ◫
                _btnSidebarToggle.Font = new Font("Segoe UI", 18f, FontStyle.Bold);
                _btnSidebarToggle.Size = new Size(SidebarCollapsedWidth, _pnlSidebarHeader.Height);
                _btnSidebarToggle.Location = new Point(0, 0);
            }
            else
            {
                int hostWidth = _pnlSidebarHeader.Width;

                if (logoPic != null)
                {
                    logoPic.Size = new Size(LogoSize, LogoSize);
                    logoPic.Location = new Point(Math.Max(0, (hostWidth - LogoSize) / 2), (_pnlSidebarHeader.Height - LogoSize) / 2);
                    logoPic.Visible = true;
                }
                lblAppBrand.Visible = logoPic == null;

                _btnSidebarToggle!.Text = "\u25EB"; // ◫
                _btnSidebarToggle.Font = new Font("Segoe UI", 15f, FontStyle.Bold); // was 11f — bumped up per your ask
                _btnSidebarToggle.Size = new Size(34, 34); // was 26x26
                _btnSidebarToggle.Location = new Point(10, (_pnlSidebarHeader.Height - 34) / 2); // vertically centered in the taller header, doesn't overlap the centered logo since it sits at x=10
            }

            _btnSidebarToggle.BringToFront();
            logoPic?.BringToFront();
            _pnlSidebarHeader.Invalidate();
        }

        private void EnsureSidebarHeaderPanel()
        {
            if (_pnlSidebarHeader != null) return;

            _pnlSidebarHeader = new Panel
            {
                Name = "pnlSidebarHeader",
                Dock = DockStyle.Top,
                Height = LogoSize + 2, // was hardcoded 64 — too short for a 90px logo, which is why it clipped to invisible
                BackColor = Color.Transparent
            };

            panelMenu.Controls.Add(_pnlSidebarHeader);
            _pnlSidebarHeader.SendToBack();
        }

        private void ToggleSidebar()
        {
            _sidebarCollapseTimer?.Stop();
            _sidebarCollapseTimer?.Dispose();

            if (!_sidebarCollapsed) _sidebarExpandedWidth = panelMenu.Width;

            int targetWidth = _sidebarCollapsed ? _sidebarExpandedWidth : SidebarCollapsedWidth;
            _sidebarCollapsed = !_sidebarCollapsed;

            var navButtons = new[]
            {
        btnDashboard, btnEquipment, btnEmployees, btnLocations, btnAllocation,
        btnReturns, btnTransfer, btnDecommission, btnDeletedItems, btnLogs,
        btnQRScanner, btnCheckInOut, btnReports, btnSettings
    };

            foreach (var b in navButtons)
            {
                if (_sidebarCollapsed)
                {
                    if (!_navButtonFullText.ContainsKey(b)) _navButtonFullText[b] = b.Text;
                    int spaceIdx = b.Text.IndexOf(' ');
                    b.Text = spaceIdx > 0 ? b.Text.Substring(0, spaceIdx) : b.Text;
                    b.TextAlign = ContentAlignment.MiddleCenter;
                    b.Padding = new Padding(0);
                }
                else if (_navButtonFullText.TryGetValue(b, out var full))
                {
                    b.Text = full;
                    b.TextAlign = ContentAlignment.MiddleLeft;
                    b.Padding = new Padding(16, 0, 0, 0);
                }
            }

            var footer = panelMenu.Controls.OfType<Panel>().FirstOrDefault(p => p.Name == "pnlUserFooter");
            if (footer != null)
                foreach (Label lbl in footer.Controls.OfType<Label>())
                    lbl.Visible = !_sidebarCollapsed;

            panelContent.SuspendLayout();
            activeForm?.SuspendLayout();

            // Elapsed-time-based (not fixed-step) so it's immune to WinForms Timer's
            // ~15ms tick-rate floor causing choppy jumps — same technique as the
            // dashboard chart animations. Short duration = fast, eased = smooth.
            int startWidth = panelMenu.Width;
            const float durationMs = 140f;
            var clock = System.Diagnostics.Stopwatch.StartNew();

            _sidebarCollapseTimer = new System.Windows.Forms.Timer { Interval = 8 };
            _sidebarCollapseTimer.Tick += (s, e) =>
            {
                float t = Math.Clamp(clock.ElapsedMilliseconds / durationMs, 0f, 1f);
                float eased = 1f - MathF.Pow(1f - t, 3f); // ease-out cubic

                int newWidth = (int)(startWidth + (targetWidth - startWidth) * eased);
                panelMenu.Width = newWidth;
                PositionSidebarHeader();
                PositionTopSearchBar();
                PositionTopProfileWidget();

                if (t >= 1f)
                {
                    panelMenu.Width = targetWidth;
                    _sidebarCollapseTimer?.Stop();
                    _sidebarCollapseTimer?.Dispose();
                    _sidebarCollapseTimer = null;
                    PositionSidebarHeader();

                    activeForm?.ResumeLayout(true);
                    panelContent.ResumeLayout(true);

                    if (panelStats.Visible)
                    {
                        LayoutStatCards();
                        LayoutCharts();
                    }
                }
            };
            _sidebarCollapseTimer.Start();
        }

        private void ApplyRolePermissions()
        {
            bool isAdmin = AuthService.IsAdmin;
            foreach (var btn in AdminOnlyButtons) btn.Visible = isAdmin;
        }

        private void BuildUserProfileFooter()
        {
            var existing = panelMenu.Controls.OfType<Panel>().FirstOrDefault(p => p.Name == "pnlUserFooter");
            if (existing != null)
            {
                // Rescue btnLogout before disposing the old footer — it's nested inside
                // footer now (see below), and Control.Dispose() disposes its children
                // too. Without this, btnLogout itself would get disposed on the very
                // next theme toggle and every click/style reference to it would break.
                existing.Controls.Remove(btnLogout);
                panelMenu.Controls.Remove(existing);
                existing.Dispose();
            }

            string fullName = AuthService.CurrentFullName;
            string username = "";
            string? picturePath = null;

            try
            {
                using var db = new InventoryDbContext();
                var user = db.Users.FirstOrDefault(u => u.Id == AuthService.CurrentUserId);
                if (user != null)
                {
                    username = user.Username;
                    picturePath = user.PicturePath;
                    if (string.IsNullOrWhiteSpace(fullName)) fullName = user.FullName;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "DashboardForm.BuildUserProfileFooter");
            }

            // footer now hosts BOTH the profile row AND btnLogout, so panelMenu only
            // ever has ONE Dock=Bottom child — no more two-siblings-racing-for-the-edge
            // ambiguity, which is what caused the profile card to vanish either way.
            const int profileRowHeight = 68;

            var footer = new Panel
            {
                Name = "pnlUserFooter",
                Dock = DockStyle.Bottom,
                Height = profileRowHeight + btnLogout.Height,
                BackColor = ControlPaint.Dark(ThemeHelper.Sidebar, 0.12f),
                Padding = new Padding(8, 4, 8, 4)
            };

            var divider = new Panel
            {
                Dock = DockStyle.Top,
                Height = 1,
                BackColor = Color.FromArgb(40, 255, 255, 255)
            };
            footer.Controls.Add(divider);

            var hoverStrip = new Panel
            {
                Location = new Point(6, 8),
                Size = new Size(footer.Width - 12, 52),
                BackColor = ControlPaint.Dark(ThemeHelper.Sidebar, 0.12f),
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top
            };
            hoverStrip.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using var path = RoundedRect(new Rectangle(0, 0, hoverStrip.Width - 1, hoverStrip.Height - 1), 10);
                if (hoverStrip.Tag is bool hover && hover)
                {
                    using var hoverBrush = new SolidBrush(Color.FromArgb(28, 255, 255, 255));
                    g.FillPath(hoverBrush, path);
                }
            };
            footer.Controls.Add(hoverStrip);
            divider.BringToFront();

            const int avatarSize = 50;
            var avatarImage = AvatarHelper.TryLoadUserImage(picturePath);
            footer.Disposed += (s, e) => avatarImage?.Dispose();

            var avatarBitmap = new Bitmap(avatarSize, avatarSize);
            using (var bg = Graphics.FromImage(avatarBitmap))
            {
                bg.SmoothingMode = SmoothingMode.AntiAlias;
                bg.InterpolationMode = InterpolationMode.HighQualityBicubic;
                bg.PixelOffsetMode = PixelOffsetMode.HighQuality;
                bg.Clear(ControlPaint.Dark(ThemeHelper.Sidebar, 0.12f));
                var rect = new Rectangle(0, 0, avatarSize - 1, avatarSize - 1);
                AvatarHelper.DrawAvatar(bg, rect, avatarImage);
                using var ringPen = new Pen(Color.FromArgb(90, 255, 255, 255), 1.3f);
                bg.DrawEllipse(ringPen, rect);
            }
            var avatarPanel = new PictureBox
            {
                Size = new Size(avatarSize, avatarSize),
                // Was measured against footer.Height (68 before merge). Now footer.Height
                // is 118 (profile row + logout), so this must use profileRowHeight
                // instead, or the avatar centers itself in the wrong band and drifts
                // down into where Logout now lives.
                Location = new Point(14, (profileRowHeight - avatarSize) / 2 - 3),
                BackColor = ControlPaint.Dark(ThemeHelper.Sidebar, 0.12f),
                Image = avatarBitmap,
                SizeMode = PictureBoxSizeMode.CenterImage
            };
            footer.Disposed += (s, e) => avatarBitmap.Dispose();
            footer.Controls.Add(avatarPanel);

            var lblName = new Label
            {
                Text = string.IsNullOrWhiteSpace(fullName) ? (string.IsNullOrWhiteSpace(username) ? "Admin" : username) : fullName,
                Location = new Point(avatarPanel.Right + 10, 14),
                AutoSize = true,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = ControlPaint.Dark(ThemeHelper.Sidebar, 0.12f)
            };
            footer.Controls.Add(lblName);

            var lblUsername = new Label
            {
                Text = "@" + (string.IsNullOrWhiteSpace(username) ? "admin" : username),
                Location = new Point(avatarPanel.Right + 10, lblName.Bottom + 1),
                AutoSize = true,
                Font = ThemeHelper.FontSmall,
                ForeColor = Color.FromArgb(190, 210, 190),
                BackColor = ControlPaint.Dark(ThemeHelper.Sidebar, 0.12f)
            };
            footer.Controls.Add(lblUsername);

            var lblChevron = new Label
            {
                Text = "›",
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.FromArgb(190, 210, 190),
                AutoSize = false,
                Size = new Size(18, profileRowHeight - 10),
                Location = new Point(footer.Width - 26, 4),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = ControlPaint.Dark(ThemeHelper.Sidebar, 0.12f),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            footer.Controls.Add(lblChevron);

            // Ensure hover strip sits behind the profile elements we just added
            hoverStrip.SendToBack();

            void SetHover(bool hovering)
            {
                hoverStrip.Tag = hovering;
                hoverStrip.Invalidate();
                lblChevron.ForeColor = hovering ? Color.White : Color.FromArgb(190, 210, 190);
            }
            void GoToUserProfile(object? s, EventArgs e)
            {
                if (!EnsureAdmin()) return;
                AnimateTitleTo("Settings");
                OpenChildForm(new SettingsForm(SettingsForm.InitialSection.Users), btnSettings);
            }

            foreach (Control c in new Control[] { footer, hoverStrip, avatarPanel, lblName, lblUsername, lblChevron })
            {
                c.Click += GoToUserProfile;
            }

            // Move btnLogout inside footer instead of leaving it as a second Dock=Bottom
            // sibling on panelMenu — this is the actual fix. Click handler, styling,
            // colors etc. are untouched; only its parent and Dock target change.
            btnLogout.Parent?.Controls.Remove(btnLogout);
            footer.Controls.Add(btnLogout);
            btnLogout.Dock = DockStyle.Bottom;

            panelMenu.Controls.Add(footer);
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

        private void BuildTopProfileWidget()
        {
            var existing = panelTop.Controls.OfType<Panel>().FirstOrDefault(p => p.Name == "pnlTopProfile");
            existing?.Dispose();

            string fullName = AuthService.CurrentFullName;
            string? picturePath = null;
            string username = "";
            string email = "";
            try
            {
                using var db = new InventoryDbContext();
                var user = db.Users.FirstOrDefault(u => u.Id == AuthService.CurrentUserId);
                if (user != null)
                {
                    picturePath = user.PicturePath;
                    username = user.Username ?? "";
                    email = user.Email ?? "";
                    if (string.IsNullOrWhiteSpace(fullName)) fullName = user.FullName;
                }
            }
            catch (Exception ex) { Logger.LogError(ex, "DashboardForm.BuildTopProfileWidget"); }

            _profileFullName = fullName;
            _profileUsername = username;
            _profileEmail = email;

            const int avatarSize = 36, gapAvatarChevron = 2, chevronW = 16; // bumped from 30 to visually
                                                                            // match the notification bell icon
            int widgetW = avatarSize + gapAvatarChevron + chevronW;
            int widgetH = Math.Max(avatarSize, 24);

            _pnlUserProfile = new Panel
            {
                Name = "pnlTopProfile",
                Size = new Size(widgetW, widgetH),
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };

            var avatarImage = AvatarHelper.TryLoadUserImage(picturePath);
            _pnlUserProfile.Disposed += (s, e) => avatarImage?.Dispose();

            var topAvatarBitmap = new Bitmap(avatarSize, avatarSize);
            using (var bg = Graphics.FromImage(topAvatarBitmap))
            {
                bg.SmoothingMode = SmoothingMode.AntiAlias;
                bg.InterpolationMode = InterpolationMode.HighQualityBicubic;
                bg.PixelOffsetMode = PixelOffsetMode.HighQuality;
                bg.Clear(ThemeHelper.TopBarBg); // exact banner color — no visible box behind the avatar
                var rect = new Rectangle(0, 0, avatarSize - 1, avatarSize - 1);
                AvatarHelper.DrawAvatar(bg, rect, avatarImage);
                using var ringPen = new Pen(
                    ThemeHelper.IsDarkMode ? Color.FromArgb(90, 255, 255, 255) : Color.FromArgb(60, 0, 0, 0), 1.3f);
                bg.DrawEllipse(ringPen, rect);
            }
            var avatarPanel = new PictureBox
            {
                Size = new Size(avatarSize, avatarSize),
                Location = new Point(0, (widgetH - avatarSize) / 2),
                BackColor = ThemeHelper.TopBarBg,
                Image = topAvatarBitmap,
                SizeMode = PictureBoxSizeMode.CenterImage
            };
            _pnlUserProfile.Disposed += (s, e) => topAvatarBitmap.Dispose();
            _pnlUserProfile.Controls.Add(avatarPanel);

            var lblChevron = new Label
            {
                Text = "▾",
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = ThemeHelper.IsDarkMode ? Color.FromArgb(210, 215, 225) : Color.FromArgb(70, 70, 70),
                AutoSize = false,
                Size = new Size(chevronW, widgetH),
                Location = new Point(avatarPanel.Right + gapAvatarChevron, 0),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };
            _pnlUserProfile.Controls.Add(lblChevron);
            _lblProfileChevron = lblChevron;

            void OpenMenu(object? s, EventArgs e)
            {
                if (_profilePopup == null || _profilePopup.IsDisposed)
                    OpenProfileDropdown();
            }
            _pnlUserProfile.Click += OpenMenu;
            avatarPanel.Click += OpenMenu;
            lblChevron.Click += OpenMenu;

            void OnEnter(object? s, EventArgs e)
            {
                if (_profilePopup == null || _profilePopup.IsDisposed)
                    OpenProfileDropdown();
            }
            _pnlUserProfile.MouseEnter += OnEnter;
            avatarPanel.MouseEnter += OnEnter;
            lblChevron.MouseEnter += OnEnter;

            panelTop.Controls.Add(_pnlUserProfile);
            _pnlUserProfile.BringToFront();
            PositionTopProfileWidget();
        }

        private void PositionTopProfileWidget()
        {
            if (_pnlUserProfile == null) return;
            const int rightMargin = 14;
            _pnlUserProfile.Location = new Point(
                panelTop.Width - _pnlUserProfile.Width - rightMargin,
                (panelTop.Height - _pnlUserProfile.Height) / 2);
            _pnlUserProfile.BringToFront();
            _pnlUserProfile.Invalidate(true);
        }

        private void ToggleProfileDropdown()
        {
            if (_profilePopup != null && !_profilePopup.IsDisposed)
                CloseProfileDropdown();
            else
                OpenProfileDropdown();
        }

        private System.Windows.Forms.Timer? _profileHoverWatcher;

        private void OpenProfileDropdown()
        {
            if (_profilePopup != null && !_profilePopup.IsDisposed) return;

            // Identity text is measured up front so the popup is sized to fit it —
            // fixes the email/name clipping regardless of how long it is.
            string roleLabel = AuthService.IsAdmin ? "ADMINISTRATOR" : "OPERATOR";
            string identityLine = !string.IsNullOrWhiteSpace(_profileEmail)
                ? _profileEmail
                : (!string.IsNullOrWhiteSpace(_profileFullName) ? _profileFullName : "@" + _profileUsername);

            // Not "using" — this Font must outlive the method since lblIdentity
            // keeps a reference to it after OpenProfileDropdown returns.
            var identityFont = new Font("Segoe UI", 8.75f, FontStyle.Bold);
            int measuredTextW = TextRenderer.MeasureText(identityLine, identityFont, Size.Empty, TextFormatFlags.NoPadding).Width;
            int popupWidth = Math.Clamp(measuredTextW + 60, 220, 360);

            var popup = new NoActivatePopup
            {
                FormBorderStyle = FormBorderStyle.None,
                StartPosition = FormStartPosition.Manual,
                ShowInTaskbar = false,
                Size = new Size(popupWidth, 130),
                BackColor = ThemeHelper.CardBg,
                Opacity = 0d // fades in via the animation below
            };
            popup.Disposed += (s, e) => identityFont.Dispose();

            popup.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using var path = RoundedRect(new Rectangle(0, 0, popup.Width - 1, popup.Height - 1), 10);
                popup.Region = new Region(path);
                using var pen = new Pen(ThemeHelper.IsDarkMode ? Color.FromArgb(55, 58, 65) : Color.FromArgb(225, 225, 225), 1);
                g.DrawPath(pen, path);
            };

            var screenPoint = _pnlUserProfile!.PointToScreen(new Point(0, _pnlUserProfile.Height));
            popup.Location = new Point(screenPoint.X - popup.Width + _pnlUserProfile.Width, screenPoint.Y + 2);

            Button MakeItem(string text, Action<Graphics, RectangleF> iconPainter, Action onClick)
            {
                var b = new Button
                {
                    Text = "   " + text, // reserves left space; the icon is drawn over it below
                    Dock = DockStyle.Top,
                    Height = 36,
                    FlatStyle = FlatStyle.Flat,
                    TextAlign = ContentAlignment.MiddleLeft,
                    Padding = new Padding(32, 0, 0, 0),
                    BackColor = ThemeHelper.CardBg,
                    ForeColor = ThemeHelper.TextPrimary,
                    Font = new Font("Segoe UI", 8.75f),
                    Cursor = Cursors.Hand
                };
                b.FlatAppearance.BorderSize = 0;
                b.FlatAppearance.MouseOverBackColor = ThemeHelper.IsDarkMode ? Color.FromArgb(50, 54, 62) : Color.FromArgb(245, 246, 245);
                b.Paint += (s, e) =>
                {
                    var iconBounds = new RectangleF(12, (b.Height - 17) / 2f, 17, 17);
                    iconPainter(e.Graphics, iconBounds);
                };
                b.Click += (s, e) =>
                {
                    // Stop hover watcher immediately so it cannot re-open
                    // the popup while we are in the middle of closing it.
                    _profileHoverWatcher?.Stop();
                    _profileHoverWatcher?.Dispose();
                    _profileHoverWatcher = null;

                    // Close popup synchronously (no fade) so the action's
                    // MessageBox does not trigger Deactivate mid-fade.
                    if (_profilePopup != null && !_profilePopup.IsDisposed)
                    {
                        var p = _profilePopup;
                        _profilePopup = null;
                        p.Close();
                    }
                    AnimateChevron(open: false);

                    onClick();
                };
                return b;
            }

            // ── Identity header — role/status + name/email, matching the
            // reference dropdown (role caption, then bold name/email below). ──
            var headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 54,
                BackColor = ThemeHelper.CardBg,
                Padding = new Padding(12, 8, 12, 6)
            };

            var lblRole = new Label
            {
                Text = roleLabel,
                Font = new Font("Segoe UI", 7.25f, FontStyle.Bold),
                ForeColor = ThemeHelper.Accent,
                AutoSize = true,
                Location = new Point(12, 8),
                BackColor = Color.Transparent
            };
            headerPanel.Controls.Add(lblRole);

            var lblIdentity = new SingleLineLabel
            {
                Text = identityLine,
                Font = identityFont, // same instance used for measuring — no mismatch
                ForeColor = ThemeHelper.TextPrimary,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoSize = false,
                Size = new Size(popup.Width - 24, 18),
                Location = new Point(12, lblRole.Bottom + 2),
                BackColor = Color.Transparent
            };
            headerPanel.Controls.Add(lblIdentity);

            var headerDivider = new Panel { Dock = DockStyle.Top, Height = 1, BackColor = ThemeHelper.IsDarkMode ? Color.FromArgb(55, 58, 65) : Color.FromArgb(235, 235, 235) };

            var divider = new Panel { Dock = DockStyle.Top, Height = 1, BackColor = ThemeHelper.IsDarkMode ? Color.FromArgb(55, 58, 65) : Color.FromArgb(235, 235, 235) };

            var btnProfileSettings = MakeItem("Account Settings",
                (g, r) => DrawAccountSettingsIcon(g, r, ThemeHelper.TextPrimary),
                () =>
                {
                    if (!EnsureAdmin()) return;
                    AnimateTitleTo("Settings");
                    OpenChildForm(new SettingsForm(SettingsForm.InitialSection.Users), btnSettings);
                });
            var btnLogoutItem = MakeItem("Logout",
                (g, r) => DrawPowerIcon(g, r, ThemeHelper.Danger),
                () => btnLogout_Click(this, EventArgs.Empty));

            // Dock.Top stacks in reverse insertion order
            popup.Controls.Add(btnLogoutItem);
            popup.Controls.Add(divider);
            popup.Controls.Add(btnProfileSettings);
            popup.Controls.Add(headerDivider);
            popup.Controls.Add(headerPanel);

            popup.Deactivate += (s, e) => CloseProfileDropdown();
            popup.FormClosed += (s, e) => { _profilePopup = null; };

            _profilePopup = popup;
            popup.Show(this);
            FadePopup(popup, fadeIn: true);

            AnimateChevron(open: true);
            StartProfileHoverWatcher();
        }

        private void CloseProfileDropdown()
        {
            _profileHoverWatcher?.Stop();
            _profileHoverWatcher?.Dispose();
            _profileHoverWatcher = null;

            if (_profilePopup != null && !_profilePopup.IsDisposed)
            {
                var popup = _profilePopup;
                _profilePopup = null;
                FadePopup(popup, fadeIn: false, onDone: () => popup.Close());
            }
            AnimateChevron(open: false);
        }

        /// Polls the real cursor position every tick instead of chaining
        /// MouseEnter/Leave across the widget's child controls and a separate
        /// popup window — those events are unreliable across window boundaries
        /// and small gaps, which is why the dropdown used to get stuck open.
        /// Closes the instant the cursor leaves both the widget and the popup.
        private void StartProfileHoverWatcher()
        {
            _profileHoverWatcher?.Stop();
            _profileHoverWatcher?.Dispose();

            _profileHoverWatcher = new System.Windows.Forms.Timer { Interval = 40 };
            _profileHoverWatcher.Tick += (s, e) =>
            {
                if (_pnlUserProfile == null || _profilePopup == null || _profilePopup.IsDisposed)
                {
                    _profileHoverWatcher?.Stop();
                    return;
                }

                var cursor = Cursor.Position;

                var widgetRect = _pnlUserProfile.RectangleToScreen(_pnlUserProfile.ClientRectangle);
                widgetRect.Inflate(4, 12);
                var popupRect = _profilePopup.Bounds;
                popupRect.Inflate(4, 4);

                bool overWidget = widgetRect.Contains(cursor);
                bool overPopup = popupRect.Contains(cursor);

                if (!overWidget && !overPopup)
                    CloseProfileDropdown();
            };
            _profileHoverWatcher.Start();
        }

        /// Simple opacity fade — smooths the popup's appear/disappear instead
        /// of the previous hard show/hide.
        private static void FadePopup(Form popup, bool fadeIn, Action? onDone = null)
        {
            const int steps = 6;
            const int intervalMs = 10;
            int step = 0;
            double from = fadeIn ? 0d : 1d;
            double to = fadeIn ? 1d : 0d;

            var timer = new System.Windows.Forms.Timer { Interval = intervalMs };
            timer.Tick += (s, e) =>
            {
                if (popup.IsDisposed) { timer.Stop(); timer.Dispose(); return; }
                step++;
                double t = Math.Min(1.0, step / (double)steps);
                popup.Opacity = from + (to - from) * t;
                if (t >= 1.0)
                {
                    timer.Stop();
                    timer.Dispose();
                    onDone?.Invoke();
                }
            };
            timer.Start();
        }

        // Smoothly rotates the ▾ chevron to point up (open) or back down
        // (closed) using a scale-flip through zero width — same visual
        // technique already used for the theme-toggle sun/moon icon flip.
        private void AnimateChevron(bool open)
        {
            if (_lblProfileChevron == null) return;

            _chevronAnimTimer?.Stop();
            _chevronAnimTimer?.Dispose();

            float target = open ? 1f : 0f;
            float start = _chevronRotation;

            const int steps = 8;
            int step = 0;
            _chevronAnimTimer = new System.Windows.Forms.Timer { Interval = 12 };
            _chevronAnimTimer.Tick += (s, e) =>
            {
                step++;
                float t = Math.Min(1f, step / (float)steps);
                _chevronRotation = start + (target - start) * t;
                DrawChevron();
                if (t >= 1f)
                {
                    _chevronAnimTimer?.Stop();
                    _chevronAnimTimer?.Dispose();
                    _chevronAnimTimer = null;
                }
            };
            _chevronAnimTimer.Start();
        }

        private void DrawChevron()
        {
            if (_lblProfileChevron == null) return;
            // Flip the glyph vertically as rotation progresses 0→1 by swapping
            // the char at the midpoint — reads as a smooth up/down rotation
            // without needing custom-painted vector arrows.
            _lblProfileChevron.Text = _chevronRotation < 0.5f ? "▾" : "▴";
        }

        // Returns 7 daily counts (oldest → today) for the sparkline. Currently
        // only "equipment" is wired up, sourced from EquipmentLogs the same way
        // RecomputeBarChartForPeriod() already reads that table. Returns all
        // zeros (sparkline just won't show anything meaningful) if the DB call
        // fails, so a bad connection never crashes card rendering.
        private float[] GetLast7DayCounts(string metric)
        {
            try
            {
                using var db = new InventoryDbContext();
                var today = DateTime.Today;
                var counts = new float[7];

                for (int i = 0; i < 7; i++)
                {
                    var day = today.AddDays(-6 + i);
                    var nextDay = day.AddDays(1);

                    counts[i] = metric switch
                    {
                        "equipment" => db.EquipmentLogs.Count(l =>
                            l.CreatedAt.HasValue && l.CreatedAt.Value >= day && l.CreatedAt.Value < nextDay),

                        "allocated" => db.EquipmentLogs.Count(l =>
                            l.ActionType == "allocation_approved" &&
                            l.CreatedAt.HasValue && l.CreatedAt.Value >= day && l.CreatedAt.Value < nextDay),

                        "in_store" => db.EquipmentLogs.Count(l =>
                            l.ActionType == "allocation_returned" &&
                            l.CreatedAt.HasValue && l.CreatedAt.Value >= day && l.CreatedAt.Value < nextDay),

                        "under_repair" => db.EquipmentLogs.Count(l =>
                            l.ActionType == "maintenance_opened" &&
                            l.CreatedAt.HasValue && l.CreatedAt.Value >= day && l.CreatedAt.Value < nextDay),

                        "decommissioned" => db.EquipmentLogs.Count(l =>
     l.ActionType == "decommission_approved" &&
     l.CreatedAt.HasValue && l.CreatedAt.Value >= day && l.CreatedAt.Value < nextDay),

                        "overdue" => db.EquipmentLogs.Count(l =>
                            l.ActionType == "allocation_overdue" &&
                            l.CreatedAt.HasValue && l.CreatedAt.Value >= day && l.CreatedAt.Value < nextDay),

                        _ => 0
                    };
                }

                return counts;
            }
            catch
            {
                return new float[] { 0, 0, 0, 0, 0, 0, 0 };
            }
        }

        private float[] GetCachedTrend(string metric)
        {
            if (!_trendCache.TryGetValue(metric, out var v)) { v = GetLast7DayCounts(metric); _trendCache[metric] = v; }
            return v;
        }

        private float[] GetLast12MonthCounts(string metric)
        {
            try
            {
                using var db = new InventoryDbContext();
                var counts = new float[12];
                var today = DateTime.Today;

                for (int i = 0; i < 12; i++)
                {
                    var monthStart = new DateTime(today.Year, today.Month, 1).AddMonths(-11 + i);
                    var monthEnd = monthStart.AddMonths(1);

                    counts[i] = metric switch
                    {
                        "allocated" => db.EquipmentLogs.Count(l =>
                            l.ActionType == "allocation_approved" &&
                            l.CreatedAt.HasValue && l.CreatedAt.Value >= monthStart && l.CreatedAt.Value < monthEnd),
                        "in_store" => db.EquipmentLogs.Count(l =>
                            l.ActionType == "allocation_returned" &&
                            l.CreatedAt.HasValue && l.CreatedAt.Value >= monthStart && l.CreatedAt.Value < monthEnd),
                        _ => 0
                    };
                }
                return counts;
            }
            catch { return new float[12]; }
        }

        private float[] GetCachedMonthlyTrend(string metric)
        {
            string key = metric + "_monthly";
            if (!_trendCache.TryGetValue(key, out var v)) { v = GetLast12MonthCounts(metric); _trendCache[key] = v; }
            return v;
        }

        private static DateTime StartOfWeek(DateTime date)
        {
            int diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
            return date.AddDays(-diff).Date;
        }
        // Full, human-readable date/time for a trend bucket — used in the chart tooltip
        // so hovering shows an actual date instead of just the short axis label ("Jan", "Mon"...).
        private static string FormatBucketDateLabel(string period, DateTime start, DateTime end)
        {
            return period switch
            {
                "Today" => $"{start:h:mm tt} – {end:h:mm tt}, {start:MMM d, yyyy}",
                "This Week" or "Last Week" or "This Month" or "Last Month" => start.ToString("MMM d, yyyy"),
                _ => start.ToString("MMMM yyyy") // "This Year" — 12 monthly buckets
            };
        }

        // Builds the actual data + labels for whichever period is selected in the
        // trend-chart dropdown — previously the dropdown existed but nothing
        // downstream read _trendPeriod, so selecting an option did nothing.
        private (string[] Labels, string[] DateLabels, float[] Allocated, float[] InStock) GetTrendData(string period)
        {
            var now = DateTime.Now;
            var buckets = new List<(DateTime start, DateTime end, string label)>();

            switch (period)
            {
                case "Today":
                    for (int h = 0; h < 24; h += 2)
                    {
                        var s = DateTime.Today.AddHours(h);
                        buckets.Add((s, s.AddHours(2), s.ToString("h tt")));
                    }
                    break;

                case "This Week":
                    {
                        var start = StartOfWeek(DateTime.Today);
                        for (int d = 0; d < 7; d++)
                        {
                            var s = start.AddDays(d);
                            buckets.Add((s, s.AddDays(1), s.ToString("ddd")));
                        }
                        break;
                    }

                case "Last Week":
                    {
                        var start = StartOfWeek(DateTime.Today).AddDays(-7);
                        for (int d = 0; d < 7; d++)
                        {
                            var s = start.AddDays(d);
                            buckets.Add((s, s.AddDays(1), s.ToString("ddd")));
                        }
                        break;
                    }

                case "This Month":
                    {
                        var start = new DateTime(now.Year, now.Month, 1);
                        int days = DateTime.DaysInMonth(now.Year, now.Month);
                        for (int d = 0; d < days; d++)
                        {
                            var s = start.AddDays(d);
                            buckets.Add((s, s.AddDays(1), (d + 1).ToString()));
                        }
                        break;
                    }

                case "Last Month":
                    {
                        var thisMonthStart = new DateTime(now.Year, now.Month, 1);
                        var start = thisMonthStart.AddMonths(-1);
                        int days = DateTime.DaysInMonth(start.Year, start.Month);
                        for (int d = 0; d < days; d++)
                        {
                            var s = start.AddDays(d);
                            buckets.Add((s, s.AddDays(1), (d + 1).ToString()));
                        }
                        break;
                    }

                default: // "This Year" — last 12 months
                    for (int i = 0; i < 12; i++)
                    {
                        var s = new DateTime(now.Year, now.Month, 1).AddMonths(-11 + i);
                        buckets.Add((s, s.AddMonths(1), s.ToString("MMM")));
                    }
                    break;
            }

            
            var labels = buckets.Select(b => b.label).ToArray();
            var dateLabels = buckets.Select(b => FormatBucketDateLabel(period, b.start, b.end)).ToArray();
            var allocated = new float[buckets.Count];
            var inStock = new float[buckets.Count];

            try
            {
                using var db = new InventoryDbContext();
                for (int i = 0; i < buckets.Count; i++)
                {
                    var (s, e, _) = buckets[i];
                    allocated[i] = db.EquipmentLogs.Count(l =>
                        l.ActionType == "allocation_approved" &&
                        l.CreatedAt.HasValue && l.CreatedAt.Value >= s && l.CreatedAt.Value < e);
                    inStock[i] = db.EquipmentLogs.Count(l =>
                        l.ActionType == "allocation_returned" &&
                        l.CreatedAt.HasValue && l.CreatedAt.Value >= s && l.CreatedAt.Value < e);
                }
            }
            catch { /* leaves zeros — chart shows a flat line instead of crashing */ }

            // AFTER
            return (labels, dateLabels, allocated, inStock);
        }

        // AFTER
        private (string[] Labels, string[] DateLabels, float[] Allocated, float[] InStock) GetCachedTrendSeries(string period)
        {
            if (!_trendSeriesCache.TryGetValue(period, out var v))
            {
                v = GetTrendData(period);
                _trendSeriesCache[period] = v;
            }
            return v;
        }

        private string? GetCachedDelta(string metric)
        {
            if (!_deltaCache.TryGetValue(metric, out var v)) { v = GetWeeklyDeltaText(metric); _deltaCache[metric] = v; }
            return v;
        }
            

        // Compares this-week total vs previous-week total for a metric, formatted
        // as delta text under the card number (e.g. "+3 this week").
        private string? GetWeeklyDeltaText(string metric)
        {
            try
            {
                using var db = new InventoryDbContext();
                var today = DateTime.Today;
                var thisWeekStart = today.AddDays(-6);
                var lastWeekStart = today.AddDays(-13);

                Func<DateTime, DateTime, int> countInRange = (start, end) => metric switch
                {
                    "equipment" => db.EquipmentLogs.Count(l => l.CreatedAt.HasValue && l.CreatedAt.Value >= start && l.CreatedAt.Value < end),
                    "allocated" => db.EquipmentLogs.Count(l => l.ActionType == "allocation_approved" && l.CreatedAt.HasValue && l.CreatedAt.Value >= start && l.CreatedAt.Value < end),
                    "in_store" => db.EquipmentLogs.Count(l => l.ActionType == "allocation_returned" && l.CreatedAt.HasValue && l.CreatedAt.Value >= start && l.CreatedAt.Value < end),
                    "under_repair" => db.EquipmentLogs.Count(l => l.ActionType == "maintenance_opened" && l.CreatedAt.HasValue && l.CreatedAt.Value >= start && l.CreatedAt.Value < end),
                    "decommissioned" => db.EquipmentLogs.Count(l => l.ActionType == "decommission_approved" && l.CreatedAt.HasValue && l.CreatedAt.Value >= start && l.CreatedAt.Value < end),
                    "overdue" => db.EquipmentLogs.Count(l => l.ActionType == "allocation_overdue" && l.CreatedAt.HasValue && l.CreatedAt.Value >= start && l.CreatedAt.Value < end),
                    _ => 0
                };

                int thisWeek = countInRange(thisWeekStart, today.AddDays(1));
                int lastWeek = countInRange(lastWeekStart, thisWeekStart);
                int delta = thisWeek - lastWeek;

                if (delta == 0) return "No change this week";
                string sign = delta > 0 ? "+" : "";
                return $"{sign}{delta} this week";
            }
            catch { return null; }
        }

        // Writes one EquipmentLogs entry the first time an allocation is
        // detected as overdue, so a 7-day trend can be built later. Checks
        // "already logged" first so it never double-logs the same item.
        private void MarkNewlyOverdueAllocations()
        {
            try
            {
                using var db = new InventoryDbContext();
                var today = DateOnly.FromDateTime(DateTime.Today);

                var overdueAllocations = db.Allocations
                    .Where(a => !a.Equipment.IsDeleted &&
                                a.Equipment.CurrentStatus == "allocated" &&
                                a.Status == "approved" &&
                                a.ExpectedReturnDate.HasValue &&
                                a.ExpectedReturnDate.Value < today &&
                                a.ActualReturnDate == null)
                    .ToList();

                foreach (var alloc in overdueAllocations)
                {
                    // Log once per overdue *episode*: skip if an allocation_overdue
                    // entry already exists for this equipment more recently than its
                    // last return/reallocation. Using a fixed 24h window meant a fresh
                    // row (with a fresh CreatedAt) got written every single day the
                    // item stayed overdue, which kept bumping it back to the top of
                    // the Logs screen even though nothing new had actually happened.
                    bool alreadyLogged = db.EquipmentLogs.Any(l =>
                        l.EquipmentId == alloc.EquipmentId &&
                        l.ActionType == "allocation_overdue" &&
                        l.CreatedAt.HasValue &&
                        l.CreatedAt.Value >= alloc.AllocationDate);

                    if (!alreadyLogged)
                    {
                        db.EquipmentLogs.Add(new EquipmentLog
                        {
                            EquipmentId = alloc.EquipmentId,
                            ActionType = "allocation_overdue",
                            CreatedAt = DateTime.UtcNow,
                            ChangedByUserId = AuthService.CurrentUserId
                        });
                    }
                }

                db.SaveChanges();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "DashboardForm.MarkNewlyOverdueAllocations");
            }
        }
        // ════════════════════════════════════════════════════════════════
        //  STATS + CHARTS
        // ════════════════════════════════════════════════════════════════
        private void LoadStats(bool instant = false)
        {
            MarkNewlyOverdueAllocations();
            try
            {
                _animTimer?.Stop();
                _animTimer?.Dispose();
                _animTimer = null;
                _cardAnims = new List<CardAnimState>();
                _donutAnimProgress = instant ? 1f : 0f;
                _barAnimProgress = instant ? 1f : 0f;
                _activitiesAnimProgress = (instant || _activitiesIntroPlayed) ? 1f : 0f;
                if (!instant) _activitiesIntroPlayed = true;
                _chartAnimClock.Restart();

                using var db = new InventoryDbContext();

                var activeEq = db.Equipment.Where(e => !e.IsDeleted);
                int totalEquipment = activeEq.Count();

                _cntAllocated      = activeEq.Count(e => e.CurrentStatus == "allocated");
                _cntInStore        = activeEq.Count(e => e.CurrentStatus == "in_store");
                _cntUnderRepair    = activeEq.Count(e => e.CurrentStatus == "under_repair");
                _cntDecommission   = activeEq.Count(e => e.CurrentStatus == "decommissioned");
                _cntEmployees      = db.Employees.Count(e => e.IsActive);

                var today = DateOnly.FromDateTime(DateTime.Today);
                _cntOverdue = db.Allocations
                    .Where(a => !a.Equipment.IsDeleted && 
                                a.Equipment.CurrentStatus == "allocated" &&
                                a.Status == "approved" &&
                                a.ExpectedReturnDate.HasValue &&
                                a.ExpectedReturnDate.Value < today &&
                                a.ActualReturnDate == null)
                    .Select(a => a.EquipmentId)
                    .Distinct()
                    .Count();


                // Category breakdown — equipment count per category
                _categoryBreakdown = db.Equipment
                    .Where(e => e.Category != null && e.Category != "")
                    .GroupBy(e => e.Category)
                    .Select(g => new { Category = g.Key, Count = g.Count() })
                    .OrderByDescending(g => g.Count)
                    .ToList()
                    .Select(g => (Category: g.Category!, Count: g.Count))
                    .ToList();

                LoadTopEmployees(db);
                LoadLocationBreakdown(db);

                // ── Stat cards — all 6 in one row ──────────────────────────
                panelStats.Controls.Clear();
                _statCards.Clear();

                var cardDefs = new[]
                {
    (title:"Total Equipment",    value:totalEquipment.ToString(),    color:Color.FromArgb(94, 53, 177), icon:"🖥", trendValues:GetCachedTrend("equipment"),      deltaText:GetCachedDelta("equipment")),
    (title:"Allocated",          value:_cntAllocated.ToString(),     color:Color.FromArgb(245, 124, 0),   icon:"📋", trendValues:GetCachedTrend("allocated"),      deltaText:GetCachedDelta("allocated")),
    (title:"In Stock",           value:_cntInStore.ToString(),       color:Color.FromArgb(56,  142, 60),  icon:"✅", trendValues:GetCachedTrend("in_store"),       deltaText:GetCachedDelta("in_store")),
    (title:"Under Repair",       value:_cntUnderRepair.ToString(),   color:Color.FromArgb(33,  150, 243), icon:"🔧", trendValues:GetCachedTrend("under_repair"),   deltaText:GetCachedDelta("under_repair")),
(title:"Currently Overdue",  value:_cntOverdue.ToString(),      color:Color.FromArgb(255, 160,  0),  icon:"⚠️", trendValues:GetCachedTrend("overdue"), deltaText:GetCachedDelta("overdue")),
    (title:"Decommissioned",     value:_cntDecommission.ToString(),  color:Color.FromArgb(239, 68,  68),  icon:"🚫", trendValues:GetCachedTrend("decommissioned"), deltaText:GetCachedDelta("decommissioned")),
    (title:"Total Employees",    value:_cntEmployees.ToString(),     color:Color.FromArgb(123, 31,  162), icon:"👥", trendValues:(float[]?)null, deltaText:(string?)null),
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

                float staggerStepMs = instant ? 0f : 70f;
                // How long the slide/bounce itself takes once it starts, derived from the
                // same step size AnimTimer_Tick uses — kept in sync automatically if that ever changes.
                int cardEntranceMs = (int)Math.Ceiling(1f / CardAnimStepPerTick) * AnimTickIntervalMs;

                foreach (var def in cardDefs)
                {
                    Rectangle targetBounds = new Rectangle(x, 8, cardW, cardH);

                    // Compute this card's delay BEFORE building it, so the number
                    // count-up can be told to wait until (delay + entrance) has elapsed —
                    // i.e. until this specific card has actually landed, not a fixed guess.
                    float delay;
                    if (cardIndex == CenterCardIndex) delay = 0f;
                    else if (cardIndex < CenterCardIndex)
                    {
                        int distanceFromCenter = CenterCardIndex - cardIndex; // 1,2,3
                        delay = (distanceFromCenter - 1) * staggerStepMs;
                    }
                    else
                    {
                        int distanceFromCenter = cardIndex - CenterCardIndex; // 1,2,3
                        delay = (distanceFromCenter - 1) * staggerStepMs;
                    }
                    int numberStartDelayMs = instant ? 0 : (int)delay + cardEntranceMs;

                    var card = ThemeHelper.CreateStatCard(def.title, def.value, def.color, def.icon, def.trendValues, def.deltaText, numberStartDelayMs);

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
                            Card = card,
                            StartBounds = startBounds,
                            TargetBounds = targetBounds,
                            Progress = 0f,
                            DelayMs = 0f,
                            Kind = CardAnimKind.Bounce
                        };
                    }
                    else if (cardIndex < CenterCardIndex)
                    {
                        // Left group (0,1,2): slide in from off-screen left, converging
                        // toward the center — closest to center starts first.
                        Rectangle startBounds = new Rectangle(
                            -targetBounds.Width, targetBounds.Y, targetBounds.Width, targetBounds.Height);

                        card.Bounds = startBounds;

                        animState = new CardAnimState
                        {
                            Card = card,
                            StartBounds = startBounds,
                            TargetBounds = targetBounds,
                            Progress = 0f,
                            DelayMs = delay,
                            Kind = CardAnimKind.Slide
                        };
                    }
                    else
                    {
                        // Right group (4,5,6): slide in from off-screen right, converging
                        // toward the center — closest to center starts first.
                        Rectangle startBounds = new Rectangle(
                            panelStats.Width, targetBounds.Y, targetBounds.Width, targetBounds.Height);

                        card.Bounds = startBounds;

                        animState = new CardAnimState
                        {
                            Card = card,
                            StartBounds = startBounds,
                            TargetBounds = targetBounds,
                            Progress = 0f,
                            DelayMs = delay,
                            Kind = CardAnimKind.Slide
                        };
                    }

                    _cardAnims.Add(animState);

                    EventHandler? clickHandler = def.title switch
                    {
                        "Total Equipment" => btnEquipment_Click,
                        "Allocated" => btnAllocation_Click,
                        "In Stock" => (EventHandler)((s, e) => OpenAllocationAvailable()),
                        "Under Repair" => (EventHandler)((s, e) => OpenEquipmentByStatus("under_repair")),
                        "Currently Overdue" => (EventHandler)((s, e) => OpenOverdueEquipment()),
                        "Decommissioned" => AuthService.IsAdmin ? btnDecommission_Click : null,
                        "Total Employees" => btnEmployees_Click,
                        _ => null
                    };  

                    if (clickHandler != null)
                    {
                        AttachCardClick(card, clickHandler);
                    }

                    panelStats.Controls.Add(card);
                    x += cardW + gap;
                    _statCards.Add(card);
                    cardIndex++;
                }

                BuildChartArea();

                _animTimer = new System.Windows.Forms.Timer { Interval = AnimTickIntervalMs };
                _animTimer.Tick += AnimTimer_Tick;
                _animTimer.Start();
            }
            catch { /* DB unavailable */ }
        }

        // Top employees by number of currently allocated items.
        // Adjust the property names below (Allocation.EmployeeId / Employee.FullName /
        // Status == "approved") to match your actual Allocations/Employees schema if different.
        private void LoadTopEmployees(InventoryDbContext db)
        {
            try
            {
                var counts = db.Allocations
                    .Where(a => a.Status == "approved" && a.ActualReturnDate == null && a.EmployeeId != null)
                    .GroupBy(a => a.EmployeeId)
                    .Select(g => new { EmployeeId = g.Key!.Value, Count = g.Count() })
                    .ToList()
                    .ToDictionary(x => x.EmployeeId, x => x.Count);

                _topEmployees = db.Employees
                        .Where(e => e.IsActive)
                        .ToList()
                        .Select(e => (Code: e.EmployeeCode, Name: e.FullName, Count: counts.TryGetValue(e.Id, out var c) ? c : 0, Dept: e.Department ?? "—", Email: e.Email ?? "—"))
                        .OrderByDescending(e => e.Count)
                        .ThenBy(e => e.Code, StringComparer.OrdinalIgnoreCase)
                        .Select(e => (Name: e.Name, Count: e.Count, Dept: e.Dept, Email: e.Email, Code: e.Code))
                        .ToList();
            }
            catch { _topEmployees = new(); }
        }

        // Equipment allocation broken down by location.
        // Location tracking runs through EquipmentLocationHistory — EndDate == null
        // marks the currently-active assignment for that piece of equipment.
        private void LoadLocationBreakdown(InventoryDbContext db)
        {
            try
            {
                _locationBreakdown = db.Allocations
                    .Where(a => a.Status == "approved" && a.ActualReturnDate == null &&
                                a.LocationId != null && a.EmployeeId == null &&
                                !a.Equipment.IsDeleted)
                    .GroupBy(a => a.LocationId)
                    .Select(g => new { LocationId = g.Key!.Value, Count = g.Count() })
                    .ToList()
                    .Select(g =>
                    {
                        var loc = db.Locations.FirstOrDefault(l => l.Id == g.LocationId);
                        string fullName = loc == null
                            ? "Unknown"
                            : string.IsNullOrWhiteSpace(loc.RoomName)
                                ? (loc.LocationName ?? "Unknown")
                                : $"{loc.LocationName} — {loc.RoomName}";
                        string roomLabel = loc == null
                            ? "Unknown"
                            : string.IsNullOrWhiteSpace(loc.RoomName) ? (loc.LocationName ?? "Unknown") : loc.RoomName;
                        return (Location: fullName, RoomLabel: roomLabel, InStock: 0, Allocated: g.Count, Repair: 0);
                    })
                    .OrderByDescending(x => x.Allocated)
                    .ToList();
            }
            catch { _locationBreakdown = new(); }
        }

        private void AnimTimer_Tick(object? sender, EventArgs e)
        {
            bool allDone = true;

            // ── Phase 1: card entrance (slide/bounce) ───────────────────────
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
                        anim.Progress += CardAnimStepPerTick;
                        if (anim.Progress >= 1f) anim.Progress = 1f;
                        else { allDone = false; }

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

            // Charts now run concurrently with the card entrance (started in LoadStats),
            // not queued behind it — durations below are tuned to land at the same time
            // the cards finish, so everything settles together.
            long elapsedMs = _chartAnimClock.ElapsedMilliseconds;

            if (_donutAnimProgress < 1f)
            {
                float t = Math.Clamp(elapsedMs / DonutAnimDurationMs, 0f, 1f);
                _donutAnimProgress = EaseOutCubic(t);
                if (_donutAnimProgress > 0.999f) _donutAnimProgress = 1f;
                _panelDonut?.Invalidate();
                allDone = false;
            }

            if (_barAnimProgress < 1f)
            {
                float t = Math.Clamp(elapsedMs / BarAnimDurationMs, 0f, 1f);
                _barAnimProgress = EaseOutCubic(t);
                if (_barAnimProgress > 0.999f) _barAnimProgress = 1f;
                _panelBar?.Invalidate();
                _panelLine?.Invalidate();
                _panelTrend?.Invalidate();
                _panelLocation?.Invalidate();
                allDone = false;
            }

            // ── Phase 3: activities panel slide-in — runs on the same elapsed
            // clock and duration as the donut/bar charts above, so all three
            // start together and finish together instead of racing at different speeds. ──
            if (_activitiesAnimProgress < 1f && _panelActivities != null)
            {
                float aT = Math.Clamp(elapsedMs / ActivitiesAnimDurationMs, 0f, 1f);
                _activitiesAnimProgress = aT;
                float ease = EaseOutCubic(aT);
                int startX = _activitiesTargetBounds.X + 250; // starts 250px to the right, slides left into place
                int curX = (int)(startX + (_activitiesTargetBounds.X - startX) * ease);
                _panelActivities.SetBounds(curX, _activitiesTargetBounds.Y, _activitiesTargetBounds.Width, _activitiesTargetBounds.Height);
                if (aT < 1f) allDone = false;
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

        private static float EaseOutCubic(float t) => 1f - MathF.Pow(1f - t, 3f);

        // ════════════════════════════════════════════════════════════════
        //  CHART AREA
        // ════════════════════════════════════════════════════════════════
        private void BuildChartArea()
        {
            if (_panelCharts != null)
            {
                panelContent.Controls.Remove(_panelCharts);
                _panelCharts.Dispose();
                _panelCharts = null;
            }

            _panelCharts = new DoubleBufferedPanel
            {
                Name = "panelCharts",
                BackColor = ThemeHelper.DashboardBg,
                Dock = DockStyle.Fill,
                Padding = new Padding(14, 12, 14, 12),
                AutoScroll = false
            };

            panelContent.Controls.Clear();
            panelContent.Controls.Add(_panelCharts);

            // Row 1
            _panelBar = MakeChartCard("Asset Status Breakdown", 140);       // has the period-selector dropdown

            _panelBar.Paint += PaintBarChart;
            _panelBar.MouseMove += (s, e) =>
            {
                _barMousePos = e.Location;
                int idx = _barHitCols.FindIndex(c => c.Bounds.Contains(e.Location));
                bool changed = idx != _barHoverIndex;
                _barHoverIndex = idx;
                if (changed || idx >= 0) _panelBar.Invalidate();
            };
            _panelBar.MouseLeave += (s, e) => { if (_barHoverIndex != -1) { _barHoverIndex = -1; _panelBar.Invalidate(); } };
            BuildBarPeriodSelector(_panelBar);

            _panelDonut = MakeChartCard("Equipment Status", 50);             // has only the small "›" link

            _panelDonut.Paint += PaintDonutChart;
            _panelDonut.MouseMove += (s, e) =>
            {
                _donutMousePos = e.Location;
                int idx = HitTestDonut(e.Location);
                bool changed = idx != _donutHoverIndex;
                _donutHoverIndex = idx;
                if (changed || idx >= 0) _panelDonut.Invalidate();
            };
            _panelDonut.MouseLeave += (s, e) => { if (_donutHoverIndex != -1) { _donutHoverIndex = -1; _panelDonut.Invalidate(); } };

            var lnkViewAllEquipment = new Label
            {
                Text = "›",
                Font = new Font("Segoe UI", 15f),
                ForeColor = ThemeHelper.ChartBodyText,
                AutoSize = true,
                Cursor = Cursors.Hand,
                BackColor = Color.Transparent,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            lnkViewAllEquipment.Click += btnEquipment_Click;
            lnkViewAllEquipment.MouseEnter += (s, e) => lnkViewAllEquipment.ForeColor = ThemeHelper.ChartTitleText;
            lnkViewAllEquipment.MouseLeave += (s, e) => lnkViewAllEquipment.ForeColor = ThemeHelper.ChartBodyText;
            _panelDonut.Controls.Add(lnkViewAllEquipment);
            _panelDonut.Resize += (s, e) => lnkViewAllEquipment.Location = new Point(_panelDonut.Width - lnkViewAllEquipment.PreferredWidth - 16, 10);
            lnkViewAllEquipment.Location = new Point(_panelDonut.Width - lnkViewAllEquipment.PreferredWidth - 16, 10);
            lnkViewAllEquipment.BringToFront();

            // Employees card — spans both rows
            _panelEmployees = MakeChartCard("Top Employees by Holdings", 20); // no right-side control

            BuildEmployeesList(_panelEmployees);

            _panelActivities = MakeChartCard("Recent Activities", 50);       // has only the small "›" link

            BuildActivitiesList(_panelActivities);

            // Row 2
            _panelLine = MakeChartCard("Equipment by Category", 20);         // no right-side control

            _panelLine.Paint += PaintCategoryBreakdownChart;
            _panelLine.MouseMove += (s, e) =>
            {
                _catMousePos = e.Location;
                int idx = _catHitRows.FindIndex(r => r.Bounds.Contains(e.Location));
                bool changed = idx != _catHoverIndex;
                _catHoverIndex = idx;
                if (changed || idx >= 0) _panelLine.Invalidate();
            };
            _panelLine.MouseLeave += (s, e) => { if (_catHoverIndex != -1) { _catHoverIndex = -1; _panelLine.Invalidate(); } };

            _panelLocation = MakeChartCard("Equipment Allocation by Location", 20); // no right-side control

            _panelLocation.Paint += PaintLocationChart;
            _panelLocation.MouseMove += (s, e) =>
            {
                _locMousePos = e.Location;
                int idx = _locHitCols.FindIndex(c => c.Bounds.Contains(e.Location));
                bool changed = idx != _locHoverIndex;
                _locHoverIndex = idx;
                if (changed || idx >= 0) _panelLocation.Invalidate();
            };
            _panelLocation.MouseLeave += (s, e) => { if (_locHoverIndex != -1) { _locHoverIndex = -1; _panelLocation.Invalidate(); } };

            _panelTrend = MakeChartCard("Asset Allocation and Usage Trend", 140);   // has the period-selector dropdown
            _panelTrend.Paint += PaintTrendChart;
            _panelTrend.MouseMove += (s, e) =>
            {
                _trendMousePos = e.Location;
                int idx = _trendHitPoints.FindIndex(p => p.Bounds.Contains(e.Location));
                bool changed = idx != _trendHoverIndex;
                _trendHoverIndex = idx;
                if (changed || idx >= 0) _panelTrend.Invalidate();
            };
            _panelTrend.MouseLeave += (s, e) => { if (_trendHoverIndex != -1) { _trendHoverIndex = -1; _panelTrend.Invalidate(); } };
            BuildTrendPeriodSelector(_panelTrend);

            _panelCharts.Controls.Add(_panelBar);
            _panelCharts.Controls.Add(_panelDonut);
            _panelCharts.Controls.Add(_panelEmployees);
            _panelCharts.Controls.Add(_panelActivities);
            _panelCharts.Controls.Add(_panelLine);
            _panelCharts.Controls.Add(_panelLocation);
            _panelCharts.Controls.Add(_panelTrend);

            LayoutCharts();
        }


        private void RecomputeBarChartForPeriod()
        {
            DateTime cutoffStart;
            DateTime cutoffEnd = DateTime.Today.AddDays(1);

            switch (_barPeriod)
            {
                case "Today":
                    cutoffStart = DateTime.Today;
                    break;
                case "This Week":
                    cutoffStart = StartOfWeek(DateTime.Today);
                    break;
                case "Last Week":
                    cutoffStart = StartOfWeek(DateTime.Today).AddDays(-7);
                    cutoffEnd = StartOfWeek(DateTime.Today);
                    break;
                case "This Month":
                    cutoffStart = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
                    break;
                case "Last Month":
                    cutoffStart = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(-1);
                    cutoffEnd = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
                    break;
                default: // "This Year"
                    cutoffStart = new DateTime(DateTime.Today.Year, 1, 1);
                    break;
            }

            try
            {
                using var db = new InventoryDbContext();
                // Approximation, see note above the section — "in period" means an
                // EquipmentLog exists for that item in this window, not a true
                // point-in-time status snapshot.
                var activeIds = db.EquipmentLogs
                    .Where(l => l.CreatedAt.HasValue && l.CreatedAt.Value >= cutoffStart && l.CreatedAt.Value < cutoffEnd)
                    .Select(l => l.EquipmentId)
                    .Distinct()
                    .ToHashSet();

                var eqInPeriod = db.Equipment.Where(e => activeIds.Contains(e.Id)).ToList();

                _periodOverrideCounts = new Dictionary<string, int>
                {
                    ["in_store"] = eqInPeriod.Count(e => e.CurrentStatus == "in_store"),
                    ["under_repair"] = eqInPeriod.Count(e => e.CurrentStatus == "under_repair"),
                    ["decommissioned"] = eqInPeriod.Count(e => e.CurrentStatus == "decommissioned"),
                };
            }
            catch { _periodOverrideCounts = null; }
        }

        private void LayoutCharts()
        {
            if (_panelCharts == null) return;

            int pad = 14, gap = 12;
            int w = _panelCharts.ClientSize.Width - pad * 2;
            int h = _panelCharts.ClientSize.Height - pad * 2;
            if (w <= 0 || h <= 0) return;

            int colW = (w - gap * 3) / 4;
            int rowH = (h - gap) / 2;

            int x0 = pad, x1 = x0 + colW + gap, x2 = x1 + colW + gap, x3 = x2 + colW + gap;
            int y0 = pad, y1 = y0 + rowH + gap;

            // Row 1 — Asset Status Breakdown now sits where the trend chart used to.
            _panelTrend?.SetBounds(x0, y0, colW, rowH);
            _panelDonut?.SetBounds(x1, y0, colW, rowH);
            _activitiesTargetBounds = new Rectangle(x3, y0, colW, rowH);
            if (_panelActivities != null) _panelActivities.Bounds = _activitiesTargetBounds;

            // Employees spans both rows, column 3
            _panelEmployees?.SetBounds(x2, y0, colW, rowH * 2 + gap);

            // Row 2 — trend chart now sits where the bar chart used to.
            _panelLine?.SetBounds(x0, y1, colW, rowH);
            _panelLocation?.SetBounds(x1, y1, colW, rowH);
            _panelBar?.SetBounds(x3, y1, colW, rowH);

            _panelTrend?.Invalidate();
            _panelDonut?.Invalidate();
            _panelEmployees?.Invalidate();
            _panelLine?.Invalidate();
            _panelLocation?.Invalidate();
            _panelBar?.Invalidate();
        }

        private void BuildActivitiesList(Panel card)
        {
            var listHost = new Panel
            {
                Location = new Point(0, 45),
                Size = new Size(card.Width, Math.Max(0, card.Height - 45)),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
                AutoScroll = true,
                BackColor = ThemeHelper.ChartHostBg,
                Tag = "activitiesHost"
            };
            card.Controls.Add(listHost);

            var lnkViewAll = new Label
            {
                Text = "›",
                Font = new Font("Segoe UI", 15f, FontStyle.Regular),
                ForeColor = ThemeHelper.ChartBodyText,
                AutoSize = true,
                Cursor = Cursors.Hand,
                BackColor = Color.Transparent,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            lnkViewAll.Click += btnLogs_Click;
            lnkViewAll.MouseEnter += (s, e) => lnkViewAll.ForeColor = ThemeHelper.ChartTitleText;
            lnkViewAll.MouseLeave += (s, e) => lnkViewAll.ForeColor = ThemeHelper.ChartBodyText;
            lnkViewAll.Location = new Point(card.Width - lnkViewAll.PreferredWidth - 16, 8);
            card.Controls.Add(lnkViewAll);
            lnkViewAll.BringToFront();

            LoadRecentActivities(listHost);
        }
        private void BuildEmployeesList(Panel card)
        {
            var listHost = new Panel
            {
                Location = new Point(0, 45),
                Size = new Size(card.Width, Math.Max(0, card.Height - 45)),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
                AutoScroll = true,
                BackColor = ThemeHelper.ChartHostBg,
                Tag = "employeesHost"
            };
            card.Controls.Add(listHost);

            // Rows were being built while card.Width was still 0 (before layout
            // ran), leaving every name label at ~0px wide. Rebuild whenever the
            // host is actually resized to its real width.
            int lastWidth = -1;
            listHost.Resize += (s, e) =>
            {
                if (listHost.Width == lastWidth) return;
                lastWidth = listHost.Width;
                LoadEmployeesRows(listHost);
            };

            LoadEmployeesRows(listHost);
        }

        private void LoadEmployeesRows(Panel listHost)
        {
            listHost.Controls.Clear();
            _employeeTooltip ??= new ToolTip { AutoPopDelay = 4000, InitialDelay = 200, ReshowDelay = 100, ShowAlways = true };

            if (_topEmployees.Count == 0)
            {
                listHost.Controls.Add(new Label
                {
                    Text = "No employee data available.",
                    ForeColor = ThemeHelper.ChartSecText,
                    Font = ThemeHelper.FontSmall,
                    AutoSize = true,
                    Location = new Point(14, 14)
                });
                return;
            }

            int maxVal = Math.Max(1, _topEmployees.Max(x => x.Count));
            bool showBars = _topEmployees.Any(x => x.Count > 0);

            int y = 4;
            for (int i = 0; i < _topEmployees.Count; i++)
            {
                var emp = _topEmployees[i];
                var row = BuildEmployeeRow(i + 1, emp.Name, emp.Count, emp.Dept, emp.Email, emp.Code, maxVal, showBars, listHost.Width - 20, _employeeTooltip);
                row.Location = new Point(10, y);
                row.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
                listHost.Controls.Add(row);
                y += row.Height + 4;
            }
        }

        private static Panel BuildEmployeeRow(int rank, string name, int count, string dept, string email, string code, int maxVal, bool showBar, int width, ToolTip tooltip)
        {
            var row = new Panel { Size = new Size(width, 34), BackColor = ThemeHelper.ContentBg };

            string tooltipText = count > 0
                ? $"{name} ({code})\n{count} allocation{(count == 1 ? "" : "s")}"
                : $"{name} ({code})\nNo allocations";

            const int avatarD = 24;
            string initial = name.Length > 0 ? name.Trim().Substring(0, 1).ToUpper() : "?";
            Color avatarColor = count > 0 ? Color.FromArgb(127, 119, 221) : ThemeHelper.ChartTrackBg;
            Color avatarFg = count > 0 ? Color.White : ThemeHelper.ChartSecText;

            var avatarPanel = new Panel
            {
                Size = new Size(avatarD, avatarD),
                Location = new Point(8, 5),
                BackColor = Color.Transparent
            };
            avatarPanel.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using var b = new SolidBrush(avatarColor);
                g.FillEllipse(b, 0, 0, avatarD - 1, avatarD - 1);
                using var fg = new SolidBrush(avatarFg);
                using var f = new Font("Segoe UI", 8.5f, FontStyle.Bold);
                using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                g.DrawString(initial, f, fg, new RectangleF(0, 0, avatarD, avatarD), sf);
            };
            row.Controls.Add(avatarPanel);

            int nameX = avatarPanel.Right + 8;
            var lblName = new Label
            {
                Text = name,
                Font = ChartCatLabelFont,
                ForeColor = ThemeHelper.ChartBodyText,
                Size = new Size(showBar ? width - nameX - 106 : width - nameX - 10, 34),
                Location = new Point(nameX, 0),
                TextAlign = ContentAlignment.MiddleLeft,
                AutoEllipsis = true
            };
            row.Controls.Add(lblName);

            row.MouseEnter += (s, e) => row.BackColor = ThemeHelper.ChartTrackBg;
            row.MouseLeave += (s, e) => row.BackColor = ThemeHelper.ContentBg;

            if (showBar)
            {
                var barTrack = new Panel
                {
                    Size = new Size(70, 8),
                    Location = new Point(row.Width - 106, 13),
                    Anchor = AnchorStyles.Top | AnchorStyles.Right,
                    BackColor = Color.Transparent
                };
                barTrack.Paint += (s, e) =>
                {
                    var g = e.Graphics;
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    using var trackPath = RoundedRectF(new RectangleF(0, 0, barTrack.Width, barTrack.Height), 4f);
                    using var trackBrush = new SolidBrush(ThemeHelper.ChartTrackBg);
                    g.FillPath(trackBrush, trackPath);

                    float fillW = (float)count / maxVal * barTrack.Width;
                    if (fillW > 1)
                    {
                        using var fillPath = RoundedRectF(new RectangleF(0, 0, fillW, barTrack.Height), 4f);
                        using var fillBrush = new SolidBrush(Color.FromArgb(127, 119, 221));
                        g.FillPath(fillBrush, fillPath);
                    }
                };
                row.Controls.Add(barTrack);

                var lblCount = new Label
                {
                    Text = count.ToString(),
                    Font = ChartLabelFont,
                    ForeColor = count > 0 ? ThemeHelper.ChartSecText : ThemeHelper.ChartTrackBg,
                    Size = new Size(30, 34),
                    Location = new Point(row.Width - 30, 0),
                    Anchor = AnchorStyles.Top | AnchorStyles.Right,
                    TextAlign = ContentAlignment.MiddleRight
                };
                row.Controls.Add(lblCount);

                tooltip.SetToolTip(barTrack, tooltipText);
                tooltip.SetToolTip(lblCount, tooltipText);
            }

            tooltip.SetToolTip(row, tooltipText);
            tooltip.SetToolTip(avatarPanel, tooltipText);
            tooltip.SetToolTip(lblName, tooltipText);

            return row;
        }

        private static Panel BuildEmployeeRow(int rank, string name, int count, int maxVal, bool showBar, int width)
        {
            var row = new Panel { Size = new Size(width, 30), BackColor = Color.Transparent };

            row.Controls.Add(new Label
            {
                Text = rank.ToString(),
                Font = ChartLabelFont,
                ForeColor = ThemeHelper.ChartSecText,
                Size = new Size(20, 30),
                Location = new Point(0, 0),
                TextAlign = ContentAlignment.MiddleLeft
            });

            row.Controls.Add(new Label
            {
                Text = name,
                Font = ChartCatLabelFont,
                ForeColor = ThemeHelper.ChartBodyText,
                Size = new Size(showBar ? width - 20 - 126 : width - 30, 30),
                Location = new Point(24, 0),
                TextAlign = ContentAlignment.MiddleLeft,
                AutoEllipsis = true
            });

            if (showBar)
            {
                var barTrack = new Panel
                {
                    Size = new Size(90, 8),
                    Location = new Point(row.Width - 126, 11),
                    Anchor = AnchorStyles.Top | AnchorStyles.Right,
                    BackColor = Color.Transparent
                };
                barTrack.Paint += (s, e) =>
                {
                    var g = e.Graphics;
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    using var trackPath = RoundedRectF(new RectangleF(0, 0, barTrack.Width, barTrack.Height), 4f);
                    using var trackBrush = new SolidBrush(ThemeHelper.ChartTrackBg);
                    g.FillPath(trackBrush, trackPath);

                    float fillW = (float)count / maxVal * barTrack.Width;
                    if (fillW > 1)
                    {
                        using var fillPath = RoundedRectF(new RectangleF(0, 0, fillW, barTrack.Height), 4f);
                        using var fillBrush = new SolidBrush(Color.FromArgb(127, 119, 221));
                        g.FillPath(fillBrush, fillPath);
                    }
                };
                row.Controls.Add(barTrack);

                row.Controls.Add(new Label
                {
                    Text = count.ToString(),
                    Font = ChartLabelFont,
                    ForeColor = ThemeHelper.ChartSecText,
                    Size = new Size(30, 30),
                    Location = new Point(row.Width - 30, 0),
                    Anchor = AnchorStyles.Top | AnchorStyles.Right,
                    TextAlign = ContentAlignment.MiddleRight
                });
            }

            return row;
        }

        private void LoadRecentActivities(Panel listHost)
        {
            int desiredCount = 25; // Load a fixed number of recent activities to allow scrolling

          

            listHost.Controls.Clear();
            try
            {
                using var db = new InventoryDbContext();
                var logs = db.EquipmentLogs
                    .OrderByDescending(l => l.CreatedAt)
                    .Take(desiredCount)
                    .ToList();

                var equipMap = db.Equipment.ToDictionary(x => x.Id, x => x);
                var userMap = db.Users.ToDictionary(x => x.Id, x => x);

                int y = 4;
                foreach (var log in logs)
                {
                    string equipName = equipMap.TryGetValue(log.EquipmentId, out var eq) ? eq.Name : "Equipment";
                    string userName = userMap.TryGetValue(log.ChangedByUserId, out var usr) ? usr.FullName : "Someone";

                    string icon = log.ActionType?.ToLower() switch
                    {
                        var a when a != null && a.Contains("alloc") => "📋",
                        var a when a != null && a.Contains("repair") => "🔧",
                        var a when a != null && a.Contains("decommiss") => "🚫",
                        var a when a != null && a.Contains("return") => "↩",
                        var a when a != null && a.Contains("check") => "✅",
                        _ => "📝"
                    };

                    string message = $"{equipName} — {log.ActionType} by {userName}";
                    string timeLine = "";
                    if (log.CreatedAt.HasValue)
                    {
                        var local = TimeHelper.ToPakistanTime(log.CreatedAt.Value);
                        timeLine = $"{FormatTimeAgo(log.CreatedAt.Value)} at {local:hh:mm tt}, {local:dd MMM yyyy}";
                    }

                    var row = BuildActivityRow(icon, message, timeLine, listHost.Width - 20);
                    row.Location = new Point(10, y);
                    row.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
                    listHost.Controls.Add(row);
                    y += row.Height + 4;
                }

                if (logs.Count == 0)
                {
                    var empty = new Label
                    {
                        Text = "No recent activity.",
                        ForeColor = Color.FromArgb(156, 163, 175),
                        Font = ThemeHelper.FontSmall,
                        AutoSize = true,
                        Location = new Point(14, 14)
                    };
                    listHost.Controls.Add(empty);
                }
            }
            catch { /* DB unavailable */ }
        }

        private static Panel BuildActivityRow(string icon, string message, string timeLine, int width)
        {
            var row = new Panel { Size = new Size(width, 46), BackColor = Color.Transparent };

            var lblIcon = new Label
            {
                Text = icon,
                Font = new Font("Segoe UI Emoji", 11f),
                AutoSize = false,
                Size = new Size(28, 28),
                Location = new Point(0, 4),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = ThemeHelper.ActivityIconBg
            };
            row.Controls.Add(lblIcon);

            var lblMsg = new Label
            {
                Text = message,
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = ThemeHelper.ChartBodyText,
                AutoSize = false,
                Size = new Size(width - 38, 32),
                Location = new Point(36, 0),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            row.Controls.Add(lblMsg);

            var lblTime = new Label
            {
                Text = timeLine,
                Font = new Font("Segoe UI", 7.5f),
                ForeColor = ThemeHelper.ChartSecText,
                AutoSize = true,
                Location = new Point(36, 30)
            };
            row.Controls.Add(lblTime);

            return row;
        }   

        private static string FormatTimeAgo(DateTime dt)
        {
            var localDt = TimeHelper.ToPakistanTime(dt);
            var span = TimeHelper.NowPakistan() - localDt;
            if (span.TotalMinutes < 1) return "just now";
            if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes} min ago";
            if (span.TotalHours < 24) return $"{(int)span.TotalHours} hr ago";
            return $"{(int)span.TotalDays} day(s) ago";
        }

        // ════════════════════════════════════════════════════════════════
        //  CHART CARD FACTORY
        // ════════════════════════════════════════════════════════════════
      
        private static Panel MakeChartCard(string title, int reservedRight = 40)
        {
            var card = new DoubleBufferedPanel { BackColor = ThemeHelper.ChartCardBg };

            card.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

                // Card body background (respects dark mode)
                using var bodyBrush = new SolidBrush(ThemeHelper.ChartCardBg);
                g.FillRectangle(bodyBrush, 0, 0, card.Width, card.Height);

                using var path = RoundedRect(new Rectangle(0, 0, card.Width - 1, card.Height - 1), 14);
                card.Region = new Region(path);

                using var hBrush = new SolidBrush(ThemeHelper.ChartCardHdrBg);
                g.FillRectangle(hBrush, 0, 0, card.Width, 44);

                using var tFg = new SolidBrush(ThemeHelper.ChartTitleText);

                string titleText = card.Tag?.ToString() ?? "";
                var titleRect = new RectangleF(16, 12, Math.Max(20, card.Width - 16 - reservedRight), 22);
                using var titleSf = new StringFormat { Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap };

                // Split off a leading emoji so it draws with an emoji-capable font —
                // drawing the whole title with ChartTitleFont (Segoe UI, no color-emoji
                // glyphs) is what produced the garbled box/red-dot icon.
                string emojiPart = "";
                string textPart = titleText;
                int splitIdx = 0;
                while (splitIdx < titleText.Length && titleText[splitIdx] > 127) splitIdx++;
                if (splitIdx > 0)
                {
                    emojiPart = titleText.Substring(0, splitIdx).TrimEnd();
                    textPart = titleText.Substring(splitIdx).TrimStart();
                }

                float textX = titleRect.X;
                if (emojiPart.Length > 0)
                {
                    g.DrawString(emojiPart, ChartTitleEmojiFont, tFg, new PointF(titleRect.X, titleRect.Y - 1));
                    textX += g.MeasureString(emojiPart, ChartTitleEmojiFont).Width + 4;
                }
                var remainingRect = new RectangleF(textX, titleRect.Y, Math.Max(10, titleRect.Right - textX), titleRect.Height);

                // Auto-shrink long headings instead of ellipsis-truncating them — steps
                // the font down in 0.5pt increments until the full title fits, with a
                // floor so it never gets illegibly small. Falls back to ellipsis only
                // if even the smallest size doesn't fit.
                Font fitFont = ChartTitleFont;
                bool ownsFitFont = false;
                if (g.MeasureString(textPart, fitFont).Width > remainingRect.Width)
                {
                    for (float size = 9.5f; size >= 7.5f; size -= 0.5f)
                    {
                        var candidate = new Font("Segoe UI", size, FontStyle.Bold, GraphicsUnit.Point);
                        if (g.MeasureString(textPart, candidate).Width <= remainingRect.Width)
                        {
                            fitFont = candidate;
                            ownsFitFont = true;
                            break;
                        }
                        candidate.Dispose();
                    }
                }
                g.DrawString(textPart, fitFont, tFg, remainingRect, titleSf);
                if (ownsFitFont) fitFont.Dispose();

                using var div = new Pen(ThemeHelper.ChartDivider, 1f);
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
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            int allocatedNotOverdue = Math.Max(0, _cntAllocated - _cntOverdue);
            int total = _cntInStore + allocatedNotOverdue + _cntUnderRepair + _cntDecommission + _cntOverdue;
            if (total == 0) total = 1;

            var segs = new[]
            {
                (Label:"In Stock",       Count:_cntInStore,      Color:Color.FromArgb(56,  142, 60)),
                (Label:"Allocated",      Count:allocatedNotOverdue, Color:Color.FromArgb(245, 124, 0)),
                (Label:"Under Repair",   Count:_cntUnderRepair,  Color:Color.FromArgb(33,  150, 243)),
                (Label:"Overdue",        Count:_cntOverdue,      Color:Color.FromArgb(255, 160, 0)),
                (Label:"Decommissioned", Count:_cntDecommission, Color:Color.FromArgb(239, 68,  68)),
            };

            int top = 54;
            int legendW = 150;
            int avail = Math.Min(p.Width - 32 - legendW, p.Height - top - 20);
            int size = Math.Max(70, avail);
            int cx = 16 + size / 2;
            int cy = top + (p.Height - top - size) / 2 + size / 2;
            var outerR = new RectangleF(cx - size / 2f, cy - size / 2f, size, size);
            float inner = size * 0.56f;
            var innerR = new RectangleF(cx - inner / 2f, cy - inner / 2f, inner, inner);

            float start = -90f;
            float relCursor = 0f;
            _donutHitSlices.Clear();
            foreach (var seg in segs)
            {
                float sweep = (float)seg.Count / total * 360f;
                float animSweep = sweep * _donutAnimProgress;
                if (animSweep >= 0.1f)
                {
                    using var br = new SolidBrush(seg.Color);
                    g.FillPie(br, outerR.X, outerR.Y, outerR.Width, outerR.Height, start, animSweep);
                }

                _donutHitSlices.Add((seg.Label, seg.Count, seg.Color, relCursor, sweep));
                start += animSweep;
                relCursor += sweep;
            }

            _donutCenter = new PointF(cx, cy);
            _donutOuterRadius = size / 2f;
            _donutInnerRadius = inner / 2f;

            using var hole = new SolidBrush(ThemeHelper.DonutHoleBg);
            g.FillEllipse(hole, innerR);

            using var fg = new SolidBrush(ThemeHelper.DonutNumText);
            using var fg2 = new SolidBrush(ThemeHelper.DonutSubText);
            int animTotal = (int)(total * _donutAnimProgress);
            var ns = g.MeasureString(animTotal.ToString(), DonutNumberFont);
            g.DrawString(animTotal.ToString(), DonutNumberFont, fg, new PointF(cx - ns.Width / 2f, cy - ns.Height / 2f - 5));
            var ss = g.MeasureString("Total", DonutSubFont);
            g.DrawString("Total", DonutSubFont, fg2, new PointF(cx - ss.Width / 2f, cy + ns.Height / 2f - 6));

            // ── Legend — vertical stack on the right ──
            int lx = (int)(outerR.Right + 24);
            int ly = top + 4;
            int rowH = Math.Max(20, (p.Height - top - 8) / segs.Length);
            foreach (var seg in segs)
            {
                using var db2 = new SolidBrush(seg.Color);
                g.FillEllipse(db2, lx, ly + rowH / 2 - 5, 10, 10);
                using var lc = new SolidBrush(ThemeHelper.ChartBodyText);
                g.DrawString(seg.Label, DonutLegendFont, lc, lx + 16, ly + rowH / 2 - 14);
                using var vc = new SolidBrush(seg.Color);
                int animCount = (int)(seg.Count * _donutAnimProgress);
                g.DrawString(animCount.ToString(), DonutLegendBoldFont, vc, lx + 16, ly + rowH / 2 + 1);
                ly += rowH;
            }

            if (_donutHoverIndex >= 0 && _donutHoverIndex < _donutHitSlices.Count)
            {
                var hs = _donutHitSlices[_donutHoverIndex];
                using var hoverPen = new Pen(Color.White, 2.5f);
                g.DrawPie(hoverPen, outerR.X - 2, outerR.Y - 2, outerR.Width + 4, outerR.Height + 4, -90f + hs.RelStart, hs.RelSweep);

                float hoverPct = hs.Count / (float)total * 100f;
                DrawHoverTooltip(g, p, _donutMousePos, hs.Label, $"{hs.Count} items ({hoverPct:0.#}%)", hs.Color);
            }
        }

        // ════════════════════════════════════════════════════════════════
        //  BAR CHART — now vertical, matches "Asset Status Breakdown"
        // ════════════════════════════════════════════════════════════════
        private void PaintBarChart(object? sender, PaintEventArgs e)
        {
            if (sender is not Panel p) return;
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            // Allocated/Overdue aren't derivable from EquipmentLogs alone (they come from
            // the Allocations table, which I don't have the schema for) — they stay live
            // regardless of the selected period until that's wired up too.
            int inStock = _periodOverrideCounts?.GetValueOrDefault("in_store", 0) ?? _cntInStore;
            int underRepair = _periodOverrideCounts?.GetValueOrDefault("under_repair", 0) ?? _cntUnderRepair;
            int decommissioned = _periodOverrideCounts?.GetValueOrDefault("decommissioned", 0) ?? _cntDecommission;

            int allocatedNotOverdue = Math.Max(0, _cntAllocated - _cntOverdue);
            var bars = new[]
            {
    (Label:"In Stock",        Count:inStock,          Color:Color.FromArgb(56,  142, 60)),
    (Label:"Allocated",       Count:allocatedNotOverdue, Color:Color.FromArgb(245, 124, 0)),
    (Label:"Under Repair",    Count:underRepair,      Color:Color.FromArgb(33,  150, 243)),
    (Label:"Decommissioned",  Count:decommissioned,   Color:Color.FromArgb(239, 68,  68)),
    (Label:"Overdue",         Count:_cntOverdue,      Color:Color.FromArgb(255, 160, 0)),
};

            int total = Math.Max(1, bars.Sum(b => b.Count));
            int maxVal = Math.Max(1, bars.Max(b => b.Count));
            int barCount = bars.Length;

            var labelFont = ChartLabelFont;
            var valFont = ChartValueFont;
            using var labelFg = new SolidBrush(ThemeHelper.ChartBodyText);
            using var bgBar = new SolidBrush(ThemeHelper.ChartTrackBg);
            _barHitCols.Clear();

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
                string line1, line2;
                if (words.Length < 2)
                {
                    // No space to split on (e.g. "Decommissioned") — break the
                    // word itself near the middle with a hyphen instead of
                    // overlapping the neighboring label.
                    int midChar = Math.Max(1, text.Length / 2);
                    line1 = text.Substring(0, midChar) + "-";
                    line2 = text.Substring(midChar);
                }
                else
                {
                    int mid = words.Length / 2;
                    line1 = string.Join(" ", words.Take(mid));
                    line2 = string.Join(" ", words.Skip(mid));
                }

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
                float pct = (float)b.Count / total * 100f;

                _barHitCols.Add((new RectangleF(chartX + colWidth * i, top, colWidth, p.Height - top), b.Label, b.Count, pct, b.Color));

                // Background track (full height column)
                using var trackPath = RoundedRectF(new RectangleF(barX, chartTopY, barWidth, chartH), 5f);
                g.FillPath(bgBar, trackPath);

                // Filled bar, growing upward from the bottom — staggered per
                // column, same convention as the category chart below it, so
                // both feel like one animation language.
                float colDelay = i * 0.12f;
                float colProgress = Math.Clamp((_barAnimProgress - colDelay) / (1f - colDelay), 0f, 1f);
                // Sqrt scale instead of linear — with a category as dominant as
                // "In Stock" (421 vs 1/1/6), a linear ratio rounds every small
                // value down to the same minimum-visible floor. Sqrt compresses
                // the dominant bar and stretches small ones apart instead.
                float ratio = maxVal == 0 ? 0 : MathF.Sqrt(b.Count) / MathF.Sqrt(maxVal);
                float targetFillH = b.Count > 0 ? Math.Max(6f, ratio * chartH) : 0;
                float fillH = targetFillH * colProgress;

                if (fillH > 0)
                {
                    float barY = chartTopY + (chartH - fillH);
                    using var grad = new LinearGradientBrush(
                        new PointF(barX, chartTopY + chartH), new PointF(barX, barY),
                        ControlPaint.Light(b.Color, 0.3f), b.Color);
                    using var fillPath = RoundedRectF(new RectangleF(barX, barY, barWidth, fillH), 5f);
                    g.FillPath(grad, fillPath);

                    // Value + pct, centered above the bar
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

            if (_barHoverIndex >= 0 && _barHoverIndex < _barHitCols.Count)
            {
                var hc = _barHitCols[_barHoverIndex];
                using var hoverBrush = new SolidBrush(Color.FromArgb(30, hc.Color));
                using var hoverPath = RoundedRectF(hc.Bounds, 5f);
                g.FillPath(hoverBrush, hoverPath);

                DrawHoverTooltip(g, p, _barMousePos, hc.Label, $"{hc.Count} items ({hc.Pct:0.#}%)", hc.Color);
            }
        }

        // ════════════════════════════════════════════════════════════════
        //  CATEGORY BREAKDOWN CHART — Equipment count by Category
        // ════════════════════════════════════════════════════════════════
        private void PaintCategoryBreakdownChart(object? sender, PaintEventArgs e)
        {
            if (sender is not Panel p) return;
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            if (_categoryBreakdown.Count == 0)
            {
                using var emptyFont = new Font("Segoe UI", 10f, FontStyle.Regular, GraphicsUnit.Point);
                using var emptyFg = new SolidBrush(Color.FromArgb(156, 163, 175));
                var msg = "No category data available";
                var msgSz = g.MeasureString(msg, emptyFont);
                g.DrawString(msg, emptyFont, emptyFg, (p.Width - msgSz.Width) / 2f, (p.Height - msgSz.Height) / 2f);
                return;
            }

            var palette = new[]
            {
        Color.FromArgb(94,  53,  177), Color.FromArgb(245, 124,   0),
        Color.FromArgb(56,  142,  60), Color.FromArgb(33,  150, 243),
        Color.FromArgb(239,  68,  68), Color.FromArgb(123,  31, 162),
    };

            int top = 54, padOuter = 16;

            // Single column now, smallest count first. This sort is local to this
            // chart only — _categoryBreakdown itself stays descending everywhere
            // else (nothing else reads it in a particular order, but keeping it
            // untouched avoids any risk of side effects).
            var catsAsc = _categoryBreakdown.OrderBy(c => c.Count).ToList();
            var bounds = new Rectangle(padOuter, top, p.Width - padOuter * 2, p.Height - top - 20);

            _catHitRows.Clear();
            PaintCategoryColumn(g, catsAsc, bounds, palette, colorOffset: 0);

            if (_catHoverIndex >= 0 && _catHoverIndex < _catHitRows.Count)
            {
                var hr = _catHitRows[_catHoverIndex];
                using var hoverBrush = new SolidBrush(Color.FromArgb(30, hr.Color));
                using var hoverPath = RoundedRectF(hr.Bounds, 5f);
                g.FillPath(hoverBrush, hoverPath);

                DrawHoverTooltip(g, p, _catMousePos, hr.Category, $"{hr.Count} items ({hr.Pct:0.#}%)", hr.Color);
            }
        }

        // ════════════════════════════════════════════════════════════════
        //  TOP EMPLOYEES BY HOLDINGS — horizontal ranked bars
        // ════════════════════════════════════════════════════════════════
        private void PaintEmployeesChart(object? sender, PaintEventArgs e)
        {
            if (sender is not Panel p) return;
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            if (_topEmployees.Count == 0)
            {
                using var emptyFont = new Font("Segoe UI", 9.5f);
                using var emptyFg = new SolidBrush(ThemeHelper.ChartSecText);
                var msg = "No allocation data available";
                var msgSz = g.MeasureString(msg, emptyFont);
                g.DrawString(msg, emptyFont, emptyFg, (p.Width - msgSz.Width) / 2f, (p.Height - msgSz.Height) / 2f);
                return;
            }

            int top = 54, padOuter = 16, rankW = 20, nameW = 30;
            int maxVal = Math.Max(1, _topEmployees.Max(x => x.Count));
            int barAreaX = padOuter + rankW + nameW;
            int barAreaW = Math.Max(20, p.Width - barAreaX - 46);

            float rowH = (float)(p.Height - top - 10) / _topEmployees.Count;
            float barH = Math.Clamp(rowH * 0.45f, 8f, 14f);

            using var labelFg = new SolidBrush(ThemeHelper.ChartBodyText);
            using var rankFg = new SolidBrush(ThemeHelper.ChartSecText);
            using var trackBrush = new SolidBrush(ThemeHelper.ChartTrackBg);
            Color barColor = Color.FromArgb(127, 119, 221); // purple, matches the mockup

            _empHitRows.Clear();

            for (int i = 0; i < _topEmployees.Count; i++)
            {
                var emp = _topEmployees[i];
                float rowDelay = i * 0.08f;
                float rowProgress = Math.Clamp((_barAnimProgress - rowDelay) / (1f - rowDelay), 0f, 1f);

                float by = top + i * rowH + (rowH - barH) / 2f;
                _empHitRows.Add((new RectangleF(padOuter, top + i * rowH, p.Width - padOuter * 2, rowH), emp.Name, emp.Count));

                g.DrawString((i + 1).ToString(), ChartLabelFont, rankFg, padOuter, by - 3);

                string shortName = emp.Name.Length > 0 ? emp.Name.Substring(0, 1) + "." : "?";
                g.DrawString(shortName, ChartLabelFont, labelFg, padOuter + rankW, by - 3);

                using var trackPath = RoundedRectF(new RectangleF(barAreaX, by, barAreaW, barH), 4f);
                g.FillPath(trackBrush, trackPath);

                float fillW = (float)emp.Count / maxVal * barAreaW * rowProgress;
                if (fillW > 1)
                {
                    using var fillPath = RoundedRectF(new RectangleF(barAreaX, by, fillW, barH), 4f);
                    using var fillBrush = new SolidBrush(barColor);
                    g.FillPath(fillBrush, fillPath);
                }

                using var valFg = new SolidBrush(ThemeHelper.ChartSecText);
                g.DrawString(emp.Count.ToString(), ChartLabelFont, valFg, barAreaX + barAreaW + 6, by - 3);
            }

            if (_empHoverIndex >= 0 && _empHoverIndex < _empHitRows.Count)
            {
                var hr = _empHitRows[_empHoverIndex];
            }
        }

        // ════════════════════════════════════════════════════════════════
        //  EQUIPMENT ALLOCATION BY LOCATION — stacked vertical bars
        // ════════════════════════════════════════════════════════════════
        private void PaintLocationChart(object? sender, PaintEventArgs e)
        {
            if (sender is not Panel p) return;
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            if (_locationBreakdown.Count == 0)
            {
                using var emptyFont = new Font("Segoe UI", 9.5f);
                using var emptyFg = new SolidBrush(ThemeHelper.ChartSecText);
                var msg = "No location data available";
                var msgSz = g.MeasureString(msg, emptyFont);
                g.DrawString(msg, emptyFont, emptyFg, (p.Width - msgSz.Width) / 2f, (p.Height - msgSz.Height) / 2f);
                return;
            }

            var locPalette = new[]
            {
        Color.FromArgb(94, 53, 177), Color.FromArgb(217, 119, 6),
        Color.FromArgb(21, 128, 61), Color.FromArgb(29, 78, 216),
        Color.FromArgb(190, 24, 93), Color.FromArgb(2, 132, 199),
    };

            int top = 54, bottomLabels = 70, padOuter = 20;
            int chartH = Math.Max(20, p.Height - top - bottomLabels);
            int maxTotal = Math.Max(1, _locationBreakdown.Max(l => l.InStock + l.Allocated + l.Repair));
            int count = _locationBreakdown.Count;
            float colWidth = (p.Width - padOuter * 2f) / count;
            float barWidth = Math.Min(40f, colWidth * 0.55f);

            _locHitCols.Clear();

            for (int i = 0; i < count; i++)
            {
                var loc = _locationBreakdown[i];
                // Delay is scaled to the actual column count and capped at 50% of the
                // progress budget, so it never exceeds 1 regardless of how many rooms
                // there are — every bar animates, just staggered proportionally.
                float colDelay = count <= 1 ? 0f : (i / (float)(count - 1)) * 0.5f;
                float colProgress = Math.Clamp((_barAnimProgress - colDelay) / (1f - colDelay), 0f, 1f);
                float colCenterX = padOuter + colWidth * i + colWidth / 2f;
                float barX = colCenterX - barWidth / 2f;
                int total = loc.InStock + loc.Allocated + loc.Repair;
                float barH = (float)total / maxTotal * chartH * colProgress;
                float baseY = top + chartH;

                _locHitCols.Add((new RectangleF(barX, top, barWidth, chartH), loc.Location, loc.RoomLabel, loc.InStock, loc.Allocated, loc.Repair));

                Color barColor = locPalette[i % locPalette.Length];
                if (barH > 0)
                {
                    using var grad = new LinearGradientBrush(
                        new PointF(barX, baseY), new PointF(barX, baseY - barH),
                        ControlPaint.Light(barColor, 0.3f), barColor);
                    using var fillPath = RoundedRectF(new RectangleF(barX, baseY - barH, barWidth, barH), 5f);
                    g.FillPath(grad, fillPath);
                }

                using var valFg = new SolidBrush(barColor);
                string valStr = total.ToString();
                var vSz = g.MeasureString(valStr, ChartValueFont);
                g.DrawString(valStr, ChartValueFont, valFg, colCenterX - vSz.Width / 2f, baseY - barH - vSz.Height - 4);

                using var roomFont = new Font("Segoe UI", 6.5f, FontStyle.Regular, GraphicsUnit.Point);
                using var labelFg = new SolidBrush(ThemeHelper.ChartBodyText);
                var labelState = g.Save();
                g.TranslateTransform(colCenterX, baseY + 8);
                g.RotateTransform(90f); // was -90 — that direction climbed upward into the bar; +90 runs downward, clear of it
                using var sf = new StringFormat
                {
                    Alignment = StringAlignment.Near,
                    LineAlignment = StringAlignment.Center,
                    Trimming = StringTrimming.EllipsisCharacter,
                    FormatFlags = StringFormatFlags.NoWrap
                };
                var labelRect = new RectangleF(0, -colWidth / 2f, bottomLabels - 12, colWidth - 4);
                g.DrawString(loc.RoomLabel, roomFont, labelFg, labelRect, sf);
                g.Restore(labelState);
            }

            if (_locHoverIndex >= 0 && _locHoverIndex < _locHitCols.Count)
            {
                var hc = _locHitCols[_locHoverIndex];
                Color hColor = locPalette[_locHoverIndex % locPalette.Length];
                DrawHoverTooltip(g, p, _locMousePos, hc.Location,
                    $"{hc.Allocated} equipment allocated", hColor);
            }
        }

        // ════════════════════════════════════════════════════════════════
        //  ASSET ALLOCATION AND USAGE TREND — dual line chart
        // ════════════════════════════════════════════════════════════════
        private void PaintTrendChart(object? sender, PaintEventArgs e)
        {
            if (sender is not Panel p) return;
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // AFTER
            var trendSeries = GetCachedTrendSeries(_trendPeriod);
            float[] allocatedTrend = trendSeries.Allocated;
            float[] inStockTrend = trendSeries.InStock;
            string[] labels = trendSeries.Labels;
            string[] dateLabels = trendSeries.DateLabels;
            if (allocatedTrend.Length < 2) return; // not enough buckets to draw a line

            int top = 58, padOuter = 24, bottom = 26;
            int chartW = p.Width - padOuter * 2;
            int chartH = p.Height - top - bottom;
            if (chartW <= 0 || chartH <= 0) return;

            // Faint horizontal watermark gridlines — drawn first so the band/lines sit on top.
            const int gridLines = 4;
            using (var gridPen = new Pen(Color.FromArgb(ThemeHelper.IsDarkMode ? 22 : 18,
                       ThemeHelper.ChartSecText.R, ThemeHelper.ChartSecText.G, ThemeHelper.ChartSecText.B), 1f))
            {
                for (int gi = 0; gi <= gridLines; gi++)
                {
                    float gy = top + chartH * gi / (float)gridLines;
                    g.DrawLine(gridPen, padOuter, gy, padOuter + chartW, gy);
                }
            }

            float allMax = Math.Max(1f, Math.Max(allocatedTrend.Max(), inStockTrend.Max()));

            PointF[] MapPoints(float[] vals)
            {
                var pts = new PointF[vals.Length];
                for (int i = 0; i < vals.Length; i++)
                {
                    float nx = padOuter + chartW * i / (float)(vals.Length - 1);
                    float ny = top + chartH - (vals[i] / allMax * chartH);
                    pts[i] = new PointF(nx, ny);
                }
                return pts;
            }

            var allocPts = MapPoints(allocatedTrend);
            var stockPts = MapPoints(inStockTrend);

            // AFTER
            _trendHitPoints.Clear();
            for (int i = 0; i < allocPts.Length; i++)
            {
                var hitRect = new RectangleF(allocPts[i].X - 8, Math.Min(allocPts[i].Y, stockPts[i].Y) - 8,
                    16, Math.Abs(stockPts[i].Y - allocPts[i].Y) + 16);
                _trendHitPoints.Add((hitRect, labels[i], dateLabels[i], allocatedTrend[i], inStockTrend[i]));
            }

            int visible = Math.Max(2, (int)(allocPts.Length * _barAnimProgress) + 1);
            visible = Math.Min(visible, allocPts.Length);

            Color allocColor = Color.FromArgb(83, 74, 183);
            Color stockColor = Color.FromArgb(15, 110, 86);
            Color bandFill = Color.FromArgb(60, 175, 169, 236);

            if (visible >= 2)
            {
                var top12 = allocPts[..visible];
                var bot12 = stockPts[..visible];

                var bandPts = new List<PointF>();
                bandPts.AddRange(top12);
                for (int i = bot12.Length - 1; i >= 0; i--) bandPts.Add(bot12[i]);

                using var band = new GraphicsPath();
                band.AddPolygon(bandPts.ToArray());
                using var bandBrush = new SolidBrush(bandFill);
                g.FillPath(bandBrush, band);

                using var stockPen = new Pen(stockColor, 2f) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };
                g.DrawLines(stockPen, bot12);
                using var allocPen = new Pen(allocColor, 2f) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };
                g.DrawLines(allocPen, top12);

                void DrawMarkers(PointF[] pts, Color color)
                {
                    using var mb = new SolidBrush(color);
                    foreach (var pt in pts)
                    {
                        var diamond = new[]
                        {
                    new PointF(pt.X, pt.Y - 4), new PointF(pt.X + 4, pt.Y),
                    new PointF(pt.X, pt.Y + 4), new PointF(pt.X - 4, pt.Y)
                };
                        g.FillPolygon(mb, diamond);
                    }
                }
                DrawMarkers(top12, allocColor);
                DrawMarkers(bot12, stockColor);
            }

            using var axisFg = new SolidBrush(ThemeHelper.ChartSecText);
            using var axisSf = new StringFormat { Alignment = StringAlignment.Center };
            int step = labels.Length > 20 ? 4 : labels.Length > 8 ? 2 : 1;
            for (int i = 0; i < labels.Length; i += step)
            {
                float lx = padOuter + chartW * i / (float)(labels.Length - 1);
                g.DrawString(labels[i], ChartLabelFont, axisFg, lx, top + chartH + 6, axisSf);
            }

            int lx2 = padOuter;
            void DrawLegendItem(string label, Color color, ref int x)
            {
                using var b = new SolidBrush(color);
                g.FillRectangle(b, x, top - 20, 8, 8);
                g.DrawString(label, ChartLabelFont, axisFg, x + 12, top - 24);
                x += (int)g.MeasureString(label, ChartLabelFont).Width + 26;
            }
            DrawLegendItem("Allocated Assets", allocColor, ref lx2);
            DrawLegendItem("In Stock Assets", stockColor, ref lx2);
            // AFTER
            if (_trendHoverIndex >= 0 && _trendHoverIndex < _trendHitPoints.Count)
            {
                var hp = _trendHitPoints[_trendHoverIndex];
                DrawHoverTooltip(g, p, _trendMousePos, hp.DateLabel,
                    $"Allocated: {(int)hp.Allocated}   In Stock: {(int)hp.InStock}", allocColor);
            }
        }

        private void PaintCategoryColumn(Graphics g, List<(string Category, int Count)> cats, Rectangle bounds, Color[] palette, int colorOffset)
        {
            if (cats.Count == 0) return;

            int padL = 130; // label column width — tune to your longest category name
            int chartX = bounds.X + padL;
            int chartW = Math.Max(20, bounds.Width - padL - 90); // 90 reserved for count/pct on the right
            int total = Math.Max(1, _categoryBreakdown.Sum(c => c.Count)); // % is relative to the whole dataset, not just this column
            int maxVal = Math.Max(1, _categoryBreakdown.Max(c => c.Count));

            var labelFont = ChartCatLabelFont;
            var valFont = ChartValueFont;
            using var labelFg = new SolidBrush(ThemeHelper.ChartBodyText);
            using var bgBar = new SolidBrush(ThemeHelper.ChartTrackBg);

            float rowH = (float)bounds.Height / cats.Count;
            float barH = Math.Clamp(rowH * 0.55f, 14f, 26f); // slimmer bars — the old up-to-44px floor was
                                                             // what made small and large counts look almost
                                                             // the same width

            for (int i = 0; i < cats.Count; i++)
            {
                var cat = cats[i];
                Color color = palette[(i + colorOffset) % palette.Length];
                float rowPct = (float)cat.Count / total * 100f;
                _catHitRows.Add((new RectangleF(bounds.X, bounds.Y + i * rowH, bounds.Width, rowH), cat.Category, cat.Count, rowPct, color));

                int rowIndexGlobal = colorOffset + i;
                float rowDelay = rowIndexGlobal * 0.08f;
                float rowProgress = Math.Clamp((_barAnimProgress - rowDelay) / (1f - rowDelay), 0f, 1f);
                float verticalOffset = (1f - rowProgress) * 24f;
                float by = bounds.Y + i * rowH + (rowH - barH) / 2f + verticalOffset;

                using var trackPath = RoundedRectF(new RectangleF(chartX, by, chartW, barH), 5f);
                g.FillPath(bgBar, trackPath);

                // Real proportional width — a small 4px floor just keeps a near-zero
                // count visible, instead of the old 44px-tall floor that made every
                // bar read as roughly the same size regardless of its actual count.
                float fillW = maxVal == 0 ? 0 : Math.Max(4f, (float)cat.Count / maxVal * chartW);
                if (fillW > 0)
                {
                    using var grad = new LinearGradientBrush(
                        new PointF(chartX, by), new PointF(chartX + fillW, by),
                        ControlPaint.Light(color, 0.3f), color);
                    using var fillPath = RoundedRectF(new RectangleF(chartX, by, fillW, barH), 5f);
                    g.FillPath(grad, fillPath);
                }

                var lSz = g.MeasureString(cat.Category, labelFont);
                float lblX = Math.Max(bounds.X, chartX - lSz.Width - 10);
                g.DrawString(cat.Category, labelFont, labelFg, lblX, by + barH / 2f - lSz.Height / 2f);

                // Value text is now FIXED at chartX + chartW + 8 for every row, instead
                // of trailing the end of each bar — that's what was pushing the number
                // on top of the bar when the fallback clamp kicked in. Now it always
                // lands in the reserved right-hand strip, forming a clean aligned column.
                float pct = (float)cat.Count / total * 100f;
                string valStr = $"{cat.Count} ({pct:0}%)";
                using var vFg = new SolidBrush(color);
                var vSz = g.MeasureString(valStr, valFont);
                float valX = chartX + chartW + 8;
                g.DrawString(valStr, valFont, vFg, valX, by + barH / 2f - vSz.Height / 2f);
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

        // AFTER
        // Tune this to move the divide up/down — 0.62 means the top (light) band
        // takes 62% of the bar's height, the bottom (dark) band the remaining 38%.
        private void PaintTopBarDiagonal(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var rect = panelTop.ClientRectangle;
            if (rect.Width <= 0 || rect.Height <= 0) return;

            using var baseBrush = new SolidBrush(ThemeHelper.TopBarBg);
            g.FillRectangle(baseBrush, rect);
        }

        private void PaintSidebarShadow(object? sender, PaintEventArgs e)
        {
            if (panelDesktop.Height <= 0) return;
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
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        // Person silhouette with a small gear badge at the bottom-right —
        // matches the "Account Settings" icon style from the reference screenshot.
        private static void DrawAccountSettingsIcon(Graphics g, RectangleF bounds, Color color)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using var pen = new Pen(color, 1.4f) { LineJoin = LineJoin.Round, StartCap = LineCap.Round, EndCap = LineCap.Round };

            // Head
            float headD = bounds.Height * 0.42f;
            var headRect = new RectangleF(bounds.X + (bounds.Width - headD) / 2f - 1f, bounds.Y, headD, headD);
            g.DrawEllipse(pen, headRect);

            // Shoulders (open arc, not a full circle)
            float bodyW = bounds.Width * 0.78f;
            float bodyH = bounds.Height * 0.55f;
            var bodyRect = new RectangleF(bounds.X + (bounds.Width - bodyW) / 2f - 1f, headRect.Bottom - 1f, bodyW, bodyH);
            g.DrawArc(pen, bodyRect, 180, 180);

            // Small gear badge, bottom-right, overlapping the silhouette slightly
            float gearD = bounds.Height * 0.46f;
            var gearCenter = new PointF(bounds.Right - gearD * 0.55f, bounds.Bottom - gearD * 0.55f);
            using var gearBg = new SolidBrush(Color.White);
            g.FillEllipse(gearBg, gearCenter.X - gearD / 2f - 1f, gearCenter.Y - gearD / 2f - 1f, gearD + 2f, gearD + 2f);
            DrawGear(g, gearCenter, gearD / 2f, color);
        }

        private static void DrawGear(Graphics g, PointF center, float radius, Color color)
        {
            using var brush = new SolidBrush(color);
            using var pen = new Pen(color, 1.1f);

            int teeth = 6;
            float toothLen = radius * 0.32f;
            float innerR = radius - toothLen * 0.4f;

            var path = new GraphicsPath();
            for (int i = 0; i < teeth; i++)
            {
                double a0 = i * (360.0 / teeth) - 90;
                double a1 = a0 + (360.0 / teeth) / 2.2;
                double a2 = a0 + (360.0 / teeth);

                PointF P(double angleDeg, float r) => new PointF(
                    center.X + r * (float)Math.Cos(angleDeg * Math.PI / 180),
                    center.Y + r * (float)Math.Sin(angleDeg * Math.PI / 180));

                if (i == 0) path.StartFigure();
                path.AddLine(P(a0, innerR), P(a0, radius));
                path.AddLine(P(a0, radius), P(a1, radius));
                path.AddLine(P(a1, radius), P(a2, innerR));
            }
            path.CloseFigure();
            g.FillPath(brush, path);

            float holeR = radius * 0.38f;
            using var holeBrush = new SolidBrush(Color.White);
            g.FillEllipse(holeBrush, center.X - holeR, center.Y - holeR, holeR * 2, holeR * 2);
        }

        // Clean power/logout glyph — a broken circle with a vertical tick,
        // reads better at small sizes than the ⏻ emoji.
        private static void DrawPowerIcon(Graphics g, RectangleF bounds, Color color)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using var pen = new Pen(color, 1.6f) { StartCap = LineCap.Round, EndCap = LineCap.Round };

            float d = Math.Min(bounds.Width, bounds.Height);
            var circleRect = new RectangleF(bounds.X + (bounds.Width - d) / 2f, bounds.Y + d * 0.12f, d * 0.92f, d * 0.92f);
            g.DrawArc(pen, circleRect, -60, 300); // gap at the top for the tick

            float tickX = circleRect.X + circleRect.Width / 2f;
            g.DrawLine(pen, tickX, bounds.Y, tickX, bounds.Y + d * 0.5f);
        }

        private static GraphicsPath RoundedRectF(RectangleF r, float radius)
        {
            var path = new GraphicsPath();
            float d = radius * 2f;
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        private static void DrawHoverTooltip(Graphics g, Panel host, Point mousePos, string title, string subtitle, Color accent)
        {
            var titleSz = g.MeasureString(title, TooltipTitleFont);
            var subSz = g.MeasureString(subtitle, TooltipSubFont);
            float boxW = Math.Max(titleSz.Width, subSz.Width) + 20;
            float boxH = titleSz.Height + subSz.Height + 16;

            float x = mousePos.X + 14;
            float y = mousePos.Y - boxH - 10;
            if (x + boxW > host.Width) x = mousePos.X - boxW - 14;
            if (y < 0) y = mousePos.Y + 14;

            // Final clamp — guarantees the box never bleeds past any edge of its host panel.
            if (x < 4) x = 4;
            if (x + boxW > host.Width - 4) x = Math.Max(4, host.Width - boxW - 4);
            if (y < 4) y = 4;
            if (y + boxH > host.Height - 4) y = Math.Max(4, host.Height - boxH - 4);

            var rect = new RectangleF(x, y, boxW, boxH);
            using var shadowPath = RoundedRectF(new RectangleF(rect.X + 2, rect.Y + 2, rect.Width, rect.Height), 6f);
            using var shadowBrush = new SolidBrush(Color.FromArgb(40, 0, 0, 0));
            g.FillPath(shadowBrush, shadowPath);

            using var path = RoundedRectF(rect, 6f);
            using var bg = new SolidBrush(ThemeHelper.IsDarkMode ? Color.FromArgb(45, 48, 55) : Color.White);
            g.FillPath(bg, path);
            using var border = new Pen(accent, 1.4f);
            g.DrawPath(border, path);

            using var accentBrush = new SolidBrush(accent);
            g.FillRectangle(accentBrush, rect.X, rect.Y + 4, 3, rect.Height - 8);

            using var titleBrush = new SolidBrush(ThemeHelper.ChartTitleText);
            using var subBrush = new SolidBrush(ThemeHelper.ChartSecText);
            g.DrawString(title, TooltipTitleFont, titleBrush, rect.X + 10, rect.Y + 6);
            g.DrawString(subtitle, TooltipSubFont, subBrush, rect.X + 10, rect.Y + 6 + titleSz.Height + 2);
        }

        private int HitTestDonut(Point mouse)
        {
            if (_donutHitSlices.Count == 0) return -1;
            float dx = mouse.X - _donutCenter.X;
            float dy = mouse.Y - _donutCenter.Y;
            float dist = MathF.Sqrt(dx * dx + dy * dy);
            if (dist < _donutInnerRadius || dist > _donutOuterRadius) return -1;

            float raw = MathF.Atan2(dy, dx) * 180f / MathF.PI;    // 0°=east, clockwise
            float relToTop = ((raw + 90f) % 360f + 360f) % 360f;  // 0°=top, matches FillPie's -90° start

            for (int i = 0; i < _donutHitSlices.Count; i++)
            {
                var s = _donutHitSlices[i];
                if (relToTop >= s.RelStart && relToTop < s.RelStart + s.RelSweep) return i;
            }
            return -1;
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
            CloseAllTopBarPopups();

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

            // AFTER
            panelContent.Controls.Clear();
            panelContent.Controls.Add(childForm);
            childForm.BringToFront();
            childForm.Show();

            panelStats.Visible = false;

            // Defensive: make sure the top-bar widgets (search, profile) stay positioned
            // and on top of the z-order after every navigation, not just on initial load/resize.
            PositionTopSearchBar();
            PositionTopProfileWidget();
            _pnlUserProfile?.BringToFront();
        }
        
        private void CloseAllTopBarPopups()
        {
            _barPeriodPopup?.Close();
            _barPeriodPopup = null;
            _searchResultsPopup?.Close();
            _searchResultsPopup = null;
            _profilePopup?.Close();
            _profilePopup = null;
        }

        private void ShowDashboard()
        {


            CloseAllTopBarPopups();
            activeForm?.Close();
            activeForm = null;
            panelStats.Visible = true;
            SetActiveButton(btnDashboard);
            AnimateTitleTo("🏠 Dashboard");
            LoadStats();
        }

        private void btnDashboard_Click(object? sender, EventArgs e) => ShowDashboard();

        private void OpenAllocationAvailable()
        {
            AnimateTitleTo("📋 Allocation");
            OpenChildForm(new AllocationForm("Available"), btnAllocation);
        }

        private void OpenEquipmentByStatus(string status)
        {
            AnimateTitleTo("🖥️ Equipment");
            var frm = new EquipmentForm();
            OpenChildForm(frm, btnEquipment);
            frm.ApplyStatusFilter(status);
        }

        private void OpenOverdueEquipment()
        {
            AnimateTitleTo("📋 Allocation");
            var frm = new AllocationForm("Overdue");
            OpenChildForm(frm, btnAllocation);
        }

        private void btnEquipment_Click(object? sender, EventArgs e)
        {
            AnimateTitleTo("🖥️ Equipment");
            OpenChildForm(new EquipmentForm(), btnEquipment);
        }

        private void btnEmployees_Click(object? sender, EventArgs e)
        {
            AnimateTitleTo("👥 Employees");
            OpenChildForm(new EmployeesForm(), btnEmployees);
        }

        public void NavigateToEmployee(string employeeCode)
        {
            AnimateTitleTo("👥 Employees");
            OpenChildForm(new EmployeesForm(employeeCode), btnEmployees);
        }


        public void NavigateToLogs(string equipmentCode)
        {
            AnimateTitleTo("📜 Logs");
            OpenChildForm(new LogsForm(equipmentCode), btnLogs);
        }


        private void btnLocations_Click(object? sender, EventArgs e)
        {
            AnimateTitleTo("📍 Locations");
            OpenChildForm(new LocationsForm(), btnLocations);
        }

        private void btnAllocation_Click(object? sender, EventArgs e)
        {
            AnimateTitleTo("📋 Allocation");
            OpenChildForm(new AllocationForm(), btnAllocation);
        }

        private void btnReturns_Click(object? sender, EventArgs e)
        {
            AnimateTitleTo("↩️ Returns");
            OpenChildForm(new ReturnsForm(), btnReturns);
        }

        private void btnTransfer_Click(object? sender, EventArgs e)
        {
            AnimateTitleTo("🔄 Transfer");
            OpenChildForm(new TransferForm(), btnTransfer);
        }

        private void btnDecommission_Click(object? sender, EventArgs e)
        {
            if (!EnsureAdmin()) return;
            AnimateTitleTo("🚫 Decommission");

            OpenChildForm(new DecommissionForm(), btnDecommission);
        }

        private void btnDeletedItems_Click(object? sender, EventArgs e)
        {
            if (!EnsureAdmin()) return;
            AnimateTitleTo("♻️ Deleted Items");
            OpenChildForm(new DeletedItemsForm(), btnDeletedItems);
        }

        private void btnLogs_Click(object? sender, EventArgs e)
        {
            AnimateTitleTo("📜 Logs");
            OpenChildForm(new LogsForm(), btnLogs);
        }

        private void btnQRScanner_Click(object? sender, EventArgs e)
        {
            AnimateTitleTo("📷 QR Scanner");
            OpenChildForm(new QRScannerForm(), btnQRScanner);
        }

        private void btnCheckInOut_Click(object? sender, EventArgs e)
        {
            AnimateTitleTo("✅ Check In/Out");
            OpenChildForm(new CheckInOutForm(), btnCheckInOut);
        }

        private void btnReports_Click(object? sender, EventArgs e)
        {
            AnimateTitleTo("📊 Reports");
            OpenChildForm(new ReportsForm(), btnReports);
        }

        private void btnSettings_Click(object? sender, EventArgs e)
        {
            if (!EnsureAdmin()) return;
            AnimateTitleTo("⚙️ Settings");
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


