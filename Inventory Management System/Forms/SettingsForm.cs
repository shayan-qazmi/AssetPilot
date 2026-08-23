// Forms/SettingsForm.cs
using Inventory_Management_System.Helpers;
using Inventory_Management_System.Models;
using Inventory_Management_System.Services;
using QRCoder;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace Inventory_Management_System.Forms
{
    public partial class SettingsForm : Form, IThemableForm
    {
        private AppSetting? _current;
        private List<User> _users = new();
        private List<Role> _roles = new();
        private List<Category> _categories = new();

        // ── Section panels ─────────────────────────────────────────────
        private Panel _panelGeneral = null!;
        private Panel _panelUsers = null!;
        private Panel _panelPassword = null!;
        private Panel _panelPrinting = null!;
        private Panel _panelCategories = null!;
        private Panel _panelSystemConfig = null!;
        private Button _activeNavBtn = null!;
        private Panel _pwdStrengthTrack = null!;
        // ── Sidebar (distinct styling + slide-in animation) ─────────────
        private Panel _sidebar = null!;
        private System.Windows.Forms.Timer? _sidebarSlideTimer;
        private const int SidebarTargetWidth = 220;

        // ─────────────────────────────────────────────────────────────
        // Modern Fluent-style palette
        // NOTE: SettingsSidebarText is left exactly as-is (already fixed) —
        // everything else (backgrounds, shadows, dividers, page bg) is new.
        // ─────────────────────────────────────────────────────────────

        private static readonly Color CardShadowColor = Color.FromArgb(22, 0, 0, 0);

        private static Color SettingsSidebarColor => ThemeHelper.CardBg;
        private static Color SettingsSidebarHover => ThemeHelper.IsDarkMode ? Color.FromArgb(50, 54, 62) : Color.FromArgb(244, 246, 248);
        private static Color SettingsSidebarText => ThemeHelper.TextPrimary;
        private static Color SettingsSidebarActive => ThemeHelper.Accent;
        private static Color SidebarDivider => ThemeHelper.ChartDivider;
        private static Color PageBackground => ThemeHelper.DashboardBg;
        private static Color SubtitleColor => ThemeHelper.TextSecondary;

        // ── User grid + buttons ────────────────────────────────────────
        private DataGridView dgvUsers = null!;
        private Button btnAddUser = null!;
        private Button btnEditUser = null!;
        private Button btnToggleUser = null!;
        private Button btnDeleteUser = null!;

        // ── Password fields ────────────────────────────────────────────
        private TextBox txtCurrentPwd = null!;
        private TextBox txtNewPwd = null!;
        private TextBox txtConfirmPwd = null!;
        private Label lblPwdStatus = null!;
        private Panel _pwdStrengthBar = null!;
        private Label _lblPwdStrengthText = null!;

        // ── Categories fields ────────────────────────────────────────────
        private TextBox txtNewCategory = null!;
        private ListBox lstCategories = null!;
        private Label lblCategoryMsg = null!;

        // ── General / dirty-check fields ────────────────────────────────
        private string _originalCompanyName = "";
        private Label _lblSavedPill = null!;

        // ── System info fields ───────────────────────────────────────────
        private Label _lblDbValue = null!;
        private Label _lblLastBackupValue = null!;
        private DateTime? _lastBackupTime;

        // ── System Configuration (DB connection) fields ──────────────────
        private Label _lblConnStatus = null!;
        private Label _lblConnServer = null!;
        private Label _lblConnLast = null!;

        private static readonly string BackupInfoPath = Path.Combine(
      Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
      "QInventory", "lastbackup.txt");

        private static readonly string MysqlToolsConfigPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "QInventory", "mysqltools.txt");

        private string? _mysqldumpPath;
        private string? _mysqlPath;

        public enum InitialSection { General, Users, Password, Printing, Categories, SystemConfiguration }

        private readonly InitialSection _initialSection;

        public SettingsForm() : this(InitialSection.General) { }

        public SettingsForm(InitialSection initialSection)
        {
            _initialSection = initialSection;
            InitializeComponent();
            LoadLastBackupTime();
            BuildUI();
            LoadSettings();
            LoadUsers();
            LoadCategories();

            Load += (s, e) => AnimateSidebarIn();
        }

        // Rebuilds the whole form fresh — simplest reliable fix since every
        // control here is created in code (BuildUI) with colors baked in at
        // creation time, rather than read live from ThemeHelper on each paint.
        public void ApplyTheme()
        {
            Controls.Clear();
            BuildUI();
            LoadSettings();
            LoadUsers();
            LoadCategories();
            _sidebar.Width = SidebarTargetWidth; // skip the slide-in animation on a re-theme
        }
            
        // ══════════════════════════════════════════════════════════════
        //  BUILD UI
        // ══════════════════════════════════════════════════════════════
        private void BuildUI()
        {
            BackColor = PageBackground;

            // ── Left nav sidebar — now white, matching every card ───────
            var sidebar = new Panel
            {
                Dock = DockStyle.Left,
                Width = 0,                     // animated in on Load
                BackColor = SettingsSidebarColor
            };
            _sidebar = sidebar;

            // Subtle 1px divider so the white sidebar still reads as a
            // distinct panel against the white cards in the content area.
            sidebar.Paint += (s, e) =>
            {
                using var pen = new Pen(SidebarDivider, 1f);
                e.Graphics.DrawLine(pen, sidebar.Width - 1, 0, sidebar.Width - 1, sidebar.Height);
            };

            var sideTitle = new Label
            {
                Text = "⚙  SETTINGS",
                Dock = DockStyle.Top,
                Height = 50,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(18, 0, 0, 0),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(74, 85, 104), // muted, not white like an active item
                BackColor = SettingsSidebarColor            // same as the sidebar background, not Header green
            };
            sideTitle.Paint += (s, e) =>
            {
                using var divider = new Pen(SidebarDivider, 1f);
                e.Graphics.DrawLine(divider, 0, sideTitle.Height - 1, sideTitle.Width, sideTitle.Height - 1);
            };
            // Added to the sidebar AFTER the nav buttons below (see comment there) —
            // this is what pins it to the TOP instead of it rendering as a fake
            // 6th nav item under Categories.

            // ── Content area ───────────────────────────────────────────
            var content = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = PageBackground
            };

            // Build all section panels
            _panelGeneral = BuildGeneralPanel();
            _panelUsers = BuildUsersPanel();
            _panelPassword = BuildPasswordPanel();
            _panelPrinting = BuildPrintingPanel();
            _panelCategories = BuildCategoriesPanel();
            _panelSystemConfig = BuildSystemConfigPanel();

            foreach (var p in new[] { _panelGeneral, _panelUsers, _panelPassword, _panelPrinting, _panelCategories, _panelSystemConfig })
            {
                p.Dock = DockStyle.Fill;
                p.Visible = false;
                content.Controls.Add(p);
            }

            // Nav button definitions — System Configuration is Admin-only.
            var navDefsList = new List<(string, Panel)>
            {
                ("⊞  General",         _panelGeneral),
                ("👥  Users",           _panelUsers),
                ("🔒  Change Password", _panelPassword),
                ("🖨  Printing",        _panelPrinting),
                ("🏷  Categories",      _panelCategories),
            };

            if (AuthService.IsAdmin)
                navDefsList.Add(("🔧  System Configuration", _panelSystemConfig));

            var navDefs = navDefsList.ToArray();

            var navBtns = new List<Button>();
            foreach (var (label, panel) in navDefs)
            {
                var capturedPanel = panel;
                var btn = new Button
                {
                    Text = label,
                    Dock = DockStyle.Top,
                    Height = 52,
                    FlatStyle = FlatStyle.Flat,
                    TextAlign = ContentAlignment.MiddleLeft,
                    Padding = new Padding(22, 0, 0, 0),
                    Font = new Font("Segoe UI", 9.25f),
                    ForeColor = SettingsSidebarText,
                    BackColor = SettingsSidebarColor,
                    Cursor = Cursors.Hand,
                    UseVisualStyleBackColor = false
                };
                btn.FlatAppearance.BorderSize = 0;
                btn.FlatAppearance.MouseOverBackColor = SettingsSidebarColor; // handled manually below instead

                var capturedBtn = btn;
                btn.Click += (s, e) => ShowSection(capturedPanel, capturedBtn, navBtns);

                btn.MouseEnter += (s, e) =>
                {
                    if (btn.Tag as string != "active")
                    {
                        ApplyNavPillRegion(btn);
                        FadeNavButtonColor(btn, btn.BackColor, SettingsSidebarHover);
                    }
                };
                btn.MouseLeave += (s, e) =>
                {
                    if (btn.Tag as string != "active")
                    {
                        FadeNavButtonColor(btn, btn.BackColor, SettingsSidebarColor);
                        btn.Region = null;
                    }
                };
                btn.Resize += (s, e) => { if (btn.Tag as string == "active") ApplyNavPillRegion(btn); };

                navBtns.Add(btn);
            }

            // Add nav buttons in reverse for DockStyle.Top stacking
            for (int i = navBtns.Count - 1; i >= 0; i--)
                sidebar.Controls.Add(navBtns[i]);

            // Add the title LAST so it docks FIRST (top of the stack).
            sidebar.Controls.Add(sideTitle);

            Controls.Add(content);
            Controls.Add(sidebar);





            // Activate the requested section (General by default, or
            // whatever the caller asked for — e.g. jumping straight to
            // Users from the sidebar profile footer on the dashboard).
            int initialIndex = _initialSection switch
            {
                InitialSection.Users => 1,
                InitialSection.Password => 2,
                InitialSection.Printing => 3,
                InitialSection.Categories => 4,
                InitialSection.SystemConfiguration => 5,
                _ => 0
            };
            // Guard: non-admins have no 6th nav button, so clamp to the last
            // one actually built (avoids an out-of-range click if a caller
            // somehow requests SystemConfiguration for a non-admin session).
            if (initialIndex >= navBtns.Count) initialIndex = 0;

            var allPanelsInOrder = new[]
                { _panelGeneral, _panelUsers, _panelPassword, _panelPrinting, _panelCategories, _panelSystemConfig };
            var initialPanel = allPanelsInOrder[initialIndex == 5 && navBtns.Count > 5 ? 5 : initialIndex];
            _activeNavBtn = navBtns[initialIndex];
            ShowSection(initialPanel, navBtns[initialIndex], navBtns);
        }

        private static void ApplyNavPillRegion(Button btn)
        {
            if (btn.Width <= 0 || btn.Height <= 0) return;
            btn.Parent?.Invalidate(btn.Bounds, true);

            var rect = new Rectangle(10, 4, btn.Width - 20, btn.Height - 8);
            using var path = new GraphicsPath();
            int radius = 14;
            path.AddArc(rect.X, rect.Y, radius, radius, 180, 90);
            path.AddArc(rect.Right - radius, rect.Y, radius, radius, 270, 90);
            path.AddArc(rect.Right - radius, rect.Bottom - radius, radius, radius, 0, 90);
            path.AddArc(rect.X, rect.Bottom - radius, radius, radius, 90, 90);
            path.CloseFigure();
            btn.Region = new Region(path);
            btn.Invalidate();
        }

        private static readonly Dictionary<Button, System.Windows.Forms.Timer> _navFadeTimers = new();

        private static void FadeNavButtonColor(Button btn, Color from, Color to)
        {
            if (_navFadeTimers.TryGetValue(btn, out var existingTimer))
            {
                existingTimer.Stop();
                existingTimer.Dispose();
                _navFadeTimers.Remove(btn);
            }

            const int steps = 8;
            int step = 0;
            var timer = new System.Windows.Forms.Timer { Interval = 12 };
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
                    _navFadeTimers.Remove(btn);
                }
            };
            _navFadeTimers[btn] = timer;
            timer.Start();
        }


        private void ShowSection(Panel panel, Button btn, List<Button> allBtns)
        {
            foreach (var p in new[] { _panelGeneral, _panelUsers, _panelPassword, _panelPrinting, _panelCategories, _panelSystemConfig })
                p.Visible = false;
            panel.Visible = true;

            foreach (var b in allBtns)
            {
                b.BackColor = SettingsSidebarColor;
                b.ForeColor = SettingsSidebarText;
                b.Font = new Font("Segoe UI", 9.25f);
                b.Tag = null;
                b.Region = null;
                // Native hover overlay must match each button's actual resting
                // color, or hovering the active item flashes it back to the
                // inactive background — same issue fixed earlier on the main sidebar.
                b.FlatAppearance.MouseOverBackColor = SettingsSidebarColor;
            }
            btn.BackColor = SettingsSidebarActive;
            btn.ForeColor = Color.White;
            btn.Font = new Font("Segoe UI Semibold", 9.5f);
            btn.Tag = "active";
            btn.FlatAppearance.MouseOverBackColor = SettingsSidebarActive;
            ApplyNavPillRegion(btn);
            _activeNavBtn = btn;

            if (panel == _panelUsers) LoadUsers();
            if (panel == _panelCategories) LoadCategories();


        }

        // ══════════════════════════════════════════════════════════════
        //  SIDEBAR SLIDE-IN ANIMATION
        // ══════════════════════════════════════════════════════════════
        private void AnimateSidebarIn()
        {
            const int step = 14;
            _sidebarSlideTimer = new System.Windows.Forms.Timer { Interval = 10 };
            _sidebarSlideTimer.Tick += (s, e) =>
            {
                int newWidth = _sidebar.Width + step;
                if (newWidth >= SidebarTargetWidth)
                {
                    _sidebar.Width = SidebarTargetWidth;
                    _sidebarSlideTimer?.Stop();
                    _sidebarSlideTimer?.Dispose();
                    _sidebarSlideTimer = null;
                }
                else
                {
                    _sidebar.Width = newWidth;
                }
                _sidebar.Invalidate(true);   // forces a full repaint each frame so the old divider line is erased before the new one is drawn

            };
            _sidebarSlideTimer.Start();
        }

        // ══════════════════════════════════════════════════════════════
        //  GENERAL PANEL  — Company (dirty-check) + System info + Danger zone
        // ══════════════════════════════════════════════════════════════
        private Panel BuildGeneralPanel()
        {
            var outer = new Panel
            {
                BackColor = PageBackground,
                AutoScroll = true
            };

            var (title, subtitle, nextY) = AddSectionHeader(outer, "General Settings",
         "Manage company information and application preferences.");
            int headerDelta = nextY - 96;

            // ═══ Card 1: Company ═══
            var card = new Panel
            {
                Location = new Point(40, nextY),
                Size = new Size(560, 250),
                BackColor = ThemeHelper.CardBg,
                Padding = new Padding(0)
            };
            ApplyCardStyle(outer, card);
            outer.Controls.Add(card);

            var cardTitle = new Label
            {
                Text = "Company Information",
                Font = new Font("Segoe UI Semibold", 11f),
                ForeColor = ThemeHelper.TextPrimary,
                AutoSize = true,
                Location = new Point(24, 20),
                BackColor = Color.Transparent
            };
            card.Controls.Add(cardTitle);

            var lblCo = new Label
            {
                Text = "Company Name",
                Font = ThemeHelper.FontBold,
                ForeColor = ThemeHelper.TextPrimary,
                AutoSize = true,
                Location = new Point(24, 56),
                BackColor = Color.Transparent
            };
            card.Controls.Add(lblCo);

            txtCompany = MakeTextBox(card, 24, 80, 490);
            txtCompany.TextChanged += (s, e) => UpdateSaveButtonState();

            var lblHint = new Label
            {
                Text = "Appears on printed labels, reports, and exported files.",
                Font = new Font("Segoe UI", 9f),
                ForeColor = SubtitleColor,
                AutoSize = true,
                Location = new Point(24, 128),
                BackColor = Color.Transparent
            };
            card.Controls.Add(lblHint);

            btnSave = new Button
            {
                Text = "💾  Save changes",
                Location = new Point(24, 164),
                Size = new Size(160, 38)
            };
            StyleGreenBtn(btnSave);
            btnSave.Click += btnSave_Click;
            card.Controls.Add(btnSave);

            btnReset = new Button
            {
                Text = "↺  Reset",
                Location = new Point(196, 164),
                Size = new Size(120, 38)
            };
            StyleGrayBtn(btnReset);
            btnReset.Click += btnReset_Click;
            card.Controls.Add(btnReset);

            _lblSavedPill = new Label
            {
                Text = "✓ Saved",
                Font = ThemeHelper.FontBold,
                ForeColor = ThemeHelper.Success,
                AutoSize = true,
                Location = new Point(330, 174),
                BackColor = Color.Transparent,
                Visible = false
            };
            card.Controls.Add(_lblSavedPill);

            // ═══ Card 2: System ═══
            var sysCard = new Panel
            {
                Location = new Point(40, 364 + headerDelta),
                Size = new Size(560, 208),
                BackColor = ThemeHelper.CardBg
            };
            ApplyCardStyle(outer, sysCard);
            outer.Controls.Add(sysCard);

            var lblSysTitle = new Label
            {
                Text = "System",
                Font = new Font("Segoe UI Semibold", 11f),
                ForeColor = ThemeHelper.TextPrimary,
                AutoSize = true,
                Location = new Point(24, 20),
                BackColor = Color.Transparent
            };
            sysCard.Controls.Add(lblSysTitle);

            var lblSysSubtitle = MakeSubtitle("Application version, database connection, and backups.", 24, 44);
            sysCard.Controls.Add(lblSysSubtitle);

            string appVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.0.0";

            sysCard.Controls.Add(MakeSysInfoLabel("App version", appVersion, 24, 66));

            _lblDbValue = MakeSysInfoValueLabel("", 220, 66, 300);
            sysCard.Controls.Add(MakeSysInfoLabel("Database", "", 220, 66, hideValue: true));
            sysCard.Controls.Add(_lblDbValue);

            _lblLastBackupValue = MakeSysInfoValueLabel(FormatLastBackup(), 24, 106, 180);
            sysCard.Controls.Add(MakeSysInfoLabel("Last backup", "", 24, 106, hideValue: true));
            sysCard.Controls.Add(_lblLastBackupValue);


            var divider = new Panel
            {
                Location = new Point(24, 142),
                Size = new Size(512, 1),
                BackColor = SidebarDivider
            };
            sysCard.Controls.Add(divider);

            var btnBackupDb = new Button
            {
                Text = "🗄  Backup database",
                Location = new Point(24, 158),
                Size = new Size(190, 38)
            };
            StyleGrayBtn(btnBackupDb);
            btnBackupDb.Click += BtnBackupDb_Click;
            sysCard.Controls.Add(btnBackupDb);

            var btnRestoreDb = new Button
            {
                Text = "⇧  Restore from file",
                Location = new Point(224, 158),
                Size = new Size(190, 38)
            };
            StyleGrayBtn(btnRestoreDb);
            btnRestoreDb.Click += BtnRestoreDb_Click;
            sysCard.Controls.Add(btnRestoreDb);

            // ═══ Card 3: Danger zone ═══
            var dangerCard = new Panel
            {
                Location = new Point(40, 588 + headerDelta),
                Size = new Size(560, 156),
                BackColor = ThemeHelper.IsDarkMode ? Color.FromArgb(45, 30, 30) : Color.FromArgb(255, 248, 248)
            };
            ApplyCardStyle(outer, dangerCard, borderColor: Color.FromArgb(252, 165, 165));
            outer.Controls.Add(dangerCard);

            var lblDangerTitle = new Label
            {
                Text = "⚠  Danger Zone",
                Font = new Font("Segoe UI Semibold", 11f),
                ForeColor = ThemeHelper.Danger,
                AutoSize = true,
                Location = new Point(24, 20),
                BackColor = Color.Transparent
            };
            dangerCard.Controls.Add(lblDangerTitle);

            var lblDangerHint = new Label
            {
                Text = "These actions are destructive and cannot be undone. Proceed carefully.",
                Font = new Font("Segoe UI", 9f),
                ForeColor = SubtitleColor,
                AutoSize = true,
                Location = new Point(24, 50),
                BackColor = Color.Transparent
            };
            dangerCard.Controls.Add(lblDangerHint);

            var btnClearLogs = new Button
            {
                Text = "🗑  Clear all logs",
                Location = new Point(24, 90),
                Size = new Size(160, 38)
            };
            StyleDangerOutlineBtn(btnClearLogs);
            btnClearLogs.Click += BtnClearLogs_Click;
            dangerCard.Controls.Add(btnClearLogs);

            var btnResetData = new Button
            {
                Text = "⚠  Reset application data",
                Location = new Point(196, 90),
                Size = new Size(220, 38)
            };
            StyleDangerOutlineBtn(btnResetData);
            btnResetData.Click += BtnResetAppData_Click;
            dangerCard.Controls.Add(btnResetData);

            return outer;
        }

        private static Label MakeSysInfoLabel(string caption, string value, int x, int y, int width = 180, bool hideValue = false)
        {
            var lbl = new Label
            {
                Text = caption,
                Font = ThemeHelper.FontSmall,
                ForeColor = ThemeHelper.TextSecondary,
                AutoSize = true,
                Location = new Point(x, y),
                BackColor = Color.Transparent
            };
            if (!hideValue)
            {
                var container = new Panel { Location = new Point(x, y), Size = new Size(width, 34), BackColor = Color.Transparent };
                lbl.Location = new Point(0, 0);
                container.Controls.Add(lbl);
                var valLbl = new Label
                {
                    Text = value,
                    Font = ThemeHelper.FontDefault,
                    ForeColor = ThemeHelper.TextPrimary,
                    AutoSize = true,
                    Location = new Point(0, 16),
                    BackColor = Color.Transparent
                };
                container.Controls.Add(valLbl);
                return CreateWrapperLabel(container);
            }
            return lbl;
        }

        // Small helper: some sysinfo rows need a caption label + separate updatable value label
        private static Label CreateWrapperLabel(Panel container)
        {
            // Returning a zero-size invisible label just so callers can add "container" via
            // sysCard.Controls.Add — simplified by directly adding container instead below.
            var placeholder = new Label { Visible = false, Size = Size.Empty };
            container.Tag = placeholder;
            return placeholder;
        }

        private static Label MakeSysInfoValueLabel(string value, int x, int y, int width)
        {
            return new Label
            {
                Text = value,
                Font = ThemeHelper.FontDefault,
                ForeColor = ThemeHelper.TextPrimary,
                AutoSize = false,
                Size = new Size(width, 18),
                Location = new Point(x, y + 16),
                BackColor = Color.Transparent
            };
        }

        private void UpdateSaveButtonState()
        {
            bool dirty = txtCompany.Text.Trim() != _originalCompanyName;
            btnSave.Enabled = dirty;
            if (dirty) _lblSavedPill.Visible = false;
        }

        private string FormatLastBackup() =>
            _lastBackupTime.HasValue ? _lastBackupTime.Value.ToString("MMM dd, yyyy · h:mm tt") : "Never";

        private void LoadLastBackupTime()
        {
            try
            {
                if (File.Exists(BackupInfoPath))
                {
                    var text = File.ReadAllText(BackupInfoPath);
                    if (DateTime.TryParse(text, out var dt)) _lastBackupTime = dt;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "SettingsForm.LoadLastBackupTime");
            }
        }

        private void SaveLastBackupTimeNow()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(BackupInfoPath)!);
                File.WriteAllText(BackupInfoPath, DateTime.Now.ToString("o"));
                _lastBackupTime = DateTime.Now;
                if (_lblLastBackupValue != null)
                    _lblLastBackupValue.Text = FormatLastBackup();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "SettingsForm.SaveLastBackupTimeNow");
            }
        }

        // ══════════════════════════════════════════════════════════════
        //  BACKUP / RESTORE  (MySQL via mysqldump / mysql CLI)
        // ══════════════════════════════════════════════════════════════
        private string? LocateMySqlTool(string exeName)
        {
            if (exeName == "mysqldump.exe" && _mysqldumpPath != null && File.Exists(_mysqldumpPath))
                return _mysqldumpPath;
            if (exeName == "mysql.exe" && _mysqlPath != null && File.Exists(_mysqlPath))
                return _mysqlPath;

            try
            {
                if (File.Exists(MysqlToolsConfigPath))
                {
                    foreach (var line in File.ReadAllLines(MysqlToolsConfigPath))
                    {
                        if (line.EndsWith(exeName, StringComparison.OrdinalIgnoreCase) && File.Exists(line))
                        {
                            CacheToolPath(exeName, line);
                            return line;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "SettingsForm.LocateMySqlTool (Config read)");
            }

            string[] commonDirs =
            {
                @"C:\xampp\mysql\bin",
                @"C:\Program Files\MySQL\MySQL Server 8.0\bin",
                @"C:\Program Files\MySQL\MySQL Server 8.4\bin",
                @"C:\wamp64\bin\mysql\mysql8.0.31\bin",
            };
            foreach (var dir in commonDirs)
            {
                var candidate = Path.Combine(dir, exeName);
                if (File.Exists(candidate))
                {
                    CacheToolPath(exeName, candidate);
                    return candidate;
                }
            }

            using var ofd = new OpenFileDialog
            {
                Title = $"Locate {exeName}",
                Filter = "Executable|*.exe"
            };
            if (ofd.ShowDialog() == DialogResult.OK && Path.GetFileName(ofd.FileName)
                    .Equals(exeName, StringComparison.OrdinalIgnoreCase))
            {
                CacheToolPath(exeName, ofd.FileName);
                return ofd.FileName;
            }

            return null;
        }

        private void CacheToolPath(string exeName, string fullPath)
        {
            if (exeName == "mysqldump.exe") _mysqldumpPath = fullPath;
            if (exeName == "mysql.exe") _mysqlPath = fullPath;

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(MysqlToolsConfigPath)!);
                var lines = File.Exists(MysqlToolsConfigPath)
                    ? File.ReadAllLines(MysqlToolsConfigPath).Where(l => !l.EndsWith(exeName, StringComparison.OrdinalIgnoreCase)).ToList()
                    : new List<string>();
                lines.Add(fullPath);
                File.WriteAllLines(MysqlToolsConfigPath, lines);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "SettingsForm.CacheToolPath");
            }
        }
        private (string host, string db, string user, string pass)? PromptDbCredentials()
        {
            using var frm = new Form
            {
                Text = "Database Credentials",
                Size = new Size(360, 300),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                StartPosition = FormStartPosition.CenterParent,
                BackColor = ThemeHelper.CardBg
            };

            int y = 20;
            var txtHost = MakeCredField(frm, "Host", _current?.DbServer ?? "localhost", ref y);
            var txtDb = MakeCredField(frm, "Database", _current?.DbName ?? "inventory_management", ref y);
            var txtUser = MakeCredField(frm, "Username", "root", ref y);
            var txtPass = MakeCredField(frm, "Password", "", ref y);
            txtPass.UseSystemPasswordChar = true;

            var btnOk = new Button
            {
                Text = "Continue",
                Location = new Point(60, y + 10),
                Size = new Size(110, 32)
            };
            StyleGreenBtn(btnOk);
            btnOk.Click += (s, e) => frm.DialogResult = DialogResult.OK;
            frm.Controls.Add(btnOk);

            var btnCancel = new Button
            {
                Text = "Cancel",
                Location = new Point(180, y + 10),
                Size = new Size(90, 32)
            };
            StyleGrayBtn(btnCancel);
            btnCancel.Click += (s, e) => frm.DialogResult = DialogResult.Cancel;
            frm.Controls.Add(btnCancel);

            frm.AcceptButton = btnOk;
            frm.CancelButton = btnCancel;
            frm.ClientSize = new Size(340, y + 60);

            if (frm.ShowDialog() != DialogResult.OK) return null;
            return (txtHost.Text.Trim(), txtDb.Text.Trim(), txtUser.Text.Trim(), txtPass.Text);
        }

        private static TextBox MakeCredField(Form frm, string label, string defaultVal, ref int y)
        {
            frm.Controls.Add(new Label
            {
                Text = label,
                Location = new Point(20, y),
                AutoSize = true,
                Font = ThemeHelper.FontBold,
                ForeColor = ThemeHelper.TextPrimary
            });
            y += 20;
            var txt = new TextBox { Location = new Point(20, y), Size = new Size(300, 26), Text = defaultVal };
            ThemeHelper.StyleTextBox(txt);
            frm.Controls.Add(txt);
            y += 36;
            return txt;
        }

        private void BtnBackupDb_Click(object? sender, EventArgs e)
        {
            using var sfd = new SaveFileDialog
            {
                Filter = "SQL Files|*.sql",
                Title = "Save Database Backup",
                FileName = $"qinventory_backup_{DateTime.Now:yyyyMMdd_HHmm}.sql"
            };
            if (sfd.ShowDialog() != DialogResult.OK) return;

            var creds = PromptDbCredentials();
            if (creds == null) return;
            var (host, db, user, pass) = creds.Value;

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = LocateMySqlTool("mysqldump.exe") ?? "mysqldump",
                    Arguments = $"-h {host} -u {user}{(string.IsNullOrEmpty(pass) ? "" : $" -p{pass}")} {db}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var proc = Process.Start(psi)!;
                string output = proc.StandardOutput.ReadToEnd();
                string errors = proc.StandardError.ReadToEnd();
                proc.WaitForExit();

                if (proc.ExitCode == 0 && output.Length > 0)
                {
                    File.WriteAllText(sfd.FileName, output);
                    SaveLastBackupTimeNow();
                    ToastHelper.Show(this, "Backup completed successfully.", ToastType.Success);

                }
                else
                {
                    ToastHelper.Show(this, "mysqldump did not complete successfully:\n\n" + errors, ToastType.Error);

                }
            }
            catch (Exception ex)
            {
                ToastHelper.Show(this, "Backup failed: " + ex.Message + "\n\nMake sure mysqldump.exe is installed and available on your system PATH (it ships with MySQL Server / XAMPP under the bin folder).", ToastType.Error);

            }
        }

        private void BtnRestoreDb_Click(object? sender, EventArgs e)
        {
            using var ofd = new OpenFileDialog { Filter = "SQL Files|*.sql", Title = "Select Backup File" };
            if (ofd.ShowDialog() != DialogResult.OK) return;

            if (MessageBox.Show(
                "Restoring will OVERWRITE existing data in the target database with the contents " +
                "of this backup file. This cannot be undone. Continue?",
                "Confirm Restore", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                return;

            var creds = PromptDbCredentials();
            if (creds == null) return;
            var (host, db, user, pass) = creds.Value;

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = LocateMySqlTool("mysql.exe") ?? "mysql",
                    Arguments = $"-h {host} -u {user}{(string.IsNullOrEmpty(pass) ? "" : $" -p{pass}")} {db}",
                    RedirectStandardInput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var proc = Process.Start(psi)!;
                string sqlText = File.ReadAllText(ofd.FileName);
                proc.StandardInput.Write(sqlText);
                proc.StandardInput.Close();
                string errors = proc.StandardError.ReadToEnd();
                proc.WaitForExit();

                if (proc.ExitCode == 0)
                {
                    ToastHelper.Show(this, "Database restored successfully. Please restart the application.", ToastType.Success);

                }
                else
                {
                    ToastHelper.Show(this, "Restore did not complete successfully:\n\n" + errors, ToastType.Error);

                }
            }
            catch (Exception ex)
            {
                ToastHelper.Show(this, "Restore failed: " + ex.Message + "\n\nMake sure mysql.exe is installed and available on your system PATH.", ToastType.Error);

            }
        }

        // ══════════════════════════════════════════════════════════════
        //  DANGER ZONE
        // ══════════════════════════════════════════════════════════════
        private void BtnClearLogs_Click(object? sender, EventArgs e)
        {
            if (MessageBox.Show(
                "This will permanently delete ALL entries in the activity log. This cannot be undone.\n\n" +
                "Note: this action will be recorded in a separate, non-deletable security audit log (who did it, when).",
                "Confirm Clear Logs", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                return;

            try
            {
                using var db = new InventoryDbContext();
                var logs = db.EquipmentLogs.ToList();
                db.EquipmentLogs.RemoveRange(logs);
                db.SaveChanges();

                SecurityAuditLogger.Log("Cleared all activity logs", $"{logs.Count} entries deleted");
                ToastHelper.Show(this, $"{logs.Count} log entries deleted.", ToastType.Success);
            }
            catch (Exception ex)
            {
                ToastHelper.Show(this, "Could not clear logs: " + ex.Message, ToastType.Error);
            }
        }

        private void BtnResetAppData_Click(object? sender, EventArgs e)
        {
            if (!ShowTypedConfirm(
      "This will permanently delete ALL operational data:\n" +
      "Equipment, Allocations, Returns, Transfers, Employees,\n" +
      "Locations, Logs, Check-In/Out history, Decommissioned &\n" +
      "Deleted items, and Maintenance records.\n\n" +
      "Users, roles, categories, and settings will NOT be affected.\n\n" +
      "This action will be recorded in a separate, non-deletable\n" +
      "security audit log (who performed it and when).\n\n" +
      "Type RESET below to confirm.", "RESET"))
                return;

            var errors = new List<string>();
            var counts = new Dictionary<string, int>();

            try
            {
                using var db = new InventoryDbContext();

                // ── 1. Equipment Logs (FK → Equipment, User) ──────────────────
                try
                {
                    var rows = db.EquipmentLogs.ToList();
                    db.EquipmentLogs.RemoveRange(rows);
                    db.SaveChanges();
                    counts["Logs"] = rows.Count;
                }
                catch (Exception ex) { errors.Add("Equipment Logs: " + ex.Message); }

                // ── 2. Equipment Location Histories (FK → Equipment, Location) ──
                try
                {
                    var rows = db.EquipmentLocationHistories.ToList();
                    db.EquipmentLocationHistories.RemoveRange(rows);
                    db.SaveChanges();
                    counts["Location Histories"] = rows.Count;
                }
                catch (Exception ex) { errors.Add("Location Histories: " + ex.Message); }

                // ── 3. Maintenance Records (FK → Equipment) ───────────────────
                try
                {
                    var rows = db.MaintenanceRecords.ToList();
                    db.MaintenanceRecords.RemoveRange(rows);
                    db.SaveChanges();
                    counts["Maintenance Records"] = rows.Count;
                }
                catch (Exception ex) { errors.Add("Maintenance Records: " + ex.Message); }

                // ── 4. Transactions / Check-In-Out history (FK → Equipment) ──
                try
                {
                    var rows = db.Transactions.ToList();
                    db.Transactions.RemoveRange(rows);
                    db.SaveChanges();
                    counts["Transactions (Check-In/Out)"] = rows.Count;
                }
                catch (Exception ex) { errors.Add("Transactions: " + ex.Message); }

                // ── 5. Allocations (FK → Equipment, Employee, Location) ───────
                try
                {
                    var rows = db.Allocations.ToList();
                    db.Allocations.RemoveRange(rows);
                    db.SaveChanges();
                    counts["Allocations"] = rows.Count;
                }
                catch (Exception ex) { errors.Add("Allocations: " + ex.Message); }

                // ── 6. Equipment ──────────────────────────────────────────────
                try
                {
                    var rows = db.Equipment.ToList();
                    db.Equipment.RemoveRange(rows);
                    db.SaveChanges();
                    counts["Equipment"] = rows.Count;
                }
                catch (Exception ex) { errors.Add("Equipment: " + ex.Message); }

                // ── 7. Employees (FK → Location via DefaultLocationId) ────────
                try
                {
                    var rows = db.Employees.ToList();
                    db.Employees.RemoveRange(rows);
                    db.SaveChanges();
                    counts["Employees"] = rows.Count;
                }
                catch (Exception ex) { errors.Add("Employees: " + ex.Message); }

                // ── 8. Locations ──────────────────────────────────────────────
                try
                {
                    var rows = db.Locations.ToList();
                    db.Locations.RemoveRange(rows);
                    db.SaveChanges();
                    counts["Locations"] = rows.Count;
                }
                catch (Exception ex) { errors.Add("Locations: " + ex.Message); }
            }
            catch (Exception ex)
            {
                errors.Add("Database connection: " + ex.Message);
            }

            // Build summary
            var summary = "Reset complete. Records deleted:\n" +
                string.Join("\n", counts.Select(kv => $"  • {kv.Key}: {kv.Value}"));

            if (errors.Count > 0)
                summary += "\n\nSome tables could not be cleared:\n" + string.Join("\n", errors);


            SecurityAuditLogger.Log("Reset application data",
    string.Join(", ", counts.Select(kv => $"{kv.Key}: {kv.Value}")) +
    (errors.Count > 0 ? $" | Errors: {errors.Count}" : ""));

            ToastHelper.Show(this, summary, errors.Count > 0 ? ToastType.Warning : ToastType.Success);
            ToastHelper.Show(this, summary, errors.Count > 0 ? ToastType.Warning : ToastType.Success);
        }

        /// Small modal requiring the user to type an exact confirmation word before proceeding.
        private bool ShowTypedConfirm(string message, string requiredWord)
        {
            using var frm = new Form
            {
                Text = "Confirm Destructive Action",
                Size = new Size(420, 260),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                StartPosition = FormStartPosition.CenterParent,
                BackColor = ThemeHelper.CardBg
            };

            var lblMsg = new Label
            {
                Text = message,
                Location = new Point(20, 18),
                Size = new Size(380, 100),
                Font = ThemeHelper.FontDefault,
                ForeColor = ThemeHelper.TextPrimary
            };
            frm.Controls.Add(lblMsg);

            var txtConfirm = new TextBox { Location = new Point(20, 126), Size = new Size(380, 28) };
            ThemeHelper.StyleTextBox(txtConfirm);
            frm.Controls.Add(txtConfirm);

            var btnConfirm = new Button
            {
                Text = $"Type \"{requiredWord}\" to confirm",
                Location = new Point(20, 166),
                Size = new Size(380, 36),
                Enabled = false
            };
            StyleDangerOutlineBtn(btnConfirm);
            btnConfirm.BackColor = ThemeHelper.Danger;
            btnConfirm.ForeColor = Color.White;
            frm.Controls.Add(btnConfirm);

            txtConfirm.TextChanged += (s, e) =>
                btnConfirm.Enabled = txtConfirm.Text.Trim() == requiredWord;

            btnConfirm.Click += (s, e) => frm.DialogResult = DialogResult.OK;

            frm.ClientSize = new Size(420, 220);
            return frm.ShowDialog() == DialogResult.OK;
        }

        // ══════════════════════════════════════════════════════════════
        //  USERS PANEL
        // ══════════════════════════════════════════════════════════════
        private Panel BuildUsersPanel()
        {
            var outer = new Panel
            {
                BackColor = PageBackground,
                AutoScroll = true
            };

            var (title, subtitle, nextY) = AddSectionHeader(outer, "User Management",
             "Manage application users and permissions.");
            int headerDelta = nextY - 96;

            var toolbar = new Panel
            {
                Location = new Point(40, nextY),
                Size = new Size(1000, 50),
                BackColor = Color.Transparent
            };

            btnAddUser = new Button
            {
                Text = "✛ Add User",
                Location = new Point(0, 0),
                Size = new Size(150, 38)
            };
            StyleGreenBtn(btnAddUser);
            btnAddUser.Click += BtnAddUser_Click;

            btnEditUser = new Button
            {
                Text = "✏  Edit",
                Location = new Point(164, 0),
                Size = new Size(130, 38)
            };
            ThemeHelper.StyleButton(btnEditUser, Color.FromArgb(245, 158, 11));
            btnEditUser.Click += BtnEditUser_Click;

            btnToggleUser = new Button
            {
                Text = "⇄ Toggle Active",
                Location = new Point(308, 0),
                Size = new Size(160, 38)
            };
            ThemeHelper.StyleButton(btnToggleUser, Color.FromArgb(33, 150, 243));
            btnToggleUser.Click += BtnToggleUser_Click;

            btnDeleteUser = new Button
            {
                Text = "🗑  Delete User",
                Location = new Point(482, 0),
                Size = new Size(150, 38)
            };
            StyleRedBtn(btnDeleteUser);
            btnDeleteUser.Click += BtnDeleteUser_Click;

            toolbar.Controls.Add(btnAddUser);
            toolbar.Controls.Add(btnEditUser);
            toolbar.Controls.Add(btnToggleUser);
            toolbar.Controls.Add(btnDeleteUser);
            outer.Controls.Add(toolbar);

            dgvUsers = new DataGridView
            {
                Location = new Point(40, 156 + headerDelta),
                Size = new Size(1100, 460),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
                AllowUserToAddRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                RowTemplate = { Height = 36 }
            };
            ThemeHelper.StyleDataGridView(dgvUsers);

            // Commercial-app grid polish: dark header w/ white text, soft
            // hover highlight, accent selection, subtle zebra striping.
            dgvUsers.EnableHeadersVisualStyles = false;
            dgvUsers.ColumnHeadersDefaultCellStyle.BackColor = ThemeHelper.Header;
            dgvUsers.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvUsers.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 9f);
            dgvUsers.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dgvUsers.ColumnHeadersHeight = 40;
            dgvUsers.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            dgvUsers.DefaultCellStyle.SelectionBackColor = ThemeHelper.Accent;
            dgvUsers.DefaultCellStyle.SelectionForeColor = Color.White;
            dgvUsers.AlternatingRowsDefaultCellStyle.BackColor = ThemeHelper.IsDarkMode ? Color.FromArgb(20, 22, 27) : Color.FromArgb(250, 250, 250);
            dgvUsers.GridColor = ThemeHelper.IsDarkMode ? Color.FromArgb(45, 48, 55) : SidebarDivider;
            dgvUsers.RowHeadersVisible = false;

            dgvUsers.Columns.Add("ID", "ID");
            dgvUsers.Columns.Add("Username", "Username");
            dgvUsers.Columns.Add("FullName", "Full Name");
            dgvUsers.Columns.Add("Email", "Email");
            dgvUsers.Columns.Add("Role", "Role");
            dgvUsers.Columns.Add("Status", "Status");
            dgvUsers.Columns["ID"].FillWeight = 30;
            dgvUsers.Columns["Status"].FillWeight = 90;
            dgvUsers.Columns["Status"].MinimumWidth = 110;
            outer.Controls.Add(dgvUsers);
            dgvUsers.CellDoubleClick += DgvUsers_CellDoubleClick;

            return outer;
        }

        private void LoadUsers()
        {
            if (dgvUsers == null) return;
            dgvUsers.Rows.Clear();
            try
            {
                using var db = new InventoryDbContext();
                _users = db.Users.ToList();
                _roles = db.Roles.ToList();
                var userRoles = db.UserRoles.ToList();

                foreach (var u in _users)
                {
                    var roleId = userRoles.FirstOrDefault(ur => ur.UserId == u.Id)?.RoleId;
                    var roleName = _roles.FirstOrDefault(r => r.Id == roleId)?.RoleName ?? "—";
                    bool active = u.IsActive == true;

                    int idx = dgvUsers.Rows.Add(
                        u.Id, u.Username, u.FullName,
                        u.Email ?? "—", roleName,
                        active ? "🟢 Active" : "🔴 Inactive");

                    var row = dgvUsers.Rows[idx];
                    row.Cells["Status"].Style.ForeColor = active ? ThemeHelper.Success : ThemeHelper.Danger;
                    row.Cells["Status"].Style.Font = ThemeHelper.FontBold;
                    if (!active)
                        row.DefaultCellStyle.ForeColor = ThemeHelper.IsDarkMode ? Color.FromArgb(120, 128, 140) : Color.FromArgb(180, 180, 180);
                }

                dgvUsers.ClearSelection();     // ← add these two lines
                dgvUsers.CurrentCell = null;
            }
            catch (Exception ex)
            {
                ToastHelper.Show(this, "Error loading users: " + ex.Message, ToastType.Error);

            }
        }

        private void BtnAddUser_Click(object? sender, EventArgs e)
        {
            using var form = new AddEditUserForm();
            if (form.ShowDialog() == DialogResult.OK)
            {
                LoadUsers();
                ToastHelper.Show(this, "User added successfully.", ToastType.Success);
            }
        }

        private void DgvUsers_CellDoubleClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            int id = Convert.ToInt32(dgvUsers.Rows[e.RowIndex].Cells["ID"].Value);
            var user = _users.FirstOrDefault(u => u.Id == id);
            if (user == null) return;

            string roleName = "—";
            try
            {
                using var db = new InventoryDbContext();
                var roleId = db.UserRoles.FirstOrDefault(ur => ur.UserId == user.Id)?.RoleId;
                roleName = _roles.FirstOrDefault(r => r.Id == roleId)?.RoleName ?? "—";
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "SettingsForm.DgvUsers_CellDoubleClick");
            }

            using var popup = new UserDetailPopup(user, roleName);
            popup.ShowDialog(this);
        }

        private void BtnEditUser_Click(object? sender, EventArgs e)
        {
            if (dgvUsers.SelectedRows.Count == 0)
            {
                ToastHelper.Show(this, "Please select a user to edit.", ToastType.Info);

                return;
            }
            int id = Convert.ToInt32(dgvUsers.SelectedRows[0].Cells["ID"].Value);
            var user = _users.FirstOrDefault(u => u.Id == id);
            if (user == null) return;
            using var form = new AddEditUserForm(user);
            if (form.ShowDialog() == DialogResult.OK)
            {
                LoadUsers();
                ToastHelper.Show(this, "User updated successfully.", ToastType.Success);
            }
        }

        private void BtnToggleUser_Click(object? sender, EventArgs e)
        {
            if (dgvUsers.SelectedRows.Count == 0)
            {
                ToastHelper.Show(this, "Please select a user.", ToastType.Info);
                return;
            }
            int id = Convert.ToInt32(dgvUsers.SelectedRows[0].Cells["ID"].Value);
            var user = _users.FirstOrDefault(u => u.Id == id);
            if (user == null) return;

            if (id == AuthService.CurrentUserId)
            {
                MessageBox.Show("You cannot deactivate your own account.", "Not Allowed",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string action = (user.IsActive == true) ? "deactivate" : "activate";
            if (MessageBox.Show($"Are you sure you want to {action} '{user.Username}'?",
                "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

            try
            {
                using var db = new InventoryDbContext();
                var entity = db.Users.FirstOrDefault(u => u.Id == id);
                if (entity == null) return;
                entity.IsActive = !(user.IsActive == true);
                db.SaveChanges();
                LoadUsers();

                SecurityAuditLogger.Log(
                    entity.IsActive == true ? "Activated user" : "Deactivated user",
                    $"Target: {user.Username} (ID {id})");

                ToastHelper.Show(this,
                    $"'{user.Username}' {(entity.IsActive == true ? "activated" : "deactivated")} successfully.",
                    ToastType.Success);
            }
            catch (Exception ex)
            {
                ToastHelper.Show(this, "Error: " + ex.Message, ToastType.Error);
            }
        }

        private void BtnDeleteUser_Click(object? sender, EventArgs e)
        {
            if (dgvUsers.SelectedRows.Count == 0)
            {
                ToastHelper.Show(this, "Please select a user to delete.", ToastType.Info);
                return;
            }

            int id = Convert.ToInt32(dgvUsers.SelectedRows[0].Cells["ID"].Value);
            var user = _users.FirstOrDefault(u => u.Id == id);
            if (user == null) return;

            if (id == AuthService.CurrentUserId)
            {
                ToastHelper.Show(this, "You cannot delete your own account.", ToastType.Warning);
                return;
            }

            if (MessageBox.Show(
                $"Are you sure you want to PERMANENTLY delete '{user.Username}'?\n" +
                "This cannot be undone.\n\n" +
                "This action will be recorded in a separate, non-deletable security audit log.",
                "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;

            try
            {
                using var db = new InventoryDbContext();
                var entity = db.Users.FirstOrDefault(u => u.Id == id);
                if (entity == null) return;

                var userRoles = db.UserRoles.Where(ur => ur.UserId == id).ToList();
                if (userRoles.Count > 0)
                    db.UserRoles.RemoveRange(userRoles);

                db.Users.Remove(entity);
                db.SaveChanges();
                LoadUsers();

                SecurityAuditLogger.Log("Deleted user", $"Target: {user.Username} (ID {id})");

                ToastHelper.Show(this, $"'{user.Username}' was deleted.", ToastType.Success);
            }
            catch (Exception ex)
            {
                ToastHelper.Show(this, "Could not delete this user — they may still be referenced elsewhere " + ex.Message, ToastType.Error);
            }
        }

        // ══════════════════════════════════════════════════════════════
        //  CHANGE PASSWORD PANEL
        // ══════════════════════════════════════════════════════════════
        // ── Placeholder tracking for password fields (Tag holds placeholder text) ──
        private static bool IsShowingPlaceholder(TextBox txt) =>
            txt.Tag is string ph && txt.Text == ph;

        private static GraphicsPath RoundedRectPath(RectangleF r, float radius)
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

        private Panel BuildPasswordPanel()
        {
            // ── Light-theme palette (white card, dark text) ──
            Color cardBg = ThemeHelper.CardBg;
            Color inputBg = ThemeHelper.IsDarkMode ? Color.FromArgb(45, 48, 55) : Color.White;
            Color inputBorder = ThemeHelper.InputBorder;
            Color textWhite = ThemeHelper.TextPrimary;
            Color textMuted = ThemeHelper.TextSecondary;
            Color placeholder = ThemeHelper.IsDarkMode ? Color.FromArgb(110, 118, 130) : Color.FromArgb(156, 163, 175); 
            Color accentGreen = Color.FromArgb(0x52, 0xA2, 0x3F);

            var outer = new Panel
            {
                BackColor = PageBackground,
                AutoScroll = true
            };

            var (title, subtitle, nextY) = AddSectionHeader(outer, "Change Password",
                "Update your account password securely.");

            // Container for both left form and right tips card
            var container = new Panel
            {
                Location = new Point(40, nextY),
                Size = new Size(1000, 460),
                BackColor = Color.Transparent
            };
            outer.Controls.Add(container);

            // ══════════════════════════════════════════════════════════════
            //  LEFT SIDE — dark "Change Password" card
            // ══════════════════════════════════════════════════════════════
            var formCard = new Panel
            {
                Location = new Point(0, 0),
                Size = new Size(560, 440),
                BackColor = cardBg
            };
            container.Controls.Add(formCard);

            void RoundCard(object? s, EventArgs e)
            {
                if (formCard.Width <= 0 || formCard.Height <= 0) return;
                using var path = RoundedRectPath(new RectangleF(0, 0, formCard.Width, formCard.Height), 16f);
                formCard.Region = new Region(path);
            }
            formCard.Resize += RoundCard;
            RoundCard(null, EventArgs.Empty);

            formCard.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var path = RoundedRectPath(new RectangleF(0.5f, 0.5f, formCard.Width - 1, formCard.Height - 1), 16f);
                using var pen = new Pen(ThemeHelper.CardBorder, 1f); e.Graphics.DrawPath(pen, path);
            };

            int cardPadX = 28;
            int cy = 24;

            // Title row — lock icon + "Change Password"
            var lblIcon = new Label
            {
                Text = "🔒",
                Font = new Font("Segoe UI Emoji", 12f),
                ForeColor = textWhite,
                AutoSize = true,
                Location = new Point(cardPadX, cy),
                BackColor = Color.Transparent
            };
            formCard.Controls.Add(lblIcon);

            var lblCardTitle = new Label
            {
                Text = "Change Password",
                Font = new Font("Segoe UI Semibold", 12.5f),
                ForeColor = textWhite,
                AutoSize = true,
                Location = new Point(cardPadX + 30, cy + 1),
                BackColor = Color.Transparent
            };
            formCard.Controls.Add(lblCardTitle);
            cy += 30;

            var lblCardSubtitle = new Label
            {
                Text = "Update your account password securely.",
                Font = ThemeHelper.FontSmall,
                ForeColor = textMuted,
                AutoSize = true,
                Location = new Point(cardPadX, cy),
                BackColor = Color.Transparent
            };
            formCard.Controls.Add(lblCardSubtitle);
            cy += 34;

            int fieldWidth = formCard.Width - cardPadX * 2;

            // Local function: builds one dark, rounded, placeholder-driven
            // password field with an eye-toggle icon on the right.
            TextBox BuildPwdField(string labelText, string placeholderText, ref int yPos)
            {
                var lbl = new Label
                {
                    Text = labelText,
                    Font = ThemeHelper.FontBold,
                    ForeColor = textWhite,
                    AutoSize = true,
                    Location = new Point(cardPadX, yPos),
                    BackColor = Color.Transparent
                };
                formCard.Controls.Add(lbl);
                yPos += 22;

                var inputPanel = new Panel
                {
                    Location = new Point(cardPadX, yPos),
                    Size = new Size(fieldWidth, 40),
                    BackColor = inputBg
                };
                formCard.Controls.Add(inputPanel);

                void RoundInput(object? s, EventArgs e)
                {
                    if (inputPanel.Width <= 0 || inputPanel.Height <= 0) return;
                    using var path = RoundedRectPath(new RectangleF(0, 0, inputPanel.Width, inputPanel.Height), 8f);
                    inputPanel.Region = new Region(path);
                }
                inputPanel.Resize += RoundInput;
                RoundInput(null, EventArgs.Empty);

                inputPanel.Paint += (s, e) =>
                {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    using var path = RoundedRectPath(new RectangleF(0.5f, 0.5f, inputPanel.Width - 1, inputPanel.Height - 1), 8f);
                    using var pen = new Pen(inputBorder, 1f);
                    e.Graphics.DrawPath(pen, path);
                };

                var eyeIcon = new Label
                {
                    Text = "👁",
                    Font = new Font("Segoe UI Emoji", 9.5f),
                    ForeColor = textMuted,
                    AutoSize = false,
                    Size = new Size(28, 40),
                    Location = new Point(inputPanel.Width - 34, 0),
                    TextAlign = ContentAlignment.MiddleCenter,
                    BackColor = Color.Transparent,
                    Cursor = Cursors.Hand,
                    Anchor = AnchorStyles.Top | AnchorStyles.Right
                };
                inputPanel.Controls.Add(eyeIcon);

                var txt = new TextBox
                {
                    Text = placeholderText,
                    Tag = placeholderText,
                    Font = ThemeHelper.FontDefault,
                    ForeColor = placeholder,
                    BackColor = inputBg,
                    BorderStyle = BorderStyle.None,
                    Location = new Point(14, 11),
                    Size = new Size(inputPanel.Width - 54, 20),
                    UseSystemPasswordChar = false,
                    Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
                };
                inputPanel.Controls.Add(txt);
                eyeIcon.BringToFront();

                txt.Enter += (s, e) =>
                {
                    if (IsShowingPlaceholder(txt))
                    {
                        txt.Text = "";
                        txt.ForeColor = textWhite;
                        txt.UseSystemPasswordChar = true;
                    }
                };
                txt.Leave += (s, e) =>
                {
                    if (string.IsNullOrEmpty(txt.Text))
                    {
                        txt.UseSystemPasswordChar = false;
                        txt.ForeColor = placeholder;
                        txt.Text = placeholderText;
                    }
                };
                eyeIcon.Click += (s, e) =>
                {
                    if (IsShowingPlaceholder(txt)) return; // nothing real to reveal yet
                    txt.UseSystemPasswordChar = !txt.UseSystemPasswordChar;
                    eyeIcon.ForeColor = txt.UseSystemPasswordChar ? textMuted : accentGreen;
                };

                yPos += 54;
                return txt;
            }

            txtCurrentPwd = BuildPwdField("Current Password", "Enter current password", ref cy);

            txtNewPwd = BuildPwdField("New Password", "Enter new password", ref cy);
            txtNewPwd.TextChanged += (s, e) =>
            {
                if (!IsShowingPlaceholder(txtNewPwd)) UpdatePasswordStrength();
            };

            // Strength bar sits just under the New Password field
            cy -= 30;
            _pwdStrengthTrack = new Panel
            {
                Location = new Point(cardPadX, cy),
                Size = new Size(fieldWidth - 90, 5),
                BackColor = Color.FromArgb(229, 231, 235),
                Visible = false
            };
            _pwdStrengthBar = new Panel
            {
                Location = new Point(0, 0),
                Size = new Size(0, 5),
                BackColor = Color.FromArgb(55, 61, 78)
            };
            _pwdStrengthTrack.Controls.Add(_pwdStrengthBar);
            formCard.Controls.Add(_pwdStrengthTrack);

            _lblPwdStrengthText = new Label
            {
                Location = new Point(cardPadX + fieldWidth - 84, cy - 8),
                AutoSize = true,
                Font = new Font("Segoe UI", 8f),
                ForeColor = textMuted,
                BackColor = Color.Transparent,
                Text = ""
            };
            formCard.Controls.Add(_lblPwdStrengthText);
            cy += 30;

            txtConfirmPwd = BuildPwdField("Confirm New Password", "Confirm new password", ref cy);

            lblPwdStatus = new Label
            {
                Location = new Point(cardPadX, cy),
                AutoSize = false,
                Size = new Size(fieldWidth, 22),
                Font = ThemeHelper.FontSmall,
                ForeColor = ThemeHelper.Danger,
                BackColor = Color.Transparent,
                Text = ""
            };
            formCard.Controls.Add(lblPwdStatus);
            cy += 26;

            // ── Rounded green "Update Password" button ──
            var btnChange = new Button
            {
                Text = "🔄  Update Password",
                Location = new Point(cardPadX, cy),
                Size = new Size(fieldWidth, 44),
                FlatStyle = FlatStyle.Flat,
                BackColor = accentGreen,
                ForeColor = Color.White,
                Font = ThemeHelper.FontBold,
                Cursor = Cursors.Hand,
                UseVisualStyleBackColor = false
            };
            btnChange.FlatAppearance.BorderSize = 0;
            btnChange.FlatAppearance.MouseOverBackColor = ControlPaint.Light(accentGreen, 0.1f);
            btnChange.FlatAppearance.MouseDownBackColor = ControlPaint.Dark(accentGreen, 0.1f);

            void RoundButton(object? s, EventArgs e)
            {
                if (btnChange.Width <= 0 || btnChange.Height <= 0) return;
                using var path = RoundedRectPath(new RectangleF(0, 0, btnChange.Width, btnChange.Height), 10f);
                btnChange.Region = new Region(path);
            }
            btnChange.Resize += RoundButton;
            RoundButton(null, EventArgs.Empty);

            btnChange.Click += BtnChangePwd_Click;
            formCard.Controls.Add(btnChange);

            // ══════════════════════════════════════════════════════════════
            //  RIGHT SIDE — Security Tips (unchanged, light card)
            // ══════════════════════════════════════════════════════════════
            var tipsCard = new Panel
            {
                Location = new Point(590, 0),
                Size = new Size(350, 240),
                BackColor = ThemeHelper.IsDarkMode ? Color.FromArgb(25, 40, 30) : Color.FromArgb(240, 253, 244)
            };
            ApplyCardStyle(container, tipsCard, borderColor: Color.FromArgb(187, 247, 208));
            container.Controls.Add(tipsCard);

            var lblTipsTitle = new Label
            {
                Text = "Security Tips",
                Font = new Font("Segoe UI Semibold", 11f),
                ForeColor = ThemeHelper.Header,
                AutoSize = true,
                Location = new Point(24, 24),
                BackColor = Color.Transparent
            };
            tipsCard.Controls.Add(lblTipsTitle);

            string[] tips = {
            "Minimum 8 characters long",
            "Include numbers and symbols",
            "Use uppercase letters",
            "Don't reuse old passwords"
        };

            int ty = 60;
            foreach (var tip in tips)
            {
                var tipIcon = new Label
                {
                    Text = "✓",
                    Font = ThemeHelper.FontBold,
                    ForeColor = ThemeHelper.Success,
                    AutoSize = true,
                    Location = new Point(24, ty),
                    BackColor = Color.Transparent
                };
                tipsCard.Controls.Add(tipIcon);

                var lblTip = new Label
                {
                    Text = tip,
                    Font = ThemeHelper.FontDefault,
                    ForeColor = Color.FromArgb(55, 65, 81),
                    AutoSize = true,
                    Location = new Point(48, ty),
                    BackColor = Color.Transparent
                };
                tipsCard.Controls.Add(lblTip);
                ty += 32;
            }

            return outer;
        }

        /// Live strength meter: weak (red, short) / medium (amber) / strong (green, full).
        private void UpdatePasswordStrength()
        {
            if (_pwdStrengthBar == null) return;
            string pwd = txtNewPwd.Text;
            _pwdStrengthTrack.Visible = pwd.Length > 0;


            int score = 0;
            if (pwd.Length >= 4) score++;
            if (pwd.Length >= 8) score++;
            if (pwd.Any(char.IsUpper) && pwd.Any(char.IsLower)) score++;
            if (pwd.Any(char.IsDigit)) score++;
            if (pwd.Any(c => !char.IsLetterOrDigit(c))) score++;

            string label;
            Color color;
            int widthPct;

            if (pwd.Length == 0)
            {
                label = "";
                color = Color.FromArgb(229, 231, 235);
                widthPct = 0;
            }
            else if (score <= 1)
            {
                label = "Weak";
                color = ThemeHelper.Danger;
                widthPct = 33;
            }
            else if (score <= 3)
            {
                label = "Medium";
                color = Color.FromArgb(217, 119, 6);
                widthPct = 66;
            }
            else
            {
                label = "Strong";
                color = ThemeHelper.Success;
                widthPct = 100;
            }

            _lblPwdStrengthText.Text = label;
            _lblPwdStrengthText.ForeColor = color;
            _pwdStrengthBar.BackColor = color;
            _pwdStrengthBar.Size = new Size((int)(300 * widthPct / 100f), 5);
        }

        private void BtnChangePwd_Click(object? sender, EventArgs e)
        {
            lblPwdStatus.ForeColor = ThemeHelper.Danger;
            lblPwdStatus.Text = "";

            string cur = txtCurrentPwd.Text;
            string nw = txtNewPwd.Text;
            string conf = txtConfirmPwd.Text;

            if (string.IsNullOrWhiteSpace(cur) || string.IsNullOrWhiteSpace(nw) || string.IsNullOrWhiteSpace(conf))
            { lblPwdStatus.Text = "All fields are required."; return; }

            if (nw != conf)
            { lblPwdStatus.Text = "New password and confirmation do not match."; return; }

            if (nw.Length < 4)
            { lblPwdStatus.Text = "Password must be at least 4 characters."; return; }

            try
            {
                using var db = new InventoryDbContext();
                var user = db.Users.FirstOrDefault(u => u.Id == AuthService.CurrentUserId);
                if (user == null) { lblPwdStatus.Text = "User not found."; return; }
                if (user.PasswordHash != cur) { lblPwdStatus.Text = "Current password is incorrect."; return; }

                user.PasswordHash = nw;
                db.SaveChanges();

                txtCurrentPwd.Text = "";
                txtNewPwd.Text = "";
                txtConfirmPwd.Text = "";
                UpdatePasswordStrength();

                lblPwdStatus.ForeColor = ThemeHelper.Success;
                lblPwdStatus.Text = "✅ Password changed successfully.";
            }
            catch (Exception ex)
            {
                lblPwdStatus.Text = "Error: " + ex.Message;
            }
        }

        // ══════════════════════════════════════════════════════════════
        //  PRINTING PANEL
        // ══════════════════════════════════════════════════════════════
        private Panel BuildPrintingPanel()
        {
            var outer = new Panel
            {
                BackColor = PageBackground,
                AutoScroll = true
            };

            var (title, subtitle, nextY) = AddSectionHeader(outer, "Barcode Label Printing",
    "Configure printers and barcode labels.");

            var card = new Panel
            {
                Location = new Point(40, nextY),
                Size = new Size(560, 500), // temporary — recalculated below once content is laid out
                BackColor = ThemeHelper.CardBg
            };
            outer.Controls.Add(card); // add first, style applied after final height is known

            var cardTitle = new Label
            {
                Text = "Printer",
                Font = new Font("Segoe UI Semibold", 11f),
                ForeColor = ThemeHelper.TextPrimary,
                AutoSize = true,
                Location = new Point(24, 20),
                BackColor = Color.Transparent
            };
            card.Controls.Add(cardTitle);

            int y = 54;

            card.Controls.Add(MakeLabel("Printer Name", 24, y));
            y += 26;
            txtPrinterName = MakeTextBox(card, 24, y, 490, "Leave blank to use default printer");
            y += 54;

            var labelSizeTitle = new Label
            {
                Text = "Label Size",
                Font = new Font("Segoe UI Semibold", 11f),
                ForeColor = ThemeHelper.TextPrimary,
                AutoSize = true,
                Location = new Point(24, y),
                BackColor = Color.Transparent
            };
            card.Controls.Add(labelSizeTitle);
            y += 30;

            card.Controls.Add(MakeLabel("Width (mm)", 24, y));
            card.Controls.Add(MakeLabel("Height (mm)", 260, y));
            y += 26;

            txtLabelWidth = MakeTextBox(card, 24, y, 200);
            txtLabelWidth.Text = "62";

            txtLabelHeight = MakeTextBox(card, 260, y, 200);
            txtLabelHeight.Text = "29";
            y += 66;

            var actionsDivider = new Panel
            {
                Location = new Point(24, y),
                Size = new Size(512, 1),
                BackColor = SidebarDivider
            };
            card.Controls.Add(actionsDivider);
            y += 16;

            btnTestPrint = new Button
            {
                Text = "🖨  Test Print Label",
                Location = new Point(24, y),
                Size = new Size(168, 38)
            };
            StyleGrayBtn(btnTestPrint);
            btnTestPrint.Click += btnTestPrint_Click;
            card.Controls.Add(btnTestPrint);

            btnSave = new Button
            {
                Text = "💾  Save",
                Location = new Point(202, y),
                Size = new Size(150, 38)
            };
            StyleGreenBtn(btnSave);
            btnSave.Click += btnSave_Click;
            card.Controls.Add(btnSave);


            // Auto-size the card to whatever the last control actually needs, plus padding —
            // so it can never silently clip content again if a field gets added later.
            int cardBottom = 0;
            foreach (Control c in card.Controls)
                cardBottom = Math.Max(cardBottom, c.Bottom);
            card.Height = cardBottom + 24;
            ApplyCardStyle(outer, card);

            return outer;
        }

        // ══════════════════════════════════════════════════════════════
        //  CATEGORIES PANEL  — backed by CategoryStore (shared)
        // ══════════════════════════════════════════════════════════════
        private Panel BuildCategoriesPanel()
        {
            var outer = new Panel
            {
                BackColor = PageBackground,
                AutoScroll = true
            };

            var (title, subtitle, nextY) = AddSectionHeader(outer, "Manage Categories",
    "Organise equipment categories.");

            var card = new Panel
            {
                Location = new Point(40, nextY),
                Size = new Size(560, 450),
                BackColor = ThemeHelper.CardBg
            };
            ApplyCardStyle(outer, card);
            outer.Controls.Add(card);

            card.Controls.Add(MakeLabel("New Category Name", 24, 22));

            txtNewCategory = MakeTextBox(card, 24, 48, 360);
            txtNewCategory.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    BtnAddCategory_Click(s, EventArgs.Empty);
                }
            };

            var btnAdd = new Button
            {
                Text = "➕  Add",
                Location = new Point(394, 48),
                Size = new Size(120, 40)
            };
            StyleGreenBtn(btnAdd);
            btnAdd.Click += BtnAddCategory_Click;
            card.Controls.Add(btnAdd);

            lblCategoryMsg = new Label
            {
                Location = new Point(24, 98),
                AutoSize = false,
                Size = new Size(490, 20),
                Font = ThemeHelper.FontSmall,
                ForeColor = ThemeHelper.Danger,
                BackColor = Color.Transparent,
                Text = ""
            };
            card.Controls.Add(lblCategoryMsg);

            card.Controls.Add(MakeLabel("Existing Categories", 24, 130));

            lstCategories = new ListBox
            {
                Location = new Point(24, 158),
                Size = new Size(490, 210),
                Font = ThemeHelper.FontDefault,
                BorderStyle = BorderStyle.FixedSingle,
                DrawMode = DrawMode.OwnerDrawFixed,
                ItemHeight = 32,
                IntegralHeight = false
            };
            lstCategories.DrawItem += LstCategories_DrawItem;
            card.Controls.Add(lstCategories);

            var btnDelete = new Button
            {
                Text = "🗑  Delete Selected",
                Location = new Point(24, 382),
                Size = new Size(170, 36)
            };
            StyleGrayBtn(btnDelete);
            btnDelete.Click += BtnDeleteCategory_Click;
            card.Controls.Add(btnDelete);

            return outer;
        }

        private void LoadCategories()
        {
            var result = new CategoryService().GetAll();
            _categories = result.Success ? (result.Data ?? new List<Category>()) : new List<Category>();

            if (lstCategories == null) return;
            lstCategories.Items.Clear();
            foreach (var c in _categories)
                lstCategories.Items.Add(c.Name);
        }

        private void BtnAddCategory_Click(object? sender, EventArgs e)
        {
            lblCategoryMsg.Text = "";
            string name = txtNewCategory.Text.Trim();

            var result = new CategoryService().Add(name);
            if (!result.Success)
            {
                lblCategoryMsg.ForeColor = ThemeHelper.Danger;
                lblCategoryMsg.Text = result.ErrorMessage ?? "Could not add category.";
                return;
            }

            txtNewCategory.Text = "";
            lblCategoryMsg.ForeColor = ThemeHelper.Success;
            lblCategoryMsg.Text = $"✅ '{name}' added.";
            LoadCategories();
        }

        private void BtnDeleteCategory_Click(object? sender, EventArgs e)
        {
            if (lstCategories.SelectedIndex < 0)
            {
                lblCategoryMsg.ForeColor = ThemeHelper.Danger;
                lblCategoryMsg.Text = "Select a category to delete.";
                return;
            }

            int index = lstCategories.SelectedIndex;
            if (index >= _categories.Count) return;
            var selected = _categories[index];

            if (MessageBox.Show($"Delete category '{selected.Name}'?", "Confirm",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

            var result = new CategoryService().Delete(selected.Id);
            if (!result.Success)
            {
                lblCategoryMsg.ForeColor = ThemeHelper.Danger;
                lblCategoryMsg.Text = result.ErrorMessage ?? "Could not delete category.";
                return;
            }

            lblCategoryMsg.ForeColor = ThemeHelper.Success;
            lblCategoryMsg.Text = $"🗑 '{selected.Name}' deleted.";
            LoadCategories();
        }

        private void LstCategories_DrawItem(object? sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;

            bool selected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;

            using var bg = new SolidBrush(selected ? ThemeHelper.Accent : ThemeHelper.CardBg);
            e.Graphics.FillRectangle(bg, e.Bounds);

            string text = lstCategories.Items[e.Index]?.ToString() ?? "";

            using var numFont = new Font("Segoe UI", 9f, FontStyle.Bold);
            using var numBrush = new SolidBrush(selected ? Color.White : ThemeHelper.TextSecondary);
            e.Graphics.DrawString($"{e.Index + 1}.", numFont, numBrush, e.Bounds.X + 12, e.Bounds.Y + 8);

            using var txtBrush = new SolidBrush(selected ? Color.White : ThemeHelper.TextPrimary);
            e.Graphics.DrawString(text, ThemeHelper.FontDefault, txtBrush, e.Bounds.X + 42, e.Bounds.Y + 8);

            using var linePen = new Pen(SidebarDivider, 1f);
            e.Graphics.DrawLine(linePen, e.Bounds.X, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1);

            e.DrawFocusRectangle();
        }

        // ══════════════════════════════════════════════════════════════
        //  SYSTEM CONFIGURATION PANEL  — Admin-only DB connection settings
        // ══════════════════════════════════════════════════════════════
        private Panel BuildSystemConfigPanel()
        {
            var outer = new Panel
            {
                BackColor = PageBackground,
                AutoScroll = true
            };

            var (title, subtitle, nextY) = AddSectionHeader(outer, "System Configuration (Admin)",
                "Manage database connection and application updates.");

            // ═══ Database Connection Details card ═══
            var card = new Panel
            {
                Location = new Point(40, nextY),
                Size = new Size(560, 500), // temporary — recalculated below once content is laid out
                BackColor = ThemeHelper.CardBg
            };
            outer.Controls.Add(card);

            card.Controls.Add(new Label
            {
                Text = "Database Connection Details",
                Font = new Font("Segoe UI Semibold", 11f),
                ForeColor = ThemeHelper.TextPrimary,
                AutoSize = true,
                Location = new Point(24, 20),
                BackColor = Color.Transparent
            });

            int y = 54;
            card.Controls.Add(MakeLabel("Database Server (IP)", 24, y)); y += 26;
            var txtDbServer = MakeTextBox(card, 24, y, 490); y += 54;

            card.Controls.Add(MakeLabel("Database Name", 24, y)); y += 26;
            var txtDbName = MakeTextBox(card, 24, y, 490); y += 54;

            card.Controls.Add(MakeLabel("Database Username", 24, y)); y += 26;
            var txtDbUser = MakeTextBox(card, 24, y, 490); y += 54;

            card.Controls.Add(MakeLabel("Database Password", 24, y)); y += 26;
            var txtDbPass = MakeTextBox(card, 24, y, 490);
            txtDbPass.UseSystemPasswordChar = true;

            // Eye icon to toggle password visibility — sits inside the same
            // rounded input panel that MakeTextBox created (txtDbPass.Parent).
            var dbPassInputPanel = (Panel)txtDbPass.Parent!;
            var eyeIconDbPass = new Label
            {
                Text = "👁",
                Font = new Font("Segoe UI Emoji", 9.5f),
                ForeColor = ThemeHelper.TextSecondary,
                AutoSize = false,
                Size = new Size(28, 40),
                Location = new Point(dbPassInputPanel.Width - 34, 0),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            dbPassInputPanel.Controls.Add(eyeIconDbPass);
            eyeIconDbPass.BringToFront();
            txtDbPass.Width -= 26; // make room so typed text doesn't sit under the icon

            eyeIconDbPass.Click += (s, e) =>
            {
                txtDbPass.UseSystemPasswordChar = !txtDbPass.UseSystemPasswordChar;
                eyeIconDbPass.ForeColor = txtDbPass.UseSystemPasswordChar
                    ? ThemeHelper.TextSecondary
                    : ThemeHelper.Accent;
            };

            y += 60;

            var lblDbMsg = new Label
            {
                Location = new Point(24, y),
                Size = new Size(490, 20),
                Font = ThemeHelper.FontSmall,
                ForeColor = ThemeHelper.Danger,
                BackColor = Color.Transparent,
                Text = ""
            };
            card.Controls.Add(lblDbMsg);
            y += 26;

            var btnTest = new Button
            {
                Text = "✔  Test Connection",
                Location = new Point(24, y),
                Size = new Size(170, 38)
            };
            StyleGreenBtn(btnTest);
            card.Controls.Add(btnTest);

            var btnSaveConn = new Button
            {
                Text = "💾  Save Connection",
                Location = new Point(202, y),
                Size = new Size(170, 38),
                Enabled = false
            };
            StyleGrayBtn(btnSaveConn);
            card.Controls.Add(btnSaveConn);

            // ═══ Connection Status card ═══
            var statusCard = new Panel
            {
                Location = new Point(620, nextY),
                Size = new Size(340, 200),
                BackColor = ThemeHelper.CardBg
            };
            ApplyCardStyle(outer, statusCard);
            outer.Controls.Add(statusCard);

            statusCard.Controls.Add(new Label
            {
                Text = "Connection Status",
                Font = new Font("Segoe UI Semibold", 11f),
                ForeColor = ThemeHelper.TextPrimary,
                AutoSize = true,
                Location = new Point(24, 20),
                BackColor = Color.Transparent
            });

            statusCard.Controls.Add(MakeLabel("Current Status", 24, 56));
            _lblConnStatus = new Label
            {
                Location = new Point(24, 74),
                AutoSize = true,
                Font = ThemeHelper.FontDefault,
                BackColor = Color.Transparent
            };
            statusCard.Controls.Add(_lblConnStatus);

            statusCard.Controls.Add(MakeLabel("Active Server", 24, 104));
            _lblConnServer = new Label
            {
                Location = new Point(24, 122),
                AutoSize = true,
                Font = ThemeHelper.FontDefault,
                ForeColor = ThemeHelper.TextPrimary,
                BackColor = Color.Transparent
            };
            statusCard.Controls.Add(_lblConnServer);

            statusCard.Controls.Add(MakeLabel("Last Successful Connection", 24, 148));
            _lblConnLast = new Label
            {
                Location = new Point(24, 166),
                AutoSize = true,
                Font = ThemeHelper.FontDefault,
                ForeColor = ThemeHelper.TextPrimary,
                BackColor = Color.Transparent
            };
            statusCard.Controls.Add(_lblConnLast);

            // ── Load current saved values into the fields ──
            void RefreshFromSaved()
            {
                try
                {
                    var s = Services.DbConnectionSettingsService.Load();
                    txtDbServer.Text = s.Server;
                    txtDbName.Text = s.Database;
                    txtDbUser.Text = s.Username;
                    txtDbPass.Text = "";
                    _lblConnServer.Text = $"{s.Database} · {s.Server}";

                    var pwd = Services.DbConnectionSettingsService.DecryptPassword(s);
                    bool ok = Services.DbConnectionSettingsService.TestConnection(s, pwd, out _);
                    _lblConnStatus.Text = ok ? "🟢 Connected" : "🔴 Disconnected";
                    _lblConnStatus.ForeColor = ok ? ThemeHelper.Success : ThemeHelper.Danger;
                    _lblConnLast.Text = ok ? DateTime.Now.ToString("yyyy-MM-dd · h:mm tt") : "—";
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "SettingsForm.BuildSystemConfigPanel.RefreshFromSaved");
                }
            }
            RefreshFromSaved();

            btnTest.Click += (s, e) =>
            {
                var settings = new Models.DbConnectionSettings
                {
                    Server = txtDbServer.Text.Trim(),
                    Port = 3306,
                    Database = txtDbName.Text.Trim(),
                    Username = txtDbUser.Text.Trim(),
                    ServerVersion = "11.8.6-mariadb"
                };
                if (Services.DbConnectionSettingsService.TestConnection(settings, txtDbPass.Text, out var err))
                {
                    lblDbMsg.ForeColor = ThemeHelper.Success;
                    lblDbMsg.Text = "✅ Connection successful.";
                    btnSaveConn.Enabled = true;
                }
                else
                {
                    lblDbMsg.ForeColor = ThemeHelper.Danger;
                    lblDbMsg.Text = "❌ " + err;
                    btnSaveConn.Enabled = false;
                }
            };

            btnSaveConn.Click += (s, e) =>
            {
                var settings = new Models.DbConnectionSettings
                {
                    Server = txtDbServer.Text.Trim(),
                    Port = 3306,
                    Database = txtDbName.Text.Trim(),
                    Username = txtDbUser.Text.Trim(),
                    ServerVersion = "11.8.6-mariadb"
                };
                Services.DbConnectionSettingsService.Save(settings, txtDbPass.Text);
                ToastHelper.Show(this, "Connection saved. New connections will use this immediately.", ToastType.Success);
                RefreshFromSaved();
                btnSaveConn.Enabled = false;
            };

            // Auto-size the card to whatever the last control actually needs, plus padding.
            int cardBottom = 0;
            foreach (Control c in card.Controls)
                cardBottom = Math.Max(cardBottom, c.Bottom);
            card.Height = cardBottom + 24;
            ApplyCardStyle(outer, card);

            return outer;
        }

        // ══════════════════════════════════════════════════════════════
        //  LOAD / SAVE SETTINGS
        // ══════════════════════════════════════════════════════════════
        private void LoadSettings()
        {
            try
            {
                using var db = new InventoryDbContext();
                _current = db.AppSettings.FirstOrDefault();

                if (_current == null)
                {
                    _current = new AppSetting
                    {
                        CompanyName = "Q-Inventory",
                        Theme = "Light",
                        DbServer = "localhost",
                        DbName = "inventory_management",
                        PrinterName = "",
                        LabelWidthMm = 62,
                        LabelHeightMm = 29
                    };
                    db.AppSettings.Add(_current);
                    db.SaveChanges();
                }

                txtCompany.Text = _current.CompanyName;
                txtPrinterName.Text = _current.PrinterName ?? "";
                txtLabelWidth.Text = (_current.LabelWidthMm ?? 62).ToString();
                txtLabelHeight.Text = (_current.LabelHeightMm ?? 29).ToString();

                _originalCompanyName = txtCompany.Text.Trim();
                if (btnSave != null) btnSave.Enabled = false;
                if (_lblSavedPill != null) _lblSavedPill.Visible = false;

                if (_lblDbValue != null)
                    _lblDbValue.Text = $"{_current.DbName} · {_current.DbServer}";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not load settings: " + ex.Message,
                    "Settings", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void btnSave_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtCompany.Text))
            {
                MessageBox.Show("Company name cannot be empty.", "Validation",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!int.TryParse(txtLabelWidth.Text, out int lw) || lw <= 0 ||
                !int.TryParse(txtLabelHeight.Text, out int lh) || lh <= 0)
            {
                MessageBox.Show("Label width and height must be positive numbers.", "Validation",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                using var db = new InventoryDbContext();
                var entity = db.AppSettings.FirstOrDefault() ?? new AppSetting();
                bool isNew = entity.Id == 0;

                entity.CompanyName = txtCompany.Text.Trim();
                entity.Theme = "Light";
                entity.PrinterName = txtPrinterName.Text.Trim();
                entity.LabelWidthMm = lw;
                entity.LabelHeightMm = lh;
                entity.UpdatedAt = DateTime.Now;

                if (isNew) db.AppSettings.Add(entity);
                db.SaveChanges();
                _current = entity;

                _originalCompanyName = txtCompany.Text.Trim();
                btnSave.Enabled = false;

                // From the General/Company card: inline "✓ Saved" confirmation instead of a popup.
                // The Printing panel reuses this same btnSave/handler, so only show the pill
                // when the Company card's pill control is on the currently visible panel.
                if (_lblSavedPill != null && _panelGeneral.Visible)
                {
                    _lblSavedPill.Visible = true;
                }
                else
                {
                    MessageBox.Show("Settings saved successfully.", "Saved",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }

                if (_lblDbValue != null)
                    _lblDbValue.Text = $"{_current.DbName} · {_current.DbServer}";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to save: " + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnReset_Click(object? sender, EventArgs e)
        {
            if (_current == null) { LoadSettings(); return; }
            txtCompany.Text = _current.CompanyName;
            txtPrinterName.Text = _current.PrinterName ?? "";
            txtLabelWidth.Text = (_current.LabelWidthMm ?? 62).ToString();
            txtLabelHeight.Text = (_current.LabelHeightMm ?? 29).ToString();

            _originalCompanyName = txtCompany.Text.Trim();
            if (btnSave != null) btnSave.Enabled = false;
            if (_lblSavedPill != null) _lblSavedPill.Visible = false;
        }

        private void btnTestPrint_Click(object? sender, EventArgs e)
        {
            if (!int.TryParse(txtLabelWidth.Text, out int lw) || lw <= 0) lw = 62;
            if (!int.TryParse(txtLabelHeight.Text, out int lh) || lh <= 0) lh = 29;

            string printerName = txtPrinterName.Text.Trim();
            string companyName = txtCompany.Text.Trim().Length > 0
                ? txtCompany.Text.Trim() : "Q-Inventory";

            try
            {
                var gen = new QRCodeGenerator();
                var data = gen.CreateQrCode("TEST-LABEL-001", QRCodeGenerator.ECCLevel.Q);
                var qr = new QRCode(data);
                using var bmp = qr.GetGraphic(4);

                var pd = new PrintDocument();
                if (!string.IsNullOrWhiteSpace(printerName))
                    pd.PrinterSettings.PrinterName = printerName;

                pd.DefaultPageSettings.PaperSize =
                    new PaperSize("Label", (int)(lw * 3.937), (int)(lh * 3.937));
                pd.DefaultPageSettings.Margins = new Margins(10, 10, 10, 10);

                pd.PrintPage += (s, ev) =>
                {
                    var g = ev.Graphics!;
                    var bounds = ev.PageBounds;
                    int qrSz = Math.Min(bounds.Height - 20, 80);
                    g.DrawImage(bmp, new Rectangle(
                        bounds.X + 5, bounds.Y + (bounds.Height - qrSz) / 2, qrSz, qrSz));
                    int tx = bounds.X + qrSz + 12;
                    int tw = bounds.Width - qrSz - 20;
                    using var bf = new Font("Segoe UI", 8f, FontStyle.Bold);
                    using var sf = new Font("Segoe UI", 6.5f);
                    g.DrawString(companyName, bf, Brushes.Black,
                        new RectangleF(tx, bounds.Y + 6, tw, 18));
                    g.DrawString("TEST-LABEL-001", bf, Brushes.Black,
                        new RectangleF(tx, bounds.Y + 22, tw, 18));
                    g.DrawString("Status: In Store", sf, Brushes.DimGray,
                        new RectangleF(tx, bounds.Y + 38, tw, 16));
                    g.DrawString(DateTime.Now.ToString("yyyy-MM-dd"), sf, Brushes.DimGray,
                        new RectangleF(tx, bounds.Y + 52, tw, 16));
                    ev.HasMorePages = false;
                };

                using var ppd = new PrintPreviewDialog
                {
                    Document = pd,
                    Width = 600,
                    Height = 400,
                    StartPosition = FormStartPosition.CenterParent,
                    Text = "Label Preview"
                };
                ppd.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Test print failed: " + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ══════════════════════════════════════════════════════════════
        //  UI HELPERS
        // ══════════════════════════════════════════════════════════════

        /// Builds a section title + subtitle pair with the subtitle positioned
        /// relative to the title's ACTUAL rendered height — not a fixed offset.
        /// Fixes overlap at non-100% DPI/font scaling, where a fixed pixel gap
        /// no longer matches the title's real height.
        private static (Label title, Label subtitle, int nextY) AddSectionHeader(
            Panel outer, string titleText, string subtitleText, int x = 40, int titleY = 32)
        {
            var title = new Label
            {
                Text = titleText,
                Font = new Font("Segoe UI Semibold", 16f),
                ForeColor = ThemeHelper.TextPrimary,
                AutoSize = true,
                Location = new Point(x, titleY)
            };
            outer.Controls.Add(title);

            var subtitle = MakeSubtitle(subtitleText, x, title.Bottom + 6);
            outer.Controls.Add(subtitle);

            return (title, subtitle, subtitle.Bottom + 28);
        }

        private static Label MakeSubtitle(string text, int x, int y) => new Label
        {
            Text = text,
            Font = new Font("Segoe UI", 9f),
            ForeColor = SubtitleColor,
            AutoSize = true,
            Location = new Point(x, y),
            BackColor = Color.Transparent
        };

        /// Rounded card with a 1px border and a soft drop-shadow painted on
        /// the parent panel behind it (native WinForms controls can't cast
        /// their own shadow, so the shadow is drawn on `parent` just before
        /// the card's location, then the card sits on top of it).
        private static void ApplyCardStyle(Panel parent, Panel card, Color? borderColor = null)
        {
            parent.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                var shadowRect = new Rectangle(card.Left + 2, card.Top + 3, card.Width - 1, card.Height - 1);
                using var shadowPath = RoundedPath(shadowRect, 14);
                using var shadowBrush = new SolidBrush(CardShadowColor);
                g.FillPath(shadowBrush, shadowPath);
            };

            card.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using var path = RoundedPath(
                    new Rectangle(0, 0, card.Width - 1, card.Height - 1), 14);
                card.Region = new Region(path);
                using var pen = new Pen(borderColor ?? SidebarDivider, 1f);
                g.DrawPath(pen, path);
            };
        }

        private static Label MakeLabel(string text, int x, int y) => new Label
        {
            Text = text,
            AutoSize = true,
            Location = new Point(x, y),
            Font = ThemeHelper.FontBold,
            ForeColor = ThemeHelper.TextPrimary,
            BackColor = Color.Transparent
        };

        private static TextBox MakeTextBox(Panel parent, int x, int y, int width, string placeholder = "")
        {
            Color inputBorder = Color.FromArgb(209, 213, 219);
            Color focusBorder = Color.FromArgb(0x52, 0xA2, 0x3F);
            Color currentBorder = inputBorder;

            var inputPanel = new Panel
            {
                Location = new Point(x, y),
                Size = new Size(width, 40),
                BackColor = ThemeHelper.CardBg
            };
            parent.Controls.Add(inputPanel);

            void RoundInput(object? s, EventArgs e)
            {
                if (inputPanel.Width <= 0 || inputPanel.Height <= 0) return;
                using var path = RoundedRectPath(new RectangleF(0, 0, inputPanel.Width, inputPanel.Height), 8f);
                inputPanel.Region = new Region(path);
            }
            inputPanel.Resize += RoundInput;
            RoundInput(null, EventArgs.Empty);

            inputPanel.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var path = RoundedRectPath(new RectangleF(0.5f, 0.5f, inputPanel.Width - 1, inputPanel.Height - 1), 8f);
                using var pen = new Pen(currentBorder, 1f);
                e.Graphics.DrawPath(pen, path);
            };

            var txt = new TextBox
            {
                Font = ThemeHelper.FontDefault,
                ForeColor = ThemeHelper.TextPrimary,
                BackColor = ThemeHelper.CardBg,
                BorderStyle = BorderStyle.None,
                Location = new Point(14, 11),
                Size = new Size(inputPanel.Width - 28, 20),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            if (!string.IsNullOrEmpty(placeholder))
                txt.PlaceholderText = placeholder;
            inputPanel.Controls.Add(txt);

            // Green focus ring — same accent as the Change Password fields
            txt.Enter += (s, e) => { currentBorder = focusBorder; inputPanel.Invalidate(); };
            txt.Leave += (s, e) => { currentBorder = inputBorder; inputPanel.Invalidate(); };

            return txt;
        }

        private static void StyleGreenBtn(Button btn)
        {
            btn.BackColor = ThemeHelper.Accent;
            btn.ForeColor = Color.White;
            btn.FlatStyle = FlatStyle.Flat;
            btn.Font = new Font("Segoe UI Semibold", 9f);
            btn.Cursor = Cursors.Hand;
            btn.UseVisualStyleBackColor = false;
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = ThemeHelper.Header;
        }

        private static void StyleGrayBtn(Button btn)
        {
            btn.BackColor = ThemeHelper.IsDarkMode ? Color.FromArgb(50, 54, 62) : Color.FromArgb(243, 244, 246);
            btn.ForeColor = ThemeHelper.TextPrimary;
            btn.FlatStyle = FlatStyle.Flat;
            btn.Font = new Font("Segoe UI", 9f);
            btn.Cursor = Cursors.Hand;
            btn.UseVisualStyleBackColor = false;
            btn.FlatAppearance.BorderSize = 1;
            btn.FlatAppearance.BorderColor = ThemeHelper.InputBorder;
            btn.FlatAppearance.MouseOverBackColor = ThemeHelper.IsDarkMode ? Color.FromArgb(65, 68, 76) : Color.FromArgb(229, 231, 235);
        }

        private static void StyleRedBtn(Button btn)
        {
            btn.BackColor = ThemeHelper.Danger;
            btn.ForeColor = Color.White;
            btn.FlatStyle = FlatStyle.Flat;
            btn.Font = new Font("Segoe UI Semibold", 9f);
            btn.Cursor = Cursors.Hand;
            btn.UseVisualStyleBackColor = false;
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(185, 28, 28);
        }

        private static void StyleDangerOutlineBtn(Button btn)
        {
            btn.BackColor = ThemeHelper.CardBg;
            btn.ForeColor = ThemeHelper.Danger;
            btn.FlatStyle = FlatStyle.Flat;
            btn.Font = new Font("Segoe UI Semibold", 9f);
            btn.Cursor = Cursors.Hand;
            btn.UseVisualStyleBackColor = false;
            btn.FlatAppearance.BorderSize = 1;
            btn.FlatAppearance.BorderColor = Color.FromArgb(252, 165, 165);
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(254, 226, 226);
        }

        private static GraphicsPath RoundedPath(Rectangle r, int radius)
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