// Forms/EquipmentDetailPopup.cs
using Inventory_Management_System.Helpers;
using Inventory_Management_System.Models;
using Inventory_Management_System.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace Inventory_Management_System.Forms
{
    public class EquipmentDetailPopup : Form
    {
        private Equipment _equipment;

        // ── Compact Layout Constants ─────────────────────────────────────────
        private const int DialogWidth = 760;
        private const int PaddingOuter = 8;
        private const int CardPadding = 8;
        private const int ColGap = 16;
        private const int RowH = 26;
        private const int SectionGap = 6;
        private const int FooterHeight = 42;

        private int ColWidth;
        private int ColLeft;
        private int ColRight;
        private int LabelW;
        private int ValueX;
        private int ValueW;

        // ── Colors ─────────────────────────────────────────────────────────
        private static readonly Color BgPage = Color.FromArgb(245, 247, 250);
        private static readonly Color CardBg = Color.White;
        private static readonly Color CardBorderColor = Color.FromArgb(220, 225, 230);
        private static readonly Color SectionTitle = Color.FromArgb(46, 125, 50);
        private static readonly Color RowEven = Color.White;
        private static readonly Color RowOdd = Color.FromArgb(248, 250, 248);
        private static readonly Color SeparatorColor = Color.FromArgb(234, 237, 241);

        public EquipmentDetailPopup(Equipment equipment)
        {
            _equipment = equipment;

            using var db = new InventoryDbContext();
            var eq = db.Equipment
                .Include(e => e.Allocations)
                    .ThenInclude(a => a.Location)
                .Include(e => e.Allocations)
                    .ThenInclude(a => a.Employee)
                .Include(e => e.ProcurementType)
                .FirstOrDefault(e => e.Id == equipment.Id);
            if (eq != null) _equipment = eq;

            InitializeUI();
        }

        private void InitializeUI()
        {
            Text = $"Equipment Details — {_equipment.EquipmentCode}";
            AutoScaleMode = AutoScaleMode.None;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            BackColor = BgPage;

            var _ = this.Handle;

            // ── Header Bar ───────────────────────────────────────────────────
            var header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 38,
                BackColor = ThemeHelper.Header,
            };

            var lblHeaderIcon = new Label
            {
                Text = "⊞",
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(12, 8),
                BackColor = Color.Transparent
            };
            header.Controls.Add(lblHeaderIcon);

            var lblTitle = new Label
            {
                Text = $"Equipment Details — {_equipment.EquipmentCode}",
                Location = new Point(34, 9),
                AutoSize = true,
                Font = new Font("Segoe UI Semibold", 10f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.Transparent
            };
            header.Controls.Add(lblTitle);
            Controls.Add(header);

            // ── Footer Bar (Centered Close Button) ───────────────────────────
            var footer = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = FooterHeight,
                BackColor = BgPage,
            };

            var btnClose = new Button
            {
                Text = "✕  Close",
                Size = new Size(100, 30),
                Font = ThemeHelper.FontSmall,
                Cursor = Cursors.Hand,
                TabIndex = 0,
            };
            ThemeHelper.StyleSecondaryButton(btnClose);
            btnClose.Click += (s, e) => Close();
            footer.Controls.Add(btnClose);

            // Keep close button precisely centered
            footer.Resize += (s, e) =>
            {
                btnClose.Location = new Point((footer.Width - btnClose.Width) / 2, (footer.Height - btnClose.Height) / 2);
            };

            Controls.Add(footer);

            // ── Scrollable Content Area ─────────────────────────────────────
            var scrollHost = new Panel
            {
                BackColor = BgPage,
                AutoScroll = true,
                TabIndex = 1,
            };

            var card = new Panel
            {
                BackColor = CardBg,
                Location = new Point(PaddingOuter, PaddingOuter),
            };
            card.Paint += DrawCardBorder;
            scrollHost.Controls.Add(card);

            var workArea = Screen.GetWorkingArea(this);
            int formWidth = Math.Min(DialogWidth, workArea.Width - 40);
            int maxContentHeight = Math.Max(200, workArea.Height - header.Height - footer.Height - 60);

            int fullCardWidth = formWidth - (PaddingOuter * 2) - 16;
            int cardH = BuildCardContent(card, fullCardWidth);

            bool needsScroll = (cardH + PaddingOuter * 2) > maxContentHeight;
            int cardWidth = fullCardWidth;

            if (needsScroll)
            {
                cardWidth = fullCardWidth - SystemInformation.VerticalScrollBarWidth;
                cardH = BuildCardContent(card, cardWidth);
            }

            card.Width = cardWidth;
            card.Height = cardH;

            // Height snapped directly to card size
            int contentHeight = cardH + (PaddingOuter * 2);
            int finalScrollHostHeight = Math.Min(contentHeight, maxContentHeight);

            scrollHost.Size = new Size(formWidth, finalScrollHostHeight);
            scrollHost.Location = new Point(0, header.Height);

            // Explicit form sizing fits contents with zero trailing empty gap
            ClientSize = new Size(formWidth, header.Height + finalScrollHostHeight + footer.Height);
            Controls.Add(scrollHost);

            // Position center on initial load
            btnClose.Location = new Point((footer.Width - btnClose.Width) / 2, (footer.Height - btnClose.Height) / 2);

            Shown += (s, e) =>
            {
                ActiveControl = btnClose;
                card.Invalidate(true);
            };
        }

        private int BuildCardContent(Panel card, int cardWidth)
        {
            card.SuspendLayout();
            card.Controls.Clear();

            ColWidth = (cardWidth - CardPadding * 2 - ColGap) / 2;
            ColLeft = CardPadding;
            ColRight = CardPadding + ColWidth + ColGap;
            LabelW = 155;
            ValueX = LabelW + 8;
            ValueW = ColWidth - ValueX - 6;

            int yLeft = SectionGap;
            int yRight = SectionGap;

            // ── Left Column: Equipment Information ──────────────────────────
            AddSectionHeader(card, "i", "Equipment Information", ColLeft, ref yLeft); // keeps "i" — reads fine as a circle badge now
            bool altLeft = false;
            AddField(card, "Equipment Code", _equipment.EquipmentCode, ColLeft, ref yLeft, ref altLeft);
            AddField(card, "QR Code", _equipment.QrCode, ColLeft, ref yLeft, ref altLeft);
            AddField(card, "Name", _equipment.Name, ColLeft, ref yLeft, ref altLeft);
            AddField(card, "Make", _equipment.Make, ColLeft, ref yLeft, ref altLeft);
            AddField(card, "Model", _equipment.Model, ColLeft, ref yLeft, ref altLeft);
            AddField(card, "Serial Number", _equipment.SerialNumber, ColLeft, ref yLeft, ref altLeft);
            AddField(card, "Category", _equipment.Category, ColLeft, ref yLeft, ref altLeft);
            AddStatusField(card, "Status", _equipment.CurrentStatus, ColLeft, ref yLeft, ref altLeft);
            AddField(card, "Condition", _equipment.ConditionGrade, ColLeft, ref yLeft, ref altLeft);
            AddField(card, "Purchase Date", _equipment.PurchaseDate?.ToString("yyyy-MM-dd"), ColLeft, ref yLeft, ref altLeft);
            AddField(card, "Purchase Price",
                _equipment.Price.HasValue ? $"₨{_equipment.Price.Value:N2}" : "—",
                ColLeft, ref yLeft, ref altLeft);
            AddField(card, "Procurement Type", _equipment.ProcurementType?.TypeName, ColLeft, ref yLeft, ref altLeft);
            AddField(card, "Vendor Name", _equipment.VendorName, ColLeft, ref yLeft, ref altLeft);
            AddField(card, "Warranty Expiry", _equipment.WarrantyExpiry?.ToString("yyyy-MM-dd") ?? "—", ColLeft, ref yLeft, ref altLeft);
            AddField(card, "Description", _equipment.Description, ColLeft, ref yLeft, ref altLeft);
            AddField(card, "Notes", _equipment.Notes, ColLeft, ref yLeft, ref altLeft);
            AddPictureField(card, "Picture", _equipment.PicturePath, ColLeft, ref yLeft, ref altLeft);

            // ── Right Column: Current Allocation ─────────────────────────────
            AddSectionHeader(card, "▣", "Current Allocation", ColRight, ref yRight);

            var activeAlloc = _equipment.Allocations.FirstOrDefault(a => a.Status == "approved");
            bool altRight = false;

            string locName = activeAlloc?.Location?.LocationName;
            string roomName = activeAlloc?.Location?.RoomName;
            string currentLocStr = "—";
            if (!string.IsNullOrEmpty(locName) && !string.IsNullOrEmpty(roomName))
                currentLocStr = $"{locName} - {roomName}";
            else if (!string.IsNullOrEmpty(locName))
                currentLocStr = locName;
            else if (!string.IsNullOrEmpty(roomName))
                currentLocStr = roomName;

            AddField(card, "Current Location", currentLocStr, ColRight, ref yRight, ref altRight);
            AddEmployeeField(card, "Currently Assigned To", activeAlloc?.Employee, ColRight, ref yRight, ref altRight);
            AddField(card, "Allocation Date", activeAlloc?.AllocationDate.ToString("yyyy-MM-dd") ?? "—", ColRight, ref yRight, ref altRight);
            AddField(card, "Expected Return Date", activeAlloc?.ExpectedReturnDate?.ToString("yyyy-MM-dd") ?? "—", ColRight, ref yRight, ref altRight);

            // ── Right Column: Maintenance History ────────────────────────────
            AddSectionHeader(card, "🔧", "Maintenance History", ColRight, ref yRight);

            List<MaintenanceRecord> records = new();
            try
            {
                var svc = new MaintenanceService();
                var result = svc.GetByEquipmentId(_equipment.Id);
                if (result.Success && result.Data != null)
                    records = result.Data;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "EquipmentDetailPopup.BuildCardContent (GetByEquipmentId)");
            }

            if (records.Count == 0)
            {
                AddField(card, "History", "No maintenance history recorded.", ColRight, ref yRight, ref altRight);
            }
            else
            {
                for (int i = 0; i < records.Count; i++)
                {
                    var rec = records[i];
                    AddField(card, $"Record #{i + 1}", rec.ReportedDate.ToString("yyyy-MM-dd"), ColRight, ref yRight, ref altRight);
                    AddMaintenanceStatusField(card, "Status", rec.Status, ColRight, ref yRight, ref altRight);
                    AddField(card, "Issue", rec.IssueDescription, ColRight, ref yRight, ref altRight);
                    AddField(card, "Serviced By", rec.ServicedBy, ColRight, ref yRight, ref altRight);
                    string costStr = rec.Cost.HasValue ? $"₨{rec.Cost.Value:N2}" : "—";
                    AddField(card, "Cost", costStr, ColRight, ref yRight, ref altRight);
                    AddField(card, "Resolved", rec.ResolvedDate?.ToString("yyyy-MM-dd"), ColRight, ref yRight, ref altRight);
                }
            }

            int dividerBottom = Math.Max(yLeft, yRight) - 4;
            card.Paint += (s, e) =>
            {
                using var pen = new Pen(SeparatorColor, 1);
                int dividerX = CardPadding + ColWidth + ColGap / 2;
                e.Graphics.DrawLine(pen, dividerX, SectionGap, dividerX, dividerBottom);
            };

            card.ResumeLayout(true);

            return Math.Max(yLeft, yRight) + 8;
        }

        private void AddSectionHeader(Panel card, string iconSymbol, string title, int x, ref int y)
        {
            var pnl = new Panel
            {
                Location = new Point(x, y),
                Size = new Size(ColWidth, 28),
                BackColor = CardBg,
            };

            const int badgeSize = 20;
            var iconBadge = new Label
            {
                Text = iconSymbol,
                Font = new Font("Segoe UI Emoji", 8.5f, FontStyle.Regular),
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                Size = new Size(badgeSize, badgeSize),
                Location = new Point(0, 3),
                TextAlign = ContentAlignment.MiddleCenter
            };
            iconBadge.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var brush = new SolidBrush(SectionTitle);
                e.Graphics.FillEllipse(brush, 0, 0, badgeSize - 1, badgeSize - 1);
                using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                using var textBrush = new SolidBrush(Color.White);
                e.Graphics.DrawString(iconSymbol, iconBadge.Font, textBrush,
                    new RectangleF(0, 0, badgeSize - 1, badgeSize - 1), sf);

                e.Graphics.DrawString(iconSymbol, iconBadge.Font, textBrush,
    new RectangleF(0, 0, badgeSize - 1, badgeSize - 1), sf);
            };

            var titleLabel = new Label
            {
                Text = title,
                Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold),
                ForeColor = SectionTitle,
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point(badgeSize + 8, 2)
            };

            pnl.Controls.Add(iconBadge);
            pnl.Controls.Add(titleLabel);

            pnl.Paint += (s, e) =>
            {
                using var pen = new Pen(Color.FromArgb(180, 215, 180), 1);
                e.Graphics.DrawLine(pen, 0, 25, ColWidth, 25);
            };

            card.Controls.Add(pnl);
            y += 32;
        }

        // Fixed: the row panel and the y-advance now use the SAME measured
        // height as the label itself, instead of a hardcoded RowH + 6. Before,
        // a wrapped multi-line value (e.g. a long location string) would be
        // sized correctly by GetPreferredSize() but then get clipped because
        // the containing panel stayed locked at 32px regardless. AutoEllipsis
        // is also off now so long text wraps instead of getting "…"-truncated.
        private void AddField(Panel card, string labelName, string? valueText, int x, ref int y, ref bool isAlt)
        {
            string val = string.IsNullOrWhiteSpace(valueText) ? "—" : valueText;

            var lblName = new Label
            {
                Text = labelName + ":",
                Location = new Point(8, 0),
                Size = new Size(LabelW, RowH),
                Font = ThemeHelper.FontBold,
                ForeColor = ThemeHelper.TextSecondary,
                TextAlign = ContentAlignment.MiddleLeft,
            };

            var lblValue = new Label
            {
                Text = val,
                Location = new Point(ValueX, 0),
                Size = new Size(ValueW, RowH),
                Font = ThemeHelper.FontDefault,
                ForeColor = ThemeHelper.TextPrimary,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoEllipsis = false,
            };

            int prefValH = lblValue.GetPreferredSize(new Size(ValueW, 0)).Height;
            int prefLblH = lblName.GetPreferredSize(new Size(LabelW, 0)).Height;
            int rowHeight = Math.Max(RowH, Math.Max(prefValH, prefLblH) + 6);
            lblName.Height = rowHeight;
            lblValue.Height = rowHeight;

            var pnl = new Panel
            {
                Location = new Point(x, y),
                Size = new Size(ColWidth, rowHeight + 6),
                BackColor = isAlt ? RowOdd : RowEven,
            };

            pnl.Paint += (s, e) =>
            {
                using var pen = new Pen(SeparatorColor, 1);
                e.Graphics.DrawLine(pen, 8, pnl.Height - 1, pnl.Width - 8, pnl.Height - 1);
            };

            pnl.Controls.Add(lblName);
            pnl.Controls.Add(lblValue);
            card.Controls.Add(pnl);

            y += rowHeight + 6;
            isAlt = !isAlt;
        }

        // New: same row shape as AddField, but the value is a clickable label
        // when an employee is present. Clicking closes this popup and opens
        // EmployeesForm pre-filtered to that employee, using the same
        // EmployeesForm(string employeeCode) constructor EquipmentForm already
        // uses for its own pending-selection flow.
        private void AddEmployeeField(Panel card, string labelName, Employee? employee, int x, ref int y, ref bool isAlt)
        {
            string val = employee?.FullName ?? "—";
            bool clickable = employee != null && !string.IsNullOrWhiteSpace(employee.EmployeeCode);

            var lblName = new Label
            {
                Text = labelName + ":",
                Location = new Point(8, 0),
                Size = new Size(LabelW, RowH),
                Font = ThemeHelper.FontBold,
                ForeColor = ThemeHelper.TextSecondary,
                TextAlign = ContentAlignment.MiddleLeft,
            };

            var lblValue = new Label
            {
                Text = val,
                Location = new Point(ValueX, 0),
                Size = new Size(ValueW, RowH),
                Font = clickable ? new Font(ThemeHelper.FontDefault, FontStyle.Underline) : ThemeHelper.FontDefault,
                ForeColor = clickable ? ThemeHelper.Header : ThemeHelper.TextPrimary,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoEllipsis = false,
                Cursor = clickable ? Cursors.Hand : Cursors.Default,
            };

            if (clickable)
            {
                lblValue.Click += (s, e) =>
                {
                    string code = employee!.EmployeeCode;
                    var dashboard = Application.OpenForms.OfType<DashboardForm>().FirstOrDefault();
                    this.Close();
                    dashboard?.NavigateToEmployee(code);
                };
            }

            int prefValH = lblValue.GetPreferredSize(new Size(ValueW, 0)).Height;
            int prefLblH = lblName.GetPreferredSize(new Size(LabelW, 0)).Height;
            int rowHeight = Math.Max(RowH, Math.Max(prefValH, prefLblH) + 6);
            lblName.Height = rowHeight;
            lblValue.Height = rowHeight;

            var pnl = new Panel
            {
                Location = new Point(x, y),
                Size = new Size(ColWidth, rowHeight + 6),
                BackColor = isAlt ? RowOdd : RowEven,
            };

            pnl.Paint += (s, e) =>
            {
                using var pen = new Pen(SeparatorColor, 1);
                e.Graphics.DrawLine(pen, 8, pnl.Height - 1, pnl.Width - 8, pnl.Height - 1);
            };

            pnl.Controls.Add(lblName);
            pnl.Controls.Add(lblValue);
            card.Controls.Add(pnl);

            y += rowHeight + 6;
            isAlt = !isAlt;
        }

        // Now matches AllocationDetailPopup's btnViewPicture exactly: same
        // size (140x30), same font, same enabled/disabled colors, and the
        // same "No Picture" disabled state instead of a plain "—" label.
        private void AddPictureField(Panel card, string labelName, string? picturePath, int x, ref int y, ref bool isAlt)
        {
            bool hasPicture = !string.IsNullOrWhiteSpace(picturePath);
            const int rowHeight = 44;

            var lblName = new Label
            {
                Text = labelName + ":",
                Location = new Point(8, 0),
                Size = new Size(LabelW, rowHeight),
                Font = ThemeHelper.FontBold,
                ForeColor = ThemeHelper.TextSecondary,
                TextAlign = ContentAlignment.MiddleLeft,
            };

            var pnl = new Panel
            {
                Location = new Point(x, y),
                Size = new Size(ColWidth, rowHeight),
                BackColor = isAlt ? RowOdd : RowEven,
            };

            pnl.Paint += (s, e) =>
            {
                using var pen = new Pen(SeparatorColor, 1);
                e.Graphics.DrawLine(pen, 8, pnl.Height - 1, pnl.Width - 8, pnl.Height - 1);
            };

            pnl.Controls.Add(lblName);

            var btnView = new Button
            {
                Text = hasPicture ? "🖼  View Picture" : "🖼  No Picture",
                Size = new Size(140, 30),
                Location = new Point(ValueX, (rowHeight - 30) / 2),
                Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Enabled = hasPicture,
                Cursor = hasPicture ? Cursors.Hand : Cursors.Default,
                UseVisualStyleBackColor = false,
            };
            if (hasPicture)
            {
                btnView.BackColor = Color.FromArgb(238, 242, 255);
                btnView.ForeColor = ThemeHelper.Header;
                btnView.FlatAppearance.BorderColor = Color.FromArgb(199, 210, 254);
                btnView.FlatAppearance.MouseOverBackColor = Color.FromArgb(224, 231, 255);
                btnView.Click += (s, e) => ShowPicture(picturePath!);
            }
            else
            {
                btnView.BackColor = Color.FromArgb(243, 244, 246);
                btnView.ForeColor = Color.FromArgb(156, 163, 175);
                btnView.FlatAppearance.BorderColor = Color.FromArgb(229, 231, 235);
            }
            btnView.FlatAppearance.BorderSize = 1;
            pnl.Controls.Add(btnView);

            card.Controls.Add(pnl);

            y += rowHeight;
            isAlt = !isAlt;
        }

        private void ShowPicture(string storedValue)
        {
            string fullPath;
            try
            {
                NetworkShareHelper.EnsureConnected();
                fullPath = NetworkPaths.GetEquipmentPictureFullPath(storedValue);
            }
            catch (Exception ex)
            {
                ToastHelper.Show(this, "Could not connect to the shared picture folder: " + ex.Message, ToastType.Error);
                return;
            }

            if (!System.IO.File.Exists(fullPath))
            {
                ToastHelper.Show(this, "The picture file could not be found on disk.", ToastType.Warning);
                return;
            }

            string ext = System.IO.Path.GetExtension(fullPath).ToLowerInvariant();

            if (ext == ".pdf")
            {
                try
                {
                    var psi = new System.Diagnostics.ProcessStartInfo(fullPath) { UseShellExecute = true };
                    System.Diagnostics.Process.Start(psi);
                }
                catch (Exception ex)
                {
                    ToastHelper.Show(this, "Could not open PDF: " + ex.Message, ToastType.Error);
                }
                return;
            }

            Image? img = null;
            try
            {
                using var stream = new System.IO.FileStream(fullPath, System.IO.FileMode.Open, System.IO.FileAccess.Read);
                img = Image.FromStream(stream);
            }
            catch (Exception ex)
            {
                ToastHelper.Show(this, "Could not load picture: " + ex.Message, ToastType.Error);
                return;
            }

            using var frm = new Form
            {
                Text = $"Picture — {_equipment.EquipmentCode}",
                FormBorderStyle = FormBorderStyle.Sizable,
                MaximizeBox = true,
                MinimizeBox = false,
                StartPosition = FormStartPosition.CenterParent,
                BackColor = Color.Black,
            };

            int maxW = 900, maxH = 700;
            double scale = Math.Min((double)maxW / img.Width, (double)maxH / img.Height);
            if (scale > 1) scale = 1;
            int initW = Math.Max(300, (int)(img.Width * scale));
            int initH = Math.Max(200, (int)(img.Height * scale));

            var pic = new PictureBox
            {
                Image = img,
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Black,
            };
            frm.Controls.Add(pic);
            frm.ClientSize = new Size(initW, initH + 20);

            frm.FormClosed += (s, e) => img?.Dispose();
            frm.ShowDialog(this);
        }

        private void AddStatusField(Panel card, string labelName, string status, int x, ref int y, ref bool isAlt)
        {
            var lblName = new Label
            {
                Text = labelName + ":",
                Location = new Point(8, 0),
                Size = new Size(LabelW, RowH),
                Font = ThemeHelper.FontBold,
                ForeColor = ThemeHelper.TextSecondary,
                TextAlign = ContentAlignment.MiddleLeft,
            };

            Color badgeBg;
            string displayText;
            switch (status)
            {
                case "in_store":
                    badgeBg = Color.FromArgb(34, 139, 34);
                    displayText = "In Store";
                    break;
                case "allocated":
                    badgeBg = Color.FromArgb(217, 119, 6);
                    displayText = "Allocated";
                    break;
                case "under_repair":
                    badgeBg = Color.FromArgb(220, 85, 40);
                    displayText = "Under Repair";
                    break;
                case "decommissioned":
                    badgeBg = Color.FromArgb(185, 28, 28);
                    displayText = "Decommissioned";
                    break;
                default:
                    badgeBg = ThemeHelper.TextSecondary;
                    displayText = status ?? "—";
                    break;
            }

            RenderBadgeRow(card, lblName, badgeBg, displayText, x, y, RowH, isAlt);

            y += RowH;
            isAlt = !isAlt;
        }

        private void AddMaintenanceStatusField(Panel card, string labelName, string status, int x, ref int y, ref bool isAlt)
        {
            var lblName = new Label
            {
                Text = labelName + ":",
                Location = new Point(8, 0),
                Size = new Size(LabelW, RowH),
                Font = ThemeHelper.FontBold,
                ForeColor = ThemeHelper.TextSecondary,
                TextAlign = ContentAlignment.MiddleLeft,
            };

            Color badgeBg;
            string displayText;
            switch (status)
            {
                case "resolved":
                    badgeBg = Color.FromArgb(34, 139, 34);
                    displayText = "Resolved";
                    break;
                case "in_progress":
                    badgeBg = Color.FromArgb(217, 119, 6);
                    displayText = "In Progress";
                    break;
                default:
                    badgeBg = Color.FromArgb(220, 85, 40);
                    displayText = "Open";
                    break;
            }

            RenderBadgeRow(card, lblName, badgeBg, displayText, x, y, RowH, isAlt);

            y += RowH;
            isAlt = !isAlt;
        }

        private void RenderBadgeRow(Panel card, Label lblName, Color badgeBg, string displayText,
            int x, int y, int rowH, bool isAlt)
        {
            var badge = new Label
            {
                Text = displayText,
                AutoSize = true,
                Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = badgeBg,
                Padding = new Padding(6, 2, 6, 2),
            };

            badge.Location = new Point(ValueX, (rowH - badge.GetPreferredSize(Size.Empty).Height) / 2);

            var pnl = new Panel
            {
                Location = new Point(x, y),
                Size = new Size(ColWidth, rowH),
                BackColor = isAlt ? RowOdd : RowEven,
            };

            pnl.Paint += (s, e) =>
            {
                using var pen = new Pen(SeparatorColor, 1);
                e.Graphics.DrawLine(pen, 8, pnl.Height - 1, pnl.Width - 8, pnl.Height - 1);
            };

            badge.Paint += (s, e) =>
            {
                var lbl = (Label)s!;
                var rect = new Rectangle(0, 0, lbl.Width - 1, lbl.Height - 1);
                using var path = RoundedRect(rect, 8);
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var brush = new SolidBrush(badgeBg);
                e.Graphics.FillPath(brush, path);
                using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                using var textBrush = new SolidBrush(Color.White);
                e.Graphics.DrawString(displayText, badge.Font, textBrush, rect, sf);
            };
            badge.BackColor = Color.Transparent;

            pnl.Controls.Add(lblName);
            pnl.Controls.Add(badge);
            card.Controls.Add(pnl);
        }

        private void DrawCardBorder(object? sender, PaintEventArgs e)
        {
            if (sender is not Panel pnl) return;
            var g = e.Graphics;
            var rect = new Rectangle(0, 0, pnl.Width - 1, pnl.Height - 1);

            using var shadowBrush = new SolidBrush(Color.FromArgb(12, 0, 0, 0));
            g.FillRectangle(shadowBrush, new Rectangle(1, 1, pnl.Width - 1, pnl.Height - 1));

            using var bgBrush = new SolidBrush(CardBg);
            g.FillRectangle(bgBrush, rect);

            using var pen = new Pen(CardBorderColor, 1);
            g.DrawRectangle(pen, rect);
        }

        private static GraphicsPath RoundedRect(Rectangle bounds, int radius)
        {
            int d = radius * 2;
            var path = new GraphicsPath();
            path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
            path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
            path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}   