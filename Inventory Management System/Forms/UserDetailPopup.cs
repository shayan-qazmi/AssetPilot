// Forms/UserDetailPopup.cs
using System;
using System.Drawing;
using System.Windows.Forms;
using Inventory_Management_System.Helpers;
using Inventory_Management_System.Models;

namespace Inventory_Management_System.Forms
{
    // Opened on double-click of a row in the Users grid (SettingsForm).
    // Shows the picture on the left (default silhouette if none was
    // uploaded) and the account details on the right — no separate
    // "View Picture" popup, unlike the equipment detail view.
    public class UserDetailPopup : Form
    {
        private readonly User _user;
        private readonly string _roleName;
        private Image? _avatarImage;

        private const int DialogWidth = 520;
        private const int PaddingOuter = 20;
        private const int AvatarSize = 140;

        private static readonly Color BgPage = Color.FromArgb(245, 247, 250);
        private static readonly Color CardBg = Color.White;
        private static readonly Color CardBorderColor = Color.FromArgb(220, 225, 230);
        private static readonly Color SeparatorColor = Color.FromArgb(234, 237, 241);

        public UserDetailPopup(User user, string roleName)
        {
            _user = user;
            _roleName = string.IsNullOrWhiteSpace(roleName) ? "—" : roleName;
            InitializeUI();
        }

        private void InitializeUI()
        {
            Text = $"User Details — {_user.Username}";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            BackColor = BgPage;

            // ── Header Strip ──
            var header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 52,
                BackColor = ThemeHelper.Header,
            };

            var lblTitle = new Label
            {
                Text = "  User Details",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 11.5f, FontStyle.Bold, GraphicsUnit.Point),
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(8, 0, 0, 0)
            };
            header.Controls.Add(lblTitle);
            Controls.Add(header);

            // ── Card ──
            var card = new Panel
            {
                BackColor = CardBg,
                Location = new Point(PaddingOuter, header.Height + PaddingOuter),
                Width = DialogWidth - PaddingOuter * 2,
            };
            card.Paint += DrawCardBorder;
            Controls.Add(card);

            _avatarImage = AvatarHelper.TryLoadUserImage(_user.PicturePath);

            var avatarPanel = new Panel
            {
                Location = new Point(24, 24),
                Size = new Size(AvatarSize, AvatarSize),
                BackColor = Color.Transparent
            };
            avatarPanel.Paint += (s, e) =>
                AvatarHelper.DrawAvatar(e.Graphics, new Rectangle(0, 0, AvatarSize - 1, AvatarSize - 1), _avatarImage);
            card.Controls.Add(avatarPanel);

            int detailX = avatarPanel.Right + 28;
            int detailW = (DialogWidth - PaddingOuter * 2) - detailX - 24;
            int y = 26;

            var lblFullName = new Label
            {
                Text = string.IsNullOrWhiteSpace(_user.FullName) ? _user.Username : _user.FullName,
                Font = new Font("Segoe UI Semibold", 14f),
                ForeColor = ThemeHelper.TextPrimary,
                AutoSize = false,
                Size = new Size(detailW, 30),
                Location = new Point(detailX, y),
                AutoEllipsis = true
            };
            card.Controls.Add(lblFullName);
            y = lblFullName.Bottom + 6;

            bool active = _user.IsActive == true;
            var badge = new Label
            {
                Text = active ? "Active" : "Inactive",
                AutoSize = true,
                Font = new Font("Segoe UI", 8.25f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = active ? Color.FromArgb(34, 139, 34) : Color.FromArgb(185, 28, 28),
                Location = new Point(detailX, y),
                Padding = new Padding(8, 4, 8, 4)
            };
            card.Controls.Add(badge);
            y = badge.Bottom + 18;

            void DetailRow(string label, string? value)
            {
                var pnl = new Panel { Location = new Point(detailX, y), Size = new Size(detailW, 34) };
                pnl.Controls.Add(new Label
                {
                    Text = label,
                    Font = ThemeHelper.FontSmall,
                    ForeColor = ThemeHelper.TextSecondary,
                    AutoSize = true,
                    Location = new Point(0, 0)
                });
                pnl.Controls.Add(new Label
                {
                    Text = string.IsNullOrWhiteSpace(value) ? "—" : value,
                    Font = ThemeHelper.FontDefault,
                    ForeColor = ThemeHelper.TextPrimary,
                    AutoSize = false,
                    Size = new Size(detailW, 18),
                    Location = new Point(0, 16),
                    AutoEllipsis = true
                });
                pnl.Paint += (s, e) =>
                {
                    using var pen = new Pen(SeparatorColor, 1);
                    e.Graphics.DrawLine(pen, 0, pnl.Height - 1, pnl.Width, pnl.Height - 1);
                };
                card.Controls.Add(pnl);
                y += pnl.Height;
            }

            DetailRow("Username", _user.Username);
            DetailRow("Email", _user.Email);
            DetailRow("Role", _roleName);

            int cardH = Math.Max(avatarPanel.Bottom + 24, y + 8);
            card.Height = cardH;

            // ── Close Button ──
            int btnY = header.Height + PaddingOuter + cardH + 14;
            var btnClose = new Button
            {
                Text = "Close",
                Size = new Size(100, 34),
                Location = new Point((DialogWidth - 100) / 2, btnY),
                Font = ThemeHelper.FontDefault,
                Cursor = Cursors.Hand,
            };
            ThemeHelper.StyleSecondaryButton(btnClose);
            btnClose.Click += (s, e) => Close();
            Controls.Add(btnClose);

            ClientSize = new Size(DialogWidth, btnY + 34 + PaddingOuter);

            FormClosed += (s, e) => _avatarImage?.Dispose();
        }

        private void DrawCardBorder(object? sender, PaintEventArgs e)
        {
            if (sender is not Panel pnl) return;
            var g = e.Graphics;
            var rect = new Rectangle(0, 0, pnl.Width - 1, pnl.Height - 1);

            using var shadowBrush = new SolidBrush(Color.FromArgb(18, 0, 0, 0));
            g.FillRectangle(shadowBrush, new Rectangle(2, 2, pnl.Width - 1, pnl.Height - 1));

            using var bgBrush = new SolidBrush(CardBg);
            g.FillRectangle(bgBrush, rect);

            using var pen = new Pen(CardBorderColor, 1);
            g.DrawRectangle(pen, rect);
        }
    }
}