// Forms/AllocationDetailPopup.cs
using System;
using System.Drawing;
using System.Windows.Forms;
using Inventory_Management_System.Helpers;

namespace Inventory_Management_System.Forms
{
    // Plain data carrier passed in from AllocationForm — avoids exposing
    // the private nested AllocationRow class outside AllocationForm.
    public class AllocationDetailInfo
    {
        public string EquipmentCode { get; set; } = "—";
        public string EquipmentName { get; set; } = "—";
        public string QrCode { get; set; } = "—";
        public string SerialNumber { get; set; } = "—";
        public string Make { get; set; } = "—";
        public string Model { get; set; } = "—";
        public string? PicturePath { get; set; }
        public string Employee { get; set; } = "—";
        public string Location { get; set; } = "—";
        public string DateAllocated { get; set; } = "—";
        public string ExpectedReturn { get; set; } = "—";
        public string Status { get; set; } = "—";
        public string Source { get; set; } = "—";
        public string AllocatedBy { get; set; } = "—";
    }

    // Opened on double-click of a row in the Allocation grid.
    // Mirrors EmployeeDetailPopup / equipment detail popup's layout:
    // icon block (+ View Picture) on the left, labeled detail rows on the right.
    public class AllocationDetailPopup : Form
    {
        private readonly AllocationDetailInfo _info;
        private Image? _pictureImage;

        private const int DialogWidth = 560;
        private const int PaddingOuter = 20;
        private const int IconSize = 140;

        private static Color BgPage => ThemeHelper.DashboardBg;
        private static Color CardBg => ThemeHelper.CardBg;
        private static Color CardBorderColor => ThemeHelper.IsDarkMode ? Color.FromArgb(55, 58, 65) : Color.FromArgb(220, 225, 230);
        private static readonly Color MutedText = Color.FromArgb(156, 163, 175);

        public AllocationDetailPopup(AllocationDetailInfo info)
        {
            _info = info;

            // Same resolution logic EmployeeDetailPopup/UserDetailPopup use for
            // their pictures — AvatarHelper knows how PicturePath values need to
            // be resolved (this is NOT a plain File.Exists() check on the raw path).
            try
            {
                _pictureImage = AvatarHelper.TryLoadEquipmentImage(_info.PicturePath);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "AllocationDetailPopup.TryLoadEquipmentImage");
                _pictureImage = null;
            }

            InitializeUI();
        }

        private (Color bg, Color fg) StatusColors(string status) => status switch
        {
            "approved" => (Color.FromArgb(220, 252, 231), Color.FromArgb(21, 128, 61)),
            "pending" or "Pending" => (Color.FromArgb(255, 249, 196), Color.FromArgb(130, 100, 0)),
            "rejected" => (Color.FromArgb(255, 205, 210), Color.FromArgb(183, 28, 28)),
            "returned" => (Color.FromArgb(224, 224, 224), Color.FromArgb(66, 66, 66)),
            "Overdue" => (Color.FromArgb(255, 205, 210), Color.FromArgb(183, 28, 28)),
            "available" => (Color.FromArgb(224, 231, 255), Color.FromArgb(63, 81, 181)),
            _ => (Color.FromArgb(229, 231, 235), Color.FromArgb(107, 114, 128))
        };

        private void InitializeUI()
        {
            Text = $"Allocation Details — {_info.EquipmentCode}";
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
                Text = "🖥",
                Font = new Font("Segoe UI Emoji", 13f),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(20, 16),
                BackColor = Color.Transparent
            };
            header.Controls.Add(lblHeaderIcon);

            var lblTitle = new Label
            {
                Text = "Allocation Details",
                Location = new Point(50, 15),
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

            int iconX = 28, iconY = 28;
            var iconPanel = new Panel
            {
                Location = new Point(iconX, iconY),
                Size = new Size(IconSize, IconSize),
                BackColor = Color.Transparent
            };
            iconPanel.Paint += (s, e) => DrawEquipmentIcon(e.Graphics, new Rectangle(0, 0, IconSize - 1, IconSize - 1));
            card.Controls.Add(iconPanel);

            var lblCodeUnder = new Label
            {
                Text = _info.EquipmentCode,
                Font = new Font("Segoe UI Semibold", 10.5f),
                ForeColor = ThemeHelper.TextPrimary,
                AutoSize = false,
                Size = new Size(IconSize, 20),
                Location = new Point(iconX, iconPanel.Bottom + 10),
                TextAlign = ContentAlignment.MiddleCenter,
                AutoEllipsis = true
            };
            card.Controls.Add(lblCodeUnder);

            var (badgeBg, badgeFg) = StatusColors(_info.Status);
            var badge = new Label
            {
                Text = "●  " + _info.Status,
                AutoSize = true,
                Font = new Font("Segoe UI Semibold", 8.25f),
                ForeColor = badgeFg,
                BackColor = badgeBg,
                Padding = new Padding(10, 5, 10, 5)
            };
            badge.Location = new Point(iconX + (IconSize - badge.PreferredWidth) / 2, lblCodeUnder.Bottom + 8);
            card.Controls.Add(badge);

            // ── View Picture — only clickable when a picture actually resolved ──
            bool hasPicture = _pictureImage != null;
            var btnViewPicture = new Button
            {
                Text = hasPicture ? "🖼  View Picture" : "🖼  No Picture",
                Size = new Size(IconSize, 30),
                Location = new Point(iconX, badge.Bottom + 12),
                Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Enabled = hasPicture,
                Cursor = hasPicture ? Cursors.Hand : Cursors.Default,
                UseVisualStyleBackColor = false
            };
            if (hasPicture)
            {
                btnViewPicture.BackColor = Color.FromArgb(238, 242, 255);
                btnViewPicture.ForeColor = ThemeHelper.Header;
                btnViewPicture.FlatAppearance.BorderColor = Color.FromArgb(199, 210, 254);
                btnViewPicture.FlatAppearance.MouseOverBackColor = Color.FromArgb(224, 231, 255);
                btnViewPicture.Click += BtnViewPicture_Click;
            }
            else
            {
                btnViewPicture.BackColor = Color.FromArgb(243, 244, 246);
                btnViewPicture.ForeColor = MutedText;
                btnViewPicture.FlatAppearance.BorderColor = Color.FromArgb(229, 231, 235);
            }
            btnViewPicture.FlatAppearance.BorderSize = 1;
            card.Controls.Add(btnViewPicture);

            var btnViewLogs = new Button
            {
                Text = "📜  View Logs",
                Size = new Size(IconSize, 30),
                Location = new Point(iconX, btnViewPicture.Bottom + 8),
                Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                UseVisualStyleBackColor = false,
                BackColor = Color.FromArgb(238, 242, 255),
                ForeColor = ThemeHelper.Header,
            };
            btnViewLogs.FlatAppearance.BorderColor = Color.FromArgb(199, 210, 254);
            btnViewLogs.FlatAppearance.MouseOverBackColor = Color.FromArgb(224, 231, 255);
            btnViewLogs.FlatAppearance.BorderSize = 1;
            btnViewLogs.Click += (s, e) =>
            {
                var dashboard = Application.OpenForms.OfType<DashboardForm>().FirstOrDefault();
                this.Close();
                dashboard?.NavigateToLogs(_info.EquipmentCode);
            };
            card.Controls.Add(btnViewLogs);

            // ── Right side: detail block with icon-labeled rows ──
            int detailX = iconPanel.Right + 32;
            int detailW = (DialogWidth - PaddingOuter * 2) - detailX - 24;

            var lblSectionHeading = new Label
            {
                Text = "ALLOCATION INFORMATION",
                Font = new Font("Segoe UI Semibold", 8f),
                ForeColor = MutedText,
                AutoSize = true,
                Location = new Point(detailX, iconY)
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
            void DetailRow(string icon, string label, string? value, bool isLast = false, bool wrap = false)
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
                    ForeColor = MutedText,
                    AutoSize = true,
                    Location = new Point(40, rowY)
                };
                detailBlock.Controls.Add(labelLbl);

                string displayValue = string.IsNullOrWhiteSpace(value) ? "—" : value;
                int valueLblWidth = detailW - 50;

                if (wrap)
                {
                    // Measure how tall the text will be when wrapped
                    var measureFont = new Font("Segoe UI Semibold", 9.25f);
                    int textHeight;
                    using (var gfx = CreateGraphics())
                    {
                        var proposed = new Size(valueLblWidth, int.MaxValue);
                        textHeight = TextRenderer.MeasureText(gfx, displayValue, measureFont, proposed,
                            TextFormatFlags.WordBreak).Height;
                    }
                    textHeight = Math.Max(textHeight, 18);

                    var valueLbl = new Label
                    {
                        Text = displayValue,
                        Font = measureFont,
                        ForeColor = ThemeHelper.TextPrimary,
                        AutoSize = false,
                        Size = new Size(valueLblWidth, textHeight),
                        Location = new Point(40, rowY + 15),
                        AutoEllipsis = false
                    };
                    detailBlock.Controls.Add(valueLbl);

                    rowY += 15 + textHeight + 14;
                }
                else
                {
                    var valueLbl = new Label
                    {
                        Text = displayValue,
                        Font = new Font("Segoe UI Semibold", 9.25f),
                        ForeColor = ThemeHelper.TextPrimary,
                        AutoSize = false,
                        Size = new Size(valueLblWidth, 18),
                        Location = new Point(40, rowY + 15),
                        AutoEllipsis = true
                    };
                    detailBlock.Controls.Add(valueLbl);

                    rowY += 46;
                }

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

            DetailRow("🏷", "QR Code / Equipment Code", $"{_info.QrCode} / {_info.EquipmentCode}");
            DetailRow("🖥", "Equipment Name", _info.EquipmentName);
            DetailRow("🔢", "Serial / Make / Model",
                $"{_info.SerialNumber}  •  {_info.Make}  {_info.Model}".Trim());
            DetailRow("👤", "Employee", _info.Employee);
            DetailRow("📍", "Location", _info.Location, wrap: true);
            DetailRow("📅", "Date Allocated", _info.DateAllocated);
            DetailRow("⏳", "Expected Return", _info.ExpectedReturn);
            DetailRow("🗂", "Source", _info.Source);
            DetailRow("✍", "Allocated By", _info.AllocatedBy, isLast: true);

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

            int cardH = Math.Max(btnViewLogs.Bottom + 24, detailBlock.Bottom + 24);
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

            FormClosed += (s, e) => _pictureImage?.Dispose();
        }

        private void BtnViewPicture_Click(object? sender, EventArgs e)
        {
            if (_pictureImage == null) return;

            using var viewer = new Form
            {
                Text = $"Picture — {_info.EquipmentCode}",
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.Sizable,
                MinimizeBox = false,
                BackColor = Color.Black,
                ClientSize = new Size(
                    Math.Clamp(_pictureImage.Width, 320, 800),
                    Math.Clamp(_pictureImage.Height, 240, 600))
            };

            var pic = new PictureBox
            {
                Image = _pictureImage,
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Black
            };
            viewer.Controls.Add(pic);

            viewer.ShowDialog(this);
        }

        /// Attempts to load an equipment item's picture from the shared network folder.
        /// Never throws — returns null (caller falls back to the default
        /// placeholder/no-picture state) if the share is unreachable or the file
        /// is missing.
        public static Image? TryLoadEquipmentImage(string? storedPicturePath)
        {
            if (string.IsNullOrWhiteSpace(storedPicturePath)) return null;

            try
            {
                NetworkShareHelper.EnsureConnected();
                string fullPath = NetworkPaths.GetEquipmentPictureFullPath(storedPicturePath);
                if (!File.Exists(fullPath)) return null;

                using var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read);
                return Image.FromStream(stream);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "AvatarHelper.TryLoadEquipmentImage");
                return null;
            }
        }

        private void DrawEquipmentIcon(Graphics g, Rectangle bounds)
        {
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            // If a real picture resolved, show a thumbnail of it instead of the placeholder icon —
            // matches the equipment popup's "picture where available" behavior.
            if (_pictureImage != null)
            {
                using var path = RoundedRect(bounds, 14);
                var oldClip = g.Clip;
                g.SetClip(path);
                g.DrawImage(_pictureImage, bounds);
                g.Clip = oldClip;
                return;
            }

            using (var path = RoundedRect(bounds, 14))
            using (var bgBrush = new SolidBrush(Color.FromArgb(238, 242, 255)))
                g.FillPath(bgBrush, path);

            var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            using var font = new Font("Segoe UI Emoji", 42f);
            using var textBrush = new SolidBrush(ThemeHelper.Header);
            g.DrawString("🖥", font, textBrush, bounds, sf);
        }

        private static System.Drawing.Drawing2D.GraphicsPath RoundedRect(Rectangle bounds, float radius)
        {
            var path = new System.Drawing.Drawing2D.GraphicsPath();
            float d = radius * 2f;
            path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
            path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
            path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
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