// Forms/EmployeeDetailPopup.cs
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Inventory_Management_System.Helpers;
using Inventory_Management_System.Models;
using Inventory_Management_System.Services;

namespace Inventory_Management_System.Forms
{
    // Opened on double-click of a row in the Employees grid.
    // Mirrors UserDetailPopup's layout: picture on the left, details on the right.
    public class EmployeeDetailPopup : Form
    {
        private readonly Employee _employee;
        private Image? _avatarImage;

        private const int DialogWidth = 580;
        private const int PaddingOuter = 20;
        private const int AvatarSize = 140;

        private static Color BgPage => ThemeHelper.DashboardBg;
        private static Color CardBg => ThemeHelper.CardBg;
        private static Color CardBorderColor => ThemeHelper.IsDarkMode ? Color.FromArgb(55, 58, 65) : Color.FromArgb(220, 225, 230);

        public EmployeeDetailPopup(Employee employee)
        {
            _employee = employee;
            InitializeUI();
        }

        private int GetActiveAllocationCount()
        {
            try
            {
                var svc = new AllocationService();
                var result = svc.GetByEmployee(_employee.Id);
                if (!result.Success || result.Data == null) return 0;
                return result.Data.Count(a => a.Status == "approved");
            }
            catch
            {
                return 0;
            }
        }

        private void InitializeUI()
        {
            Text = $"Employee Details — {_employee.EmployeeCode}";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            BackColor = BgPage;

            // ── Header Strip ──
            var header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 56,
                BackColor = ThemeHelper.Header,
            };

            var lblHeaderIcon = new Label
            {
                Text = "👤",
                Font = new Font("Segoe UI Emoji", 13f),
                ForeColor = Color.White,
                AutoSize = false,
                Size = new Size(30, 30),
                Location = new Point(18, 13),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };
            header.Controls.Add(lblHeaderIcon);

            var lblTitle = new Label
            {
                Text = "Employee Details",
                Location = new Point(lblHeaderIcon.Right + 8, 15),
                AutoSize = true,
                Font = new Font("Segoe UI Semibold", 12.5f, FontStyle.Bold, GraphicsUnit.Point),
                ForeColor = Color.White,
                BackColor = Color.Transparent
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

            _avatarImage = AvatarHelper.TryLoadEmployeeImage(_employee.PicturePath);

            int avatarX = 28, avatarY = 28;
            var avatarPanel = new Panel
            {
                Location = new Point(avatarX, avatarY),
                Size = new Size(AvatarSize, AvatarSize),
                BackColor = Color.Transparent
            };
            avatarPanel.Paint += (s, e) =>
                AvatarHelper.DrawAvatar(e.Graphics, new Rectangle(0, 0, AvatarSize - 1, AvatarSize - 1), _avatarImage);
            card.Controls.Add(avatarPanel);

            var lblFullNameUnder = new Label
            {
                Text = _employee.FullName,
                Font = new Font("Segoe UI Semibold", 10.5f),
                ForeColor = ThemeHelper.TextPrimary,
                AutoSize = false,
                Size = new Size(AvatarSize, 36),
                Location = new Point(avatarX, avatarPanel.Bottom + 10),
                TextAlign = ContentAlignment.MiddleCenter,
                AutoEllipsis = true
            };
            card.Controls.Add(lblFullNameUnder);

            bool active = _employee.IsActive;
            var badge = new Label
            {
                Text = "●  " + (active ? "Active" : "Inactive"),
                AutoSize = true,
                Font = new Font("Segoe UI Semibold", 8.25f),
                ForeColor = active ? Color.FromArgb(21, 128, 61) : Color.FromArgb(185, 28, 28),
                BackColor = active ? Color.FromArgb(220, 252, 231) : Color.FromArgb(254, 226, 226),
                Padding = new Padding(10, 5, 10, 5)
            };
            badge.Location = new Point(avatarX + (AvatarSize - badge.PreferredWidth) / 2, lblFullNameUnder.Bottom + 8);
            card.Controls.Add(badge);

            // ── Right side: detail block with icon-labeled rows ──
            int detailX = avatarPanel.Right + 32;
            int detailW = (DialogWidth - PaddingOuter * 2) - detailX - 24;

            var lblSectionHeading = new Label
            {
                Text = "EMPLOYEE INFORMATION",
                Font = new Font("Segoe UI Semibold", 8f),
                ForeColor = Color.FromArgb(156, 163, 175),
                AutoSize = true,
                Location = new Point(detailX, avatarY)
            };
            card.Controls.Add(lblSectionHeading);

            var detailBlock = new Panel
            {
                Location = new Point(detailX, lblSectionHeading.Bottom + 8),
                Size = new Size(detailW, 0),
                BackColor = ThemeHelper.IsDarkMode ? Color.FromArgb(36, 39, 46) : Color.FromArgb(249, 250, 251)
            };
            card.Controls.Add(detailBlock);

            int rowY = 14;
            void DetailRow(string icon, string label, string? value, bool isLast = false)
            {
                var iconLbl = new Label
                {
                    Text = icon,
                    Font = new Font("Segoe UI Emoji", 9.5f),
                    ForeColor = ThemeHelper.TextSecondary,
                    AutoSize = false,
                    Size = new Size(22, 18),
                    Location = new Point(14, rowY),
                    TextAlign = ContentAlignment.MiddleCenter
                };
                detailBlock.Controls.Add(iconLbl);

                var labelLbl = new Label
                {
                    Text = label,
                    Font = new Font("Segoe UI", 8f),
                    ForeColor = ThemeHelper.TextSecondary,
                    AutoSize = true,
                    Location = new Point(40, rowY)
                };
                detailBlock.Controls.Add(labelLbl);

                string displayValue = string.IsNullOrWhiteSpace(value) ? "—" : value;
                int valueWidth = detailW - 50;

                var valueLbl = new Label
                {
                    Text = displayValue,
                    Font = new Font("Segoe UI Semibold", 9.25f),
                    ForeColor = ThemeHelper.TextPrimary,
                    AutoSize = false,
                    Size = new Size(valueWidth, 18),
                    Location = new Point(40, rowY + 15),
                    AutoEllipsis = false
                };
                // Grow the value label (and thus the row) to fit wrapped text
                // instead of clipping — long emails/names now wrap to a 2nd
                // line rather than getting cut off or ellipsis-truncated.
                int prefH = valueLbl.GetPreferredSize(new Size(valueWidth, 0)).Height;
                valueLbl.Height = Math.Max(18, prefH);
                detailBlock.Controls.Add(valueLbl);

                rowY += Math.Max(46, 15 + valueLbl.Height + 12);

                if (!isLast)
                {
                    var divider = new Panel
                    {
                        Location = new Point(14, rowY - 10),
                        Size = new Size(detailW - 28, 1),
                        BackColor = ThemeHelper.IsDarkMode ? Color.FromArgb(55, 58, 65) : Color.FromArgb(229, 231, 235)
                    };
                    detailBlock.Controls.Add(divider);
                }
            }

            DetailRow("🆔", "Employee Code", _employee.EmployeeCode);
            DetailRow("💼", "Designation", _employee.Department);
            DetailRow("✉", "Email", _employee.Email);
            DetailRow("📞", "Phone", _employee.Phone);
            DetailRow("📋", "Active Allocations", GetActiveAllocationCount().ToString(), isLast: true);

            detailBlock.Height = rowY;

            void RoundDetailBlock(object? s, EventArgs e)
            {
                if (detailBlock.Width <= 0 || detailBlock.Height <= 0) return;
                using var path = new System.Drawing.Drawing2D.GraphicsPath();
                float r = 10f, d = r * 2f;
                var rect = new RectangleF(0, 0, detailBlock.Width, detailBlock.Height);
                path.AddArc(rect.X, rect.Y, d, d, 180, 90);
                path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
                path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
                path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
                path.CloseFigure();
                detailBlock.Region = new Region(path);
            }
            detailBlock.Resize += RoundDetailBlock;
            RoundDetailBlock(null, EventArgs.Empty);

            int cardH = Math.Max(badge.Bottom + 24, detailBlock.Bottom + 24);
            card.Height = cardH;

            // ── Close Button ──
            int btnY = header.Height + PaddingOuter + cardH + 16;
            var btnClose = new Button
            {
                Text = "Close",
                Size = new Size(120, 38),
                Location = new Point((DialogWidth - 120) / 2, btnY),
                Font = ThemeHelper.FontBold,
                FlatStyle = FlatStyle.Flat,
                BackColor = ThemeHelper.Accent,
                ForeColor = Color.White,
                Cursor = Cursors.Hand,
                UseVisualStyleBackColor = false
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.FlatAppearance.MouseOverBackColor = ThemeHelper.Header;

            void RoundButton(object? s, EventArgs e)
            {
                if (btnClose.Width <= 0 || btnClose.Height <= 0) return;
                using var path = new System.Drawing.Drawing2D.GraphicsPath();
                float r = 8f, d = r * 2f;
                var rect = new RectangleF(0, 0, btnClose.Width, btnClose.Height);
                path.AddArc(rect.X, rect.Y, d, d, 180, 90);
                path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
                path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
                path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
                path.CloseFigure();
                btnClose.Region = new Region(path);
            }
            btnClose.Resize += RoundButton;
            RoundButton(null, EventArgs.Empty);

            btnClose.Click += (s, e) => Close();
            Controls.Add(btnClose);

            ClientSize = new Size(DialogWidth, btnY + 38 + PaddingOuter);

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