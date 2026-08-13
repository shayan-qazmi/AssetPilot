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
        private Panel? _panelTopRow;   
        private Panel? _panelBotRow;
        private Panel? _panelActivities = null;
        private Panel? _pnlSidebarHeader;
        private Panel? _pnlSearchBox;
        private TextBox? _txtSearch;
        private Panel? _pnlUserProfile;
        private Form? _profilePopup;
        private Label? _lblBarPeriod;
        private Form? _barPeriodPopup;
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
        private const int CenterCardIndex = 3; // "Under Repair"
        private const int BellRightOffset = 130;
        private float _barAnimProgress = 1f;
        private float _activitiesAnimProgress = 1f;
        private Rectangle _activitiesTargetBounds;
        


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


        private string _barPeriod = "All Time";
        private Dictionary<string, int>? _periodOverrideCounts; // null = show live/all-time counts
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
                // message has already been processed, guaranteeing panelStats.Width is
                // final before cards get built — no more re-click needed.
                BeginInvoke(new Action(() => { if (panelStats.Visible) LoadStats(); }));
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
            panelTop.BackColor    = TopBarYellow;
            NotificationHelper.AttachBell(panelTop, this, rightOffset: BellRightOffset);
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
            lblTitle.ForeColor    = Color.FromArgb(33, 33, 33);
            lblTitle.Visible      = true;
            this.Text             = "Q-Inventory";

            AnimateTitleTo("Dashboard");

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

            // Nav buttons
            ThemeHelper.StyleSidebarButton(btnDashboard, active: true);
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
            // Icon-only, always — keep just the leading glyph, drop the label text.
            int logoutSpaceIdx = btnLogout.Text.IndexOf(' ');
            if (logoutSpaceIdx > 0) btnLogout.Text = btnLogout.Text.Substring(0, logoutSpaceIdx);
            btnLogout.TextAlign = ContentAlignment.MiddleCenter;
            btnLogout.Padding = new Padding(0);

            panelDesktop.BackColor = Color.FromArgb(240, 244, 240);
            panelStats.BackColor   = Color.FromArgb(240, 244, 240);

            // Who's logged in — avatar + full name + @username, clickable
            // through to Settings > Users.
            BuildUserProfileFooter();
            BuildSidebarToggle();
        }

        private void BuildBarPeriodSelector(Panel host)
        {
            var wrap = new Panel
            {
                Size = new Size(100, 24),
                BackColor = Color.White,   // SOLID — covers the title underneath completely
                Cursor = Cursors.Hand
            };
            var lbl = new Label
            {
                Text = _barPeriod + "  ▾",
                Font = new Font("Segoe UI", 8f),
                ForeColor = ThemeHelper.TextSecondary,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleRight,
                BackColor = Color.White,   // also solid, not transparent
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

        private void ToggleBarPeriodPopup(Label anchor)
        {
            if (_barPeriodPopup != null && !_barPeriodPopup.IsDisposed)
            {
                _barPeriodPopup.Close();
                _barPeriodPopup = null;
                return;
            }

            var options = new[] { "All Time", "This Month", "Last Month", "This Year" };
            var popup = new NoActivatePopup
            {
                FormBorderStyle = FormBorderStyle.None,
                StartPosition = FormStartPosition.Manual,
                ShowInTaskbar = false,
                Size = new Size(120, options.Length * 28 + 8),
                BackColor = Color.White
            };
            popup.Paint += (s, e) =>
            {
                using var pen = new Pen(Color.FromArgb(220, 220, 220), 1);
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
                    Size = new Size(100, 26),
                    Font = ThemeHelper.FontSmall,
                    ForeColor = opt == _barPeriod ? ThemeHelper.Accent : ThemeHelper.TextPrimary,
                    TextAlign = ContentAlignment.MiddleLeft,
                    Cursor = Cursors.Hand,
                    BackColor = Color.White
                };
                row.MouseEnter += (s, e) => row.BackColor = Color.FromArgb(243, 244, 246);
                row.MouseLeave += (s, e) => row.BackColor = Color.White;
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
        private void BuildTopSearchBar()
        {
            var existing = panelTop.Controls.OfType<Panel>().FirstOrDefault(p => p.Name == "pnlSearchBox");
            existing?.Dispose();

            const int boxHeight = 36;
            const int boxWidth = 280;

            _pnlSearchBox = new Panel
            {
                Name = "pnlSearchBox",
                Size = new Size(boxWidth, boxHeight),
                BackColor = Color.Transparent,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };

            void ApplySearchBoxRegion()
            {
                using var path = RoundedRect(new Rectangle(0, 0, _pnlSearchBox.Width - 1, _pnlSearchBox.Height - 1), boxHeight / 2);
                _pnlSearchBox.Region = new Region(path);
            }
            ApplySearchBoxRegion();
            _pnlSearchBox.Resize += (s, e) => ApplySearchBoxRegion();

            _pnlSearchBox.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;

                using var path = RoundedRect(new Rectangle(0, 0, _pnlSearchBox.Width - 1, _pnlSearchBox.Height - 1), boxHeight / 2);

                using var fillBrush = new SolidBrush(Color.White);
                g.FillPath(fillBrush, path);
                using var borderPen = new Pen(Color.FromArgb(40, 0, 0, 0), 1f);
                g.DrawPath(borderPen, path);

                // magnifying glass icon
                using var iconPen = new Pen(Color.FromArgb(140, 140, 140), 1.6f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
                int glassCx = 20, glassCy = boxHeight / 2 - 2;
                int glassR = 6;
                g.DrawEllipse(iconPen, glassCx - glassR, glassCy - glassR, glassR * 2, glassR * 2);
                g.DrawLine(iconPen,
                    glassCx + (int)(glassR * 0.7f), glassCy + (int)(glassR * 0.7f),
                    glassCx + glassR + 4, glassCy + glassR + 4);
            };

            _txtSearch = new TextBox
            {
                Name = "txtSearch",
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 9f),
                ForeColor = Color.FromArgb(55, 65, 81),
                BackColor = Color.White,
                Location = new Point(36, (boxHeight - 18) / 2),
                Size = new Size(boxWidth - 36 - 52, 18),
                PlaceholderText = "Search anything..."
            };
            _pnlSearchBox.Controls.Add(_txtSearch);

            var lblShortcut = new Label
            {
                Text = "Ctrl K",
                Font = new Font("Segoe UI", 7f),
                ForeColor = Color.FromArgb(170, 170, 170),
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
            ? Color.FromArgb(230, 240, 255)
            : Color.White;

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
            var popup = new NoActivatePopup
            {
                FormBorderStyle = FormBorderStyle.None,
                StartPosition = FormStartPosition.Manual,
                ShowInTaskbar = false,
                Size = new Size(_pnlSearchBox!.Width, Math.Min(380, 20 + rowCount * 40 + (equipItems.Count > 0 && empItems.Count > 0 ? 24 : 0))),
                BackColor = Color.White
            };
            popup.Paint += (s, e) =>
            {
                using var pen = new Pen(Color.FromArgb(220, 220, 220), 1);
                e.Graphics.DrawRectangle(pen, 0, 0, popup.Width - 1, popup.Height - 1);
            };

            var screenPoint = _pnlSearchBox.PointToScreen(new Point(0, _pnlSearchBox.Height));
            popup.Location = new Point(screenPoint.X, screenPoint.Y + 4);

            var listPanel = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Color.White };
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
                var row = new Panel { Location = new Point(0, y), Size = new Size(listPanel.Width - 18, 36), BackColor = Color.White, Cursor = Cursors.Hand };
                var lblT = new Label
                {
                    Text = title,
                    Font = ThemeHelper.FontBold,
                    ForeColor = ThemeHelper.TextPrimary,
                    Location = new Point(12, 2),
                    Size = new Size(row.Width - 24, 16),
                    AutoEllipsis = true
                };
                var lblS = new Label
                {
                    Text = subtitle,
                    Font = ThemeHelper.FontSmall,
                    ForeColor = ThemeHelper.TextSecondary,
                    Location = new Point(12, 18),
                    Size = new Size(row.Width - 24, 16),
                    AutoEllipsis = true
                };
                row.Controls.Add(lblT);
                row.Controls.Add(lblS);

                void Go()
                {
                    popup.Close();
                    if (_txtSearch != null) _txtSearch.Text = "";
                    onClick();
                }

                row.MouseEnter += (s, e) => { if (row.BackColor != Color.FromArgb(230, 240, 255)) row.BackColor = Color.FromArgb(243, 244, 246); };
                row.MouseLeave += (s, e) => { if (_searchResultRows.Count > 0 && _searchResultRows[_searchSelectedIndex == -1 ? 0 : _searchSelectedIndex].Row != row) row.BackColor = Color.White; };
                row.Click += (s2, e2) => Go();
                lblT.Click += (s2, e2) => Go();
                lblS.Click += (s2, e2) => Go();

                listPanel.Controls.Add(row);
                _searchResultRows.Add((row, Go));
                y += 36;
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
                        () => { AnimateTitleTo("Equipment"); OpenChildForm(new EquipmentForm(eq.EquipmentCode), btnEquipment); });
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

        private void PositionTopSearchBar()
        {
            if (_pnlSearchBox == null) return;
            const int gapFromBell = 20;
            int bellLeftEdge = panelTop.Width - BellRightOffset;   // was "- 60"
            _pnlSearchBox.Location = new Point(
                bellLeftEdge - gapFromBell - _pnlSearchBox.Width,
                (panelTop.Height - _pnlSearchBox.Height) / 2);
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

            _sidebarCollapseTimer = new System.Windows.Forms.Timer { Interval = 10 };
            _sidebarCollapseTimer.Tick += (s, e) =>
            {
                const int step = 20;
                int newWidth = _sidebarCollapsed
                    ? Math.Max(targetWidth, panelMenu.Width - step)
                    : Math.Min(targetWidth, panelMenu.Width + step);
                panelMenu.Width = newWidth;
                PositionSidebarHeader();
                PositionTopSearchBar();
                PositionTopProfileWidget();

                if (newWidth == targetWidth)
                {
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
            // Rebuild from scratch each time ApplyTheme runs (e.g. after
            // login) so the picture/name/username always reflect who's
            // actually signed in.
            var existing = panelMenu.Controls.OfType<Panel>().FirstOrDefault(p => p.Name == "pnlUserFooter");
            existing?.Dispose();

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

            var footer = new Panel
            {
                Name = "pnlUserFooter",
                Dock = DockStyle.Bottom,
                Height = 64,
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand
            };

            var divider = new Panel
            {
                Dock = DockStyle.Top,
                Height = 1,
                BackColor = Color.FromArgb(40, 255, 255, 255)
            };
            footer.Controls.Add(divider);

            const int avatarSize = 40;
            var avatarImage = AvatarHelper.TryLoadUserImage(picturePath);
            footer.Disposed += (s, e) => avatarImage?.Dispose();

            var avatarPanel = new Panel
            {
                Size = new Size(avatarSize, avatarSize),
                Location = new Point(16, (footer.Height - avatarSize) / 2),
                BackColor = Color.Transparent
            };
            avatarPanel.Paint += (s, e) =>
                AvatarHelper.DrawAvatar(e.Graphics, new Rectangle(0, 0, avatarSize - 1, avatarSize - 1), avatarImage);
            footer.Controls.Add(avatarPanel);

            var lblName = new Label
            {
                Text = string.IsNullOrWhiteSpace(fullName) ? username : fullName,
                Location = new Point(avatarPanel.Right + 10, 15),
                AutoSize = true,
                Font = new Font("Segoe UI Semibold", 9.5f),
                ForeColor = Color.White,
                BackColor = Color.Transparent
            };
            footer.Controls.Add(lblName);

            var lblUsername = new Label
            {
                Text = "@" + username,
                Location = new Point(avatarPanel.Right + 10, lblName.Bottom + 1),
                AutoSize = true,
                Font = ThemeHelper.FontSmall,
                ForeColor = Color.FromArgb(190, 210, 190),
                BackColor = Color.Transparent
            };
            footer.Controls.Add(lblUsername);

            void GoToUserProfile(object? s, EventArgs e)
            {
                if (!EnsureAdmin()) return;
                AnimateTitleTo("Settings");
                OpenChildForm(new SettingsForm(SettingsForm.InitialSection.Users), btnSettings);
            }

            footer.Click += GoToUserProfile;
            avatarPanel.Click += GoToUserProfile;
            lblName.Click += GoToUserProfile;
            lblUsername.Click += GoToUserProfile;

            panelMenu.Controls.Add(footer);
            panelMenu.Controls.SetChildIndex(footer, 0);
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
            try
            {
                using var db = new InventoryDbContext();
                var user = db.Users.FirstOrDefault(u => u.Id == AuthService.CurrentUserId);
                if (user != null)
                {
                    picturePath = user.PicturePath;
                    if (string.IsNullOrWhiteSpace(fullName)) fullName = user.FullName;
                }
            }
            catch (Exception ex) { Logger.LogError(ex, "DashboardForm.BuildTopProfileWidget"); }

            const int widgetW = 52, avatarSize = 34;

            _pnlUserProfile = new Panel
            {
                Name = "pnlTopProfile",
                Size = new Size(widgetW, panelTop.Height),
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };

            var avatarImage = AvatarHelper.TryLoadUserImage(picturePath);
            _pnlUserProfile.Disposed += (s, e) => avatarImage?.Dispose();

            var avatarPanel = new Panel
            {
                Size = new Size(avatarSize, avatarSize),
                BackColor = Color.Transparent
            };
            avatarPanel.Paint += (s, e) =>
                AvatarHelper.DrawAvatar(e.Graphics, new Rectangle(0, 0, avatarSize - 1, avatarSize - 1), avatarImage);
            _pnlUserProfile.Controls.Add(avatarPanel);

            var lblChevron = new Label
            {
                Text = "▾",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(60, 60, 60),
                AutoSize = false,
                Size = new Size(16, avatarSize),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };
            _pnlUserProfile.Controls.Add(lblChevron);

            void OpenMenu(object? s, EventArgs e) => ToggleProfileDropdown();
            _pnlUserProfile.Click += OpenMenu;
            avatarPanel.Click += OpenMenu;
            lblChevron.Click += OpenMenu;

            panelTop.Controls.Add(_pnlUserProfile);
            _pnlUserProfile.BringToFront();
            PositionTopProfileWidget();

        }

        private void PositionTopProfileWidget()
        {
            if (_pnlUserProfile == null) return;
            _pnlUserProfile.Location = new Point(panelTop.Width - _pnlUserProfile.Width - 14, 0);
            _pnlUserProfile.Height = panelTop.Height;
            foreach (Control c in _pnlUserProfile.Controls)
                c.Top = (_pnlUserProfile.Height - c.Height) / 2;

            // Re-flow avatar/chevron left-to-right within the widget
            int x = 0;
            foreach (Control c in _pnlUserProfile.Controls)
            {
                c.Left = x;
                x += c.Width + 2;
            }
        }

        private void ToggleProfileDropdown()
        {
            if (_profilePopup != null && !_profilePopup.IsDisposed)
            {
                _profilePopup.Close();
                _profilePopup = null;
                return;
            }

            var popup = new Form
            {
                FormBorderStyle = FormBorderStyle.None,
                StartPosition = FormStartPosition.Manual,
                ShowInTaskbar = false,
                Size = new Size(168, 76),   // was 200x88
                BackColor = Color.White
            };

            popup.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using var path = RoundedRect(new Rectangle(0, 0, popup.Width - 1, popup.Height - 1), 10);
                popup.Region = new Region(path);
                using var pen = new Pen(Color.FromArgb(225, 225, 225), 1);
                g.DrawPath(pen, path);
            };

            var screenPoint = _pnlUserProfile!.PointToScreen(new Point(0, _pnlUserProfile.Height));
            popup.Location = new Point(screenPoint.X - popup.Width + _pnlUserProfile.Width, screenPoint.Y + 6);

            Button MakeItem(string text, Action onClick)
            {
                var b = new Button
                {
                    Text = text,
                    Dock = DockStyle.Top,
                    Height = 36,
                    FlatStyle = FlatStyle.Flat,
                    TextAlign = ContentAlignment.MiddleLeft,
                    Padding = new Padding(12, 0, 0, 0),
                    BackColor = Color.White,
                    ForeColor = ThemeHelper.TextPrimary,
                    Font = new Font("Segoe UI", 8.75f),
                    Cursor = Cursors.Hand
                };
                b.FlatAppearance.BorderSize = 0;
                b.FlatAppearance.MouseOverBackColor = Color.FromArgb(245, 246, 245);
                b.Click += (s, e) => { popup.Close(); onClick(); };
                return b;
            }

            var divider = new Panel { Dock = DockStyle.Top, Height = 1, BackColor = Color.FromArgb(235, 235, 235) };

            var btnProfileSettings = MakeItem("⚙  Profile settings", () =>
            {
                if (!EnsureAdmin()) return;
                AnimateTitleTo("Settings");
                OpenChildForm(new SettingsForm(SettingsForm.InitialSection.Users), btnSettings);
            });
            var btnLogoutItem = MakeItem("⏻  Logout", () => btnLogout_Click(this, EventArgs.Empty));

            // Dock.Top stacks in reverse insertion order
            popup.Controls.Add(btnLogoutItem);
            popup.Controls.Add(divider);
            popup.Controls.Add(btnProfileSettings);

            popup.Deactivate += (s, e) => popup.Close();
            popup.FormClosed += (s, e) => { _profilePopup = null; };

            _profilePopup = popup;
            popup.Show(this);
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
                _activitiesAnimProgress = 0f;

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

                // ── Stat cards — all 6 in one row ──────────────────────────
                panelStats.Controls.Clear();
                _statCards.Clear();

                var cardDefs = new[]
                {
    (title:"Total Equipment",    value:totalEquipment.ToString(),    color:Color.FromArgb(94, 53, 177), icon:"🖥", trendValues:GetLast7DayCounts("equipment"),      deltaText:GetWeeklyDeltaText("equipment")),
    (title:"Allocated",          value:_cntAllocated.ToString(),     color:Color.FromArgb(245, 124, 0),   icon:"📋", trendValues:GetLast7DayCounts("allocated"),      deltaText:GetWeeklyDeltaText("allocated")),
    (title:"In Stock",           value:_cntInStore.ToString(),       color:Color.FromArgb(56,  142, 60),  icon:"✅", trendValues:GetLast7DayCounts("in_store"),       deltaText:GetWeeklyDeltaText("in_store")),
    (title:"Under Repair",       value:_cntUnderRepair.ToString(),   color:Color.FromArgb(33,  150, 243), icon:"🔧", trendValues:GetLast7DayCounts("under_repair"),   deltaText:GetWeeklyDeltaText("under_repair")),
    (title:"Currently Overdue",  value:_cntOverdue.ToString(),      color:Color.FromArgb(255, 160,  0),  icon:"⚠️", trendValues:(float[]?)null, deltaText:(string?)null),
    (title:"Decommissioned",     value:_cntDecommission.ToString(),  color:Color.FromArgb(239, 68,  68),  icon:"🚫", trendValues:GetLast7DayCounts("decommissioned"), deltaText:GetWeeklyDeltaText("decommissioned")),
    (title:"Total Employees",    value:_cntEmployees.ToString(),     color:Color.FromArgb(123, 31,  162), icon:"👤", trendValues:(float[]?)null, deltaText:(string?)null),
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
                    var card = ThemeHelper.CreateStatCard(def.title, def.value, def.color, def.icon, def.trendValues, def.deltaText);
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
                float raw = Math.Min(1f, _donutAnimProgress + 0.0015f);
                _donutAnimProgress = raw >= 1f ? 1f : 1f - MathF.Pow(1f - raw, 3f);
                if (_donutAnimProgress > 0.999f) _donutAnimProgress = 1f;
                _panelDonut?.Invalidate();
                allDone = false;
            }

            if (_barAnimProgress < 1f)
            {
                float raw = Math.Min(1f, _barAnimProgress + 0.0015f);
                _barAnimProgress = raw >= 1f ? 1f : 1f - MathF.Pow(1f - raw, 3f);
                if (_barAnimProgress > 0.999f) _barAnimProgress = 1f;
                _panelBar?.Invalidate();
                _panelLine?.Invalidate();
                allDone = false;
            }

            if (_activitiesAnimProgress < 1f && _panelActivities != null)
            {
                _activitiesAnimProgress = Math.Min(1f, _activitiesAnimProgress + 0.05f);
                float ease = 1f - MathF.Pow(1f - _activitiesAnimProgress, 3f);
                int startX = _activitiesTargetBounds.X + 250; // starts 250px to the right, slides left into place
                int curX = (int)(startX + (_activitiesTargetBounds.X - startX) * ease);
                _panelActivities.SetBounds(curX, _activitiesTargetBounds.Y, _activitiesTargetBounds.Width, _activitiesTargetBounds.Height);
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
            if (_panelCharts != null)
            {
                panelContent.Controls.Remove(_panelCharts);
                _panelCharts.Dispose();
                _panelCharts = null;
            }

            _panelCharts = new DoubleBufferedPanel
            {
                Name = "panelCharts",
                BackColor = Color.FromArgb(240, 244, 240),
                Dock = DockStyle.Fill,
                Padding = new Padding(14, 12, 14, 12),
                AutoScroll = false
            };

            panelContent.Controls.Clear();
            panelContent.Controls.Add(_panelCharts);

            _panelTopRow = new Panel { BackColor = Color.Transparent, Dock = DockStyle.None };

            _panelDonut = MakeChartCard("Equipment Status");
            _panelDonut.Paint += PaintDonutChart;

            var lnkViewAllEquipment = new Label
            {
                Text = "›",
                Font = new Font("Segoe UI", 15f, FontStyle.Regular),
                ForeColor = Color.FromArgb(33, 33, 33),
                AutoSize = true,
                Cursor = Cursors.Hand,
                BackColor = Color.Transparent,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            lnkViewAllEquipment.Click += btnEquipment_Click;
            lnkViewAllEquipment.MouseEnter += (s, e) => lnkViewAllEquipment.ForeColor = Color.FromArgb(80, 80, 80);
            lnkViewAllEquipment.MouseLeave += (s, e) => lnkViewAllEquipment.ForeColor = Color.FromArgb(33, 33, 33);
            _panelDonut.Controls.Add(lnkViewAllEquipment);
            lnkViewAllEquipment.Location = new Point(_panelDonut.Width - lnkViewAllEquipment.PreferredWidth - 16, 10);
            lnkViewAllEquipment.BringToFront();

            _panelBar = MakeChartCard("Asset Status Breakdown");
            _panelBar.Paint += PaintBarChart;
            BuildBarPeriodSelector(_panelBar);

            _panelActivities = MakeChartCard("Recent Activities");
            BuildActivitiesList(_panelActivities);

            _panelTopRow.Controls.Add(_panelDonut);
            _panelTopRow.Controls.Add(_panelBar);
            _panelTopRow.Controls.Add(_panelActivities);

            _panelBotRow = new Panel { BackColor = Color.Transparent, Dock = DockStyle.None };

            _panelLine = MakeChartCard("Equipment by Category");
            _panelLine.Paint += PaintCategoryBreakdownChart;

            _panelBotRow.Controls.Add(_panelLine);

            _panelCharts.Controls.Add(_panelBotRow);
            _panelCharts.Controls.Add(_panelTopRow);

            LayoutCharts();
        }

        private void RecomputeBarChartForPeriod()
        {
            if (_barPeriod == "All Time") { _periodOverrideCounts = null; return; }

            DateTime cutoffStart = _barPeriod switch
            {
                "This Month" => new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1),
                "Last Month" => new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(-1),
                "This Year" => new DateTime(DateTime.Today.Year, 1, 1),
                _ => DateTime.MinValue
            };
            DateTime cutoffEnd = _barPeriod == "Last Month"
                ? new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1)
                : DateTime.Today.AddDays(1);

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

            int pad  = 14;
            int gap  = 14;
            int w    = _panelCharts.ClientSize.Width  - pad * 2;
            int h    = _panelCharts.ClientSize.Height - pad * 2;

            // Top row: 32% donut | 38% bar | 30% activities
            int topH = (int)(h * 0.56);
            int donutW = (int)(w * 0.32) - gap;
            int actW = (int)(w * 0.30) - gap;
            int barW = w - donutW - actW - gap * 2;

            if (_panelTopRow != null)
            {
                _panelTopRow.SetBounds(pad, pad, w, topH);

                _panelDonut?.SetBounds(0, 0, donutW, topH);
                _panelBar?.SetBounds(donutW + gap, 0, barW, topH);
                _activitiesTargetBounds = new Rectangle(donutW + barW + gap * 2, 0, actW, topH);
                if (_activitiesAnimProgress >= 1f)
                    _panelActivities?.SetBounds(_activitiesTargetBounds.X, _activitiesTargetBounds.Y, _activitiesTargetBounds.Width, _activitiesTargetBounds.Height);
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

        private void BuildActivitiesList(Panel card)
        {
            var listHost = new Panel
            {
                Location = new Point(0, 45),
                Size = new Size(card.Width, Math.Max(0, card.Height - 45)),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
                AutoScroll = true,
                BackColor = Color.White,
                Tag = "activitiesHost"
            };
            card.Controls.Add(listHost);

            var lnkViewAll = new Label
            {
                Text = "›",
                Font = new Font("Segoe UI", 15f, FontStyle.Regular),
                ForeColor = Color.FromArgb(33, 33, 33),
                AutoSize = true,
                Cursor = Cursors.Hand,
                BackColor = Color.Transparent,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            lnkViewAll.Click += btnLogs_Click;
            lnkViewAll.MouseEnter += (s, e) => lnkViewAll.ForeColor = Color.FromArgb(80, 80, 80);
            lnkViewAll.MouseLeave += (s, e) => lnkViewAll.ForeColor = Color.FromArgb(33, 33, 33);
            lnkViewAll.Location = new Point(card.Width - lnkViewAll.PreferredWidth - 16, 8);
            card.Controls.Add(lnkViewAll);
            lnkViewAll.BringToFront();

            LoadRecentActivities(listHost);
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
                BackColor = Color.FromArgb(243, 244, 246)
            };
            row.Controls.Add(lblIcon);

            var lblMsg = new Label
            {
                Text = message,
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = ThemeHelper.TextPrimary,
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
                ForeColor = Color.FromArgb(156, 163, 175),
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
        private static Panel MakeChartCard(string title)
        {
            var card = new DoubleBufferedPanel { BackColor = Color.White };

            card.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

                using var path = RoundedRect(new Rectangle(0, 0, card.Width - 1, card.Height - 1), 14);
                card.Region = new Region(path);

                using var hBrush = new SolidBrush(Color.FromArgb(248, 250, 248));
                g.FillRectangle(hBrush, 0, 0, card.Width, 44);

                using var tFont = new Font("Segoe UI", 10f, FontStyle.Bold, GraphicsUnit.Point);
                using var tFg = new SolidBrush(Color.FromArgb(27, 46, 26));

                string titleText = card.Tag?.ToString() ?? "";
                const int reservedRight = 130;
                var titleRect = new RectangleF(16, 12, Math.Max(20, card.Width - 16 - reservedRight), 22);
                using var titleSf = new StringFormat { Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap };
                g.DrawString(titleText, tFont, tFg, titleRect, titleSf);

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

            using var nf = new Font("Segoe UI", 16f, FontStyle.Bold, GraphicsUnit.Point);
            using var sf = new Font("Segoe UI", 7.5f, FontStyle.Regular, GraphicsUnit.Point);
            using var fg = new SolidBrush(Color.FromArgb(17, 24, 39));
            using var fg2 = new SolidBrush(Color.FromArgb(107, 114, 128));
            var ns = g.MeasureString(total.ToString(), nf);
            g.DrawString(total.ToString(), nf, fg, new PointF(cx - ns.Width / 2f, cy - ns.Height / 2f - 5));
            var ss = g.MeasureString("Total", sf);
            g.DrawString("Total", sf, fg2, new PointF(cx - ss.Width / 2f, cy + ns.Height / 2f - 6));

            // ── Legend — vertical stack on the right ──
            int lx = (int)(outerR.Right + 24);
            int ly = top + 4;
            int rowH = Math.Max(20, (p.Height - top - 8) / segs.Length);
            using var lf = new Font("Segoe UI", 8.25f, FontStyle.Regular, GraphicsUnit.Point);
            using var lfB = new Font("Segoe UI", 8.25f, FontStyle.Bold, GraphicsUnit.Point);
            foreach (var seg in segs)
            {
                using var db2 = new SolidBrush(seg.Color);
                g.FillEllipse(db2, lx, ly + rowH / 2 - 5, 10, 10);
                using var lc = new SolidBrush(Color.FromArgb(55, 65, 81));
                g.DrawString(seg.Label, lf, lc, lx + 16, ly + rowH / 2 - 14);
                using var vc = new SolidBrush(seg.Color);
                g.DrawString(seg.Count.ToString(), lfB, vc, lx + 16, ly + rowH / 2 + 1);
                ly += rowH;
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

            int top = 54, colGap = 40, padOuter = 16;
            int colWidth = (p.Width - padOuter * 2 - colGap) / 2;

            // Split roughly in half — left gets the first ceil(n/2), right gets the rest.
            int half = (int)Math.Ceiling(_categoryBreakdown.Count / 2.0);
            var leftCats = _categoryBreakdown.Take(half).ToList();
            var rightCats = _categoryBreakdown.Skip(half).ToList();

            var leftBounds = new Rectangle(padOuter, top, colWidth, p.Height - top - 20);
            var rightBounds = new Rectangle(padOuter + colWidth + colGap, top, colWidth, p.Height - top - 20);

            PaintCategoryColumn(g, leftCats, leftBounds, palette, colorOffset: 0);
            PaintCategoryColumn(g, rightCats, rightBounds, palette, colorOffset: half);
        }

        private void PaintCategoryColumn(Graphics g, List<(string Category, int Count)> cats, Rectangle bounds, Color[] palette, int colorOffset)
        {
            if (cats.Count == 0) return;

            int padL = 130; // label column width — tune to your longest category name
            int chartX = bounds.X + padL;
            int chartW = Math.Max(20, bounds.Width - padL - 90); // 90 reserved for count/pct on the right
            int total = Math.Max(1, _categoryBreakdown.Sum(c => c.Count)); // % is relative to the whole dataset, not just this column
            int maxVal = Math.Max(1, _categoryBreakdown.Max(c => c.Count));

            using var labelFont = new Font("Segoe UI", 9f, FontStyle.Regular, GraphicsUnit.Point);
            using var valFont = new Font("Segoe UI", 8.5f, FontStyle.Bold, GraphicsUnit.Point);
            using var labelFg = new SolidBrush(Color.FromArgb(55, 65, 81));
            using var bgBar = new SolidBrush(Color.FromArgb(240, 244, 240));

            float rowH = (float)bounds.Height / cats.Count;
            float barH = Math.Clamp(rowH * 0.50f, 14f, 32f);

            for (int i = 0; i < cats.Count; i++)
            {
                var cat = cats[i];
                Color color = palette[(i + colorOffset) % palette.Length];

                int rowIndexGlobal = colorOffset + i;
                float rowDelay = rowIndexGlobal * 0.08f;
                float rowProgress = Math.Clamp((_barAnimProgress - rowDelay) / (1f - rowDelay), 0f, 1f);
                float verticalOffset = (1f - rowProgress) * 24f;
                float by = bounds.Y + i * rowH + (rowH - barH) / 2f + verticalOffset;

                using var trackPath = RoundedRectF(new RectangleF(chartX, by, chartW, barH), 5f);
                g.FillPath(bgBar, trackPath);

                float fillW = maxVal == 0 ? 0 : Math.Max(barH, (float)cat.Count / maxVal * chartW);
                if (fillW > 0)
                {
                    using var grad = new LinearGradientBrush(
                        new PointF(chartX, by), new PointF(chartX + fillW, by),
                        ControlPaint.Light(color, 0.3f), color);
                    using var fillPath = RoundedRectF(new RectangleF(chartX, by, fillW, barH), 5f);
                    g.FillPath(grad, fillPath);
                }

                var lSz = g.MeasureString(cat.Category, labelFont);
                g.DrawString(cat.Category, labelFont, labelFg, chartX - lSz.Width - 10, by + barH / 2f - lSz.Height / 2f);

                float pct = (float)cat.Count / total * 100f;
                string valStr = $"{cat.Count}  ({pct:0}%)";
                var vSz = g.MeasureString(valStr, valFont);
                using var vFg = new SolidBrush(color);
                g.DrawString(valStr, valFont, vFg, chartX + fillW + 8, by + barH / 2f - vSz.Height / 2f);
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

            panelContent.Controls.Clear();
            panelContent.Controls.Add(childForm);
            childForm.BringToFront();
            childForm.Show();

            panelStats.Visible = false;
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
            AnimateTitleTo("Dashboard");
            LoadStats();
        }

        private void btnDashboard_Click(object? sender, EventArgs e) => ShowDashboard();

        private void OpenAllocationAvailable()
        {
            AnimateTitleTo("Allocation");
            OpenChildForm(new AllocationForm(true), btnAllocation);
        }

        private void OpenEquipmentByStatus(string status)
        {
            AnimateTitleTo("Equipment");
            var frm = new EquipmentForm();
            OpenChildForm(frm, btnEquipment);
            frm.ApplyStatusFilter(status);
        }

        private void OpenOverdueEquipment()
        {
            AnimateTitleTo("Equipment");
            var frm = new EquipmentForm();
            OpenChildForm(frm, btnEquipment);

            var overdueIds = new HashSet<int>();
            try
            {
                using var db = new InventoryDbContext();
                var today = DateOnly.FromDateTime(DateTime.Today);
                overdueIds = db.Allocations
                    .Where(a => !a.Equipment.IsDeleted &&
                                a.Equipment.CurrentStatus == "allocated" &&
                                a.Status == "approved" &&
                                a.ExpectedReturnDate.HasValue &&
                                a.ExpectedReturnDate.Value < today &&
                                a.ActualReturnDate == null)
                    .Select(a => a.EquipmentId)
                    .Distinct()
                    .ToHashSet();
            }
            catch { /* leaves overdueIds empty — grid will just show nothing */ }

            frm.ApplyCustomFilter(eq => overdueIds.Contains(eq.Id));
        }

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

        public void NavigateToEmployee(string employeeCode)
        {
            AnimateTitleTo("Employees");
            OpenChildForm(new EmployeesForm(employeeCode), btnEmployees);
        }


        public void NavigateToLogs(string equipmentCode)
        {
            AnimateTitleTo("Logs");
            OpenChildForm(new LogsForm(equipmentCode), btnLogs);
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


