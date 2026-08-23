// Forms/LoginForm.cs
using ClosedXML.Excel;
using Inventory_Management_System.Helpers;
using Inventory_Management_System.Models;
using Inventory_Management_System.Services;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace Inventory_Management_System.Forms
{
    public partial class LoginForm : Form
    {
        private System.Windows.Forms.Timer _animTimer = null!;
        private float _tick = 0f;
        private Image? _personImage;

        private static readonly Color GreenDark = Color.FromArgb(27, 94, 32);
        private static readonly Color GreenMid = Color.FromArgb(46, 125, 50);
        private static readonly Color GreenLight = Color.FromArgb(76, 175, 80);
        private static readonly Color GreenPale = Color.FromArgb(200, 230, 201);

        private readonly List<Bubble> _bubbles = new();
        private readonly Random _rng = new();
        private float _introProgress = 0f;
        private float _waveOffset = 0f;

        private class Bubble
        {
            public float X, Speed, Phase, Size, Opacity, Wobble;
            public float Y;
            public bool Foreground;
        }

        private bool _keepMeLoggedIn = false;

        // ── Sign-in button state ─────────────────────────────────────────
        private enum LoginBtnState { Normal, Hover, Pressed, Loading }
        private LoginBtnState _loginBtnState = LoginBtnState.Normal;
        private float _loginBtnHoverT = 0f;
        private System.Windows.Forms.Timer? _loginBtnAnimTimer;
        private System.Windows.Forms.Timer? _loginBtnSpinTimer;
        private float _loginBtnSpinAngle = 0f;

        // ── Slide-up animation state ─────────────────────────────────────
        private float _slideProgress = 0f;
        private System.Windows.Forms.Timer? _slideTimer;
        private int _slideStartY;

        // ── Shake animation state ─────────────────────────────────────────
        private System.Windows.Forms.Timer? _shakeTimer;
        private int _shakeStep;
        private Control? _shakeTarget;
        private int _shakeBaseX;

        // ── Toast notification state ──────────────────────────────────────
        private Panel? _toastPanel;
        private System.Windows.Forms.Timer? _toastTimer;

        public LoginForm()
        {
            InitializeComponent();
            ApplyTheme();
            LoadPersonImage();
            SetupAnimation();
            LoadRememberedUser();
        }

        private void ApplyTheme()
        {
            lblError.ForeColor = ThemeHelper.Danger;

            panelLeft.BackColor = GreenDark;
            panelAnim.BackColor = GreenDark;

            typeof(Panel).GetProperty("DoubleBuffered",
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic)
                ?.SetValue(panelAnim, true);

            panelAnim.Paint += PanelAnim_Paint;
            SetupLoginPillButton();

            // ── Toggle cursor feedback ──────────────────────────────────
            pnlToggle.Cursor = Cursors.Hand;
            lblToggleLabel.Cursor = Cursors.Hand;

            // ── Sign-in button interactivity ────────────────────────────
            btnLogin.Cursor = Cursors.Hand;
            btnLogin.MouseEnter += (s, e) =>
            {
                if (_loginBtnState != LoginBtnState.Loading)
                    AnimateLoginBtnTo(LoginBtnState.Hover);
            };
            btnLogin.MouseLeave += (s, e) =>
            {
                if (_loginBtnState != LoginBtnState.Loading)
                    AnimateLoginBtnTo(LoginBtnState.Normal);
            };
            btnLogin.MouseDown += (s, e) =>
            {
                if (_loginBtnState != LoginBtnState.Loading)
                {
                    _loginBtnState = LoginBtnState.Pressed;
                    btnLogin.Invalidate();
                }
            };
            btnLogin.MouseUp += (s, e) =>
            {
                if (_loginBtnState != LoginBtnState.Loading)
                {
                    _loginBtnState = LoginBtnState.Hover;
                    btnLogin.Invalidate();
                }
            };
        }

        private void AnimateLoginBtnTo(LoginBtnState target)
        {
            _loginBtnState = target;
            _loginBtnAnimTimer?.Stop();
            _loginBtnAnimTimer?.Dispose();
            _loginBtnAnimTimer = new System.Windows.Forms.Timer { Interval = 12 };
            _loginBtnAnimTimer.Tick += (s, e) =>
            {
                float dest = (target == LoginBtnState.Hover || target == LoginBtnState.Pressed) ? 1f : 0f;
                _loginBtnHoverT += (dest - _loginBtnHoverT) * 0.25f;
                if (Math.Abs(_loginBtnHoverT - dest) < 0.01f)
                {
                    _loginBtnHoverT = dest;
                    _loginBtnAnimTimer?.Stop();
                    _loginBtnAnimTimer?.Dispose();
                    _loginBtnAnimTimer = null;
                }
                btnLogin.Invalidate();
            };
            _loginBtnAnimTimer.Start();
        }

        private void SetLoginBtnLoading(bool loading)
        {
            if (loading)
            {
                _loginBtnState = LoginBtnState.Loading;
                btnLogin.Enabled = false;
                _loginBtnSpinTimer?.Stop();
                _loginBtnSpinTimer?.Dispose();
                _loginBtnSpinTimer = new System.Windows.Forms.Timer { Interval = 16 };
                _loginBtnSpinTimer.Tick += (s, e) =>
                {
                    _loginBtnSpinAngle = (_loginBtnSpinAngle + 8f) % 360f;
                    btnLogin.Invalidate();
                };
                _loginBtnSpinTimer.Start();
            }
            else
            {
                _loginBtnSpinTimer?.Stop();
                _loginBtnSpinTimer?.Dispose();
                _loginBtnSpinTimer = null;
                btnLogin.Enabled = true;
                _loginBtnState = LoginBtnState.Normal;
                _loginBtnHoverT = 0f;
                btnLogin.Invalidate();
            }
        }

        private static Region MakeRoundedRegion(int w, int h, int r)
        {
            var path = new GraphicsPath();
            int d = r * 2;
            path.AddArc(0, 0, d, d, 180, 90);
            path.AddArc(w - d, 0, d, d, 270, 90);
            path.AddArc(w - d, h - d, d, d, 0, 90);
            path.AddArc(0, h - d, d, d, 90, 90);
            path.CloseFigure();
            return new Region(path);
        }

        private void SetupLoginPillButton()
        {
            // Match the input fields exactly: same height, same corner radius
            int fieldH = panelUsername.Height;
            int fieldRadius = (panelUsername is Helpers.RoundedPanel rpField) ? rpField.CornerRadius : 10;

            btnLogin.Size = new Size(panelUsername.Width, fieldH);
            btnLogin.Left = panelUsername.Left;
            btnLogin.Top = pnlToggle.Bottom + 46; // extra gap so the link above doesn't crowd the button
            btnLogin.Region = MakeRoundedRegion(btnLogin.Width, btnLogin.Height, fieldRadius);
            btnLogin.Resize += (s, e) =>
                btnLogin.Region = MakeRoundedRegion(btnLogin.Width, btnLogin.Height, fieldRadius);

            SetupCreateAccountLink();
        }
        private Panel? _signUpPanel;
        private readonly Dictionary<Control, Point> _signUpOriginalPositions = new();

        private void ShowSignUpPanel()
        {
            if (_signUpPanel == null)
                _signUpPanel = BuildSignUpPanel();

            RecenterSignUpPanel();
            _signUpPanel.BringToFront();
            _signUpPanel.Visible = true;
            this.Text = "Q-Inventory — Create account";
            AnimateSignUpPanelIn(_signUpPanel);
        }

        private void AnimateSignUpPanelIn(Panel panel)
        {
            int startY = 40;
            int targetY = 0;
            panel.Location = new Point(0, startY);

            float progress = 0f;
            var timer = new System.Windows.Forms.Timer { Interval = 12 };
            timer.Tick += (s, e) =>
            {
                progress = Math.Min(1f, progress + 0.045f);
                float eased = EaseOutCubic(progress);
                int newY = (int)(startY + (targetY - startY) * eased);
                panel.Location = new Point(0, newY);

                if (progress >= 1f)
                {
                    timer.Stop();
                    timer.Dispose();
                    panel.Location = new Point(0, targetY);
                }
            };
            timer.Start();
        }

        private void HideSignUpPanel()
        {
            // Reset the login card to its "below" starting position while the
            // sign-up panel still covers it, so hiding it reveals the slide-up.
            int targetY = (panelRight.Height - panelCard.Height) / 2;
            panelCard.Location = new Point(panelCard.Left, targetY + 60);

            if (_signUpPanel != null)
                _signUpPanel.Visible = false;
            this.Text = "Q-Inventory — Login";
            StartSlideUpAnimation();
        }

        private Panel BuildSignUpPanel()
        {
            panelRight.Resize += (s, e) => { if (_signUpPanel != null && _signUpPanel.Visible) RecenterSignUpPanel(); };
            // Fully covers panelRight (title, fields, everything) so we don't
            // need to know the exact names/positions of the login controls —
            // it just sits on top of them and swaps back out on demand.
            var panel = new Panel
            {
                BackColor = Color.White,
                Location = new Point(0, 0),
                Size = panelRight.ClientSize,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                AutoScroll = true,
                Visible = false
            };
            panelRight.Controls.Add(panel);

            int contentLeft = panelUsername.Left;
            int contentWidth = panelUsername.Width;
            int y = 48;

            var lblTitle = new Label
            {
                Text = "Create your account",
                Font = new Font("Segoe UI", 22f, FontStyle.Bold),
                ForeColor = Color.FromArgb(17, 24, 39),
                AutoSize = true,
                Left = contentLeft,
                Top = y
            };
            panel.Controls.Add(lblTitle);
            y += lblTitle.PreferredHeight + 6;

            var lblSubtitle = new Label
            {
                Text = "Set up Q-Inventory in a couple of minutes",
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = Color.FromArgb(107, 114, 128),
                AutoSize = true,
                Left = contentLeft,
                Top = y
            };
            panel.Controls.Add(lblSubtitle);
            y += lblSubtitle.PreferredHeight + 32;

            // ── Full name ────────────────────────────────────────────
            y = AddSignUpField(panel, "Full name", contentLeft, contentWidth, y, out var txtFullName, isPassword: false);

            // ── Work email ───────────────────────────────────────────
            y = AddSignUpField(panel, "Work email", contentLeft, contentWidth, y, out var txtEmail, isPassword: false);

            // ── Password ─────────────────────────────────────────────
            y = AddSignUpField(panel, "Password", contentLeft, contentWidth, y, out var txtPassword, isPassword: true);
            y += 8;

            // ── Strength bars ────────────────────────────────────────
            var strengthBars = new Panel[4];
            int barGap = 6;
            int barWidth = (contentWidth - barGap * 3) / 4;
            for (int i = 0; i < 4; i++)
            {
                var bar = new Panel
                {
                    Width = barWidth,
                    Height = 4,
                    Left = contentLeft + i * (barWidth + barGap),
                    Top = y,
                    BackColor = Color.FromArgb(229, 231, 235)
                };
                panel.Controls.Add(bar);
                strengthBars[i] = bar;
            }
            txtPassword.TextChanged += (s, e) =>
            {
                int len = txtPassword.Text.Length;
                int filled = len == 0 ? 0 : len < 4 ? 1 : len < 7 ? 2 : len < 10 ? 3 : 4;
                for (int i = 0; i < strengthBars.Length; i++)
                    strengthBars[i].BackColor = i < filled ? GreenMid : Color.FromArgb(229, 231, 235);
            };
            y += 24;

            // ── Terms checkbox (measure wrapped height BEFORE using it) ──
            bool termsAccepted = false;
            var chkTerms = new Panel
            {
                Width = 18,
                Height = 18,
                Left = contentLeft,
                Top = y + 1,
                BackColor = Color.FromArgb(229, 231, 235),
                Cursor = Cursors.Hand
            };

            chkTerms.Region = MakeRoundedRegion(18, 18, 5);

            var lblTerms = new Label
            {
                Text = "I agree to the Terms of Service and Privacy Policy",
                Font = new Font("Segoe UI", 9f),
                ForeColor = Color.FromArgb(75, 85, 99),
                AutoSize = true,
                MaximumSize = new Size(contentWidth - 28, 0),
                Left = contentLeft + 28,
                Top = y
            };
            panel.Controls.Add(lblTerms);
            panel.Controls.Add(chkTerms);

            chkTerms.Paint += (s, e) =>
            {
                if (!termsAccepted) return;
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using var pen = new Pen(Color.White, 2f) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };
                g.DrawLines(pen, new[] { new Point(4, 9), new Point(7, 12), new Point(14, 5) });
            };
            chkTerms.Click += (s, e) =>
            {
                termsAccepted = !termsAccepted;
                chkTerms.BackColor = termsAccepted ? GreenMid : Color.FromArgb(229, 231, 235);
                chkTerms.Invalidate();
            };

            // Use whichever is taller — the checkbox or the wrapped label —
            // so nothing below it overlaps.
            y += Math.Max(chkTerms.Height, lblTerms.PreferredHeight) + 24;

            // ── Create account button ────────────────────────────────
            int fieldRadius = (panelUsername is Helpers.RoundedPanel rpF) ? rpF.CornerRadius : 10;
            var btnCreate = new Button
            {
                FlatStyle = FlatStyle.Flat,
                Left = contentLeft,
                Top = y,
                Width = contentWidth,
                Height = panelUsername.Height,
                Cursor = Cursors.Hand,
                BackColor = GreenMid
            };
            btnCreate.FlatAppearance.BorderSize = 0;
            btnCreate.Region = MakeRoundedRegion(btnCreate.Width, btnCreate.Height, fieldRadius);
            btnCreate.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                var rect = new RectangleF(0, 0, btnCreate.Width, btnCreate.Height);
                using var path = RoundRect(new Rectangle(0, 0, btnCreate.Width, btnCreate.Height), fieldRadius);
                using var grad = new LinearGradientBrush(rect, GreenMid, GreenDark, LinearGradientMode.Vertical);
                g.FillPath(grad, path);
                DrawLabelWithArrow(g, rect, "Create account");
            };
            btnCreate.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtFullName.Text)) { ShowToast("Please enter your full name."); return; }
                if (string.IsNullOrWhiteSpace(txtEmail.Text) || !txtEmail.Text.Contains("@")) { ShowToast("Please enter a valid email address."); return; }
                if (string.IsNullOrWhiteSpace(txtPassword.Text)) { ShowToast("Please choose a password."); return; }
                if (!termsAccepted) { ShowToast("Please accept the Terms of Service to continue."); return; }
                ShowToast("This feature is coming soon in the next update!", isError: false);
            };
            panel.Controls.Add(btnCreate);
            y += btnCreate.Height + 24;

            // ── Divider ──────────────────────────────────────────────
            var lblOr = new Label { Text = "or continue with", Font = new Font("Segoe UI", 8.5f), ForeColor = Color.FromArgb(156, 163, 175), AutoSize = true, Top = y };
            panel.Controls.Add(lblOr);
            lblOr.Left = contentLeft + (contentWidth - lblOr.PreferredWidth) / 2;
            var lineLeft = new Panel { Width = lblOr.Left - contentLeft - 10, Height = 1, Left = contentLeft, Top = y + lblOr.PreferredHeight / 2, BackColor = Color.FromArgb(229, 231, 235) };
            var lineRight = new Panel { Width = contentLeft + contentWidth - (lblOr.Left + lblOr.PreferredWidth + 10), Height = 1, Left = lblOr.Left + lblOr.PreferredWidth + 10, Top = y + lblOr.PreferredHeight / 2, BackColor = Color.FromArgb(229, 231, 235) };
            panel.Controls.Add(lineLeft);
            panel.Controls.Add(lineRight);
            y += lblOr.PreferredHeight + 20;

            // ── SSO buttons ──────────────────────────────────────────
            int ssoGap = 12;
            int ssoWidth = (contentWidth - ssoGap) / 2;
            var btnGoogle = MakeSsoButtonLocal("Google", contentLeft, y, ssoWidth, fieldRadius);
            var btnMicrosoft = MakeSsoButtonLocal("Microsoft", contentLeft + ssoWidth + ssoGap, y, ssoWidth, fieldRadius);
            btnGoogle.Click += (s, e) => ShowToast("This feature is coming soon in the next update!", isError: false);
            btnMicrosoft.Click += (s, e) => ShowToast("This feature is coming soon in the next update!", isError: false);
            panel.Controls.Add(btnGoogle);
            panel.Controls.Add(btnMicrosoft);
            y += 42 + 28;

            // ── Sign in link ─────────────────────────────────────────
            var lblSignInPrompt = new Label
            {
                Text = "Already have an account? ",
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = Color.FromArgb(107, 114, 128),
                AutoSize = true,
                Top = y
            };
            var lblSignInLink = new Label
            {
                Text = "Sign in",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = GreenMid,
                AutoSize = true,
                Cursor = Cursors.Hand,
                Top = y
            };
            int signInTotalWidth = lblSignInPrompt.PreferredWidth + lblSignInLink.PreferredWidth;
            int signInStartX = contentLeft + (contentWidth - signInTotalWidth) / 2;
            lblSignInPrompt.Left = signInStartX;
            lblSignInLink.Left = signInStartX + lblSignInPrompt.PreferredWidth;
            lblSignInLink.Click += (s, e) => HideSignUpPanel();
            panel.Controls.Add(lblSignInPrompt);
            panel.Controls.Add(lblSignInLink);
            y += lblSignInLink.PreferredHeight + 16;

            // ── Admin note ───────────────────────────────────────────
            var lblAdminNote = new Label
            {
                Text = "New accounts are currently created by your system administrator.",
                Font = new Font("Segoe UI", 8f, FontStyle.Italic),
                ForeColor = Color.FromArgb(156, 163, 175),
                AutoSize = true,
                MaximumSize = new Size(contentWidth, 0),
                TextAlign = ContentAlignment.MiddleCenter,
                Top = y
            };
            panel.Controls.Add(lblAdminNote);
            lblAdminNote.Left = contentLeft + (contentWidth - lblAdminNote.PreferredWidth) / 2;

            _signUpOriginalPositions.Clear();
            foreach (Control c in panel.Controls)
                _signUpOriginalPositions[c] = new Point(c.Left, c.Top);

            panel.AutoScrollMinSize = new Size(0, y + lblAdminNote.PreferredHeight + 40);

            return panel;
        }

        private void RecenterSignUpPanel()
        {
            if (_signUpPanel == null || _signUpOriginalPositions.Count == 0) return;

            int minLeft = int.MaxValue, maxRight = int.MinValue;
            int minTop = int.MaxValue, maxBottom = int.MinValue;
            foreach (var kvp in _signUpOriginalPositions)
            {
                if (kvp.Key.IsDisposed) continue;
                int left = kvp.Value.X;
                int right = kvp.Value.X + kvp.Key.Width;
                int top = kvp.Value.Y;
                int bottom = kvp.Value.Y + kvp.Key.Height;
                if (left < minLeft) minLeft = left;
                if (right > maxRight) maxRight = right;
                if (top < minTop) minTop = top;
                if (bottom > maxBottom) maxBottom = bottom;
            }
            int contentWidthMeasured = maxRight - minLeft;
            int contentHeight = maxBottom - minTop;

            int availableWidth = panelRight.ClientSize.Width;
            int availableHeight = panelRight.ClientSize.Height;

            int desiredLeftMargin = Math.Max(0, (availableWidth - contentWidthMeasured) / 2);
            int offsetX = desiredLeftMargin - minLeft;

            int desiredTopMargin = Math.Max(24, (availableHeight - contentHeight) / 2);
            int offsetY = desiredTopMargin - minTop;

            foreach (var kvp in _signUpOriginalPositions)
            {
                if (kvp.Key.IsDisposed) continue;
                kvp.Key.Left = kvp.Value.X + offsetX;
                kvp.Key.Top = kvp.Value.Y + offsetY;
            }

            int newContentBottom = maxBottom + offsetY;
            _signUpPanel.AutoScrollMinSize = new Size(0, Math.Max(newContentBottom + 20, availableHeight));
        }

        private int AddSignUpField(Panel parent, string labelText, int left, int width, int y,
            out TextBox textBox, bool isPassword)
        {
            var lbl = new Label
            {
                Text = labelText,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(17, 24, 39),
                AutoSize = true,
                Left = left,
                Top = y
            };
            parent.Controls.Add(lbl);
            y += lbl.PreferredHeight + 6;

            var field = new Helpers.RoundedPanel
            {
                Left = left,
                Top = y,
                Width = width,
                Height = panelUsername.Height,
                BackColor = Color.FromArgb(243, 244, 246),
                BorderColor = Color.FromArgb(209, 213, 219),
                BorderWidth = 1,
                CornerRadius = (panelUsername is Helpers.RoundedPanel rp) ? rp.CornerRadius : 10
            };
            parent.Controls.Add(field);

            var tb = new TextBox
            {
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 10.5f),
                ForeColor = Color.FromArgb(17, 24, 39),
                BackColor = field.BackColor,
                Left = 16,
                Width = width - (isPassword ? 46 : 32),
                UseSystemPasswordChar = isPassword
            };
            tb.Top = (field.Height - tb.Height) / 2;
            field.Controls.Add(tb);

            if (isPassword)
            {
                var lblEye = new Label
                {
                    Text = "👁",
                    Font = new Font("Segoe UI", 11f),
                    ForeColor = Color.FromArgb(107, 114, 128),
                    AutoSize = true,
                    Cursor = Cursors.Hand,
                    BackColor = Color.Transparent
                };
                lblEye.Left = field.Width - 34;
                lblEye.Top = (field.Height - lblEye.PreferredHeight) / 2;
                lblEye.Click += (s, e) =>
                {
                    tb.UseSystemPasswordChar = !tb.UseSystemPasswordChar;
                    lblEye.Text = tb.UseSystemPasswordChar ? "👁" : "🚫";
                };
                field.Controls.Add(lblEye);
            }

            textBox = tb;
            return y + field.Height + 18;
        }

        private Button MakeSsoButtonLocal(string label, int left, int top, int width, int radius)
        {
            var btn = new Button
            {
                FlatStyle = FlatStyle.Flat,
                Left = left,
                Top = top,
                Width = width,
                Height = 42,
                Text = string.Empty,
                BackColor = Color.White,
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderColor = Color.FromArgb(209, 213, 219);
            btn.FlatAppearance.BorderSize = 1;
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(243, 244, 246);
            btn.Region = MakeRoundedRegion(width, 42, radius);

            bool isGoogle = label == "Google";
            btn.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                using var font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
                using var brush = new SolidBrush(Color.FromArgb(17, 24, 39));
                var textSize = g.MeasureString(label, font);

                float iconSize = 16f;
                float gap = 8f;
                float totalW = iconSize + gap + textSize.Width;
                float startX = (btn.Width - totalW) / 2f;
                float iconY = (btn.Height - iconSize) / 2f;
                float textY = (btn.Height - textSize.Height) / 2f;

                if (isGoogle)
                    DrawGoogleIcon(g, startX, iconY, iconSize);
                else
                    DrawMicrosoftIcon(g, startX, iconY, iconSize);

                g.DrawString(label, font, brush, startX + iconSize + gap, textY);
            };

            return btn;
        }

        private static void DrawGoogleIcon(Graphics g, float x, float y, float size)
        {
            var outerRect = new RectangleF(x, y, size, size);
            float cx = x + size / 2f;
            float cy = y + size / 2f;
            float outerR = size / 2f;
            float ringThickness = size * 0.34f;
            float innerR = outerR - ringThickness;

            Color blue = Color.FromArgb(66, 133, 244);
            Color green = Color.FromArgb(52, 168, 83);
            Color yellow = Color.FromArgb(251, 188, 5);
            Color red = Color.FromArgb(234, 67, 53);

            // Four color wedges, leaving a ~40° gap on the right for the "mouth" of the G.
            using (var bRed = new SolidBrush(red))
                g.FillPie(bRed, outerRect, 20, 85);
            using (var bYellow = new SolidBrush(yellow))
                g.FillPie(bYellow, outerRect, 105, 90);
            using (var bGreen = new SolidBrush(green))
                g.FillPie(bGreen, outerRect, 195, 90);
            using (var bBlue = new SolidBrush(blue))
                g.FillPie(bBlue, outerRect, 285, 55);

            // Punch the donut hole to turn the pie into a ring.
            var innerRect = new RectangleF(cx - innerR, cy - innerR, innerR * 2, innerR * 2);
            using (var white = new SolidBrush(Color.White))
                g.FillEllipse(white, innerRect);

            // Crossbar of the G, sitting in the gap.
            float barHeight = ringThickness * 0.62f;
            using (var bBlueBar = new SolidBrush(blue))
                g.FillRectangle(bBlueBar, cx - 1f, cy - barHeight / 2f, outerR - (cx - 1f - x) + 1f, barHeight);
        }

        private static void DrawMicrosoftIcon(Graphics g, float x, float y, float size)
        {
            float half = size / 2f;
            float gap = 1.5f;
            float sq = half - gap / 2f;

            using var red = new SolidBrush(Color.FromArgb(242, 80, 34));
            using var green = new SolidBrush(Color.FromArgb(127, 186, 0));
            using var blue = new SolidBrush(Color.FromArgb(0, 164, 239));
            using var yellow = new SolidBrush(Color.FromArgb(255, 185, 0));

            g.FillRectangle(red, x, y, sq, sq);
            g.FillRectangle(green, x + sq + gap, y, sq, sq);
            g.FillRectangle(blue, x, y + sq + gap, sq, sq);
            g.FillRectangle(yellow, x + sq + gap, y + sq + gap, sq, sq);
        }

        private void SetupCreateAccountLink()
        {
            var font = new Font("Segoe UI", 8.5f, FontStyle.Regular);

            var lblPrompt = new Label
            {
                AutoSize = true,
                Text = "Don't have an account? ",
                Font = font,
                ForeColor = Color.FromArgb(107, 114, 128),
                BackColor = Color.Transparent
            };

            var lblCreateAccount = new Label
            {
                AutoSize = true,
                Text = "Create account",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = GreenMid,
                Cursor = Cursors.Hand,
                BackColor = Color.Transparent
            };

            // Add to the same parent as btnLogin so coordinates line up.
            var parent = btnLogin.Parent;
            parent.Controls.Add(lblPrompt);
            parent.Controls.Add(lblCreateAccount);

            int totalWidth = lblPrompt.PreferredWidth + lblCreateAccount.PreferredWidth;
            int startX = btnLogin.Left + (btnLogin.Width - totalWidth) / 2;
            int rowY = btnLogin.Top - 26;

            lblPrompt.Location = new Point(startX, rowY);
            lblCreateAccount.Location = new Point(startX + lblPrompt.PreferredWidth, rowY);

            lblCreateAccount.Click += (s, e) => ShowSignUpPanel();

            lblPrompt.BringToFront();
            lblCreateAccount.BringToFront();
        }

        // ── Paint the full-width "Sign in" pill button ────────────────────
        private void BtnLogin_PaintArrow(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            int w = btnLogin.Width;
            int h = btnLogin.Height;
            float t = _loginBtnHoverT;
            bool pressed = _loginBtnState == LoginBtnState.Pressed;
            bool loading = _loginBtnState == LoginBtnState.Loading;

            float scale = pressed ? 0.97f : 1f;
            int sw = (int)(w * scale);
            int sh = (int)(h * scale);
            int ox = (w - sw) / 2;
            int oy = (h - sh) / 2;
            var rect = new RectangleF(ox, oy, sw, sh);
            float radius = (panelUsername is Helpers.RoundedPanel rpFieldPaint) ? rpFieldPaint.CornerRadius : 10f;

            using var pillPath = RoundRect(new Rectangle((int)rect.X, (int)rect.Y, (int)rect.Width, (int)rect.Height), (int)radius);

            float shadowAlpha = pressed ? 35 : 45 + t * 30;
            float shadowLift = pressed ? 2f : 3f + t * 3f;
            using (var shadowBrush = new SolidBrush(Color.FromArgb((int)shadowAlpha, 27, 94, 32)))
            {
                var shadowRect = new RectangleF(rect.X, rect.Y + shadowLift, rect.Width, rect.Height);
                using var shadowPath = RoundRect(new Rectangle((int)shadowRect.X, (int)shadowRect.Y, (int)shadowRect.Width, (int)shadowRect.Height), (int)radius);
                g.FillPath(shadowBrush, shadowPath);
            }

            Color topColor = pressed
                ? GreenDark
                : LerpColor(GreenMid, GreenLight, t * 0.6f);
            Color bottomColor = pressed ? Color.FromArgb(255, 15, 55, 18) : GreenDark;

            using var grad = new LinearGradientBrush(rect, topColor, bottomColor, LinearGradientMode.Vertical);
            g.FillPath(grad, pillPath);

            using var glossBrush = new SolidBrush(Color.FromArgb(28, 255, 255, 255));
            var glossRect = new RectangleF(rect.X + 6, rect.Y + 3, rect.Width - 12, rect.Height * 0.42f);
            using var glossPath = RoundRect(new Rectangle((int)glossRect.X, (int)glossRect.Y, (int)glossRect.Width, (int)glossRect.Height), (int)(glossRect.Height / 2f));
            g.FillPath(glossBrush, glossPath);

            if (t > 0.01f && !pressed)
            {
                using var ringPen = new Pen(Color.FromArgb((int)(70 * t), GreenLight), 2f);
                var ringRect = new RectangleF(rect.X - 3, rect.Y - 3, rect.Width + 6, rect.Height + 6);
                using var ringPath = RoundRect(new Rectangle((int)ringRect.X, (int)ringRect.Y, (int)ringRect.Width, (int)ringRect.Height), (int)(radius + 3));
                g.DrawPath(ringPen, ringPath);
            }

            if (loading)
            {
                float spinR = sh * 0.16f;
                float spinCx = rect.X + rect.Width * 0.36f;
                float spinCy = rect.Y + rect.Height / 2f;

                using var spinPen = new Pen(Color.FromArgb(90, 255, 255, 255), spinR * 0.28f)
                { StartCap = LineCap.Round, EndCap = LineCap.Round };
                g.DrawArc(spinPen, spinCx - spinR, spinCy - spinR, spinR * 2, spinR * 2, 0, 360);

                using var spinPenActive = new Pen(Color.White, spinR * 0.28f)
                { StartCap = LineCap.Round, EndCap = LineCap.Round };
                g.DrawArc(spinPenActive, spinCx - spinR, spinCy - spinR, spinR * 2, spinR * 2, _loginBtnSpinAngle, 110);

                using var loadFont = new Font("Segoe UI", 19f, FontStyle.Bold, GraphicsUnit.Pixel);
                using var loadBrush = new SolidBrush(Color.White);
                using var sfLoad = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center };
                g.DrawString("Signing in", loadFont, loadBrush,
                    new RectangleF(spinCx + spinR + 10, rect.Y, rect.Width, rect.Height), sfLoad);
            }
            else
            {
                float nudge = t * 3f;
                DrawLabelWithArrow(g, rect, "Sign in", nudge);
            }
        }

        private static Color LerpColor(Color a, Color b, float t)
        {
            t = Math.Max(0, Math.Min(1, t));
            return Color.FromArgb(
                255,
                (int)(a.R + (b.R - a.R) * t),
                (int)(a.G + (b.G - a.G) * t),
                (int)(a.B + (b.B - a.B) * t));
        }

        // Shared by btnLogin and btnCreate so both render an identical centered label + arrow.
        private static void DrawLabelWithArrow(Graphics g, RectangleF rect, string label, float nudge = 0f)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            using var labelFont = new Font("Segoe UI", 19f, FontStyle.Bold, GraphicsUnit.Pixel);
            using var labelBrush = new SolidBrush(Color.White);
            var textSize = g.MeasureString(label, labelFont);

            float shaftLen = 10f;
            float headW = 7f;
            float headH = 9f;
            float totalContentW = textSize.Width + 10 + shaftLen + headW;
            float startX = rect.X + (rect.Width - totalContentW) / 2f + nudge;

            using var sfLabel = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center };
            g.DrawString(label, labelFont, labelBrush,
                new RectangleF(startX, rect.Y, textSize.Width + 4, rect.Height), sfLabel);

            float cy = rect.Y + rect.Height / 2f;
            float ax = startX + textSize.Width + 10;
            float tipX = ax + shaftLen + headW;

            using var pen = new Pen(Color.White, 2.5f)
            { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };
            g.DrawLine(pen, ax, cy, ax + shaftLen, cy);
            g.DrawLine(pen, tipX, cy, tipX - headW, cy - headH / 2f);
            g.DrawLine(pen, tipX, cy, tipX - headW, cy + headH / 2f);
        }



        // ── Slide-up entrance animation ───────────────────────────────────
        private void StartSlideUpAnimation()
        {
            int targetY = (panelRight.Height - panelCard.Height) / 2;
            _slideStartY = targetY + 60;
            _slideProgress = 0f;

            panelCard.Location = new Point(panelCard.Left, _slideStartY);
            panelCard.Visible = true;

            _slideTimer?.Stop();
            _slideTimer = new System.Windows.Forms.Timer { Interval = 12 };
            _slideTimer.Tick += (s, e) =>
            {
                _slideProgress = Math.Min(1f, _slideProgress + 0.045f);
                float eased = EaseOutCubic(_slideProgress);

                int targetYFinal = (panelRight.Height - panelCard.Height) / 2;
                int newY = (int)(_slideStartY + (targetYFinal - _slideStartY) * eased);
                panelCard.Location = new Point(panelCard.Left, newY);

                if (_slideProgress >= 1f)
                {
                    _slideTimer?.Stop();
                    _slideTimer?.Dispose();
                    _slideTimer = null;
                    panelCard.Location = new Point(panelCard.Left, targetYFinal);
                }
            };
            _slideTimer.Start();
        }

        // ── Toast notification ─────────────────────────────────────────────
        private void ShowToast(string message, bool isError = true)
        {
            DismissToast(immediate: true);

            const int toastWidth = 340;
            const int iconR = 20;
            const int iconX = 16;
            const int leftTextX = iconX + iconR * 2 + 10;
            const int rightPad = 18;
            int textAreaWidth = toastWidth - leftTextX - rightPad;

            using var titleFontMeasure = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            using var msgFontMeasure = new Font("Segoe UI", 8.5f);

            Size titleSize = TextRenderer.MeasureText(
                isError ? "Authentication Failed" : "Success",
                titleFontMeasure, new Size(textAreaWidth, int.MaxValue), TextFormatFlags.WordBreak);

            Size msgSize = TextRenderer.MeasureText(
                message, msgFontMeasure, new Size(textAreaWidth, int.MaxValue), TextFormatFlags.WordBreak);

            int contentHeight = 10 + titleSize.Height + 6 + msgSize.Height + 14;
            int toastHeight = Math.Max(64, contentHeight);
            toastHeight = Math.Min(toastHeight, 140);

            var toast = new Panel
            {
                Size = new Size(toastWidth, toastHeight),
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand
            };
            toast.Click += (s, e) => DismissToast();

            toast.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;

                using var shadow = new SolidBrush(Color.FromArgb(18, 0, 0, 0));
                using var shadowPath = RoundRect(new Rectangle(3, 5, toast.Width - 4, toast.Height - 4), 14);
                g.FillPath(shadow, shadowPath);

                Color cardBg = isError ? Color.FromArgb(254, 242, 242) : Color.FromArgb(240, 253, 244);
                Color accentColor = isError ? Color.FromArgb(220, 38, 38) : Color.FromArgb(22, 163, 74);

                using var cardBrush = new SolidBrush(cardBg);
                using var cardPath = RoundRect(new Rectangle(0, 2, toast.Width - 3, toast.Height - 5), 14);
                g.FillPath(cardBrush, cardPath);

                using var accentBrush = new SolidBrush(accentColor);
                using var barPath = RoundRect(new Rectangle(0, 2, 5, toast.Height - 5), 3);
                g.FillPath(accentBrush, barPath);

                int iconY = 14;
                using var iconBg = new SolidBrush(Color.FromArgb(30, accentColor));
                g.FillEllipse(iconBg, iconX, iconY, iconR * 2, iconR * 2);

                string icon = isError ? "✕" : "✓";
                using var iconFont = new Font("Segoe UI", 9f, FontStyle.Bold);
                using var iconBrush = new SolidBrush(accentColor);
                using var sfIcon = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                g.DrawString(icon, iconFont, iconBrush, new RectangleF(iconX, iconY, iconR * 2, iconR * 2), sfIcon);

                string title = isError ? "Authentication Failed" : "Success";
                using var titleFont = new Font("Segoe UI", 9.5f, FontStyle.Bold);
                using var titleBrush = new SolidBrush(Color.FromArgb(17, 24, 39));
                var titleRect = new RectangleF(leftTextX, 10, textAreaWidth, titleSize.Height);
                g.DrawString(title, titleFont, titleBrush, titleRect);

                using var msgFont = new Font("Segoe UI", 8.5f);
                using var msgBrush = new SolidBrush(Color.FromArgb(75, 85, 99));
                var msgRect = new RectangleF(leftTextX, 10 + titleSize.Height + 6, textAreaWidth, msgSize.Height + 4);
                g.DrawString(message, msgFont, msgBrush, msgRect);

                using var borderPen = new Pen(Color.FromArgb(25, accentColor), 1f);
                g.DrawPath(borderPen, cardPath);

                using var dismissFont = new Font("Segoe UI", 10f);
                using var dismissBrush = new SolidBrush(Color.FromArgb(120, 75, 85, 99));
                g.DrawString("×", dismissFont, dismissBrush, new PointF(toast.Width - 20, 6));
            };

            int margin = 16;
            int startX = panelRight.Width + 10;
            int endX = panelRight.Width - toast.Width - margin;
            toast.Location = new Point(startX, margin);

            panelRight.Controls.Add(toast);
            toast.BringToFront();
            _toastPanel = toast;

            float prog = 0f;
            var slideIn = new System.Windows.Forms.Timer { Interval = 10 };
            slideIn.Tick += (s, e) =>
            {
                prog = Math.Min(1f, prog + 0.08f);
                float eased = 1f - (float)Math.Pow(1f - prog, 3f);
                toast.Location = new Point((int)(startX + (endX - startX) * eased), margin);
                toast.Invalidate();
                if (prog >= 1f) { slideIn.Stop(); slideIn.Dispose(); }
            };
            slideIn.Start();

            _toastTimer?.Stop();
            _toastTimer = new System.Windows.Forms.Timer { Interval = 4000 };
            _toastTimer.Tick += (s, e) => { _toastTimer?.Stop(); DismissToast(); };
            _toastTimer.Start();
        }

        private void DismissToast(bool immediate = false)
        {
            _toastTimer?.Stop();
            _toastTimer?.Dispose();
            _toastTimer = null;

            if (_toastPanel == null || _toastPanel.IsDisposed) return;
            var toast = _toastPanel;
            _toastPanel = null;

            if (immediate)
            {
                if (!toast.IsDisposed)
                {
                    toast.Parent?.Controls.Remove(toast);
                    toast.Dispose();
                }
                return;
            }

            int startX = toast.Left;
            int endX = panelRight.Width + 10;
            float prog = 0f;
            var slideOut = new System.Windows.Forms.Timer { Interval = 10 };
            slideOut.Tick += (s, e) =>
            {
                prog = Math.Min(1f, prog + 0.09f);
                float eased = (float)Math.Pow(prog, 2f);
                toast.Location = new Point((int)(startX + (endX - startX) * eased), toast.Top);
                if (prog >= 1f)
                {
                    slideOut.Stop();
                    slideOut.Dispose();
                    if (!toast.IsDisposed)
                    {
                        toast.Parent?.Controls.Remove(toast);
                        toast.Dispose();
                    }
                }
            };
            slideOut.Start();
        }

        private static GraphicsPath RoundRect(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            int d = radius * 2;
            d = Math.Min(d, Math.Min(rect.Width, rect.Height));
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        // ── Shake (vibrate) animation on a control ────────────────────────
        private void ShakeControl(Control ctrl)
        {
            _shakeTimer?.Stop();
            _shakeTimer?.Dispose();

            _shakeTarget = ctrl;
            _shakeBaseX = ctrl.Left;
            _shakeStep = 0;

            if (ctrl.Parent is Helpers.RoundedPanel rp)
            {
                rp.BorderColor = Color.FromArgb(220, 38, 38);
                rp.BorderWidth = 2;

                var restoreTimer = new System.Windows.Forms.Timer { Interval = 1200 };
                restoreTimer.Tick += (s, e) =>
                {
                    restoreTimer.Stop();
                    restoreTimer.Dispose();
                    rp.BorderColor = Color.FromArgb(209, 213, 219);
                    rp.BorderWidth = 1;
                    rp.Invalidate();
                };
                restoreTimer.Start();
            }
            else if (ctrl is Helpers.RoundedPanel rp2)
            {
                rp2.BorderColor = Color.FromArgb(220, 38, 38);
                rp2.BorderWidth = 2;

                var restoreTimer = new System.Windows.Forms.Timer { Interval = 1200 };
                restoreTimer.Tick += (s, e) =>
                {
                    restoreTimer.Stop();
                    restoreTimer.Dispose();
                    rp2.BorderColor = Color.FromArgb(209, 213, 219);
                    rp2.BorderWidth = 1;
                    rp2.Invalidate();
                };
                restoreTimer.Start();
            }

            int[] offsets = { -8, 8, -6, 6, -4, 4, -2, 2, 0 };
            _shakeTimer = new System.Windows.Forms.Timer { Interval = 30 };
            _shakeTimer.Tick += (s, e) =>
            {
                if (_shakeTarget == null || _shakeTarget.IsDisposed || _shakeStep >= offsets.Length)
                {
                    _shakeTimer?.Stop();
                    if (_shakeTarget != null && !_shakeTarget.IsDisposed)
                        _shakeTarget.Left = _shakeBaseX;
                    return;
                }
                _shakeTarget.Left = _shakeBaseX + offsets[_shakeStep];
                _shakeStep++;
            };
            _shakeTimer.Start();
        }

        private void LoadPersonImage()
        {
            try
            {
                var path = System.IO.Path.Combine(Application.StartupPath, "Images", "login-illustration.png");
                if (System.IO.File.Exists(path))
                    _personImage = Image.FromFile(path);
            }
            catch { }
        }

        private void DrawPersonImage(Graphics g, float cx, float groundY, float scale, float breathe)
        {
            float imgH = 380f * scale * breathe;
            float imgW = imgH * ((float)_personImage!.Width / _personImage.Height);

            float px = cx - imgW / 2f;
            float py = groundY - imgH + 55f * scale;

            g.DrawImage(_personImage, px, py, imgW, imgH);
        }

        private void SetupAnimation()
        {
            for (int i = 0; i < 14; i++)
            {
                _bubbles.Add(new Bubble
                {
                    X = (float)_rng.NextDouble(),
                    Y = (float)_rng.NextDouble(),
                    Speed = 4f + (float)_rng.NextDouble() * 7f,
                    Phase = (float)_rng.NextDouble() * (float)Math.PI * 2f,
                    Wobble = 4f + (float)_rng.NextDouble() * 6f,
                    Size = 3f + (float)_rng.NextDouble() * 6f,
                    Opacity = 0.06f + (float)_rng.NextDouble() * 0.12f,
                    Foreground = false
                });
            }
            for (int i = 0; i < 10; i++)
            {
                _bubbles.Add(new Bubble
                {
                    X = (float)_rng.NextDouble(),
                    Y = (float)_rng.NextDouble(),
                    Speed = 10f + (float)_rng.NextDouble() * 16f,
                    Phase = (float)_rng.NextDouble() * (float)Math.PI * 2f,
                    Wobble = 8f + (float)_rng.NextDouble() * 12f,
                    Size = 6f + (float)_rng.NextDouble() * 12f,
                    Opacity = 0.14f + (float)_rng.NextDouble() * 0.24f,
                    Foreground = true
                });
            }

            _animTimer = new System.Windows.Forms.Timer();
            _animTimer.Interval = 16;
            _animTimer.Tick += (s, e) =>
            {
                _tick += 0.022f;
                _waveOffset += 0.014f;

                if (_introProgress < 1f)
                    _introProgress = Math.Min(1f, _introProgress + 0.02f);

                foreach (var b in _bubbles)
                {
                    b.Y -= b.Speed * 0.0016f;
                    if (b.Y < -0.05f) b.Y = 1.05f;
                }

                panelAnim.Invalidate();
            };
            _animTimer.Start();
        }

        private static float EaseOutCubic(float t)
        {
            t = Math.Max(0f, Math.Min(1f, t));
            float f = t - 1f;
            return f * f * f + 1f;
        }

        private void PanelAnim_Paint(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            int w = panelAnim.Width;
            int h = panelAnim.Height;
            if (w <= 0 || h <= 0) return;

            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.CompositingQuality = CompositingQuality.HighQuality;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;

            float ease = EaseOutCubic(_introProgress);

            g.Clear(GreenDark);

            float sweepShift = (float)Math.Sin(_tick * 0.4f) * 60f;
            using (var grad = new LinearGradientBrush(
                new Rectangle(-100 + (int)sweepShift, 0, w + 200, h),
                Color.FromArgb((int)(55 * ease), 76, 175, 80),
                Color.FromArgb((int)(55 * ease), 27, 94, 32),
                LinearGradientMode.ForwardDiagonal))
            {
                g.FillRectangle(grad, 0, 0, w, h);
            }

            DrawDriftingBlobs(g, w, h, ease);
            DrawBubbleLayer(g, w, h, ease, foreground: false);

            DrawIllustrationScene(g, w, h, ease);

            DrawBubbleLayer(g, w, h, ease, foreground: true);

            DrawWave(g, w, h, ease);
        }

        private void DrawDriftingBlobs(Graphics g, int w, int h, float ease)
        {
            float t1 = _tick * 0.18f;
            float t2 = _tick * 0.13f;
            float t3 = _tick * 0.22f;

            float x1 = w * 0.15f + (float)Math.Sin(t1) * 70f;
            float y1 = h * 0.18f + (float)Math.Cos(t1 * 0.8f) * 50f;

            float x2 = w * 0.85f + (float)Math.Cos(t2) * 60f;
            float y2 = h * 0.55f + (float)Math.Sin(t2 * 0.9f) * 60f;

            float x3 = w * 0.45f + (float)Math.Sin(t3 + 1.5f) * 80f;
            float y3 = h * 0.82f + (float)Math.Cos(t3) * 40f;

            int alpha = (int)(28 * ease);

            using var b1 = new SolidBrush(Color.FromArgb(alpha, 255, 255, 255));
            using var b2 = new SolidBrush(Color.FromArgb(alpha, 76, 175, 80));
            using var b3 = new SolidBrush(Color.FromArgb((int)(alpha * 0.8f), 255, 255, 255));

            g.FillEllipse(b1, x1 - 110, y1 - 110, 220, 220);
            g.FillEllipse(b2, x2 - 130, y2 - 130, 260, 260);
            g.FillEllipse(b3, x3 - 90, y3 - 90, 180, 180);
        }

        private void DrawBubbleLayer(Graphics g, int w, int h, float ease, bool foreground)
        {
            foreach (var b in _bubbles)
            {
                if (b.Foreground != foreground) continue;

                float parallaxMul = foreground ? 1f : 0.5f;
                float x = b.X * w + (float)Math.Sin(_tick * 1.2f + b.Phase) * b.Wobble * parallaxMul;
                float y = b.Y * h;

                float fade = 1f;
                if (b.Y > 0.85f) fade = (1f - b.Y) / 0.15f;
                if (b.Y < 0.10f) fade = b.Y / 0.10f;
                fade = Math.Max(0f, Math.Min(1f, fade));

                int alpha = (int)(b.Opacity * 255 * ease * fade);
                if (alpha <= 0) continue;

                using var pb = new SolidBrush(Color.FromArgb(alpha, 255, 255, 255));
                g.FillEllipse(pb, x, y, b.Size, b.Size);

                using var hl = new SolidBrush(Color.FromArgb(Math.Min(255, alpha + 40), 255, 255, 255));
                g.FillEllipse(hl, x + b.Size * 0.2f, y + b.Size * 0.15f, b.Size * 0.3f, b.Size * 0.3f);
            }
        }

        private void DrawWave(Graphics g, int w, int h, float ease)
        {
            using var path = new GraphicsPath();
            path.StartFigure();
            path.AddLine(0, h, 0, h - 30);
            for (int x = 0; x <= w; x += 4)
            {
                float y = h - 30 - (float)Math.Sin(x * 0.02f + _waveOffset) * 15f * ease;
                path.AddLine(x, y, x + 4, y);
            }
            path.AddLine(w, h - 30, w, h);
            path.CloseFigure();
            using var wb = new SolidBrush(Color.FromArgb((int)(34 * ease), 27, 94, 32));
            g.FillPath(wb, path);

            using var path2 = new GraphicsPath();
            path2.StartFigure();
            path2.AddLine(0, h, 0, h - 18);
            for (int x = 0; x <= w; x += 4)
            {
                float y = h - 18 - (float)Math.Sin(x * 0.024f - _waveOffset * 1.4f) * 9f * ease;
                path2.AddLine(x, y, x + 4, y);
            }
            path2.AddLine(w, h - 18, w, h);
            path2.CloseFigure();
            using var wb2 = new SolidBrush(Color.FromArgb((int)(26 * ease), 76, 175, 80));
            g.FillPath(wb2, path2);
        }

        private void DrawIllustrationScene(Graphics g, int w, int h, float ease)
        {
            float groundY = h * 0.78f;
            float scale = Math.Min(w / 420f, h / 640f);
            scale = Math.Max(0.65f, Math.Min(1.3f, scale));

            float cx = w * 0.52f;

            using (var halo = new SolidBrush(Color.FromArgb((int)(26 * ease), 255, 255, 255)))
                g.FillEllipse(halo, cx - 170 * scale, groundY - 230 * scale, 340 * scale, 340 * scale);

            float breathe = 1f + (float)Math.Sin(_tick * 0.9f) * 0.012f;

            if (_personImage != null)
            {
                DrawContactShadow(g, cx, groundY, scale);
                DrawPersonImage(g, cx, groundY, scale, breathe);
            }
            else
            {
                DrawDeskSurface(g, w, groundY, scale);
                DrawContactShadow(g, cx, groundY, scale);
                DrawLaptop(g, cx, groundY, scale, ease);
                DrawPersonVector(g, cx, groundY, scale, breathe);
            }

            float pulse = 1f + (float)Math.Sin(_tick * 1.4f) * 0.05f;
            DrawFloatingIcon(g, "📦", w * 0.12f, groundY - 230f * scale + (float)Math.Sin(_tick * 0.7f) * 10f, pulse, ease);
            DrawFloatingIcon(g, "📋", w * 0.90f, groundY - 270f * scale + (float)Math.Cos(_tick * 0.5f) * 12f, pulse, ease);
            DrawFloatingIcon(g, "🔍", cx - 150f * scale, groundY - 190f * scale + (float)Math.Sin(_tick * 0.9f + 1f) * 8f, pulse, ease);
        }

        private static void DrawDeskSurface(Graphics g, int w, float groundY, float scale)
        {
            float deskW = 260f * scale;
            float deskX = w * 0.52f - deskW / 2f;
            float deskH = 14f * scale;

            using var top = new LinearGradientBrush(
                new RectangleF(deskX, groundY, deskW, deskH),
                Color.FromArgb(215, 139, 95, 48),
                Color.FromArgb(215, 101, 67, 33),
                LinearGradientMode.Vertical);
            FillRoundedF(g, top, new RectangleF(deskX, groundY, deskW, deskH), 5f * scale);

            using var highlight = new Pen(Color.FromArgb(70, 255, 255, 255), 1.4f);
            g.DrawLine(highlight, deskX + 6, groundY + 1, deskX + deskW - 6, groundY + 1);
        }

        private static void DrawContactShadow(Graphics g, float cx, float groundY, float scale)
        {
            using var shadow = new SolidBrush(Color.FromArgb(40, 0, 0, 0));
            g.FillEllipse(shadow, cx - 95f * scale, groundY + 10f * scale, 190f * scale, 22f * scale);
        }

        private static void DrawLaptop(Graphics g, float cx, float groundY, float scale, float ease)
        {
            float lw = 130f * scale;
            float lh = 84f * scale;
            float lx = cx - lw - 14f * scale;
            float ly = groundY - lh - 4f * scale;

            float glowPulse = 0.5f + 0.5f * (float)Math.Sin(System.Environment.TickCount * 0.002f);
            using var glow = new SolidBrush(Color.FromArgb((int)((24 + 14 * glowPulse) * ease), 165, 214, 167));
            g.FillEllipse(glow, lx - 18, ly - 18, lw + 36, lh + 36);

            using var basePen = new SolidBrush(Color.FromArgb(235, 60, 60, 60));
            FillRoundedF(g, basePen, new RectangleF(lx - 6, groundY - 10 * scale, lw + 12, 12 * scale), 4f * scale);

            using var body = new SolidBrush(Color.FromArgb(235, 45, 45, 45));
            FillRoundedF(g, body, new RectangleF(lx, ly, lw, lh), 8f * scale);

            using var bezel = new SolidBrush(Color.FromArgb(200, 28, 28, 28));
            FillRoundedF(g, bezel, new RectangleF(lx + 4, ly + 4, lw - 8, lh - 10), 6f * scale);

            using var screen = new LinearGradientBrush(
                new RectangleF(lx + 8, ly + 8, lw - 16, lh - 18),
                Color.FromArgb(235, 129, 199, 132),
                Color.FromArgb(235, 46, 125, 50),
                LinearGradientMode.Vertical);
            FillRoundedF(g, screen, new RectangleF(lx + 8, ly + 8, lw - 16, lh - 18), 5f * scale);

            using var shine = new SolidBrush(Color.FromArgb(20, 255, 255, 255));
            FillRoundedF(g, shine, new RectangleF(lx + 8, ly + 8, lw - 16, (lh - 18) / 2f), 5f * scale);

            using var lp = new Pen(Color.FromArgb(95, 255, 255, 255), 1.4f * scale);
            for (int i = 0; i < 3; i++)
            {
                float ly2 = ly + 16 * scale + i * 13 * scale;
                g.DrawLine(lp, lx + 16 * scale, ly2, lx + lw - 16 * scale, ly2);
            }
        }

        private static void DrawPersonVector(Graphics g, float cx, float groundY, float scale, float breathe)
        {
            float px = cx + 70f * scale;
            float headR = 34f * scale * breathe;
            float shoulderY = groundY - 150f * scale;
            float footY = groundY - 6f * scale;

            using var trouser = new SolidBrush(Color.FromArgb(245, 38, 38, 46));
            FillRoundedF(g, trouser, new RectangleF(px - 24 * scale, shoulderY + 86 * scale, 18 * scale, footY - (shoulderY + 86 * scale)), 7f * scale);
            FillRoundedF(g, trouser, new RectangleF(px + 6 * scale, shoulderY + 86 * scale, 18 * scale, footY - (shoulderY + 86 * scale)), 7f * scale);

            using var shoe = new SolidBrush(Color.FromArgb(255, 24, 24, 24));
            FillRoundedF(g, shoe, new RectangleF(px - 26 * scale, footY - 2 * scale, 22 * scale, 9 * scale), 4f * scale);
            FillRoundedF(g, shoe, new RectangleF(px + 4 * scale, footY - 2 * scale, 22 * scale, 9 * scale), 4f * scale);

            float torsoW = 64f * scale * breathe;
            float torsoH = 92f * scale * breathe;
            using var shirt = new LinearGradientBrush(
                new RectangleF(px - torsoW / 2f, shoulderY, torsoW, torsoH),
                Color.FromArgb(248, 76, 175, 80),
                Color.FromArgb(248, 27, 94, 32),
                LinearGradientMode.Vertical);
            FillRoundedF(g, shirt, new RectangleF(px - torsoW / 2f, shoulderY, torsoW, torsoH), 18f * scale);

            using var collar = new SolidBrush(Color.FromArgb(45, 255, 255, 255));
            FillRoundedF(g, collar, new RectangleF(px - torsoW / 2f, shoulderY, torsoW, torsoH * 0.32f), 18f * scale);

            using var skin = new SolidBrush(Color.FromArgb(248, 222, 178, 122));
            g.FillRectangle(skin, px - 8 * scale, shoulderY - 8 * scale, 16 * scale, 14 * scale);

            g.FillEllipse(skin, px - headR, shoulderY - 8 * scale - headR * 1.7f, headR * 2f, headR * 2f);

            using var hair = new SolidBrush(Color.FromArgb(255, 28, 22, 18));
            var hairRect = new RectangleF(px - headR * 1.02f, shoulderY - 8 * scale - headR * 1.85f, headR * 2.04f, headR * 1.5f);
            g.FillEllipse(hair, hairRect);
            g.FillRectangle(hair, px - headR, shoulderY - 8 * scale - headR * 1.15f, headR * 2f, headR * 0.5f);

            g.FillEllipse(skin, px - headR * 1.08f, shoulderY - 8 * scale - headR * 0.7f, headR * 0.36f, headR * 0.46f);
            g.FillEllipse(skin, px + headR * 0.72f, shoulderY - 8 * scale - headR * 0.7f, headR * 0.36f, headR * 0.46f);

            float elbowX = px - torsoW * 0.55f;
            float elbowY = shoulderY + 50f * scale;
            float handX = cx - 28f * scale;
            float handY = groundY - 16f * scale;

            using var armPen = new Pen(Color.FromArgb(248, 46, 125, 50), 12f * scale)
            { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };
            using var armPath = new GraphicsPath();
            armPath.AddBezier(
                px - torsoW * 0.3f, shoulderY + 18f * scale,
                elbowX, elbowY,
                elbowX + 10f * scale, handY - 20f * scale,
                handX, handY);
            g.DrawPath(armPen, armPath);

            g.FillEllipse(skin, handX - 7 * scale, handY - 7 * scale, 14 * scale, 14 * scale);

            using var armPen2 = new Pen(Color.FromArgb(248, 46, 125, 50), 12f * scale)
            { StartCap = LineCap.Round, EndCap = LineCap.Round };
            g.DrawLine(armPen2,
                px + torsoW * 0.42f, shoulderY + 18f * scale,
                px + torsoW * 0.62f, shoulderY + 60f * scale);
            g.FillEllipse(skin, px + torsoW * 0.62f - 6 * scale, shoulderY + 60f * scale - 6 * scale, 12 * scale, 12 * scale);
        }

        private static void DrawFloatingIcon(Graphics g, string icon, float x, float y, float pulse, float ease)
        {
            float r = 22f * pulse;

            using var glow = new SolidBrush(Color.FromArgb((int)(20 * ease), 255, 255, 255));
            g.FillEllipse(glow, x - r - 4, y - r - 4, (r + 4) * 2, (r + 4) * 2);

            using var bg = new SolidBrush(Color.FromArgb((int)(50 * ease), 255, 255, 255));
            g.FillEllipse(bg, x - r, y - r, r * 2, r * 2);

            using var ring = new Pen(Color.FromArgb((int)(60 * ease), 255, 255, 255), 1f);
            g.DrawEllipse(ring, x - r, y - r, r * 2, r * 2);

            using var sf = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };
            using var f = new Font("Segoe UI Emoji", 15f, FontStyle.Regular, GraphicsUnit.Point);
            g.DrawString(icon, f, Brushes.White, new PointF(x, y), sf);
        }

        private static void FillRoundedF(Graphics g, Brush brush, RectangleF rect, float r)
        {
            using var path = new GraphicsPath();
            float d = r * 2f;
            d = Math.Min(d, Math.Min(rect.Width, rect.Height));
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            g.FillPath(brush, path);
        }

        // ── Login logic ───────────────────────────────────────────────────
        private async void btnLogin_Click(object sender, EventArgs e)
        {
            if (_loginBtnState == LoginBtnState.Loading) return;

            lblError.Text = string.Empty;

            string username = txtUsername.Text.Trim();
            string password = txtPassword.Text;

            if (string.IsNullOrWhiteSpace(username))
            {
                ShowToast("Please enter your username.");
                ShakeControl(panelUsername);
                return;
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                ShowToast("Please enter your password.");
                ShakeControl(panelPassword);
                return;
            }

            SetLoginBtnLoading(true);
            var loginStopwatch = System.Diagnostics.Stopwatch.StartNew();
            const int minSpinnerMs = 1500; // spinner always shows at least this long
            const int maxSpinnerMs = 2000; // and never longer than this before failing

            try
            {
                var dbTask = System.Threading.Tasks.Task.Run(() =>
                {
                    using var db = new InventoryDbContext();
                    return db.Users
                        .FirstOrDefault(u => u.Username == username && u.IsActive == true);
                });

                var timeoutTask = System.Threading.Tasks.Task.Delay(maxSpinnerMs);
                var completed = await System.Threading.Tasks.Task.WhenAny(dbTask, timeoutTask);

                if (completed == timeoutTask)
                {
                    SetLoginBtnLoading(false);
                    ShowToast("Connection error: request timed out.");
                    return;
                }

                User? user = await dbTask;

                // Enforce a minimum spinner duration so it never flashes by
                // too fast to register as "working" on a fast connection.
                int elapsed = (int)loginStopwatch.ElapsedMilliseconds;
                if (elapsed < minSpinnerMs)
                    await System.Threading.Tasks.Task.Delay(minSpinnerMs - elapsed);

                if (user == null)
                {
                    SetLoginBtnLoading(false);
                    ShowToast("Invalid username or password.");
                    ShakeControl(panelUsername);
                    ShakeControl(panelPassword);
                    return;
                }

                bool ok = user.PasswordHash == password;
                if (!ok)
                {
                    SetLoginBtnLoading(false);
                    ShowToast("Invalid username or password.");
                    ShakeControl(panelPassword);
                    return;
                }

                AuthService.SetCurrentUser(user);
                SaveRememberedUser(username);

                _animTimer?.Stop();
                SetLoginBtnLoading(false);
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                SetLoginBtnLoading(false);
                ShowToast("Connection error: " + ex.Message);
            }
        }

        private void btnCancel_Click(object sender, EventArgs e) => Application.Exit();

        private void lblShowHidePassword_Click(object? sender, EventArgs e)
        {
            txtPassword.UseSystemPasswordChar = !txtPassword.UseSystemPasswordChar;
            lblShowHidePassword.Text = txtPassword.UseSystemPasswordChar ? "👁" : "🚫";
        }

        private void pnlToggle_Paint(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var rect = new Rectangle(0, 2, 60, 32);
            int d = rect.Height;
            using var path = new GraphicsPath();
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();

            Color bgColor = _keepMeLoggedIn
                ? Color.FromArgb(46, 125, 50)
                : Color.FromArgb(209, 213, 219);
            using var bgBrush = new SolidBrush(bgColor);
            g.FillPath(bgBrush, path);

            int knobSize = 26;
            int knobY = rect.Y + (rect.Height - knobSize) / 2;
            int knobX = _keepMeLoggedIn
                ? (rect.Right - knobSize - 3)
                : (rect.X + 3);

            using var knobBrush = new SolidBrush(Color.White);
            g.FillEllipse(knobBrush, knobX, knobY, knobSize, knobSize);

            if (!_keepMeLoggedIn)
            {
                using var knobPen = new Pen(Color.FromArgb(156, 163, 175), 0.5f);
                g.DrawEllipse(knobPen, knobX, knobY, knobSize, knobSize);
            }
        }

        private void pnlToggle_Click(object? sender, EventArgs e)
        {
            _keepMeLoggedIn = !_keepMeLoggedIn;
            pnlToggle.Invalidate();
        }

        private void lblToggleLabel_Click(object? sender, EventArgs e)
        {
            _keepMeLoggedIn = !_keepMeLoggedIn;
            pnlToggle.Invalidate();
        }

        private void LoadRememberedUser()
        {
            try
            {
                string filePath = System.IO.Path.Combine(Application.StartupPath, "remember_me.txt");
                if (System.IO.File.Exists(filePath))
                {
                    var lines = System.IO.File.ReadAllLines(filePath);
                    if (lines.Length >= 2 && bool.TryParse(lines[0], out bool keepMe) && keepMe)
                    {
                        _keepMeLoggedIn = true;
                        txtUsername.Text = lines[1];
                        this.ActiveControl = txtPassword;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "LoginForm.LoadRememberedUser");
            }
        }

        private void SaveRememberedUser(string username)
        {
            try
            {
                string filePath = System.IO.Path.Combine(Application.StartupPath, "remember_me.txt");
                if (_keepMeLoggedIn)
                {
                    System.IO.File.WriteAllLines(filePath, new[] { "True", username });
                }
                else
                {
                    if (System.IO.File.Exists(filePath))
                        System.IO.File.Delete(filePath);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "LoginForm.SaveRememberedUser");
            }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Enter) { btnLogin_Click(this, EventArgs.Empty); return true; }
            return base.ProcessCmdKey(ref msg, keyData);
        }
    }
}