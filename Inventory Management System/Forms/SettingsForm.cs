// Forms/SettingsForm.cs
using Inventory_Management_System.Helpers;
using Inventory_Management_System.Models;
using Inventory_Management_System.Services;
using QRCoder;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Printing;
using System.Linq;
using System.Windows.Forms;

namespace Inventory_Management_System.Forms
{
    public partial class SettingsForm : Form
    {
        private AppSetting? _current;
        private List<User>  _users = new();
        private List<Role>  _roles = new();
        private List<string> _categories = new();

        // ── Section panels ─────────────────────────────────────────────
        private Panel  _panelGeneral    = null!;
        private Panel  _panelUsers      = null!;
        private Panel  _panelPassword   = null!;
        private Panel  _panelPrinting   = null!;
        private Panel  _panelCategories = null!;
        private Button _activeNavBtn    = null!;

        // ── Sidebar (distinct styling + slide-in animation) ─────────────
        private Panel _sidebar = null!;
        private System.Windows.Forms.Timer? _sidebarSlideTimer;
        private const int SidebarTargetWidth = 220;

        private static readonly Color SettingsSidebarColor  = Color.FromArgb(27, 51, 36);
        private static readonly Color SettingsSidebarHover  = Color.FromArgb(40, 74, 52);
        private static readonly Color SettingsSidebarActive = Color.FromArgb(52, 105, 61);

        // ── User grid + buttons ────────────────────────────────────────
        private DataGridView dgvUsers      = null!;
        private Button       btnAddUser    = null!;
        private Button       btnEditUser   = null!;
        private Button       btnToggleUser = null!;
        private Button       btnDeleteUser = null!;

        // ── Password fields ────────────────────────────────────────────
        private TextBox txtCurrentPwd = null!;
        private TextBox txtNewPwd     = null!;
        private TextBox txtConfirmPwd = null!;
        private Label   lblPwdStatus  = null!;

        // ── Categories fields ────────────────────────────────────────────
        private TextBox txtNewCategory  = null!;
        private ListBox lstCategories   = null!;
        private Label   lblCategoryMsg  = null!;

        public SettingsForm()
        {
            InitializeComponent();
            BuildUI();
            LoadSettings();
            LoadUsers();
            LoadCategories();

            Load += (s, e) => AnimateSidebarIn();
        }

        // ══════════════════════════════════════════════════════════════
        //  BUILD UI
        // ══════════════════════════════════════════════════════════════
        private void BuildUI()
        {
            BackColor = Color.FromArgb(245, 246, 250);

            // ── Left nav sidebar (distinct shade from main app sidebar) ─
            var sidebar = new Panel
            {
                Dock      = DockStyle.Left,
                Width     = 0,                     // animated in on Load
                BackColor = SettingsSidebarColor
            };
            _sidebar = sidebar;

            var sideTitle = new Label
            {
                Text      = "⚙  Settings",
                Dock      = DockStyle.Top,
                Height    = 56,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding   = new Padding(18, 0, 0, 0),
                Font      = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = ThemeHelper.Header
            };
            // Added to the sidebar AFTER the nav buttons below (see comment there) —
            // this is what pins it to the TOP instead of it rendering as a fake
            // 6th nav item under Categories.

            // ── Content area ───────────────────────────────────────────
            var content = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = Color.FromArgb(245, 246, 250)
            };

            // Build all section panels
            _panelGeneral    = BuildGeneralPanel();
            _panelUsers      = BuildUsersPanel();
            _panelPassword   = BuildPasswordPanel();
            _panelPrinting   = BuildPrintingPanel();
            _panelCategories = BuildCategoriesPanel();

            foreach (var p in new[] { _panelGeneral, _panelUsers, _panelPassword, _panelPrinting, _panelCategories })
            {
                p.Dock    = DockStyle.Fill;
                p.Visible = false;
                content.Controls.Add(p);
            }

            // Nav button definitions
            var navDefs = new[]
            {
                ("⊞  General",         _panelGeneral),
                ("👥  Users",           _panelUsers),
                ("🔒  Change Password", _panelPassword),
                ("🖨  Printing",        _panelPrinting),
                ("🏷  Categories",      _panelCategories),
            };

            var navBtns = new List<Button>();
            foreach (var (label, panel) in navDefs)
            {
                var capturedPanel = panel;
                var btn = new Button
                {
                    Text      = label,
                    Dock      = DockStyle.Top,
                    Height    = 46,
                    FlatStyle = FlatStyle.Flat,
                    TextAlign = ContentAlignment.MiddleLeft,
                    Padding   = new Padding(18, 0, 0, 0),
                    Font      = ThemeHelper.FontDefault,
                    ForeColor = ThemeHelper.SidebarText,
                    BackColor = SettingsSidebarColor,
                    Cursor    = Cursors.Hand,
                    UseVisualStyleBackColor = false
                };
                btn.FlatAppearance.BorderSize         = 0;
                btn.FlatAppearance.MouseOverBackColor = SettingsSidebarHover;

                var capturedBtn = btn;
                btn.Click += (s, e) => ShowSection(capturedPanel, capturedBtn, navBtns);
                navBtns.Add(btn);
            }

            // Add nav buttons in reverse for DockStyle.Top stacking
            for (int i = navBtns.Count - 1; i >= 0; i--)
                sidebar.Controls.Add(navBtns[i]);

            // Add the title LAST so it docks FIRST (top of the stack).
            sidebar.Controls.Add(sideTitle);

            Controls.Add(content);
            Controls.Add(sidebar);

            // Activate General by default
            _activeNavBtn = navBtns[0];
            ShowSection(_panelGeneral, navBtns[0], navBtns);
        }

        private void ShowSection(Panel panel, Button btn, List<Button> allBtns)
        {
            foreach (var p in new[] { _panelGeneral, _panelUsers, _panelPassword, _panelPrinting, _panelCategories })
                p.Visible = false;
            panel.Visible = true;

            foreach (var b in allBtns)
            {
                b.BackColor = SettingsSidebarColor;
                b.ForeColor = ThemeHelper.SidebarText;
                b.Font      = ThemeHelper.FontDefault;
            }
            btn.BackColor = SettingsSidebarActive;
            btn.ForeColor = Color.White;
            btn.Font      = ThemeHelper.FontSidebarB;
            _activeNavBtn = btn;

            if (panel == _panelUsers)      LoadUsers();
            if (panel == _panelCategories) LoadCategories();
        }

        // ══════════════════════════════════════════════════════════════
        //  SIDEBAR SLIDE-IN ANIMATION
        // ══════════════════════════════════════════════════════════════
        private void AnimateSidebarIn()
        {
            const int step = 22;
            _sidebarSlideTimer = new System.Windows.Forms.Timer { Interval = 8 };
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
            };
            _sidebarSlideTimer.Start();
        }

        // ══════════════════════════════════════════════════════════════
        //  GENERAL PANEL
        // ══════════════════════════════════════════════════════════════
        private Panel BuildGeneralPanel()
        {
            var outer = new Panel
            {
                BackColor  = Color.FromArgb(245, 246, 250),
                AutoScroll = true
            };

            var title = new Label
            {
                Text      = "⊞  General Settings",
                Font      = new Font("Segoe UI", 14f, FontStyle.Bold),
                ForeColor = ThemeHelper.TextPrimary,
                AutoSize  = true,
                Location  = new Point(32, 28)
            };
            outer.Controls.Add(title);

            var card = new Panel
            {
                Location  = new Point(32, 72),
                Size      = new Size(560, 220),
                BackColor = Color.White,
                Padding   = new Padding(0)
            };
            ApplyCardStyle(card);
            outer.Controls.Add(card);

            var lblCo = new Label
            {
                Text      = "Company Name",
                Font      = ThemeHelper.FontBold,
                ForeColor = ThemeHelper.TextPrimary,
                AutoSize  = true,
                Location  = new Point(28, 28),
                BackColor = Color.Transparent
            };
            card.Controls.Add(lblCo);

            txtCompany = new TextBox
            {
                Location = new Point(28, 54),
                Size     = new Size(490, 28)
            };
            ThemeHelper.StyleTextBox(txtCompany);
            card.Controls.Add(txtCompany);

            btnSave = new Button
            {
                Text     = "💾  Save",
                Location = new Point(28, 110),
                Size     = new Size(150, 36)
            };
            StyleGreenBtn(btnSave);
            btnSave.Click += btnSave_Click;
            card.Controls.Add(btnSave);

            btnReset = new Button
            {
                Text     = "↺  Reset",
                Location = new Point(192, 110),
                Size     = new Size(150, 36)
            };
            StyleGrayBtn(btnReset);
            btnReset.Click += btnReset_Click;
            card.Controls.Add(btnReset);

            return outer;
        }

        // ══════════════════════════════════════════════════════════════
        //  USERS PANEL
        // ══════════════════════════════════════════════════════════════
        private Panel BuildUsersPanel()
        {
            var outer = new Panel
            {
                BackColor = Color.FromArgb(245, 246, 250)
            };

            var title = new Label
            {
                Text      = "👥  User Management",
                Font      = new Font("Segoe UI", 14f, FontStyle.Bold),
                ForeColor = ThemeHelper.TextPrimary,
                AutoSize  = true,
                Location  = new Point(28, 28)
            };
            outer.Controls.Add(title);

            var toolbar = new Panel
            {
                Location  = new Point(28, 72),
                Size      = new Size(1000, 50),
                BackColor = Color.Transparent
            };

            btnAddUser = new Button
            {
                Text     = "➕  Add User",
                Location = new Point(0, 0),
                Size     = new Size(150, 38)
            };
            StyleGreenBtn(btnAddUser);
            btnAddUser.Click += BtnAddUser_Click;

            btnEditUser = new Button
            {
                Text     = "✏  Edit",
                Location = new Point(164, 0),
                Size     = new Size(130, 38)
            };
            StyleGrayBtn(btnEditUser);
            btnEditUser.Click += BtnEditUser_Click;

            btnToggleUser = new Button
            {
                Text     = "🔄  Toggle Active",
                Location = new Point(308, 0),
                Size     = new Size(160, 38)
            };
            StyleGrayBtn(btnToggleUser);
            btnToggleUser.Click += BtnToggleUser_Click;

            btnDeleteUser = new Button
            {
                Text     = "🗑  Delete User",
                Location = new Point(482, 0),
                Size     = new Size(150, 38)
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
                Location            = new Point(28, 136),
                Size                = new Size(1100, 480),
                Anchor              = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
                AllowUserToAddRows  = false,
                ReadOnly            = true,
                SelectionMode       = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect         = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor     = Color.White,
                BorderStyle         = BorderStyle.None
            };
            ThemeHelper.StyleDataGridView(dgvUsers);
            dgvUsers.Columns.Add("ID",       "ID");
            dgvUsers.Columns.Add("Username", "Username");
            dgvUsers.Columns.Add("FullName", "Full Name");
            dgvUsers.Columns.Add("Email",    "Email");
            dgvUsers.Columns.Add("Role",     "Role");
            dgvUsers.Columns.Add("Status",   "Status");
            dgvUsers.Columns["ID"].FillWeight     = 30;
            dgvUsers.Columns["Status"].FillWeight = 60;
            outer.Controls.Add(dgvUsers);

            return outer;
        }

        private void LoadUsers()
        {
            if (dgvUsers == null) return;
            dgvUsers.Rows.Clear();
            try
            {
                using var db  = new InventoryDbContext();
                _users        = db.Users.ToList();
                _roles        = db.Roles.ToList();
                var userRoles = db.UserRoles.ToList();

                foreach (var u in _users)
                {
                    var roleId   = userRoles.FirstOrDefault(ur => ur.UserId == u.Id)?.RoleId;
                    var roleName = _roles.FirstOrDefault(r => r.Id == roleId)?.RoleName ?? "—";
                    bool active  = u.IsActive == true;

                    int idx = dgvUsers.Rows.Add(
                        u.Id, u.Username, u.FullName,
                        u.Email ?? "—", roleName,
                        active ? "Active" : "Inactive");

                    var row = dgvUsers.Rows[idx];
                    row.Cells["Status"].Style.ForeColor = active ? ThemeHelper.Success : ThemeHelper.Danger;
                    row.Cells["Status"].Style.Font      = ThemeHelper.FontBold;
                    if (!active)
                        row.DefaultCellStyle.ForeColor = Color.FromArgb(180, 180, 180);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading users: " + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnAddUser_Click(object? sender, EventArgs e)
        {
            using var form = new AddEditUserForm();
            if (form.ShowDialog() == DialogResult.OK) LoadUsers();
        }

        private void BtnEditUser_Click(object? sender, EventArgs e)
        {
            if (dgvUsers.SelectedRows.Count == 0)
            {
                MessageBox.Show("Please select a user to edit.", "No Selection",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            int id   = Convert.ToInt32(dgvUsers.SelectedRows[0].Cells["ID"].Value);
            var user = _users.FirstOrDefault(u => u.Id == id);
            if (user == null) return;
            using var form = new AddEditUserForm(user);
            if (form.ShowDialog() == DialogResult.OK) LoadUsers();
        }

        private void BtnToggleUser_Click(object? sender, EventArgs e)
        {
            if (dgvUsers.SelectedRows.Count == 0)
            {
                MessageBox.Show("Please select a user.", "No Selection",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            int id   = Convert.ToInt32(dgvUsers.SelectedRows[0].Cells["ID"].Value);
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
                using var db  = new InventoryDbContext();
                var entity    = db.Users.FirstOrDefault(u => u.Id == id);
                if (entity == null) return;
                entity.IsActive = !(user.IsActive == true);
                db.SaveChanges();
                LoadUsers();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnDeleteUser_Click(object? sender, EventArgs e)
        {
            if (dgvUsers.SelectedRows.Count == 0)
            {
                MessageBox.Show("Please select a user to delete.", "No Selection",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            int id   = Convert.ToInt32(dgvUsers.SelectedRows[0].Cells["ID"].Value);
            var user = _users.FirstOrDefault(u => u.Id == id);
            if (user == null) return;

            if (id == AuthService.CurrentUserId)
            {
                MessageBox.Show("You cannot delete your own account.", "Not Allowed",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (MessageBox.Show(
                $"Are you sure you want to PERMANENTLY delete '{user.Username}'?\nThis cannot be undone.",
                "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;

            try
            {
                using var db     = new InventoryDbContext();
                var entity       = db.Users.FirstOrDefault(u => u.Id == id);
                if (entity == null) return;

                var userRoles = db.UserRoles.Where(ur => ur.UserId == id).ToList();
                if (userRoles.Count > 0)
                    db.UserRoles.RemoveRange(userRoles);

                db.Users.Remove(entity);
                db.SaveChanges();
                LoadUsers();

                MessageBox.Show($"'{user.Username}' was deleted.", "Deleted",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Could not delete this user — they may still be referenced elsewhere " +
                    "(allocations, logs, transactions, etc.).\n\n" + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ══════════════════════════════════════════════════════════════
        //  CHANGE PASSWORD PANEL
        // ══════════════════════════════════════════════════════════════
        private Panel BuildPasswordPanel()
        {
            var outer = new Panel
            {
                BackColor  = Color.FromArgb(245, 246, 250),
                AutoScroll = true
            };

            var title = new Label
            {
                Text      = "🔒  Change Password",
                Font      = new Font("Segoe UI", 14f, FontStyle.Bold),
                ForeColor = ThemeHelper.TextPrimary,
                AutoSize  = true,
                Location  = new Point(32, 28)
            };
            outer.Controls.Add(title);

            var card = new Panel
            {
                Location  = new Point(32, 72),
                Size      = new Size(560, 340),
                BackColor = Color.White
            };
            ApplyCardStyle(card);
            outer.Controls.Add(card);

            int y = 28;

            card.Controls.Add(MakeLabel("Current Password", 28, y));
            y += 26;
            txtCurrentPwd = MakeTextBox(card, 28, y, 490);
            txtCurrentPwd.UseSystemPasswordChar = true;
            y += 50;

            card.Controls.Add(MakeLabel("New Password", 28, y));
            y += 26;
            txtNewPwd = MakeTextBox(card, 28, y, 490);
            txtNewPwd.UseSystemPasswordChar = true;
            y += 50;

            card.Controls.Add(MakeLabel("Confirm New Password", 28, y));
            y += 26;
            txtConfirmPwd = MakeTextBox(card, 28, y, 490);
            txtConfirmPwd.UseSystemPasswordChar = true;
            y += 50;

            lblPwdStatus = new Label
            {
                Location  = new Point(28, y),
                AutoSize  = false,
                Size      = new Size(490, 22),
                Font      = ThemeHelper.FontSmall,
                ForeColor = ThemeHelper.Danger,
                BackColor = Color.Transparent,
                Text      = ""
            };
            card.Controls.Add(lblPwdStatus);
            y += 30;

            var btnChange = new Button
            {
                Text     = "🔒  Change Password",
                Location = new Point(28, y),
                Size     = new Size(180, 36)
            };
            StyleGreenBtn(btnChange);
            btnChange.Click += BtnChangePwd_Click;
            card.Controls.Add(btnChange);

            return outer;
        }

        private void BtnChangePwd_Click(object? sender, EventArgs e)
        {
            lblPwdStatus.ForeColor = ThemeHelper.Danger;
            lblPwdStatus.Text      = "";

            string cur  = txtCurrentPwd.Text;
            string nw   = txtNewPwd.Text;
            string conf = txtConfirmPwd.Text;

            if (string.IsNullOrWhiteSpace(cur) || string.IsNullOrWhiteSpace(nw) || string.IsNullOrWhiteSpace(conf))
            { lblPwdStatus.Text = "All fields are required."; return; }

            if (nw != conf)
            { lblPwdStatus.Text = "New password and confirmation do not match."; return; }

            if (nw.Length < 4)
            { lblPwdStatus.Text = "Password must be at least 4 characters."; return; }

            try
            {
                using var db  = new InventoryDbContext();
                var user      = db.Users.FirstOrDefault(u => u.Id == AuthService.CurrentUserId);
                if (user == null) { lblPwdStatus.Text = "User not found."; return; }
                if (user.PasswordHash != cur) { lblPwdStatus.Text = "Current password is incorrect."; return; }

                user.PasswordHash  = nw;
                db.SaveChanges();

                txtCurrentPwd.Text = "";
                txtNewPwd.Text     = "";
                txtConfirmPwd.Text = "";

                lblPwdStatus.ForeColor = ThemeHelper.Success;
                lblPwdStatus.Text      = "✅ Password changed successfully.";
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
                BackColor  = Color.FromArgb(245, 246, 250),
                AutoScroll = true
            };

            var title = new Label
            {
                Text      = "🖨  Barcode Label Printing",
                Font      = new Font("Segoe UI", 14f, FontStyle.Bold),
                ForeColor = ThemeHelper.TextPrimary,
                AutoSize  = true,
                Location  = new Point(32, 28)
            };
            outer.Controls.Add(title);

            var card = new Panel
            {
                Location  = new Point(32, 72),
                Size      = new Size(560, 300),
                BackColor = Color.White
            };
            ApplyCardStyle(card);
            outer.Controls.Add(card);

            int y = 28;

            card.Controls.Add(MakeLabel("Printer Name", 28, y));
            y += 26;
            txtPrinterName = MakeTextBox(card, 28, y, 490);
            txtPrinterName.PlaceholderText = "Leave blank to use default printer";
            y += 50;

            card.Controls.Add(MakeLabel("Label Width (mm)", 28, y));
            card.Controls.Add(MakeLabel("Label Height (mm)", 260, y));
            y += 26;

            txtLabelWidth = new TextBox { Location = new Point(28, y), Size = new Size(200, 28) };
            ThemeHelper.StyleTextBox(txtLabelWidth);
            txtLabelWidth.Text = "62";
            card.Controls.Add(txtLabelWidth);

            txtLabelHeight = new TextBox { Location = new Point(260, y), Size = new Size(200, 28) };
            ThemeHelper.StyleTextBox(txtLabelHeight);
            txtLabelHeight.Text = "29";
            card.Controls.Add(txtLabelHeight);
            y += 54;

            btnTestPrint = new Button
            {
                Text     = "🖨  Test Print Label",
                Location = new Point(28, y),
                Size     = new Size(168, 36)
            };
            StyleGrayBtn(btnTestPrint);
            btnTestPrint.Click += btnTestPrint_Click;
            card.Controls.Add(btnTestPrint);

            btnSave = new Button
            {
                Text     = "💾  Save",
                Location = new Point(210, y),
                Size     = new Size(150, 36)
            };
            StyleGreenBtn(btnSave);
            btnSave.Click += btnSave_Click;
            card.Controls.Add(btnSave);

            return outer;
        }

        // ══════════════════════════════════════════════════════════════
        //  CATEGORIES PANEL  — now backed by CategoryStore (shared)
        // ══════════════════════════════════════════════════════════════
        private Panel BuildCategoriesPanel()
        {
            var outer = new Panel
            {
                BackColor  = Color.FromArgb(245, 246, 250),
                AutoScroll = true
            };

            var title = new Label
            {
                Text      = "🏷  Manage Categories",
                Font      = new Font("Segoe UI", 14f, FontStyle.Bold),
                ForeColor = ThemeHelper.TextPrimary,
                AutoSize  = true,
                Location  = new Point(32, 28)
            };
            outer.Controls.Add(title);

            var card = new Panel
            {
                Location  = new Point(32, 72),
                Size      = new Size(560, 420),
                BackColor = Color.White
            };
            ApplyCardStyle(card);
            outer.Controls.Add(card);

            card.Controls.Add(MakeLabel("New Category Name", 28, 24));

            txtNewCategory = new TextBox { Location = new Point(28, 50), Size = new Size(360, 28) };
            ThemeHelper.StyleTextBox(txtNewCategory);
            txtNewCategory.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    BtnAddCategory_Click(s, EventArgs.Empty);
                }
            };
            card.Controls.Add(txtNewCategory);

            var btnAdd = new Button
            {
                Text     = "➕  Add",
                Location = new Point(398, 49),
                Size     = new Size(120, 30)
            };
            StyleGreenBtn(btnAdd);
            btnAdd.Click += BtnAddCategory_Click;
            card.Controls.Add(btnAdd);

            lblCategoryMsg = new Label
            {
                Location  = new Point(28, 86),
                AutoSize  = false,
                Size      = new Size(490, 20),
                Font      = ThemeHelper.FontSmall,
                ForeColor = ThemeHelper.Danger,
                BackColor = Color.Transparent,
                Text      = ""
            };
            card.Controls.Add(lblCategoryMsg);

            card.Controls.Add(MakeLabel("Existing Categories", 28, 116));

          lstCategories = new ListBox
            {
                Location    = new Point(28, 142),
                Size        = new Size(490, 200),
                Font        = ThemeHelper.FontDefault,
                BorderStyle = BorderStyle.FixedSingle,
                DrawMode    = DrawMode.OwnerDrawFixed,
                ItemHeight  = 30,
                IntegralHeight = false
            };
            lstCategories.DrawItem += LstCategories_DrawItem;
            card.Controls.Add(lstCategories);

            var btnDelete = new Button
            {
                Text     = "🗑  Delete Selected",
                Location = new Point(28, 354),
                Size     = new Size(170, 34)
            };
            StyleGrayBtn(btnDelete);
            btnDelete.Click += BtnDeleteCategory_Click;
            card.Controls.Add(btnDelete);

            return outer;
        }

        private void LoadCategories()
        {
            _categories = CategoryStore.GetAll();

            if (lstCategories == null) return;
            lstCategories.Items.Clear();
            foreach (var c in _categories)
                lstCategories.Items.Add(c);
        }

        private void BtnAddCategory_Click(object? sender, EventArgs e)
        {
            lblCategoryMsg.Text = "";
            string name = txtNewCategory.Text.Trim();

            if (!CategoryStore.Add(name, out string? error))
            {
                lblCategoryMsg.ForeColor = ThemeHelper.Danger;
                lblCategoryMsg.Text      = error ?? "Could not add category.";
                return;
            }

            txtNewCategory.Text = "";
            lblCategoryMsg.ForeColor = ThemeHelper.Success;
            lblCategoryMsg.Text      = $"✅ '{name}' added.";
            LoadCategories();
        }

        private void BtnDeleteCategory_Click(object? sender, EventArgs e)
        {
            if (lstCategories.SelectedItem == null)
            {
                lblCategoryMsg.ForeColor = ThemeHelper.Danger;
                lblCategoryMsg.Text      = "Select a category to delete.";
                return;
            }

            string selected = lstCategories.SelectedItem.ToString()!;
            if (MessageBox.Show($"Delete category '{selected}'?", "Confirm",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

            CategoryStore.Delete(selected);
            lblCategoryMsg.ForeColor = ThemeHelper.Success;
            lblCategoryMsg.Text      = $"🗑 '{selected}' deleted.";
            LoadCategories();
        }
        private void LstCategories_DrawItem(object? sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;

            bool selected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;

            using var bg = new SolidBrush(selected ? ThemeHelper.Accent : Color.White);
            e.Graphics.FillRectangle(bg, e.Bounds);

            string text = lstCategories.Items[e.Index]?.ToString() ?? "";

            using var numFont = new Font("Segoe UI", 9f, FontStyle.Bold);
            using var numBrush = new SolidBrush(selected ? Color.White : ThemeHelper.TextSecondary);
            e.Graphics.DrawString($"{e.Index + 1}.", numFont, numBrush, e.Bounds.X + 10, e.Bounds.Y + 6);

            using var txtBrush = new SolidBrush(selected ? Color.White : ThemeHelper.TextPrimary);
            e.Graphics.DrawString(text, ThemeHelper.FontDefault, txtBrush, e.Bounds.X + 40, e.Bounds.Y + 6);

            using var linePen = new Pen(Color.FromArgb(230, 230, 230), 1f);
            e.Graphics.DrawLine(linePen, e.Bounds.X, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1);

            e.DrawFocusRectangle();
        }

        // ══════════════════════════════════════════════════════════════
        //  LOAD / SAVE SETTINGS
        // ══════════════════════════════════════════════════════════════
        private void LoadSettings()
        {
            try
            {
                using var db = new InventoryDbContext();
                _current     = db.AppSettings.FirstOrDefault();

                if (_current == null)
                {
                    _current = new AppSetting
                    {
                        CompanyName   = "Q-Inventory",
                        Theme         = "Light",
                        DbServer      = "localhost",
                        DbName        = "inventory_management",
                        PrinterName   = "",
                        LabelWidthMm  = 62,
                        LabelHeightMm = 29
                    };
                    db.AppSettings.Add(_current);
                    db.SaveChanges();
                }

                txtCompany.Text     = _current.CompanyName;
                txtPrinterName.Text = _current.PrinterName ?? "";
                txtLabelWidth.Text  = (_current.LabelWidthMm  ?? 62).ToString();
                txtLabelHeight.Text = (_current.LabelHeightMm ?? 29).ToString();
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

            if (!int.TryParse(txtLabelWidth.Text,  out int lw) || lw <= 0 ||
                !int.TryParse(txtLabelHeight.Text, out int lh) || lh <= 0)
            {
                MessageBox.Show("Label width and height must be positive numbers.", "Validation",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                using var db = new InventoryDbContext();
                var entity   = db.AppSettings.FirstOrDefault() ?? new AppSetting();
                bool isNew   = entity.Id == 0;

                entity.CompanyName   = txtCompany.Text.Trim();
                entity.Theme         = "Light";
                entity.PrinterName   = txtPrinterName.Text.Trim();
                entity.LabelWidthMm  = lw;
                entity.LabelHeightMm = lh;
                entity.UpdatedAt     = DateTime.Now;

                if (isNew) db.AppSettings.Add(entity);
                db.SaveChanges();
                _current = entity;

                MessageBox.Show("Settings saved successfully.", "Saved",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
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
            txtCompany.Text     = _current.CompanyName;
            txtPrinterName.Text = _current.PrinterName ?? "";
            txtLabelWidth.Text  = (_current.LabelWidthMm  ?? 62).ToString();
            txtLabelHeight.Text = (_current.LabelHeightMm ?? 29).ToString();
        }

        private void btnTestPrint_Click(object? sender, EventArgs e)
        {
            if (!int.TryParse(txtLabelWidth.Text,  out int lw) || lw <= 0) lw = 62;
            if (!int.TryParse(txtLabelHeight.Text, out int lh) || lh <= 0) lh = 29;

            string printerName = txtPrinterName.Text.Trim();
            string companyName = txtCompany.Text.Trim().Length > 0
                ? txtCompany.Text.Trim() : "Q-Inventory";

            try
            {
                var gen  = new QRCodeGenerator();
                var data = gen.CreateQrCode("TEST-LABEL-001", QRCodeGenerator.ECCLevel.Q);
                var qr   = new QRCode(data);
                using var bmp = qr.GetGraphic(4);

                var pd = new PrintDocument();
                if (!string.IsNullOrWhiteSpace(printerName))
                    pd.PrinterSettings.PrinterName = printerName;

                pd.DefaultPageSettings.PaperSize =
                    new PaperSize("Label", (int)(lw * 3.937), (int)(lh * 3.937));
                pd.DefaultPageSettings.Margins = new Margins(10, 10, 10, 10);

                pd.PrintPage += (s, ev) =>
                {
                    var g      = ev.Graphics!;
                    var bounds = ev.PageBounds;
                    int qrSz   = Math.Min(bounds.Height - 20, 80);
                    g.DrawImage(bmp, new Rectangle(
                        bounds.X + 5, bounds.Y + (bounds.Height - qrSz) / 2, qrSz, qrSz));
                    int tx = bounds.X + qrSz + 12;
                    int tw = bounds.Width - qrSz - 20;
                    using var bf = new Font("Segoe UI", 8f, FontStyle.Bold);
                    using var sf = new Font("Segoe UI", 6.5f);
                    g.DrawString(companyName,      bf, Brushes.Black,
                        new RectangleF(tx, bounds.Y + 6,  tw, 18));
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
                    Document      = pd,
                    Width         = 600,
                    Height        = 400,
                    StartPosition = FormStartPosition.CenterParent,
                    Text          = "Label Preview"
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
        private static void ApplyCardStyle(Panel card)
        {
            card.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using var path = RoundedPath(
                    new Rectangle(0, 0, card.Width - 1, card.Height - 1), 12);
                card.Region = new Region(path);
                using var pen = new Pen(ThemeHelper.CardBorder, 1f);
                g.DrawPath(pen, path);
            };
        }

        private static Label MakeLabel(string text, int x, int y) => new Label
        {
            Text      = text,
            AutoSize  = true,
            Location  = new Point(x, y),
            Font      = ThemeHelper.FontBold,
            ForeColor = ThemeHelper.TextPrimary,
            BackColor = Color.Transparent
        };

        private static TextBox MakeTextBox(Panel parent, int x, int y, int width)
        {
            var txt = new TextBox { Location = new Point(x, y), Size = new Size(width, 28) };
            ThemeHelper.StyleTextBox(txt);
            parent.Controls.Add(txt);
            return txt;
        }

        private static void StyleGreenBtn(Button btn)
        {
            btn.BackColor = ThemeHelper.Accent;
            btn.ForeColor = Color.White;
            btn.FlatStyle = FlatStyle.Flat;
            btn.Font      = ThemeHelper.FontBold;
            btn.Cursor    = Cursors.Hand;
            btn.UseVisualStyleBackColor = false;
            btn.FlatAppearance.BorderSize         = 0;
            btn.FlatAppearance.MouseOverBackColor = ThemeHelper.Header;
        }

        private static void StyleGrayBtn(Button btn)
        {
            btn.BackColor = Color.FromArgb(243, 244, 246);
            btn.ForeColor = ThemeHelper.TextPrimary;
            btn.FlatStyle = FlatStyle.Flat;
            btn.Font      = ThemeHelper.FontDefault;
            btn.Cursor    = Cursors.Hand;
            btn.UseVisualStyleBackColor = false;
            btn.FlatAppearance.BorderSize  = 1;
            btn.FlatAppearance.BorderColor = ThemeHelper.InputBorder;
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(229, 231, 235);
        }

        private static void StyleRedBtn(Button btn)
        {
            btn.BackColor = ThemeHelper.Danger;
            btn.ForeColor = Color.White;
            btn.FlatStyle = FlatStyle.Flat;
            btn.Font      = ThemeHelper.FontBold;
            btn.Cursor    = Cursors.Hand;
            btn.UseVisualStyleBackColor = false;
            btn.FlatAppearance.BorderSize         = 0;
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(185, 28, 28);
        }

        private static GraphicsPath RoundedPath(Rectangle r, int radius)
        {
            var path = new GraphicsPath();
            int d    = radius * 2;
            path.AddArc(r.X,         r.Y,          d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y,          d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0,   90);
            path.AddArc(r.X,         r.Bottom - d, d, d, 90,  90);
            path.CloseFigure();
            return path;
        }
    }
}