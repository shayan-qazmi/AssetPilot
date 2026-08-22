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
    public enum NotificationSection { Urgent, Info }

    public class NotificationItem
    {
        public string Title { get; set; } = "";
        public string Subtitle { get; set; } = "";
        public string Icon { get; set; } = "⚠";
        public Color AccentColor { get; set; } = Color.FromArgb(239, 68, 68);
        public NotificationSection Section { get; set; } = NotificationSection.Urgent;
    }

    public static class NotificationHelper
    {
        private static Panel _bellLabel = null!;
        private static Label _badgeLabel = null!;
        private static System.Windows.Forms.Timer? _refreshTimer;
        private static Form? _popup;

        private const int RefreshIntervalMs = 60000; // 1 minute

        /// Call once, from DashboardForm, after panelTop exists.
        public static void AttachBell(Panel hostTopPanel, Form ownerForm, int rightOffset = 60)
        {
            hostTopPanel.Controls.OfType<Control>().Where(c => c == _bellLabel || c == _badgeLabel).ToList()
        .ForEach(c => { hostTopPanel.Controls.Remove(c); c.Dispose(); });

            const int d = 34;

            _bellLabel = new Panel
            {
                Size = new Size(d, d),
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };

            _bellLabel.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using var iconFont = new Font("Segoe UI Emoji", 15f);
                using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                using var iconFg = new SolidBrush(Color.FromArgb(33, 33, 33));
                g.DrawString("🔔", iconFont, iconFg, new Rectangle(0, 0, d, d), sf);
            };

            _bellLabel.Location = new Point(hostTopPanel.Width - rightOffset, (hostTopPanel.Height - d) / 2);

            hostTopPanel.Controls.Add(_bellLabel);
            _bellLabel.BringToFront();

            hostTopPanel.Resize += (s, e) =>
            {
                _bellLabel.Location = new Point(hostTopPanel.Width - rightOffset, (hostTopPanel.Height - d) / 2);
                PositionBadge();
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
            _badgeLabel.Location = new Point(_bellLabel.Right - 14, _bellLabel.Top - 2);
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
                var now = DateTime.UtcNow;

                // ── Urgent: overdue allocations ──
                var overdueAllocations = db.Allocations
                    .Where(a => a.Status == "approved" &&
                                a.ExpectedReturnDate.HasValue &&
                                a.ExpectedReturnDate.Value < DateOnly.FromDateTime(DateTime.Today) &&
                                a.ActualReturnDate == null)
                    .Include(a => a.Equipment)
                    .Include(a => a.Employee)
                    .OrderBy(a => a.ExpectedReturnDate)
                    .Take(8)
                    .ToList();

                foreach (var a in overdueAllocations)
                {
                    int daysLate = (DateTime.Today - a.ExpectedReturnDate!.Value.ToDateTime(TimeOnly.MinValue)).Days;
                    results.Add(new NotificationItem
                    {
                        Title = $"{a.Equipment?.EquipmentCode} overdue",
                        Subtitle = $"With {a.Employee?.FullName ?? "—"} • {daysLate} day(s) late",
                        Icon = "⏰",
                        AccentColor = Color.FromArgb(239, 68, 68),
                        Section = NotificationSection.Urgent
                    });
                }

                // ── Urgent: overdue Check-In/Out returns ──
                var overdueCheckouts = db.Transactions
                    .Include(t => t.Equipment)
                    .Where(t => t.TransactionType == "Check-Out" && t.Status == "Overdue")
                    .OrderBy(t => t.ExpectedReturn)
                    .Take(8)
                    .ToList();

                foreach (var t in overdueCheckouts)
                {
                    string daysLate = t.ExpectedReturn.HasValue
                        ? $"{(DateTime.Today - t.ExpectedReturn.Value.ToDateTime(TimeOnly.MinValue)).Days} day(s) late"
                        : "overdue";
                    results.Add(new NotificationItem
                    {
                        Title = $"{t.Equipment?.EquipmentCode} checked out — overdue",
                        Subtitle = $"With {(string.IsNullOrWhiteSpace(t.IssuedTo) ? "—" : t.IssuedTo)} • {daysLate}",
                        Icon = "⏰",
                        AccentColor = Color.FromArgb(239, 68, 68),
                        Section = NotificationSection.Urgent
                    });
                }

                // ── Urgent: pending allocation approvals ──
                var pendingAllocations = db.Allocations
                    .Where(a => a.Status == "pending")
                    .Include(a => a.Equipment)
                    .Include(a => a.Employee)
                    .OrderBy(a => a.AllocationDate)
                    .Take(8)
                    .ToList();

                foreach (var a in pendingAllocations)
                {
                    var waiting = now - (a.AllocationDate.Kind == DateTimeKind.Utc ? a.AllocationDate : DateTime.SpecifyKind(a.AllocationDate, DateTimeKind.Utc));
                    string waitStr = waiting.TotalHours < 24
                        ? $"{(int)waiting.TotalHours} hr(s) ago"
                        : $"{(int)waiting.TotalDays} day(s) ago";
                    results.Add(new NotificationItem
                    {
                        Title = $"{a.Equipment?.EquipmentCode} awaiting approval",
                        Subtitle = $"Requested for {a.Employee?.FullName ?? "—"} • {waitStr}",
                        Icon = "📋",
                        AccentColor = Color.FromArgb(245, 158, 11),
                        Section = NotificationSection.Urgent
                    });
                }

                // ── Info: equipment under repair too long (>7 days since maintenance_opened) ──
                var underRepair = db.Equipment
                    .Where(eq => eq.CurrentStatus == "under_repair")
                    .OrderBy(eq => eq.EquipmentCode)
                    .ToList();

                if (underRepair.Count > 0)
                {
                    var repairIds = underRepair.Select(e => e.Id).ToList();
                    var openedDates = db.EquipmentLogs
                        .Where(l => l.ActionType == "maintenance_opened" && repairIds.Contains(l.EquipmentId))
                        .GroupBy(l => l.EquipmentId)
                        .Select(g => new { EquipmentId = g.Key, Opened = g.Max(l => l.CreatedAt) })
                        .ToList()
                        .ToDictionary(x => x.EquipmentId, x => x.Opened);

                    foreach (var eq in underRepair.Take(8))
                    {
                        string subtitle = eq.Name;
                        if (openedDates.TryGetValue(eq.Id, out var opened) && opened.HasValue)
                        {
                            int days = (DateTime.UtcNow - opened.Value).Days;
                            subtitle = days > 0 ? $"{eq.Name} • {days} day(s) in repair" : eq.Name;
                        }
                        results.Add(new NotificationItem
                        {
                            Title = $"{eq.EquipmentCode} under repair",
                            Subtitle = subtitle,
                            Icon = "🔧",
                            AccentColor = Color.FromArgb(255, 160, 0),
                            Section = NotificationSection.Info
                        });
                    }
                }

                // ── Info: recently decommissioned (last 24h) — only items that are
                // still decommissioned right now; a later reversal (edit back to
                // in_store) should clear this notification even though the log entry
                // itself never goes away.
                var recentDecomm = db.EquipmentLogs
                    .Where(l => l.ActionType == "decommission_approved" &&
                                l.CreatedAt.HasValue && l.CreatedAt.Value >= now.AddHours(-24))
                    .OrderByDescending(l => l.CreatedAt)
                    .ToList();

                if (recentDecomm.Count > 0)
                {
                    var eqIds = recentDecomm.Select(l => l.EquipmentId).Distinct().ToList();
                    var eqMap = db.Equipment
                        .Where(e => eqIds.Contains(e.Id) && e.CurrentStatus == "decommissioned")
                        .ToDictionary(e => e.Id, e => e);

                    foreach (var log in recentDecomm)
                    {
                        if (!eqMap.TryGetValue(log.EquipmentId, out var eq)) continue; // no longer decommissioned — skip

                        results.Add(new NotificationItem
                        {
                            Title = $"{eq.EquipmentCode} decommissioned",
                            Subtitle = log.CreatedAt.HasValue ? $"{(int)(now - log.CreatedAt.Value).TotalHours} hr(s) ago" : "Recently",
                            Icon = "🚫",
                            AccentColor = Color.FromArgb(107, 114, 128),
                            Section = NotificationSection.Info
                        });

                        if (results.Count(r => r.Section == NotificationSection.Info) >= 8) break;
                    }
                }
            }
            catch
            {
                // Silent — notifications are a non-critical enhancement.
            }

            // Urgent first, most-relevant subset from each type already capped above;
            // hard cap the total so the dropdown never grows unbounded.
            return results
                .OrderBy(r => r.Section)
                .Take(20)
                .ToList();
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
                Size = new Size(340, Math.Min(460, 60 + Math.Max(1, items.Count) * 58 + 52)),
                BackColor = ThemeHelper.PopupBg
            };

            var screenPoint = _bellLabel.PointToScreen(new Point(0, _bellLabel.Height));
            popup.Location = new Point(screenPoint.X - popup.Width + _bellLabel.Width, screenPoint.Y + 4);

            popup.Paint += (s, e) =>
            {
                using var pen = new Pen(ThemeHelper.PopupBorder, 1);
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
                BackColor = ThemeHelper.PopupHoverBg
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
                    BackColor = ThemeHelper.PopupBg
                };
                popup.Controls.Add(listPanel);
                listPanel.BringToFront();

                int y = 0;

                void AddSectionLabel(string text)
                {
                    var lbl = new Label
                    {
                        Text = text,
                        Location = new Point(14, y + 8),
                        AutoSize = true,
                        Font = ThemeHelper.FontSmall,
                        ForeColor = ThemeHelper.TextSecondary
                    };
                    listPanel.Controls.Add(lbl);
                    y += 26;
                }

                void AddRow(NotificationItem item)
                {
                    var row = new Panel
                    {
                        Location = new Point(0, y),
                        Size = new Size(listPanel.Width - 18, 58),
                        BackColor = ThemeHelper.PopupBg
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
                        using var pen = new Pen(ThemeHelper.PopupBorder, 1);
                        e.Graphics.DrawLine(pen, 12, row.Height - 1, row.Width - 12, row.Height - 1);
                    };

                    listPanel.Controls.Add(row);
                    y += 58;
                }

                var urgent = items.Where(i => i.Section == NotificationSection.Urgent).ToList();
                var info = items.Where(i => i.Section == NotificationSection.Info).ToList();

                if (urgent.Count > 0)
                {
                    AddSectionLabel("NEEDS ATTENTION");
                    foreach (var item in urgent) AddRow(item);
                }
                if (info.Count > 0)
                {
                    AddSectionLabel("RECENT ACTIVITY");
                    foreach (var item in info) AddRow(item);
                }
            }

            popup.Deactivate += (s, e) => { popup.Close(); };
            popup.FormClosed += (s, e) => { _popup = null; };

            _popup = popup;
            popup.Show(ownerForm);
        }
    }
}