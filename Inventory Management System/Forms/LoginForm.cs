// Forms/LoginForm.cs
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

        private static readonly Color GreenDark  = Color.FromArgb(27,  94,  32);
        private static readonly Color GreenMid   = Color.FromArgb(46,  125, 50);
        private static readonly Color GreenLight = Color.FromArgb(76,  175, 80);
        private static readonly Color GreenPale  = Color.FromArgb(200, 230, 201);

        private readonly List<Bubble> _bubbles = new();
        private readonly Random _rng = new();
        private float _introProgress = 0f;
        private float _waveOffset    = 0f;

        private class Bubble
        {
            public float X, Speed, Phase, Size, Opacity, Wobble;
            public float Y;
            public bool Foreground;
        }

        private bool _keepMeLoggedIn = false;

        public LoginForm()
        {
            InitializeComponent();
            ApplyTheme();
            SetupAnimation();
            LoadRememberedUser();
        }

        private void ApplyTheme()
        {
            lblError.ForeColor = ThemeHelper.Danger;

            panelLeft.BackColor = GreenDark;
            panelAnim.BackColor = GreenDark;

            btnLogin.Region  = MakeRoundedRegion(btnLogin.Width, btnLogin.Height, 12);
            btnCancel.Region = MakeRoundedRegion(btnCancel.Width, btnCancel.Height, 10);

            typeof(Panel).GetProperty("DoubleBuffered",
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic)
                ?.SetValue(panelAnim, true);

            panelAnim.Paint += PanelAnim_Paint;
        }

        private static Region MakeRoundedRegion(int w, int h, int r)
        {
            var path = new GraphicsPath();
            int d = r * 2;
            path.AddArc(0,     0,     d, d, 180, 90);
            path.AddArc(w - d, 0,     d, d, 270, 90);
            path.AddArc(w - d, h - d, d, d, 0,   90);
            path.AddArc(0,     h - d, d, d, 90,  90);
            path.CloseFigure();
            return new Region(path);
        }

        private void SetupAnimation()
        {
            for (int i = 0; i < 14; i++)
            {
                _bubbles.Add(new Bubble
                {
                    X          = (float)_rng.NextDouble(),
                    Y          = (float)_rng.NextDouble(),
                    Speed      = 4f + (float)_rng.NextDouble() * 7f,
                    Phase      = (float)_rng.NextDouble() * (float)Math.PI * 2f,
                    Wobble     = 4f + (float)_rng.NextDouble() * 6f,
                    Size       = 3f + (float)_rng.NextDouble() * 6f,
                    Opacity    = 0.06f + (float)_rng.NextDouble() * 0.12f,
                    Foreground = false
                });
            }
            for (int i = 0; i < 10; i++)
            {
                _bubbles.Add(new Bubble
                {
                    X          = (float)_rng.NextDouble(),
                    Y          = (float)_rng.NextDouble(),
                    Speed      = 10f + (float)_rng.NextDouble() * 16f,
                    Phase      = (float)_rng.NextDouble() * (float)Math.PI * 2f,
                    Wobble     = 8f + (float)_rng.NextDouble() * 12f,
                    Size       = 6f + (float)_rng.NextDouble() * 12f,
                    Opacity    = 0.14f + (float)_rng.NextDouble() * 0.24f,
                    Foreground = true
                });
            }

            _animTimer          = new System.Windows.Forms.Timer();
            _animTimer.Interval = 16;
            _animTimer.Tick    += (s, e) =>
            {
                _tick       += 0.022f;
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

            g.SmoothingMode      = SmoothingMode.AntiAlias;
            g.CompositingQuality = CompositingQuality.HighQuality;
            g.InterpolationMode  = InterpolationMode.HighQualityBicubic;

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
            g.FillEllipse(b3, x3 - 90,  y3 - 90,  180, 180);
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
            float scale   = Math.Min(w / 420f, h / 640f);
            scale = Math.Max(0.65f, Math.Min(1.3f, scale));

            float cx = w * 0.52f;

            using (var halo = new SolidBrush(Color.FromArgb((int)(26 * ease), 255, 255, 255)))
                g.FillEllipse(halo, cx - 170 * scale, groundY - 230 * scale, 340 * scale, 340 * scale);

            float breathe = 1f + (float)Math.Sin(_tick * 0.9f) * 0.012f;

            DrawDeskSurface(g, w, groundY, scale);
            DrawContactShadow(g, cx, groundY, scale);
            DrawLaptop(g, cx, groundY, scale, ease);
            DrawPersonVector(g, cx, groundY, scale, breathe);

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
            FillRoundedF(g, trouser, new RectangleF(px + 6  * scale, shoulderY + 86 * scale, 18 * scale, footY - (shoulderY + 86 * scale)), 7f * scale);

            using var shoe = new SolidBrush(Color.FromArgb(255, 24, 24, 24));
            FillRoundedF(g, shoe, new RectangleF(px - 26 * scale, footY - 2 * scale, 22 * scale, 9 * scale), 4f * scale);
            FillRoundedF(g, shoe, new RectangleF(px + 4  * scale, footY - 2 * scale, 22 * scale, 9 * scale), 4f * scale);

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
            g.FillEllipse(skin, px + headR * 0.72f,  shoulderY - 8 * scale - headR * 0.7f, headR * 0.36f, headR * 0.46f);

            float elbowX = px - torsoW * 0.55f;
            float elbowY = shoulderY + 50f * scale;
            float handX  = cx - 28f * scale;
            float handY  = groundY - 16f * scale;

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
                Alignment     = StringAlignment.Center,
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
            path.AddArc(rect.X,             rect.Y,              d, d, 180, 90);
            path.AddArc(rect.Right - d,     rect.Y,              d, d, 270, 90);
            path.AddArc(rect.Right - d,     rect.Bottom - d,     d, d, 0,   90);
            path.AddArc(rect.X,             rect.Bottom - d,     d, d, 90,  90);
            path.CloseFigure();
            g.FillPath(brush, path);
        }

        // ── Login logic ───────────────────────────────────────────────────
        private void btnLogin_Click(object sender, EventArgs e)
        {
            lblError.Text = string.Empty;

            string username = txtUsername.Text.Trim();
            string password = txtPassword.Text;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                lblError.Text = "Please enter username and password.";
                return;
            }

            try
            {
                using var db   = new InventoryDbContext();
                var user = db.Users
                    .FirstOrDefault(u => u.Username == username && u.IsActive == true);

                if (user == null)
                {
                    lblError.Text = "Invalid username or password.";
                    return;
                }

                bool ok = user.PasswordHash == password;
                if (!ok)
                {
                    lblError.Text = "Invalid username or password.";
                    return;
                }

                // Cache the logged-in user + role for the rest of the
                // session. Every form needing role-based access reads
                // from AuthService rather than querying the DB again.
                AuthService.SetCurrentUser(user);

                SaveRememberedUser(username);

                _animTimer?.Stop();
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                lblError.Text = "Connection error: " + ex.Message;
            }
        }

        private void btnCancel_Click(object sender, EventArgs e) => Application.Exit();

        private void lblShowHidePassword_Click(object? sender, EventArgs e)
        {
            txtPassword.UseSystemPasswordChar = !txtPassword.UseSystemPasswordChar;
            lblShowHidePassword.Text          = txtPassword.UseSystemPasswordChar ? "👁" : "🚫";
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
    int knobY    = rect.Y + (rect.Height - knobSize) / 2;
    int knobX    = _keepMeLoggedIn
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
            catch { }
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
            catch { }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Enter) { btnLogin_Click(this, EventArgs.Empty); return true; }
            return base.ProcessCmdKey(ref msg, keyData);
        }
    }
}