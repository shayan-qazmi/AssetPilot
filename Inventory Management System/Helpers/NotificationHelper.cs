// Helpers/NotificationHelper.cs
using Inventory_Management_System.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace Inventory_Management_System.Helpers
{
    public class NotificationItem
    {
        public string Title { get; set; } = "";
        public string Subtitle { get; set; } = "";
        public string Icon { get; set; } = "⚠";
        public Color AccentColor { get; set; } = Color.FromArgb(239, 68, 68);
    }

    public static class NotificationHelper
    {
        private static Label _bellLabel = null!;
        private static Label _badgeLabel = null!;
        private static System.Windows.Forms.Timer? _refreshTimer;
        private static Form? _popup;

        private const int RefreshIntervalMs = 60000; // 1 minute

        /// Call once, from DashboardForm, after panelTop exists.
        public static void AttachBell(Panel hostTopPanel, Form ownerForm)
        {
            _bellLabel = new Label
            {
                Text = "🔔",
                Font = new Font("Segoe UI Emoji", 15f, FontStyle.Regular),
                ForeColor = Color.FromArgb(33, 33, 33),
                BackColor = Color.Transparent,
                AutoSize = false,
                Size = new Size(44, 44),
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            _bellLabel.Location = new Point(hostTopPanel.Width - 60, (hostTopPanel.Height - 44) / 2);
            hostTopPanel.Controls.Add(_bellLabel);
            _bellLabel.BringToFront();

            hostTopPanel.Resize += (s, e) =>
            {
                _bellLabel.Location = new Point(hostTopPanel.Width - 60, (hostTopPanel.Height - 44) / 2);
            };

            _badgeLabel = new Label
            {
                Text = "0",
                Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                AutoSize = false,
                Size = new Size(18, 18),
                TextAlign = ContentAlignment.MiddleCenter,
                Visible = false
            };
            _badgeLabel.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using var brush = new SolidBrush(Color.FromArgb(239, 68, 68));
                g.FillEllipse(brush, 0, 0, _badgeLabel.Width - 1, _badgeLabel.Height - 1);
                using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                g.DrawString(_badgeLabel.Text, _badgeLabel.Font, Brushes.White,
                    new RectangleF(0, 0, _badgeLabel.Width, _badgeLabel.Height), sf);
            };
            hostTopPanel.Controls.Add(_badgeLabel);
            _badgeLabel.BringToFront();
            PositionBadge();

            _bellLabel.Click += (s, e) => ToggleDropdown(ownerForm);

            RefreshNow();

            _refreshTimer?.Stop();
            _refreshTimer?.Dispose();
            _refreshTimer = new System.Windows.Forms.Timer { Interval = RefreshIntervalMs };
            _refreshTimer.Tick += (s, e) => RefreshNow();
            _refreshTimer.Start();
        }

        private static void PositionBadge()
        {
            if (_bellLabel == null || _badgeLabel == null) return;
            _badgeLabel.Location = new Point(_bellLabel.Right - 16, _bellLabel.Top + 2);
        }

        public static void RefreshNow()
        {
            var items = GetNotifications();

            if (_badgeLabel == null) return;

            if (items.Count == 0)
            {
                _badgeLabel.Visible = false;
            }
            else
            {
                _badgeLabel.Text = items.Count > 9 ? "9+" : items.Count.ToString();
                _badgeLabel.Visible = true;
                PositionBadge();
                _badgeLabel.Invalidate();
            }
        }

        private static List<NotificationItem> GetNotifications()
        {
            var results = new List<NotificationItem>();

            try
            {
                using var db = new InventoryDbContext();

                var overdueAllocations = db.Allocations
                    .Where(a => a.Status == "approved" &&
                                a.ExpectedReturnDate.HasValue &&
                                a.ExpectedReturnDate.Value < DateOnly.FromDateTime(DateTime.Today) &&
                                a.ActualReturnDate == null)
                    .Include(a => a.Equipment)
                    .Include(a => a.Employee)
                    .OrderBy(a => a.ExpectedReturnDate)
                    .Take(10)
                    .ToList();

                foreach (var a in overdueAllocations)
                {
                    int daysLate = (DateTime.Today - a.ExpectedReturnDate!.Value.ToDateTime(TimeOnly.MinValue)).Days;
                    results.Add(new NotificationItem
                    {
                        Title = $"{a.Equipment?.EquipmentCode} overdue",
                        Subtitle = $"With {a.Employee?.FullName ?? "—"} • {daysLate} day(s) late",
                        Icon = "⏰",
                        AccentColor = Color.FromArgb(239, 68, 68)
                    });
                }

                var underRepair = db.Equipment
                    .Where(eq => eq.CurrentStatus == "under_repair")
                    .OrderBy(eq => eq.EquipmentCode)
                    .Take(10)
                    .ToList();

                foreach (var eq in underRepair)
                {
                    results.Add(new NotificationItem
                    {
                        Title = $"{eq.EquipmentCode} under repair",
                        Subtitle = eq.Name,
                        Icon = "🔧",
                        AccentColor = Color.FromArgb(255, 160, 0)
                    });
                }
            }
            catch
            {
                // Silent — notifications are a non-critical enhancement.
            }

            return results;
        }

        private static void ToggleDropdown(Form ownerForm)
        {
            if (_popup != null && !_popup.IsDisposed)
            {
                _popup.Close();
                _popup = null;
                return;
            }

            var items = GetNotifications();
            BuildDropdown(items, ownerForm);
        }

        private static void BuildDropdown(List<NotificationItem> items, Form ownerForm)
        {
            var popup = new Form
            {
                FormBorderStyle = FormBorderStyle.None,
                StartPosition = FormStartPosition.Manual,
                ShowInTaskbar = false,
                Size = new Size(340, Math.Min(420, 60 + Math.Max(1, items.Count) * 60)),
                BackColor = Color.White
            };

            var screenPoint = _bellLabel.PointToScreen(new Point(0, _bellLabel.Height));
            popup.Location = new Point(screenPoint.X - popup.Width + _bellLabel.Width, screenPoint.Y + 4);

            popup.Paint += (s, e) =>
            {
                using var pen = new Pen(Color.FromArgb(220, 220, 220), 1);
                e.Graphics.DrawRectangle(pen, 0, 0, popup.Width - 1, popup.Height - 1);
            };

            var header = new Label
            {
                Text = "Notifications",
                Dock = DockStyle.Top,
                Height = 40,
                Font = ThemeHelper.FontBold,
                ForeColor = ThemeHelper.TextPrimary,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(14, 0, 0, 0),
                BackColor = Color.FromArgb(249, 250, 251)
            };
            popup.Controls.Add(header);

            if (items.Count == 0)
            {
                var lblEmpty = new Label
                {
                    Text = "You're all caught up 🎉",
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleCenter,
                    ForeColor = ThemeHelper.TextSecondary,
                    Font = ThemeHelper.FontDefault
                };
                popup.Controls.Add(lblEmpty);
                lblEmpty.BringToFront();
            }
            else
            {
                var listPanel = new Panel
                {
                    Dock = DockStyle.Fill,
                    AutoScroll = true,
                    BackColor = Color.White
                };
                popup.Controls.Add(listPanel);
                listPanel.BringToFront();

                int y = 0;
                foreach (var item in items)
                {
                    var row = new Panel
                    {
                        Location = new Point(0, y),
                        Size = new Size(listPanel.Width - 18, 58),
                        BackColor = Color.White
                    };

                    var lblIcon = new Label
                    {
                        Text = item.Icon,
                        Font = new Font("Segoe UI Emoji", 12f),
                        ForeColor = item.AccentColor,
                        Location = new Point(12, 10),
                        Size = new Size(30, 30),
                        TextAlign = ContentAlignment.MiddleCenter
                    };
                    row.Controls.Add(lblIcon);

                    var lblTitle = new Label
                    {
                        Text = item.Title,
                        Font = ThemeHelper.FontBold,
                        ForeColor = ThemeHelper.TextPrimary,
                        Location = new Point(50, 8),
                        Size = new Size(270, 20),
                        AutoEllipsis = true
                    };
                    row.Controls.Add(lblTitle);

                    var lblSubtitle = new Label
                    {
                        Text = item.Subtitle,
                        Font = ThemeHelper.FontSmall,
                        ForeColor = ThemeHelper.TextSecondary,
                        Location = new Point(50, 28),
                        Size = new Size(270, 20),
                        AutoEllipsis = true
                    };
                    row.Controls.Add(lblSubtitle);

                    row.Paint += (s, e) =>
                    {
                        using var pen = new Pen(Color.FromArgb(240, 240, 240), 1);
                        e.Graphics.DrawLine(pen, 12, row.Height - 1, row.Width - 12, row.Height - 1);
                    };

                    listPanel.Controls.Add(row);
                    y += 58;
                }
            }

            popup.Deactivate += (s, e) => { popup.Close(); };
            popup.FormClosed += (s, e) => { _popup = null; };

            _popup = popup;
            popup.Show(ownerForm);
        }
    }
}